using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace Ride.TextToSpeech
{
    /// <summary>
    /// Azure Cognitive Services Text-to-Speech system implementation for the RIDE framework.
    /// </summary>
    /// <remarks>
    /// This class provides full TTS and viseme-driven lipsync using **Azure Speech**. It mirrors the
    /// Polly implementation’s coroutine pattern so it works in both standalone (native SDK) and
    /// WebGL (via proxy Lambda) builds.
    ///
    /// ### Platform behavior
    /// - **Non-WebGL (Editor/Standalone):**
    ///   - Uses the Azure Speech SDK directly.
    ///   - Voices are fetched with <see cref="Microsoft.CognitiveServices.Speech.SpeechSynthesizer.GetVoicesAsync"/>.
    ///   - Audio synthesis uses MP3 (24 kHz / 160 kbps mono) and is written to
    ///     <c>Application.persistentDataPath</c>.
    ///   - Lipsync generation subscribes to SDK events (VisemeReceived / WordBoundary) while synthesizing
    ///     to memory using fast PCM output for minimal overhead.
    /// - **WebGL:**
    ///   - Uses an HTTP proxy (AWS Lambda Function URL) with endpoints:
    ///     - <c>GET /voices</c> → returns available en-US short voice names (e.g., <c>AriaNeural</c>)
    ///     - <c>POST /audio</c>  → returns a public MP3 URL (no local file is written in WebGL)
    ///     - <c>POST /visemes</c> → returns viseme + word timing events for lipsync
    ///   - <see cref="GenerateAudioSpeechMap"/> builds an <see cref="AudioSpeechMap"/> from the proxy’s
    ///     viseme/word events. On WebGL, <c>soundFile</c> is left empty to avoid referencing local files.
    ///   - <see cref="StartTextToSpeechGeneration(string, string)"/> completes with the **remote URL**
    ///     returned by the proxy (the client may download/play it later).
    ///
    /// ### Lifecycle & gating
    /// - <see cref="SystemInit"/> starts a coroutine to populate the voice list.
    /// - Voice-dependent calls wait internally on <see cref="VoicesReady"/> via <c>WaitForVoices()</c>.
    /// - If a voice is not specified, a sensible default (e.g., <c>AriaNeural</c>) is chosen.
    ///
    /// ### Output & mapping
    /// - Audio output (non-WebGL): MP3 24 kHz / 160 kbps mono.
    /// - Lipsync: Azure viseme IDs are mapped to FaceFX visemes via <see cref="ILipsyncMapper"/>.
    /// - Word/mark timings are normalized with helper methods to ensure proper open/close marks and
    ///   contiguous word ranges for the XML schedule.
    /// - Final lipsync XML is produced with <see cref="TextToSpeechXMLBuilder.BuildSpeechXML(AudioSpeechMap)"/>.
    ///
    /// ### Key APIs
    /// - <see cref="GetAvailableVoices"/> / <see cref="VoicesReady"/>
    /// - <see cref="GenerateAudioSpeechMap(string, string, System.Action{AudioSpeechMap})"/>
    /// - <see cref="StartTextToSpeechGeneration(string, string)"/>
    ///
    /// ### Configuration
    /// - **Non-WebGL** reads Azure credentials (key, region) from the RIDE configuration system.
    /// - **WebGL** expects the proxy Function URL to be set in this script (single place) and reachable
    ///   from the client. The proxy is responsible for Azure auth and S3 hosting of MP3 assets.
    ///
    /// ### Performance notes
    /// - Viseme/word event capture uses **PCM (16 kHz mono)** to minimize synthesis time when audio
    ///   bytes are not needed.
    /// - Coroutines poll SDK Tasks so the flow remains frame-driven and WebGL-friendly.
    ///
    /// ### Error handling
    /// - Network/SDK failures log details and return <c>null</c> (for lipsync) or call completion with
    ///   <c>null</c> (for audio), letting callers decide on retries/fallbacks.
    /// </remarks>
    public class TextToSpeechSystemAzure : TextToSpeechSystemLipsynced, ILipsyncMapper
    {
        #region IPAtoFacefxMap
        /// <summary>
        /// Represents a mapping from an IPA phoneme to corresponding FaceFX visemes and their blend amounts.
        /// </summary>
        class IPAtoFacefxMap
        {
            public readonly string ipaPhoneme = "";
            public readonly string[] facefxVisemes;
            public readonly float[] amounts;

            public IPAtoFacefxMap(string _ipaPhoneme, string _facefxViseme, float _amount)
            {
                ipaPhoneme = _ipaPhoneme;
                facefxVisemes = new string[] { _facefxViseme };
                amounts = new float[] { _amount };
            }

            public IPAtoFacefxMap(string _ipaPhoneme, string[] _facefxVisemes, float[] _amounts)
            {
                ipaPhoneme = _ipaPhoneme;
                facefxVisemes = _facefxVisemes;
                amounts = _amounts;
            }
        }
        #endregion

#if UNITY_WEBGL
        [Serializable]
        private class VoicesReply
        {
            public string[] voices;
        }

        [Serializable]
        private class TTSRequest
        {
            public string text;
            public string voice;
        }

        [Serializable]
        private class TTSReplyAudio
        {
            public string url;
        }

        [Serializable]
        private class TTSReplyVisemes
        {
            // For step 3; same shape as Polly’s marks response
            public SpeechMark[] visemes;
        }

        [Serializable]
        private class SpeechMark
        {
            public double time;
            public string type;
            public int start;
            public int end;
            public string value;
        }
#endif

        Dictionary<string, IPAtoFacefxMap> m_IPAtoFacefxMap = new Dictionary<string, IPAtoFacefxMap>
        {
            { "0", new IPAtoFacefxMap("silence", new[] { "open" }, new[] { 0.0f }) },
            { "2", new IPAtoFacefxMap("ɑ", new[] { "open", "wide" }, new[] { 0.7f, 0.15f }) },
            { "3", new IPAtoFacefxMap("ɔ", new[] { "open", "W" }, new[] { 0.25f, 0.45f }) },
            { "4", new IPAtoFacefxMap("ɛ, ʊ", new[] { "open", "wide" }, new[] { 0.4f, 0.25f }) },
            { "5", new IPAtoFacefxMap("ɝ", new[] { "open", "wide" }, new[] { 0.4f, 0.1f }) },
            { "6", new IPAtoFacefxMap("j, i, ɪ", new[] { "open", "wide" }, new[] { 0.4f, 0.1f }) },
            { "7", new IPAtoFacefxMap("w, u", new[] { "W" }, new[] { 0.9f }) },
            { "8", new IPAtoFacefxMap("o", new[] { "open", "W" }, new[] { 0.25f, 0.45f }) },
            { "9", new IPAtoFacefxMap("aʊ", new[] { "open", "wide" }, new[] { 0.7f, 0.15f }) },
            { "10", new IPAtoFacefxMap("ɔɪ", new[] { "open", "wide" }, new[] { 0.35f, 0.1f }) },
            { "11", new IPAtoFacefxMap("aɪ", new[] { "open", "wide" }, new[] { 0.7f, 0.15f }) },
            { "12", new IPAtoFacefxMap("h", new[] { "open", "wide" }, new[] { 0.4f, 0.1f }) },
            { "13", new IPAtoFacefxMap("ɹ", new[] { "ShCh", "W" }, new[] { 0.2f, 0.1f }) },
            { "14", new IPAtoFacefxMap("l", new[] { "ShCh", "W" }, new[] { 0.2f, 0.1f }) },
            { "15", new IPAtoFacefxMap("s, z", new[] { "open", "tRoof" }, new[] { 0.15f, 0.8f }) },
            { "16", new IPAtoFacefxMap("ʃ, tʃ, dʒ, ʒ", new[] { "ShCh" }, new[] { 1f }) },
            { "17", new IPAtoFacefxMap("ð", new[] { "open", "tTeeth" }, new[] { 0.4f, 0.9f }) },
            { "18", new IPAtoFacefxMap("f, v", new[] { "FV" }, new[] { 1f }) },
            { "19", new IPAtoFacefxMap("d, t, n, θ", new[] { "open", "tRoof" }, new[] { 0.6f, 1 }) },
            { "20", new IPAtoFacefxMap("k, g, ŋ", new[] { "open", "tBack" }, new[] { 0.3f, 1 }) },
            { "21", new IPAtoFacefxMap("p, b, m", new[] { "BMP" }, new[] { 1f }) },
        };

        string[] m_voices = new string[] { "Loading...", };
        bool m_voicesReady = false;

        /// <summary>
        /// Indicates whether the list of available voices has been loaded from Azure.
        /// </summary>
        public bool VoicesReady => m_voicesReady;

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            StartCoroutine(RequestAvailableVoicesCoroutine());
        }

        /// <inheritdoc/>
        public void GenerateAudioSpeechMap(string voice, string text, Action<AudioSpeechMap> resultCallback)
        {
            StartCoroutine(GenerateSpeechMarksCoroutine(voice, text, resultCallback));
        }

        private IEnumerator GenerateSpeechMarksCoroutine(string voice, string text, Action<AudioSpeechMap> resultCallback)
        {
            yield return WaitForVoices();

            if (string.IsNullOrEmpty(voice))
                voice = GetAvailableVoices().Length > 0 ? GetAvailableVoices()[0] : "AriaNeural";

#if !UNITY_WEBGL
            // We we synthesize to memory so events still fire
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            string key = configSystem.config.azureSpeech.apiKey;
            string region = configSystem.config.azureSpeech.region;

            var speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(key, region);
            speechConfig.SpeechSynthesisLanguage = "en-US";
            speechConfig.SpeechSynthesisVoiceName = "en-US-" + voice;
            // choose the fastest option since we do not keep the results in this function.
            speechConfig.SetSpeechSynthesisOutputFormat(Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            // Initialize collections to store the visemes and other marks
            var visemeList = new List<GenerateAudioReplyViseme>();
            var markList = new List<KeyValuePairS<string, double>>();
            var wordBreakList = new List<KeyValuePairS<double, double>>();
            int markIndex = 0;

            using (var synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig))
            {
                // Hook events before kicking off synthesis
                synthesizer.VisemeReceived += (s, e) => HandleVisemeReceived(e, visemeList);
                synthesizer.WordBoundary  += (s, e) => HandleWordBoundary(e, markList, wordBreakList, ref markIndex, text);

                //Debug.Log("Starting text-to-speech synthesis...");

                Task<Microsoft.CognitiveServices.Speech.SpeechSynthesisResult> task = null;
                try
                {
                    task = synthesizer.SpeakTextAsync(text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GenerateSpeechMarksCoroutine() - SpeakTextAsync threw: {ex.Message}");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                // Pump the task
                while (!task.IsCompleted)
                    yield return null;

                if (task.IsFaulted || task.Result == null)
                {
                    Debug.LogError("GenerateSpeechMarksCoroutine() - SpeakTextAsync failed.");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                var synthesisResult = task.Result;
                if (synthesisResult.Reason != Microsoft.CognitiveServices.Speech.ResultReason.SynthesizingAudioCompleted)
                {
                    if (synthesisResult.Reason == Microsoft.CognitiveServices.Speech.ResultReason.Canceled)
                    {
                        var cancel = Microsoft.CognitiveServices.Speech.SpeechSynthesisCancellationDetails.FromResult(synthesisResult);
                        Debug.LogError($"GenerateSpeechMarksCoroutine() - canceled: {cancel.Reason} | {cancel.ErrorDetails}");
                    }
                    resultCallback?.Invoke(null);
                    yield break;
                }

                //Debug.Log($"Synthesis completed. Reason: {synthesisResult.Reason}");

                // Build AudioSpeechMap and normalize timings (same as your async version)
                var map = new AudioSpeechMap
                {
                    soundFile = $"{Application.persistentDataPath}/azureTTS.mp3",
                    VisemeList = visemeList,
                    MarkList = markList,
                    WordBreakList = wordBreakList
                };

                AdjustWordTimings(ref map);
                AdjustEndMarkTimings(ref map);

                resultCallback?.Invoke(map);
            }
#else
            // WebGL: call the proxy /visemes, then build AudioSpeechMap from the events
            string lambda = "ik5zqibyechvqdv2w4zkstfb2m0hybqr";
            string url = $"https://{lambda}.lambda-url.us-west-2.on.aws/visemes";

            var payload = new TTSRequest { text = text, voice = voice };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

            using (var req = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(body);
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();

                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"GenerateSpeechMarksCoroutine(WebGL) - visemes POST failed: {req.error}");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                TTSReplyVisemes reply = null;
                try
                {
                    reply = JsonUtility.FromJson<TTSReplyVisemes>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GenerateSpeechMarksCoroutine(WebGL) - JSON parse error: {ex.Message}");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                var visemeList    = new List<GenerateAudioReplyViseme>();
                var markList      = new List<KeyValuePairS<string, double>>();
                var wordBreakList = new List<KeyValuePairS<double, double>>();
                int markIndex     = 0;

                if (reply?.visemes != null)
                {
                    foreach (var m in reply.visemes)
                    {
                        if (m == null) continue;

                        if (m.type == "viseme")
                        {
                            // Map Azure visemeId -> FaceFX visemes using your table
                            if (m_IPAtoFacefxMap.TryGetValue(m.value, out var map))
                            {
                                for (int i = 0; i < map.facefxVisemes.Length; i++)
                                    visemeList.Add(new GenerateAudioReplyViseme(map.facefxVisemes[i], m.time, map.amounts[i]));
                            }
                        }
                        else if (m.type == "word")
                        {
                            // Mark before and after the word; set provisional end to start (fixed below)
                            double t = m.time;
                            markList.Add(new KeyValuePairS<string, double>($"T{markIndex++}", t));
                            wordBreakList.Add(new KeyValuePairS<double, double>(t, t));
                            markList.Add(new KeyValuePairS<string, double>($"T{markIndex++}", t));
                        }
                    }
                }

                var mapOut = new AudioSpeechMap
                {
                    soundFile     = "",
                    VisemeList    = visemeList,
                    MarkList      = markList,
                    WordBreakList = wordBreakList
                };

                // Normalize timings to fill word ends and end-marks
                AdjustWordTimings(ref mapOut);
                AdjustEndMarkTimings(ref mapOut);

                resultCallback?.Invoke(mapOut);
            }
            yield break;
#endif
        }

        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => m_voices;

        /// <inheritdoc/>
        protected override void StartLipsyncGeneration(string voice, string text)
        {
            GenerateAudioSpeechMap(voice, text, OnAudioSpeechGeneration);
        }

        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            StartCoroutine(StartTextToSpeechGenerationCoroutine(voice, text));
        }

        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text)
        {
            // Ensure voices are available
            yield return WaitForVoices();

            if (string.IsNullOrEmpty(voice))
                voice = GetAvailableVoices().Length > 0 ? GetAvailableVoices()[0] : "AriaNeural";

#if !UNITY_WEBGL
            string filePath = $"{Application.persistentDataPath}/azureTTS.mp3";

            Microsoft.CognitiveServices.Speech.SpeechConfig speechConfig = null;
            Microsoft.CognitiveServices.Speech.SpeechSynthesizer synthesizer = null;

            try
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                string key = configSystem.config.azureSpeech.apiKey;
                string region = configSystem.config.azureSpeech.region;

                speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(key, region);
                speechConfig.SpeechSynthesisLanguage = "en-US";
                speechConfig.SpeechSynthesisVoiceName = "en-US-" + voice;
                speechConfig.SetSpeechSynthesisOutputFormat(Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3);

                synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig);

                Task<Microsoft.CognitiveServices.Speech.SpeechSynthesisResult> task = null;
                try
                {
                    task = synthesizer.SpeakTextAsync(text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine() - SpeakTextAsync threw: {ex.Message}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                // Pump the task so this works on WebGL‑friendly coroutines (even though this block is non‑WebGL)
                while (!task.IsCompleted)
                    yield return null;

                if (task.IsFaulted || task.Result == null)
                {
                    Debug.LogError("StartTextToSpeechGenerationCoroutine() - SpeakTextAsync failed.");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                var result = task.Result;
                if (result.Reason != Microsoft.CognitiveServices.Speech.ResultReason.SynthesizingAudioCompleted)
                {
                    if (result.Reason == Microsoft.CognitiveServices.Speech.ResultReason.Canceled)
                    {
                        var cancel = Microsoft.CognitiveServices.Speech.SpeechSynthesisCancellationDetails.FromResult(result);
                        Debug.LogError($"StartTextToSpeechGenerationCoroutine() - canceled: {cancel.Reason} | {cancel.ErrorDetails}");
                    }

                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                // Write MP3 to disk
                try
                {
                    File.WriteAllBytes(filePath, result.AudioData);
                }
                catch (Exception io)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine() - write failed: {io.Message}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                // Success
                CompleteTextToSpeechGeneration(filePath);
            }
            finally
            {
                synthesizer?.Dispose();
                speechConfig = null;
            }
#else
            // WebGL: POST /audio to proxy, then download the MP3 locally
            string lambda = "ik5zqibyechvqdv2w4zkstfb2m0hybqr";
            string postUrl = $"https://{lambda}.lambda-url.us-west-2.on.aws/audio";

            var payload = new TTSRequest { text = text, voice = voice };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

            // 1) Ask proxy to synthesize, get back a URL
            using (var req = new UnityEngine.Networking.UnityWebRequest(postUrl, "POST"))
            {
                req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(body);
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();

                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine(WebGL) - /audio POST failed: {req.error}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                TTSReplyAudio reply = null;
                try
                {
                    reply = JsonUtility.FromJson<TTSReplyAudio>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine(WebGL) - JSON parse error: {ex.Message}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                if (reply == null || string.IsNullOrEmpty(reply.url))
                {
                    Debug.LogError("StartTextToSpeechGenerationCoroutine(WebGL) - no url returned.");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                Debug.Log($"[WebGL] MP3 available at: {reply.url}");

                CompleteTextToSpeechGeneration(reply.url);
            }
#endif
        }

        /// <summary>
        /// Callback for handling the generated AudioSpeechMap data.
        /// </summary>
        /// <param name="audioSpeechMap">The generated speech data including visemes and timings.<see cref="AudioSpeechMap"/></param>
        void OnAudioSpeechGeneration(AudioSpeechMap audioSpeechMap)
        {
            CompleteLipsyncGeneration(TextToSpeechXMLBuilder.BuildSpeechXML(audioSpeechMap));
        }

        /// <summary>
        /// Asynchronously requests available TTS voices from Azure and stores them locally.
        /// </summary>
        private IEnumerator RequestAvailableVoicesCoroutine()
        {
#if !UNITY_WEBGL
            var voices = new List<string>();

            Microsoft.CognitiveServices.Speech.SpeechConfig speechConfig = null;
            Microsoft.CognitiveServices.Speech.SpeechSynthesizer synthesizer = null;
            try
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                string key = configSystem.config.azureSpeech.apiKey;
                string region = configSystem.config.azureSpeech.region;

                speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(key, region);
                synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig);

                Task<Microsoft.CognitiveServices.Speech.SynthesisVoicesResult> task = null;
                try
                {
                    task = synthesizer.GetVoicesAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"RequestAvailableVoicesCoroutine() - GetVoicesAsync threw: {e.Message}");
                    m_voices = Array.Empty<string>();
                    yield break;
                }

                while (!task.IsCompleted)
                    yield return null;

                if (task.IsFaulted || task.Result == null)
                {
                    Debug.LogError("RequestAvailableVoicesCoroutine() - GetVoicesAsync failed.");
                    m_voices = Array.Empty<string>();
                    yield break;
                }

                var voicesResult = task.Result;
                if (voicesResult.Reason == Microsoft.CognitiveServices.Speech.ResultReason.VoicesListRetrieved)
                {
                    foreach (var v in voicesResult.Voices)
                    {
                        if (!string.IsNullOrEmpty(v.ShortName) && v.ShortName.StartsWith("en-US-"))
                            voices.Add(v.ShortName.Substring("en-US-".Length));
                    }

                    m_voices = voices.ToArray();
                    m_voicesReady = true;
                }
                else
                {
                    Debug.LogError($"RequestAvailableVoicesCoroutine() - Voices not retrieved. Reason: {voicesResult.Reason}");
                    m_voices = Array.Empty<string>();
                    // Do not set ready; callers waiting will continue waiting until we retry (if you add retries later).
                }
            }
            finally
            {
                synthesizer?.Dispose();
                speechConfig = null;
            }
#else
            string lambda = "ik5zqibyechvqdv2w4zkstfb2m0hybqr";
            string lambdaUrl = $"https://{lambda}.lambda-url.us-west-2.on.aws/voices";
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(lambdaUrl))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"RequestAvailableVoicesCoroutine() - WebGL voices fetch failed: {request.error}");
                    m_voices = Array.Empty<string>();
                    m_voicesReady = true; // avoid deadlocks, caller can retry later if desired
                    yield break;
                }

                VoicesReply reply = null;
                try
                {
                    reply = JsonUtility.FromJson<VoicesReply>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RequestAvailableVoicesCoroutine() - JSON parse error: {ex.Message}");
                    m_voices = Array.Empty<string>();
                    m_voicesReady = true;
                    yield break;
                }

                m_voices = reply?.voices ?? Array.Empty<string>();
                m_voicesReady = true;
            }
