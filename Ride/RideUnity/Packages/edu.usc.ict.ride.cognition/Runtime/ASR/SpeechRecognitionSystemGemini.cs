using Newtonsoft.Json.Linq;
using Ride.NLP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.SpeechRecognition
{
    /// <summary>Selectable Gemini models for audio-understanding ASR. Model ids live in code (not
    /// RideConfig); mirrors the ChatGPT NLP pattern.</summary>
    public enum GeminiAsrModel
    {
        Pro25 = 10,
        Flash35 = 20,
        Flash31Lite = 30,
    }

    /// <summary>
    /// Gemini-backed speech recognition system that records a local utterance and submits it
    /// to the Gemini audio-understanding generateContent API for transcription.
    /// </summary>
    public class SpeechRecognitionSystemGemini : SpeechRecognitionSystemUnity
    {
        GeminiAsrModel m_model = GeminiAsrModel.Flash35;
             
        private readonly Dictionary<GeminiAsrModel, string> m_modelDictionary = new()
        {
            { GeminiAsrModel.Pro25,         "gemini-2.5-pro"        },
            { GeminiAsrModel.Flash35,       "gemini-3.5-flash"      },
            { GeminiAsrModel.Flash31Lite,   "gemini-3.1-flash-lite" },
        };

        private string ModelId => m_modelDictionary[m_model];

        [Header("Gemini")]
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 20;

        [Header("Audio Capture")]
        [SerializeField, Min(8000)] private int m_sampleRate = 16000;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField, Min(2)] private int m_maxRecordingSeconds = 15;
#pragma warning restore 0414
        [SerializeField, Range(0.0001f, 0.5f)] private float m_speechAmplitudeThreshold = 0.01f;

        private AudioClip m_recordingClip;
        private string m_microphoneDevice;
        private int m_lastSamplePosition;
        private bool m_detectedSpeech;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        private float m_recordingElapsedSeconds;
#pragma warning restore 0414
        private float m_silenceElapsedSeconds;
        private bool m_requestInFlight;

        /// <inheritdoc/>
        public override bool IsSupported => !RideUtils.IsWebGL();

        /// <inheritdoc/>
        public override bool SupportsContinuousRecognition => true;

        private void OnDisable()
        {
            StopCapture();
        }

        /// <inheritdoc/>
        public override void SystemShutdown()
        {
            StopCapture();
            base.SystemShutdown();
        }

        /// <inheritdoc/>
        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);
            m_microphoneDevice = SelectedMicrophone;
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            base.OnRecognizingStarted();
            StartCapture();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            StopCapture();
            base.OnRecognizingStopped();
        }

        /// <inheritdoc/>
        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            if (!IsRecognizing || m_requestInFlight)
                return;

            if (m_recordingClip == null)
            {
                StartCapture();
                return;
            }

            UpdateCaptureState();
        }

        private void StartCapture()
        {
            if (!IsSupported || !IsRecognizing)
                return;

            if (string.IsNullOrEmpty(m_microphoneDevice))
                m_microphoneDevice = SelectedMicrophone;

            if (string.IsNullOrEmpty(m_microphoneDevice))
            {
                Debug.LogWarning("[Gemini ASR] No microphone is selected.");
                return;
            }

#if !UNITY_WEBGL
            if (Microphone.IsRecording(m_microphoneDevice))
                Microphone.End(m_microphoneDevice);

            m_recordingClip = Microphone.Start(m_microphoneDevice, false, m_maxRecordingSeconds, m_sampleRate);
#endif

            m_lastSamplePosition = 0;
            m_detectedSpeech = false;
            m_recordingElapsedSeconds = 0f;
            m_silenceElapsedSeconds = 0f;

            if (m_recordingClip == null)
                Debug.LogError("[Gemini ASR] Failed to start microphone capture.");
        }

        private void StopCapture()
        {
#if !UNITY_WEBGL
            if (!string.IsNullOrEmpty(m_microphoneDevice) && Microphone.IsRecording(m_microphoneDevice))
                Microphone.End(m_microphoneDevice);
#endif

            m_recordingClip = null;
            m_lastSamplePosition = 0;
            m_detectedSpeech = false;
            m_recordingElapsedSeconds = 0f;
            m_silenceElapsedSeconds = 0f;
        }

        private void RestartCapture()
        {
            StopCapture();

            if (IsRecognizing && !m_requestInFlight)
                StartCapture();
        }

        private void UpdateCaptureState()
        {
#if UNITY_WEBGL
            return;
#else
            int currentPosition = Microphone.GetPosition(m_microphoneDevice);
            if (currentPosition < 0)
                return;

            if (currentPosition > m_lastSamplePosition)
                AnalyzeLatestSamples(currentPosition);

            m_recordingElapsedSeconds = currentPosition / (float)m_sampleRate;

            if (!m_detectedSpeech && m_recordingElapsedSeconds >= InitialSilenceTimeoutSeconds)
            {
                Debug.Log("[Gemini ASR] Initial silence timeout reached. Restarting capture.");
                RestartCapture();
                return;
            }

            if (m_detectedSpeech && m_silenceElapsedSeconds >= AutoSilenceTimeoutSeconds)
            {
                FinalizeUtterance(currentPosition);
                return;
            }

            if (currentPosition >= m_recordingClip.samples - 1)
                FinalizeUtterance(currentPosition);
#endif
        }

        private void AnalyzeLatestSamples(int currentPosition)
        {
            int framesToAnalyze = currentPosition - m_lastSamplePosition;
            if (framesToAnalyze <= 0)
                return;

            int channels = m_recordingClip.channels;
            float[] sampleBuffer = new float[framesToAnalyze * channels];
            m_recordingClip.GetData(sampleBuffer, m_lastSamplePosition);

            float maxAmplitude = 0f;
            for (int i = 0; i < sampleBuffer.Length; i++)
                maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(sampleBuffer[i]));

            float chunkDurationSeconds = framesToAnalyze / (float)m_sampleRate;
            if (maxAmplitude >= m_speechAmplitudeThreshold)
            {
                m_detectedSpeech = true;
                m_silenceElapsedSeconds = 0f;
            }
            else if (m_detectedSpeech)
            {
                m_silenceElapsedSeconds += chunkDurationSeconds;
            }

            m_lastSamplePosition = currentPosition;
        }

        private void FinalizeUtterance(int currentPosition)
        {
            int sampleFrames = Mathf.Clamp(currentPosition, 0, m_recordingClip.samples);
            if (!m_detectedSpeech || sampleFrames <= 0)
            {
                RestartCapture();
                return;
            }

            AudioClip utteranceClip = CreateUtteranceClip(sampleFrames);
            StopCapture();

            if (utteranceClip == null)
            {
                RestartCapture();
                return;
            }

            StartCoroutine(SubmitUtteranceCoroutine(utteranceClip));
        }

        private AudioClip CreateUtteranceClip(int sampleFrames)
        {
            if (m_recordingClip == null || sampleFrames <= 0)
                return null;

            int channels = m_recordingClip.channels;
            float[] samples = new float[sampleFrames * channels];
            m_recordingClip.GetData(samples, 0);

            AudioClip utteranceClip = AudioClip.Create("GeminiUtterance", sampleFrames, channels, m_sampleRate, false);
            utteranceClip.SetData(samples, 0);
            return utteranceClip;
        }

        private IEnumerator SubmitUtteranceCoroutine(AudioClip utteranceClip)
        {
            m_requestInFlight = true;

            byte[] wavBytes = BuildWavBytes(utteranceClip);
            Destroy(utteranceClip);

            var config = Systems.Get<ConfigurationSystemUnity>()?.config.gemini ?? RideConfig.GeminiSettings.Default;
            string url = BuildGenerateContentUrl(config.endpoint, ModelId);
            string requestJson = BuildRequestJson(wavBytes);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-goog-api-key", config.endpointKey);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string text = ExtractText(webRequest.downloadHandler.text);
                GeminiTranscription transcription = ParseTranscription(text);
                if (!string.IsNullOrWhiteSpace(transcription.text))
                    OnSpeechRecognized(transcription.text, 1f, transcription.language);
            }
            else
            {
                Debug.LogWarning($"[Gemini ASR] Transcription request failed: {webRequest.result} - {ExtractErrorMessage(webRequest.downloadHandler.text)}");
            }

            m_requestInFlight = false;

            if (IsRecognizing)
                StartCapture();
        }

        private static string BuildGenerateContentUrl(string endpoint, string model)
        {
            string baseEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? RideConfig.GeminiSettings.Default.endpoint
                : endpoint.TrimEnd('/');
            string resolvedModel = string.IsNullOrWhiteSpace(model) ? "gemini-3.5-flash" : model;
            return $"{baseEndpoint}/{resolvedModel}:generateContent";
        }

        private static string BuildRequestJson(byte[] wavBytes)
        {
            var body = new JObject
            {
                ["contents"] = new JArray(
                    new JObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JArray(
                            new JObject
                            {
                                ["text"] = "Transcribe the speech in this audio. Return only compact JSON with keys text and language. Use a BCP-47 language code for language when known, otherwise use an empty string."
                            },
                            new JObject
                            {
                                ["inlineData"] = new JObject
                                {
                                    ["mimeType"] = "audio/wav",
                                    ["data"] = Convert.ToBase64String(wavBytes)
                                }
                            })
                    })
            };

            return body.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static byte[] BuildWavBytes(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] pcmBytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short pcmSample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(pcmSample);
                pcmBytes[i * 2] = bytes[0];
                pcmBytes[i * 2 + 1] = bytes[1];
            }

            return BuildWavBytes(pcmBytes, clip.channels, clip.frequency);
        }

        private static byte[] BuildWavBytes(byte[] pcmBytes, int channels, int sampleRate)
        {
            byte[] wavBytes = new byte[44 + pcmBytes.Length];
            using var memoryStream = new MemoryStream(wavBytes);
            using var writer = new BinaryWriter(memoryStream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + pcmBytes.Length);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(pcmBytes.Length);
            writer.Write(pcmBytes);

            return wavBytes;
        }

        private static string ExtractText(string json)
        {
            try
            {
                JObject response = JObject.Parse(json);
                JArray parts = response["candidates"]?.FirstOrDefault()?["content"]?["parts"] as JArray;
                if (parts == null)
                    return string.Empty;

                StringBuilder builder = new StringBuilder();
                foreach (JToken part in parts)
                    builder.Append(part?["text"]?.ToString());
                return builder.ToString();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gemini ASR] Failed to parse response: {exception.Message}");
                return string.Empty;
            }
        }

        private static GeminiTranscription ParseTranscription(string text)
        {
            string cleaned = StripJsonFence(text);

            try
            {
                JObject json = JObject.Parse(cleaned);
                return new GeminiTranscription
                {
                    text = json["text"]?.ToString() ?? string.Empty,
                    language = json["language"]?.ToString() ?? string.Empty
                };
            }
            catch
            {
                return new GeminiTranscription { text = cleaned, language = string.Empty };
            }
        }

        private static string StripJsonFence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string cleaned = text.Trim();
            if (!cleaned.StartsWith("```", StringComparison.Ordinal))
                return cleaned;

            int firstLineEnd = cleaned.IndexOf('\n');
            int lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                cleaned = cleaned.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1);

            return cleaned.Trim();
        }

        private static string ExtractErrorMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "empty Gemini error response";

            try
            {
                return JObject.Parse(json)["error"]?["message"]?.ToString() ?? json;
            }
            catch
            {
                return json;
            }
        }

        private struct GeminiTranscription
        {
            public string text;
            public string language;
        }
    }
}
