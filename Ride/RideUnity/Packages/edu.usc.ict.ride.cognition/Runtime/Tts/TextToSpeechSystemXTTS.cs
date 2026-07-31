using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Local Coqui XTTS v2-backed text-to-speech via a small HTTP service exposing GET /voices and
    /// POST /synthesize (base64 WAV back). Proxy lipsync is generated from the clip duration.
    ///
    /// Connection config is code-authoritative (NOT [SerializeField]) so script changes take effect
    /// without a prefab/scene edit; for per-deployment overrides, source from RideConfig.
    /// NOTE: XTTS v2 weights are CPML (non-commercial); slated for replacement by Chatterbox (R1).
    /// </summary>
    public class TextToSpeechSystemXTTS : TextToSpeechSystemProxyLipsynced
    {
        private string m_endpoint = "http://127.0.0.1:9004";
        private bool m_sendAuthorizationHeader = false;
        private string m_authorizationToken = string.Empty;
        private int m_requestTimeoutSeconds = 20;
        private string m_fallbackVoice = "Ana Florence";

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

            SynthesizeRequest requestBody = new() { text = text, voice = voice };
            string requestJson = JsonUtility.ToJson(requestBody);

            using var webRequest = new UnityWebRequest($"{m_endpoint}/synthesize", UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = GetRequestTimeoutSeconds(m_requestTimeoutSeconds, text);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            AddAuthorizationHeader(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[XTTS TTS] Synthesis request failed: {webRequest.result} - {webRequest.error}. " +
                    $"Response: {webRequest.downloadHandler?.text}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            SynthesizeResponse response;
            try
            {
                response = JsonUtility.FromJson<SynthesizeResponse>(webRequest.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[XTTS TTS] Failed to parse synthesis response: {exception.Message}. " +
                    $"Response: {webRequest.downloadHandler.text}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            if (response == null || string.IsNullOrWhiteSpace(response.audio_base64))
            {
                Debug.LogWarning(
                    $"[XTTS TTS] Synthesis response did not contain audio. Response: {webRequest.downloadHandler.text}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            byte[] audioBytes;
            try
            {
                audioBytes = Convert.FromBase64String(response.audio_base64);
            }
            catch (FormatException exception)
            {
                Debug.LogWarning($"[XTTS TTS] Invalid base64 audio payload: {exception.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            string filePath = Path.Combine(
                Application.persistentDataPath, $"xttsTTS_{DateTime.UtcNow.Ticks}.wav");

            try
            {
                File.WriteAllBytes(filePath, audioBytes);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[XTTS TTS] Failed to write audio file: {exception.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            CompleteTextToSpeechGeneration(filePath, response.duration_seconds);
        }

        /// <inheritdoc/>
        protected override IEnumerator FetchAvailableVoices()
        {
            using var webRequest = UnityWebRequest.Get($"{m_endpoint}/voices");
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
                    Debug.LogWarning($"[XTTS TTS] Failed to parse voices response: {exception.Message}");
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
                Debug.LogWarning($"[XTTS TTS] Failed to retrieve voices: {webRequest.result} - {webRequest.error}. " +
                    "Using the fallback voice; selecting this provider again will retry.");
            }

            m_voices = new[] { m_fallbackVoice };
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

        [Serializable] private class VoicesResponse { public string[] voices; }
        [Serializable] private class SynthesizeRequest { public string text; public string voice; }

        [Serializable]
        private class SynthesizeResponse
        {
            public string audio_base64;
            public string audio_format;
            public int sample_rate_hz;
            public float duration_seconds;
            public string voice;
        }
    }
}