#endif
        }

#if !UNITY_WEBGL
        /// <summary>
        /// Handles the VisemeReceived event from Azure synthesis and maps visemes to FaceFX values.
        /// </summary>
        /// <param name="e">The viseme event data.< see cref = "SpeechSynthesisVisemeEventArgs" /></param>
        /// <param name="visemeList">The list to populate with mapped visemes.< see cref = "GenerateAudioReplyViseme" /></param>
        private void HandleVisemeReceived(Microsoft.CognitiveServices.Speech.SpeechSynthesisVisemeEventArgs e, List<GenerateAudioReplyViseme> visemeList)
        {
            // Mapping viseme ID to facefx visemes
            if (m_IPAtoFacefxMap.TryGetValue(e.VisemeId.ToString(), out IPAtoFacefxMap map))
            {
                for (int i = 0; i < map.facefxVisemes.Length; i++)
                {
                    visemeList.Add(new GenerateAudioReplyViseme(map.facefxVisemes[i], e.AudioOffset / 10000000.0, map.amounts[i]));
                }
            }
            else
            {
                Debug.Log($"Failed to map viseme ID {e.VisemeId} to facefx. Discarding.");
            }
        }

        /// <summary>
        /// Handles word boundary events and records timing for lipsync marks.
        /// </summary>
        /// <param name="e">The word boundary event data.</param>
        /// <param name="markList">List to store marks.</param>
        /// <param name="wordBreakList">List to store word boundaries.</param>
        /// <param name="markIndex">Current mark index.</param>
        /// <param name="text">The full input text.</param>
        private void HandleWordBoundary(Microsoft.CognitiveServices.Speech.SpeechSynthesisWordBoundaryEventArgs e, List<KeyValuePairS<string, double>> markList, List<KeyValuePairS<double, double>> wordBreakList, ref int markIndex, string text)
        {
            char[] punctuationMarks = { '.', ',', '!', '?', ';', ':' };
            double startTime = e.AudioOffset / 10000000.0; // Convert to seconds
            double endTime = startTime + e.Duration.TotalSeconds;

            int wordStartIndex = (int)e.TextOffset;
            int wordEndIndex = wordStartIndex + (int)e.WordLength;

            if (wordStartIndex >= 0 && wordEndIndex <= text.Length)
            {
                string word = text.Substring(wordStartIndex, (int)e.WordLength);

                // Skip if the word is just a punctuation mark
                if (word.Length > 0 && !punctuationMarks.Contains(word[0]))
                {
                    // Add a mark before the word starts
                    markList.Add(new KeyValuePairS<string, double>($"T{markIndex}", startTime));
                    markIndex++;

                    // Add the word boundaries
                    wordBreakList.Add(new KeyValuePairS<double, double>(startTime, endTime));

                    // Add a mark after the word ends
                    markList.Add(new KeyValuePairS<string, double>($"T{markIndex}", endTime));
                    markIndex++;
                }
            }
        }
