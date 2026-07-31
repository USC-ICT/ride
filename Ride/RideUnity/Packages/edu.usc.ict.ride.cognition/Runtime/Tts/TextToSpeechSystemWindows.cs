using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace Ride.TextToSpeech
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    /// <summary>
    /// Windows-specific text-to-speech implementation backed by an out-of-process PowerShell bridge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class adapts Windows text-to-speech into the shared RIDE cognition text-to-speech contract.
    /// It enumerates locally installed Windows voices, synthesizes a WAV file into
    /// <c>Application.persistentDataPath</c>, and returns that file path through the standard
    /// <see cref="ITextToSpeechSystem.CreateTextToSpeech"/> callback flow.
    /// </para>
    /// <para>
    /// The implementation delegates synthesis to a hidden PowerShell process that uses the Windows desktop
    /// speech stack. For lipsynced requests, the same PowerShell invocation hosts a small in-memory C#
    /// helper around <c>System.Speech.Synthesis.SpeechSynthesizer</c> so SAPI viseme and word timing events
    /// can be converted into the shared RIDE lipsync XML schedule format.
    /// </para>
    /// <para>
    /// Related RIDE implementations:
    /// <see cref="TextToSpeechSystemAzure"/>,
    /// <see cref="TextToSpeechSystemAWSPolly"/>,
    /// <see cref="TextToSpeechSystemElevenLabs"/>,
    /// <see cref="TextToSpeechSystemPiper"/>.
    /// External reference:
    /// <see href="https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ms723627(v=vs.85)">Speech API (SAPI)</see>.
    /// </para>
    /// </remarks>
    public class TextToSpeechSystemWindows : TextToSpeechSystemLipsynced
    {
        [Header("Initialization")]
        [Tooltip("When enabled, Windows TTS initializes during SystemInit. When disabled, initialization is deferred until a TTS interface method first needs voices or synthesis.")]
        [SerializeField] private bool m_initializeOnSystemInit = false;

        private string[] m_voices = Array.Empty<string>();
        private string m_powerShellPath;
        private string m_tempScriptDirectory;
        private bool m_initializationAttempted;
        private bool m_requestIncludesDirectLipsync;
        private GenerateSpeechRequest m_pendingRequest;
        private bool m_hasPendingRequest;
        private Coroutine m_pendingStartCoroutine;

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            if (m_initializeOnSystemInit)
                EnsureInitialized();
        }

        /// <inheritdoc/>
        public override string[] GetAvailableVoices()
        {
            EnsureInitialized();
            return m_voices;
        }

        /// <inheritdoc/>
        public override bool ContainsVoice(string voice)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(voice))
                return m_voices != null && m_voices.Length > 0;

            return base.ContainsVoice(voice);
        }

        private bool EnsureInitialized()
        {
            if (m_initializationAttempted)
                return !string.IsNullOrWhiteSpace(m_powerShellPath);

            m_initializationAttempted = true;

            m_powerShellPath = ResolvePowerShellPath();
            if (string.IsNullOrWhiteSpace(m_powerShellPath))
            {
                Debug.Log("[TextToSpeechSystemWindows] Could not resolve a usable PowerShell executable.");
                m_voices = Array.Empty<string>();
                return false;
            }

            m_tempScriptDirectory = ResolveTemporaryScriptDirectory();
            m_voices = GetInstalledVoices();
            if (m_voices.Length == 0)
                Debug.LogWarning("[TextToSpeechSystemWindows] No installed voices were detected.");

            return true;
        }

        /// <summary>
        /// Starts background speech generation using a PowerShell helper process.
        /// </summary>
        /// <param name="voice">The voice to use, or the first installed voice when empty.</param>
        /// <param name="text">The text to synthesize.</param>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            Debug.Log($"[TextToSpeechSystemWindows] Requested synthesis. RequestedVoice='{voice ?? "<null>"}' TextLength={(text ?? string.Empty).Length}");

            if (!EnsureInitialized() || m_voices == null || m_voices.Length == 0)
            {
                Debug.LogWarning("[TextToSpeechSystemWindows] PowerShell bridge is unavailable or no Windows voices are installed.");
                if (lipsyncProcessing)
                    CompleteLipsyncGeneration(string.Empty);

                CompleteTextToSpeechGeneration(null);
                return;
            }

            string resolvedVoice = ResolveVoiceOrDefault(voice);
            if (string.IsNullOrWhiteSpace(resolvedVoice))
            {
                Debug.LogWarning("[TextToSpeechSystemWindows] No Windows voice is available for synthesis.");
                if (lipsyncProcessing)
                    CompleteLipsyncGeneration(string.Empty);

                CompleteTextToSpeechGeneration(null);
                return;
            }

            string outputFilePath = Path.Combine(Application.persistentDataPath, $"windowsTTS_{DateTime.UtcNow.Ticks}.wav");

            m_pendingRequest = GenerateSpeechRequest.Create(resolvedVoice, text ?? string.Empty, outputFilePath);
            m_hasPendingRequest = true;

            if (m_pendingStartCoroutine != null)
                StopCoroutine(m_pendingStartCoroutine);

            m_pendingStartCoroutine = StartCoroutine(BeginDeferredSynthesisNextFrame());
        }

        /// <inheritdoc/>
        protected override void StartLipsyncGeneration(string voice, string text) => m_requestIncludesDirectLipsync = true;

        private IEnumerator BeginDeferredSynthesisNextFrame()
        {
            yield return null;

            m_pendingStartCoroutine = null;
            if (!m_hasPendingRequest)
                yield break;

            GenerateSpeechRequest request = m_pendingRequest;
            m_hasPendingRequest = false;

            bool includeLipsync = m_requestIncludesDirectLipsync;
            m_requestIncludesDirectLipsync = false;

            if (!includeLipsync && lipsyncProcessing)
                CompleteLipsyncGeneration(string.Empty);

            Debug.Log($"[TextToSpeechSystemWindows] Starting synthesis with resolved voice '{request.voice}' to '{request.outputFilePath}'. IncludeLipsync={includeLipsync}");

            Task<GenerateSpeechResult> generationTask = includeLipsync
                ? Task.Run(() => GenerateSpeechAndLipsync(request))
                : Task.Run(() => GenerateSpeechFile(request));

            yield return StartCoroutine(WaitForGenerationTask(generationTask, includeLipsync));
        }

        private GenerateSpeechResult GenerateSpeechAndLipsync(GenerateSpeechRequest request)
        {
            GenerateSpeechResult result = new()
            {
                audioFilePath = request.outputFilePath,
                lipsyncXml = string.Empty
            };

            try
            {
                string stdout = SynthesizeToWaveFileWithDirectLipsync(request.voice, request.text, request.outputFilePath);
                if (!WaitForAudioFile(request.outputFilePath))
                {
                    result.audioFilePath = null;
                    result.error = $"PowerShell synthesis did not produce an audio file. OutputPath='{request.outputFilePath}' Stdout='{SummarizeBridgeOutput(stdout)}'";
                }
                else
                {
                    result.audioLength = GetWaveDurationSeconds(request.outputFilePath);
                    result.bridgeOutput = stdout;
                    Debug.Log($"[TextToSpeechSystemWindows] Background synthesis completed. Output='{request.outputFilePath}' Duration={result.audioLength:0.###}s");
                }
            }
            catch (Exception exception)
            {
                result.audioFilePath = null;
                result.error = exception.ToString();
            }

            return result;
        }

        private GenerateSpeechResult GenerateSpeechFile(GenerateSpeechRequest request)
        {
            GenerateSpeechResult result = new()
            {
                audioFilePath = request.outputFilePath,
                lipsyncXml = string.Empty
            };

            try
            {
                if (!SynthesizeToWaveFile(request.voice, request.text, request.outputFilePath))
                {
                    result.audioFilePath = null;
                    result.error = "PowerShell synthesis did not produce an audio file.";
                }
                else
                {
                    result.audioLength = GetWaveDurationSeconds(request.outputFilePath);
                    Debug.Log($"[TextToSpeechSystemWindows] Background synthesis completed. Output='{request.outputFilePath}' Duration={result.audioLength:0.###}s");
                }
            }
            catch (Exception exception)
            {
                result.audioFilePath = null;
                result.error = exception.ToString();
            }

            return result;
        }

        private IEnumerator WaitForGenerationTask(Task<GenerateSpeechResult> generationTask, bool includeLipsync)
        {
            while (!generationTask.IsCompleted)
                yield return null;

            GenerateSpeechResult result;
            if (generationTask.IsFaulted)
            {
                result = new GenerateSpeechResult
                {
                    audioFilePath = null,
                    lipsyncXml = string.Empty,
                    error = generationTask.Exception?.ToString() ?? "Background synthesis task faulted."
                };
            }
            else
            {
                result = generationTask.Result ?? new GenerateSpeechResult
                {
                    audioFilePath = null,
                    lipsyncXml = string.Empty,
                    error = "Background synthesis returned no result."
                };
            }

            if (!string.IsNullOrWhiteSpace(result.error))
                Debug.LogWarning($"[TextToSpeechSystemWindows] Synthesis failed: {result.error}");
            else
                Debug.Log($"[TextToSpeechSystemWindows] Completing synthesis on main thread. Output='{result.audioFilePath}' Duration={result.audioLength:0.###}s");

            if (includeLipsync && lipsyncProcessing)
            {
                string lipsyncXml = string.IsNullOrWhiteSpace(result.error)
                    ? BuildLipsyncXmlFromBridgeOutput(result.bridgeOutput, result.audioFilePath, result.audioLength)
                    : string.Empty;
                CompleteLipsyncGeneration(lipsyncXml ?? string.Empty);
            }

            CompleteTextToSpeechGeneration(result.audioFilePath, result.audioLength);
        }

        private string ResolveVoiceOrDefault(string voice)
        {
            if (!string.IsNullOrWhiteSpace(voice) && base.ContainsVoice(voice))
                return voice;

            return m_voices != null && m_voices.Length > 0 ? m_voices[0] : null;
        }

        private string[] GetInstalledVoices()
        {
            if (string.IsNullOrWhiteSpace(m_powerShellPath))
                return Array.Empty<string>();

            string script =
                "Add-Type -AssemblyName System.Speech\r\n" +
                "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\r\n" +
                "try {\r\n" +
                "    $voices = $s.GetInstalledVoices() | Where-Object { $_.Enabled } | ForEach-Object { $_.VoiceInfo.Name }\r\n" +
                "    @($voices) | ForEach-Object { Write-Output $_ }\r\n" +
                "}\r\n" +
                "finally {\r\n" +
                "    $s.Dispose()\r\n" +
                "}";

            string stdout = RunPowerShellScript(script);
            if (string.IsNullOrWhiteSpace(stdout))
            {
                Debug.LogWarning("[TextToSpeechSystemWindows] PowerShell returned no voice output.");
                return Array.Empty<string>();
            }

            List<string> voices = new();
            string[] lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    voices.Add(trimmed);
            }

            return voices.ToArray();
        }

        private string SynthesizeToWaveFileWithDirectLipsync(string voice, string text, string outputFilePath)
        {
            string outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string encodedVoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(voice ?? string.Empty));
            string encodedText = Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));
            string encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(outputFilePath));

            string script =
                "$systemSpeechPath = 'C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\WPF\\System.Speech.dll'\r\n" +
                "if (-not (Test-Path -LiteralPath $systemSpeechPath)) {\r\n" +
                "    $systemSpeechPath = 'C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\WPF\\System.Speech.dll'\r\n" +
                "}\r\n" +
                "if (-not (Test-Path -LiteralPath $systemSpeechPath)) {\r\n" +
                "    Write-Error 'System.Speech.dll was not found in the .NET Framework WPF directories.'\r\n" +
                "    exit 1\r\n" +
                "}\r\n" +
                $"$voice = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedVoice}'))\r\n" +
                $"$text = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedText}'))\r\n" +
                $"$outputPath = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedPath}'))\r\n" +
                "$source = @'\r\n" +
                "using System;\r\n" +
                "using System.Globalization;\r\n" +
                "using System.IO;\r\n" +
                "using System.Speech.Synthesis;\r\n" +
                "using System.Text;\r\n" +
                "\r\n" +
                "public static class RideWindowsTtsHostedBridge\r\n" +
                "{\r\n" +
                "    public static string Run(string voice, string text, string outputPath)\r\n" +
                "    {\r\n" +
                "        StringBuilder sb = new StringBuilder();\r\n" +
                "        string outputDirectory = Path.GetDirectoryName(outputPath);\r\n" +
                "        if (!string.IsNullOrWhiteSpace(outputDirectory))\r\n" +
                "            Directory.CreateDirectory(outputDirectory);\r\n" +
                "        using (SpeechSynthesizer synth = new SpeechSynthesizer())\r\n" +
                "        {\r\n" +
                "            if (!string.IsNullOrWhiteSpace(voice))\r\n" +
                "                synth.SelectVoice(voice);\r\n" +
                "\r\n" +
                "            synth.VisemeReached += delegate(object sender, VisemeReachedEventArgs e)\r\n" +
                "            {\r\n" +
                "                sb.Append(\"VI|\")\r\n" +
                "                    .Append(e.Viseme)\r\n" +
                "                    .Append('|')\r\n" +
                "                    .Append(e.Duration.TotalSeconds.ToString(\"R\", CultureInfo.InvariantCulture))\r\n" +
                "                    .AppendLine();\r\n" +
                "            };\r\n" +
                "\r\n" +
                "            synth.SpeakProgress += delegate(object sender, SpeakProgressEventArgs e)\r\n" +
                "            {\r\n" +
                "                string word = e.Text ?? string.Empty;\r\n" +
                "                string encodedWord = Convert.ToBase64String(Encoding.UTF8.GetBytes(word));\r\n" +
                "                sb.Append(\"WD|\")\r\n" +
                "                    .Append(encodedWord)\r\n" +
                "                    .Append('|')\r\n" +
                "                    .Append(e.AudioPosition.TotalSeconds.ToString(\"R\", CultureInfo.InvariantCulture))\r\n" +
                "                    .AppendLine();\r\n" +
                "            };\r\n" +
                "\r\n" +
                "            synth.SetOutputToWaveFile(outputPath);\r\n" +
                "            synth.Speak(text);\r\n" +
                "            synth.SetOutputToNull();\r\n" +
                "        }\r\n" +
                "\r\n" +
                "        FileInfo fileInfo = new FileInfo(outputPath);\r\n" +
                "        sb.Append(\"FILE|\").Append(outputPath).AppendLine();\r\n" +
                "        sb.Append(\"SIZE|\").Append(fileInfo.Exists ? fileInfo.Length.ToString(CultureInfo.InvariantCulture) : \"-1\").AppendLine();\r\n" +
                "\r\n" +
                "        return sb.ToString();\r\n" +
                "    }\r\n" +
                "}\r\n" +
                "'@\r\n" +
                "Add-Type -Path $systemSpeechPath\r\n" +
                "Add-Type -TypeDefinition $source -Language CSharp -ReferencedAssemblies $systemSpeechPath\r\n" +
                "try {\r\n" +
                "    [RideWindowsTtsHostedBridge]::Run($voice, $text, $outputPath)\r\n" +
                "}\r\n" +
                "catch {\r\n" +
                "    Write-Error $_\r\n" +
                "    exit 1\r\n" +
                "}";

            return RunPowerShellScript(script);
        }

        private bool SynthesizeToWaveFile(string voice, string text, string outputFilePath)
        {
            string outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string encodedVoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(voice ?? string.Empty));
            string encodedText = Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));
            string encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(outputFilePath));

            string script =
                $"$voice = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedVoice}'))\r\n" +
                $"$text = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedText}'))\r\n" +
                $"$outputPath = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedPath}'))\r\n" +
                "Add-Type -AssemblyName System.Speech\r\n" +
                "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\r\n" +
                "try {\r\n" +
                "    if (-not [string]::IsNullOrWhiteSpace($voice)) {\r\n" +
                "        $s.SelectVoice($voice)\r\n" +
                "    }\r\n" +
                "    $s.SetOutputToWaveFile($outputPath)\r\n" +
                "    $s.Speak($text)\r\n" +
                "    Write-Output $outputPath\r\n" +
                "}\r\n" +
                "finally {\r\n" +
                "    $s.Dispose()\r\n" +
                "}";

            string stdout = RunPowerShellScript(script);
            return !string.IsNullOrWhiteSpace(stdout) || File.Exists(outputFilePath);
        }

        private string RunPowerShellScript(string script)
        {
            if (string.IsNullOrWhiteSpace(m_powerShellPath))
            {
                Debug.LogError("[TextToSpeechSystemWindows] No PowerShell executable was resolved for Windows TTS.");
                return null;
            }

            string tempScriptDirectory = m_tempScriptDirectory;
            if (string.IsNullOrWhiteSpace(tempScriptDirectory))
                tempScriptDirectory = Path.GetTempPath();

            string tempScriptPath = Path.Combine(tempScriptDirectory, $"ride-windows-tts-{Guid.NewGuid():N}.ps1");
            try
            {
                Directory.CreateDirectory(tempScriptDirectory);
                File.WriteAllText(tempScriptPath, script, new UTF8Encoding(false));

                ProcessStartInfo startInfo = new()
                {
                    FileName = m_powerShellPath,
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using Process process = new() { StartInfo = startInfo };
                process.Start();

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Debug.Log($"[TextToSpeechSystemWindows] PowerShell exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(stderr))
                    Debug.LogWarning($"[TextToSpeechSystemWindows] PowerShell stderr: {stderr.Trim()}");

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[TextToSpeechSystemWindows] PowerShell bridge failed with exit code {process.ExitCode}. Stderr: {stderr}".Trim());
                    return null;
                }

                return stdout;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempScriptPath))
                        File.Delete(tempScriptPath);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning($"[TextToSpeechSystemWindows] Failed to delete temporary PowerShell script '{tempScriptPath}': {cleanupException.Message}");
                }
            }
        }

        private static string ResolvePowerShellPath()
        {
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

            string[] candidates =
            {
                Path.Combine(systemFolder, "WindowsPowerShell", "v1.0", "powershell.exe"), "powershell.exe"
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (!Path.IsPathRooted(candidate))
                    return candidate;

                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string ResolveTemporaryScriptDirectory()
        {
            string temporaryCachePath = Application.temporaryCachePath;
            if (!string.IsNullOrWhiteSpace(temporaryCachePath))
                return temporaryCachePath;

            return Path.GetTempPath();
        }

        private static float GetWaveDurationSeconds(string audioFilePath)
        {
            using FileStream stream = new(audioFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(stream);

            if (new string(reader.ReadChars(4)) != "RIFF")
                return 0f;

            reader.ReadInt32();
            if (new string(reader.ReadChars(4)) != "WAVE")
                return 0f;

            int bytesPerSecond = 0;
            int dataSize = 0;

            while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
            {
                string chunkId = new(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    reader.ReadInt16();
                    reader.ReadInt16();
                    reader.ReadInt32();
                    bytesPerSecond = reader.ReadInt32();
                    reader.ReadInt16();
                    reader.ReadInt16();

                    int remainingFormatBytes = chunkSize - 16;
                    if (remainingFormatBytes > 0)
                        reader.ReadBytes(remainingFormatBytes);
                }
                else if (chunkId == "data")
                {
                    dataSize = chunkSize;
                    break;
                }
                else
                {
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }

                if ((chunkSize & 1) == 1 && reader.BaseStream.Position < reader.BaseStream.Length)
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
            }

            if (bytesPerSecond <= 0 || dataSize <= 0)
                return 0f;

            return (float)dataSize / bytesPerSecond;
        }

        private static bool WaitForAudioFile(string audioFilePath)
        {
            if (string.IsNullOrWhiteSpace(audioFilePath))
                return false;

            const int attempts = 20;
            const int delayMilliseconds = 100;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        FileInfo fileInfo = new(audioFilePath);
                        if (fileInfo.Length > 0)
                            return true;
                    }
                }
                catch
                {
                    // Retry for transient file visibility/lock timing after the bridge process exits.
                }

                System.Threading.Thread.Sleep(delayMilliseconds);
            }

            return false;
        }

        private static string SummarizeBridgeOutput(string stdout)
        {
            if (string.IsNullOrWhiteSpace(stdout))
                return "<empty>";

            string flattened = stdout.Replace("\r", " ").Replace("\n", " ").Trim();
            const int maxLength = 300;
            return flattened.Length <= maxLength ? flattened : flattened.Substring(0, maxLength) + "...";
        }

        private string BuildLipsyncXmlFromBridgeOutput(string stdout, string outputFilePath, float audioDurationSeconds)
        {
            AudioSpeechMap map = BuildAudioSpeechMapFromBridgeOutput(stdout, outputFilePath, audioDurationSeconds);
            if (map == null)
                return string.Empty;

            ApplyConfiguredVisemeTrimming(map, "Windows TTS");
            string xml = TextToSpeechXMLBuilder.BuildSpeechXML(map);
            LogSpeechXmlDebug(map, xml, "Windows TTS");
            return xml;
        }

        private static AudioSpeechMap BuildAudioSpeechMapFromBridgeOutput(string stdout, string outputFilePath, float audioDurationSeconds)
        {
            AudioSpeechMap map = new()
            {
                soundFile = outputFilePath,
                WordTimingList = new List<WordTimingData>(),
                MarkList = new List<KeyValuePairS<string, double>>(),
                VisemeList = new List<GenerateAudioReplyViseme>()
            };

            if (string.IsNullOrWhiteSpace(stdout))
            {
                FinalizeWordTimings(map, audioDurationSeconds);
                return map;
            }

            string[] lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            double visemeStartTime = 0.0;
            int markIndex = 1;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.StartsWith("VI|", StringComparison.Ordinal))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 3 &&
                        int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int visemeId) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double durationSeconds))
                    {
                        AddFaceFxVisemes(map.VisemeList, visemeId, visemeStartTime);
                        visemeStartTime += Math.Max(0.0, durationSeconds);
                    }

                    continue;
                }

                if (line.StartsWith("WD|", StringComparison.Ordinal))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 3 &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double wordStartTime))
                    {
                        string wordText = DecodeBase64Utf8(parts[1]);
                        AddWordTiming(map, wordText, wordStartTime, ref markIndex);
                    }
                }
            }

            FinalizeWordTimings(map, Math.Max(audioDurationSeconds, (float)visemeStartTime));
            return map;
        }

        private static void AddWordTiming(AudioSpeechMap map, string wordText, double wordStartTime, ref int markIndex)
        {
            if (map.WordTimingList.Count > 0)
            {
                int previousWordIndex = map.WordTimingList.Count - 1;
                WordTimingData previousWord = map.WordTimingList[previousWordIndex];
                previousWord.End = wordStartTime;
                map.WordTimingList[previousWordIndex] = previousWord;

                int previousMarkIndex = map.MarkList.Count - 1;
                if (previousMarkIndex >= 0)
                    map.MarkList[previousMarkIndex] = new KeyValuePairS<string, double>(map.MarkList[previousMarkIndex].Key, wordStartTime);
            }

            map.MarkList.Add(new KeyValuePairS<string, double>("T" + markIndex, wordStartTime));
            markIndex++;
            map.WordTimingList.Add(new WordTimingData(wordText, wordStartTime, 0.0));
            map.MarkList.Add(new KeyValuePairS<string, double>("T" + markIndex, 0.0));
            markIndex++;
        }

        private static void FinalizeWordTimings(AudioSpeechMap map, float audioDurationSeconds)
        {
            if (map == null || map.WordTimingList == null || map.WordTimingList.Count == 0)
                return;

            double finalWordEndTime = Math.Max(map.WordTimingList[map.WordTimingList.Count - 1].Start, audioDurationSeconds);
            int lastWordIndex = map.WordTimingList.Count - 1;
            WordTimingData lastWord = map.WordTimingList[lastWordIndex];
            lastWord.End = finalWordEndTime;
            map.WordTimingList[lastWordIndex] = lastWord;

            int lastMarkIndex = map.MarkList.Count - 1;
            if (lastMarkIndex >= 0)
                map.MarkList[lastMarkIndex] = new KeyValuePairS<string, double>(map.MarkList[lastMarkIndex].Key, finalWordEndTime);
        }

        private static string DecodeBase64Utf8(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AddFaceFxVisemes(List<GenerateAudioReplyViseme> visemes, int sapiViseme, double startTime)
        {
            if (visemes == null)
                return;

            switch (sapiViseme)
            {
                case 0:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.0));
                    break;
                case 1:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("wide", startTime, 0.6));
                    visemes.Add(new GenerateAudioReplyViseme("tBack", startTime, 0.4));
                    break;
                case 2:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.55));
                    break;
                case 3:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.55));
                    break;
                case 4:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("wide", startTime, 0.6));
                    visemes.Add(new GenerateAudioReplyViseme("tBack", startTime, 0.4));
                    break;
                case 5:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("ShCh", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.5));
                    break;
                case 6:
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("ShCh", startTime, 0.3));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.4));
                    break;
                case 7:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.85));
                    break;
                case 8:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.55));
                    break;
                case 9:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("wide", startTime, 0.6));
                    visemes.Add(new GenerateAudioReplyViseme("tBack", startTime, 0.4));
                    break;
                case 10:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.55));
                    break;
                case 11:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("wide", startTime, 0.6));
                    visemes.Add(new GenerateAudioReplyViseme("tBack", startTime, 0.4));
                    break;
                case 12:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.2));
                    break;
                case 13:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.1));
                    visemes.Add(new GenerateAudioReplyViseme("W", startTime, 0.7));
                    break;
                case 14:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.8));
                    break;
                case 15:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.15));
                    visemes.Add(new GenerateAudioReplyViseme("wide", startTime, 0.5));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.4));
                    break;
                case 16:
                    visemes.Add(new GenerateAudioReplyViseme("ShCh", startTime, 0.85));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.4));
                    break;
                case 17:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.45));
                    visemes.Add(new GenerateAudioReplyViseme("tTeeth", startTime, 0.9));
                    break;
                case 18:
                    visemes.Add(new GenerateAudioReplyViseme("FV", startTime, 0.75));
                    break;
                case 19:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.4));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.8));
                    break;
                case 20:
                    visemes.Add(new GenerateAudioReplyViseme("open", startTime, 0.25));
                    visemes.Add(new GenerateAudioReplyViseme("tBack", startTime, 0.8));
                    visemes.Add(new GenerateAudioReplyViseme("tRoof", startTime, 0.8));
                    break;
                case 21:
                    visemes.Add(new GenerateAudioReplyViseme("PBM", startTime, 0.9));
                    break;
                default:
                    break;
            }
        }

        private struct GenerateSpeechRequest
        {
            public string voice;
            public string text;
            public string outputFilePath;

            private GenerateSpeechRequest(string voice, string text, string outputFilePath)
            {
                this.voice = voice;
                this.text = text;
                this.outputFilePath = outputFilePath;
            }

            public static GenerateSpeechRequest Create(string voice, string text, string outputFilePath) =>
                new(voice, text, outputFilePath);
        }

        private sealed class GenerateSpeechResult
        {
            public string audioFilePath;
            public float audioLength;
            public string bridgeOutput;
            public string lipsyncXml;
            public string error;
        }
    }
#else
    /// <summary>
    /// Non-Windows stub for the Windows text-to-speech system.
    /// </summary>
    /// <remarks>
    /// This placeholder keeps the shared type available on platforms where the Windows speech stack
    /// does not exist. It reports no available voices and completes requests without generating audio.
    /// </remarks>
    public class TextToSpeechSystemWindows : TextToSpeechSystemLipsynced
    {
        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => Array.Empty<string>();

        /// <inheritdoc/>
        public override bool ContainsVoice(string voice) => false;

        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            Debug.LogWarning("[Windows TTS] This system is only supported on Windows editor/standalone builds.");
            CompleteTextToSpeechGeneration(null);
        }

        /// <inheritdoc/>
        protected override void StartLipsyncGeneration(string voice, string text)
        {
            CompleteLipsyncGeneration(string.Empty);
        }
    }
#endif
}
