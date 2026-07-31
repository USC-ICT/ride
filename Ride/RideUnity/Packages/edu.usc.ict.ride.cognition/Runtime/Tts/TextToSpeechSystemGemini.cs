using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.TextToSpeech
{
    /// <summary>Selectable Gemini TTS models. Model ids live in code (not RideConfig); mirrors the
    /// ChatGPT NLP pattern. Note these are TTS-specific models, distinct from the text models.</summary>
    public enum GeminiTtsModel
    {
        Flash31Tts = 10,
        Flash25Tts = 20,
        Pro25Tts   = 30,
    }

    /// <summary>
    /// Gemini-backed text-to-speech system that uses Gemini TTS preview models and
    /// RIDE's proxy lipsync generation for word/viseme timing.
    /// </summary>
    public class TextToSpeechSystemGemini : TextToSpeechSystemProxyLipsynced
    {        
        GeminiTtsModel m_model = GeminiTtsModel.Flash31Tts;
             
        private readonly Dictionary<GeminiTtsModel, string> m_modelDictionary = new()
        {
            { GeminiTtsModel.Flash31Tts, "gemini-3.1-flash-tts-preview" },
            { GeminiTtsModel.Flash25Tts, "gemini-2.5-flash-preview-tts" },
            { GeminiTtsModel.Pro25Tts,   "gemini-2.5-pro-preview-tts"   },
        };

        private string ModelId => m_modelDictionary[m_model];

        [Header("Gemini")]
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 20;

        [Header("Audio")]
        [SerializeField, Min(8000)] private int m_sampleRate = 24000;
        [SerializeField] private string m_stylePrefix = "Say clearly and naturally:";
        [SerializeField] private string m_fallbackVoice = "Kore";

        private readonly string[] m_voices =
        {
            "Zephyr",
            "Puck",
            "Charon",
            "Kore",
            "Fenrir",
            "Leda",
            "Orus",
            "Aoede",
            "Callirrhoe",
            "Autonoe",
            "Enceladus",
            "Iapetus",
            "Umbriel",
            "Algieba",
            "Despina",
            "Erinome",
            "Algenib",
            "Rasalgethi",
            "Laomedeia",
            "Achernar",
            "Alnilam",
            "Schedar",
            "Gacrux",
            "Pulcherrima",
            "Achird",
            "Zubenelgenubi",
            "Vindemiatrix",
            "Sadachbia",
            "Sadaltager",
            "Sulafat",
        };

        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => m_voices;

        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            StartCoroutine(StartTextToSpeechGenerationCoroutine(ResolveVoiceOrDefault(voice), text));
        }

        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text)
        {
            var config = Systems.Get<ConfigurationSystemUnity>()?.config.gemini ?? RideConfig.GeminiSettings.Default;
            string url = BuildGenerateContentUrl(config.endpoint, ModelId);
            string requestJson = BuildRequestJson(voice, text);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = GetRequestTimeoutSeconds(m_requestTimeoutSeconds, text);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-goog-api-key", config.endpointKey);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Gemini TTS] Synthesis request failed: {webRequest.result} - {ExtractErrorMessage(webRequest.downloadHandler.text)}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            byte[] pcmBytes;
            try
            {
                string audioBase64 = ExtractAudioBase64(webRequest.downloadHandler.text);
                if (string.IsNullOrWhiteSpace(audioBase64))
                {
                    Debug.LogWarning("[Gemini TTS] Synthesis response did not contain audio.");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                pcmBytes = Convert.FromBase64String(audioBase64);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gemini TTS] Failed to parse audio response: {exception.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            byte[] wavBytes = BuildWavBytes(pcmBytes, 1, m_sampleRate);
            float durationSeconds = pcmBytes.Length / (float)(m_sampleRate * 2);

            try
            {
                CompleteTextToSpeechGeneration(CreateAudioReference(wavBytes), durationSeconds);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gemini TTS] Failed to prepare audio output: {exception.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }
        }

        private static string CreateAudioReference(byte[] wavBytes)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "data:audio/wav;base64," + Convert.ToBase64String(wavBytes);
#else
            string filePath = Path.Combine(Application.persistentDataPath, $"geminiTTS_{DateTime.UtcNow.Ticks}.wav");
            File.WriteAllBytes(filePath, wavBytes);
            return filePath;
#endif
        }

        private string BuildRequestJson(string voice, string text)
        {
            string prompt = string.IsNullOrWhiteSpace(m_stylePrefix)
                ? text
                : $"{m_stylePrefix} {text}";

            var body = new JObject
            {
                ["contents"] = new JArray(
                    new JObject
                    {
                        ["parts"] = new JArray(new JObject { ["text"] = prompt })
                    }),
                ["generationConfig"] = new JObject
                {
                    ["responseModalities"] = new JArray("AUDIO"),
                    ["speechConfig"] = new JObject
                    {
                        ["voiceConfig"] = new JObject
                        {
                            ["prebuiltVoiceConfig"] = new JObject
                            {
                                ["voiceName"] = voice
                            }
                        }
                    }
                }
            };

            return body.ToString(Newtonsoft.Json.Formatting.None);
        }

        private string ResolveVoiceOrDefault(string voice)
        {
            if (!string.IsNullOrWhiteSpace(voice) && ContainsVoice(voice))
                return voice;

            if (!string.IsNullOrWhiteSpace(m_fallbackVoice) && ContainsVoice(m_fallbackVoice))
                return m_fallbackVoice;

            return m_voices.Length > 0 ? m_voices[0] : "Kore";
        }

        private static string BuildGenerateContentUrl(string endpoint, string model)
        {
            string baseEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? RideConfig.GeminiSettings.Default.endpoint
                : endpoint.TrimEnd('/');
            string resolvedModel = string.IsNullOrWhiteSpace(model)
                ? "gemini-3.1-flash-tts-preview"
                : model;
            return $"{baseEndpoint}/{resolvedModel}:generateContent";
        }

        private static string ExtractAudioBase64(string json)
        {
            JObject response = JObject.Parse(json);
            JArray parts = response["candidates"]?.FirstOrDefault()?["content"]?["parts"] as JArray;
            if (parts == null)
                return string.Empty;

            foreach (JToken part in parts)
            {
                string data = part?["inlineData"]?["data"]?.ToString()
                    ?? part?["inline_data"]?["data"]?.ToString();
                if (!string.IsNullOrWhiteSpace(data))
                    return data;
            }

            return string.Empty;
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
    }
}