#endif

        /// <summary>
        /// Adjusts the end timings of words in the AudioSpeechMap for more accurate playback alignment.
        /// </summary>
        /// <param name="audioSpeechMap">The AudioSpeechMap to modify.<see cref = "AudioSpeechMap"/></ param>
        private static void AdjustWordTimings(ref AudioSpeechMap audioSpeechMap)
        {
            double lastTime = 0;

            for (int i = 0; i < audioSpeechMap.WordBreakList.Count; i++)
            {
                var wordBreak = audioSpeechMap.WordBreakList[i];
                double startTime = wordBreak.Key;
                double endTime = wordBreak.Value;
                lastTime = endTime;

                if (i > 0)
                {
                    // Update the end time of the previous word
                    ChangeEndWordTiming(audioSpeechMap.WordBreakList, i - 1, startTime);
                }
            }

            // Update the end time of the last word
            if (audioSpeechMap.WordBreakList.Count > 0)
                ChangeEndWordTiming(audioSpeechMap.WordBreakList, audioSpeechMap.WordBreakList.Count - 1, lastTime);
        }

        /// <summary>
        /// Updates the end time of a word timing entry.
        /// </summary>
        /// <param name="wordTimings">List of word timings.</param>
        /// <param name="index">Index of the word to update.</param>
        /// <param name="endTime">The new end time.</param>
        private static void ChangeEndWordTiming(List<KeyValuePairS<double, double>> wordTimings, int index, double endTime)
        {
            var pair = wordTimings[index];
            pair.Value = endTime;
            wordTimings[index] = pair;
        }

        /// <summary>
        /// Adjusts the end timings of mark entries in the AudioSpeechMap.
        /// </summary>
        /// <param name="audioSpeechMap">The AudioSpeechMap to modify.<see cref = "AudioSpeechMap"/></param>
        private static void AdjustEndMarkTimings(ref AudioSpeechMap audioSpeechMap)
        {
            double lastTime = 0;

            for (int i = 0; i < audioSpeechMap.MarkList.Count; i++)
            {
                var mark = audioSpeechMap.MarkList[i];
                double time = mark.Value;
                lastTime = time;

                // Check if the mark index is odd
                if (i % 2 != 0)
                {
                    // Find the corresponding end time of the word the mark is inside
                    double endTime = FindWordEndTime(audioSpeechMap, time);

                    // Update the mark time with the word end time
                    audioSpeechMap.MarkList[i] = new KeyValuePairS<string, double>(mark.Key, endTime);
                }
            }

            // Update the end time of the last mark if there are odd number of marks
            if (audioSpeechMap.MarkList.Count > 0 && audioSpeechMap.MarkList.Count % 2 != 0)
            {
                audioSpeechMap.MarkList[audioSpeechMap.MarkList.Count - 1] = new KeyValuePairS<string, double>(
                    audioSpeechMap.MarkList[audioSpeechMap.MarkList.Count - 1].Key,
                    lastTime
                );
            }
        }

        /// <summary>
        /// Finds the ending time of a word that contains the given mark time.
        /// </summary>
        /// <param name="audioSpeechMap">The speech map containing word data.<see cref = "AudioSpeechMap"/></param>
        /// <param name="markTime">The time of the mark to resolve.</param>
        /// <returns>The corresponding word's end time.</returns>
        private static double FindWordEndTime(AudioSpeechMap audioSpeechMap, double markTime)
        {
            foreach (var wordBreak in audioSpeechMap.WordBreakList)
            {
                if (markTime >= wordBreak.Key && markTime <= wordBreak.Value)
                {
                    // The mark is inside this word, return its end time
                    return wordBreak.Value;
                }
            }

            // If the mark is not found within any word, return the mark time itself
            return markTime;
        }

        private IEnumerator WaitForVoices()
        {
            while (!m_voicesReady)
                yield return null;
        }
    }
}
