using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.TextToSpeech
{
    /*
     * TextToSpeechSystemElevenLabs (RIDE system)
     * ------------------------------------------
     * Purpose
     *   RIDE-facing TTS system that mirrors the Polly/Azure pattern while delegating actual ElevenLabs
     *   REST calls to the embedded ElevenLabsTextToSpeech MonoBehaviour (non-WebGL) or to a Lambda
     *   Function URL proxy (WebGL). Lip-sync is handled by TextToSpeechSystemProxyLipsynced using a
     *   different provider (e.g., Polly speech marks); this class does NOT fetch visemes.
     *
     * Behavior
     *   - GetAvailableVoices(): returns the cached voice names exposed by ElevenLabsTextToSpeech.
     *   - StartTextToSpeechGeneration(voice, text):
     *       * Non-WebGL: waits for voices, sets SelectedVoiceIndex on the MB, runs ConvertTextToSpeechCoroutine(),
     *                    then calls CompleteTextToSpeechGeneration(filePath, seconds).
     *       * WebGL: waits for voices, POSTs {text, voice} to Lambda /audio, expects {url}, then completes with that URL.
     *
     * WebGL Lambda contract (audio only)
     *   - POST /audio
     *       Body: { "text": "...", "voice": "Name" }
     *       Reply: { "url": "https://<bucket>.s3.amazonaws.com/<prefix>/<uuid>.mp3" }
     *   - No /visemes endpoint (lip-sync is provided by other systems in the proxy pipeline).
     *
     * Configuration
     *   - ElevenLabsTextToSpeech must exist on the same GameObject (or be attached in SystemInit()).
     *   - For WebGL, configure the Lambda Function URL (either via serialized fields or your config system).
     *
     * Notes / Gotchas
     *   - Ensure you call/await the MB's WaitForVoices() before generating audio (this class does it internally).
     *   - Avoid hard-coding Lambda IDs; prefer serialized fields or config so you can swap environments easily.
     *   - Consider adding UnityWebRequest.timeout in WebGL to prevent UI hangs.
     */
    public class TextToSpeechSystemElevenLabs : TextToSpeechSystemProxyLipsynced
    {
#if UNITY_WEBGL
        [Serializable]
        private class WebGlTtsRequest { public string text; public string voice; }

        [Serializable]
        private class WebGlTtsReply { public string url; }
#endif

        [SerializeField]
        private ElevenLabsTextToSpeech textToSpeech;

        public ElevenLabsTextToSpeech TextToSpeech => textToSpeech;

        public int SelectedVoiceIndex
        {
            get => textToSpeech.SelectedVoiceIndex;
            set => textToSpeech.SelectedVoiceIndex = value;
        }

        public List<string> Voices => textToSpeech.Voices;

        /// <summary>
        /// Indicates whether the list of available voices has been loaded from AWS.
        /// </summary>
        public bool VoicesReady => textToSpeech.VoicesReady;


        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => textToSpeech.Voices.ToArray();


        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            StartCoroutine(StartTextToSpeechGenerationCoroutine(voice, text));
        }

        /// <summary>
        /// Coroutine that waits for ElevenLabs TTS generation and completes processing.
        /// </summary>
        /// <param name="text">The input text for speech generation.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text)
        {
            yield return StartCoroutine(textToSpeech.WaitForVoices());

            if (string.IsNullOrEmpty(voice))
                voice = GetAvailableVoices().Length > 0 ? GetAvailableVoices()[0] : "Aria";

#if !UNITY_WEBGL
            SelectedVoiceIndex = textToSpeech.Voices.IndexOf(voice);
            yield return StartCoroutine(textToSpeech.ConvertTextToSpeechCoroutine(text));

            // Assuming SaveAudioClipToFile method exists that saves the AudioClip to a file and returns the path.
            string audioFilePath = textToSpeech.savedFilePath;

            CompleteTextToSpeechGeneration(audioFilePath, textToSpeech.clipTime);
#else
            string lambda = "zmxqrfujpmzuoaobpa57aattpm0omenq";
            string url = $"https://{lambda}.lambda-url.us-west-2.on.aws/audio";
            var body = JsonUtility.ToJson(new WebGlTtsRequest { text = text, voice = voice });

            using (var req = UnityWebRequest.Put(url, body))
            {
                req.method = UnityWebRequest.kHttpVerbPOST;
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"TextToSpeechSystemElevenLabs(WebGL): request failed: {req.error}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                WebGlTtsReply reply = null;
                try
                {
                    reply = JsonUtility.FromJson<WebGlTtsReply>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextToSpeechSystemElevenLabs(WebGL): JSON parse error: {ex.Message}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                if (reply == null || string.IsNullOrEmpty(reply.url))
                {
                    Debug.LogError("TextToSpeechSystemElevenLabs(WebGL): Missing URL in Lambda reply.");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                Debug.Log($"[WebGL] MP3 available at: {reply.url}");

                // For WebGL, return the URL for the client to download/use later.
                CompleteTextToSpeechGeneration(reply.url);
            }
#endif
        }
    }
}
