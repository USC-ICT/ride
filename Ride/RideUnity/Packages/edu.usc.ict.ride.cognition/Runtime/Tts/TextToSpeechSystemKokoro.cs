using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Local Kokoro text-to-speech via the upstream <c>remsky/Kokoro-FastAPI</c> server (Apache-2.0),
    /// which exposes an OpenAI-compatible audio API: <c>POST /v1/audio/speech</c> (raw audio bytes back)
    /// and <c>GET /v1/audio/voices</c>. Replaces the previous custom <c>/synthesize</c> wrapper (R7).
    /// Lipsync is proxy-generated from the resulting clip duration.
    ///
    /// Connection config is code-authoritative (not [SerializeField]) so script changes take effect
    /// without an Editor/prefab edit; for per-deployment overrides, source from RideConfig.
    /// </summary>
    public class TextToSpeechSystemKokoro : TextToSpeechSystemProxyLipsynced
    {
        // Default port for Kokoro-FastAPI is 8880 (the custom wrapper used 9003).
        private string m_endpoint = "http://127.0.0.1:8880";
        private string m_model = "kokoro";
        private bool m_sendAuthorizationHeader = false;
        private string m_authorizationToken = string.Empty;
        private int m_requestTimeoutSeconds = 20;
        private string m_fallbackVoice = "af_heart";

        // Used only if GET /v1/audio/voices fails (offline / older server).
        private static readonly string[] s_builtinVoices =
            { "af_heart", "af_bella", "am_adam", "am_michael", "bf_emma", "bm_george" };

        private string[] m_voices = Array.Empty<string>();
        public override void SystemInit()
        {
            base.SystemInit();
            VoiceListStatus = VoiceListState.NotFetched;
            RefreshVoices();
        }

        public override string[] GetAvailableVoices() => m_voices;

        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            StartCoroutine(StartTextToSpeechGenerationCoroutine(ResolveVoiceOrDefault(voice), text));
        }

        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text)
        {
            yield return WaitForVoices();

            SpeechRequest requestBody = new()
            {
                model = m_model,
                input = text,
                voice = voice,
                response_format = "wav",
                speed = 1.0f
            };

            string requestJson = JsonUtility.ToJson(requestBody);
            using var webRequest = new UnityWebRequest($"{m_endpoint}/v1/audio/speech", UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = GetRequestTimeoutSeconds(m_requestTimeoutSeconds, text);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            AddAuthorizationHeader(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[Kokoro TTS] Synthesis request failed: {webRequest.result} - {webRequest.error}. " +
                    $"Response: {webRequest.downloadHandler?.text}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            byte[] audioBytes = webRequest.downloadHandler.data;
            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogWarning("[Kokoro TTS] Synthesis response did not contain audio.");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            string filePath = Path.Combine(
                Application.persistentDataPath,
                $"kokoroTTS_{DateTime.UtcNow.Ticks}.wav");

            try
            {
                File.WriteAllBytes(filePath, audioBytes);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Kokoro TTS] Failed to write audio file: {exception.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            CompleteTextToSpeechGeneration(filePath, EstimateWavDurationSeconds(audioBytes));
        }

        /// <inheritdoc/>
        protected override IEnumerator FetchAvailableVoices()
        {
            using var webRequest = UnityWebRequest.Get($"{m_endpoint}/v1/audio/voices");
            webRequest.timeout = m_requestTimeoutSeconds;
            AddAuthorizationHeader(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                VoicesResponse response;
                try
                {
                    response = JsonUtility.FromJson<VoicesResponse>(webRequest.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Kokoro TTS] Failed to parse voices response: {exception.Message}");
                    response = null;
                }

                if (response != null && response.voices != null && response.voices.Length > 0)
                {
                    m_voices = response.voices;
                    CompleteVoiceFetch(true);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"[Kokoro TTS] Failed to retrieve voices: {webRequest.result} - {webRequest.error}. " +
                    "Using the built-in list; selecting this provider again will retry.");
            }

            m_voices = s_builtinVoices;
            CompleteVoiceFetch(false);
        }

        private void AddAuthorizationHeader(UnityWebRequest webRequest)
        {
            if (m_sendAuthorizationHeader && !string.IsNullOrWhiteSpace(m_authorizationToken))
                webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationToken}");
        }

        private string ResolveVoiceOrDefault(string voice)
        {
            if (!string.IsNullOrWhiteSpace(voice) && ContainsVoice(voice))
                return voice;

            if (m_voices != null && m_voices.Length > 0)
                return m_voices[0];

            return m_fallbackVoice;
        }

        /// <summary>
        /// Estimates duration from a canonical PCM WAV header (Kokoro-FastAPI returns 24 kHz mono 16-bit).
        /// Returns 0 if the header can't be parsed; proxy lipsync still completes, just without rescaling.
        /// </summary>
        private static float EstimateWavDurationSeconds(byte[] wav)
        {
            try
            {
                if (wav == null || wav.Length < 44)
                    return 0f;

                int channels = BitConverter.ToInt16(wav, 22);
                int sampleRate = BitConverter.ToInt32(wav, 24);
                int bitsPerSample = BitConverter.ToInt16(wav, 34);
                int dataSize = BitConverter.ToInt32(wav, 40);

                int bytesPerSecond = sampleRate * channels * (bitsPerSample / 8);
                if (bytesPerSecond <= 0)
                    return 0f;

                return dataSize / (float)bytesPerSecond;
            }
            catch
            {
                return 0f;
            }
        }

        [Serializable]
        private class VoicesResponse
        {
            public string[] voices;
        }

        [Serializable]
        private class SpeechRequest
        {
            public string model;
            public string input;
            public string voice;
            public string response_format;
            public float speed;
        }
    }
}
