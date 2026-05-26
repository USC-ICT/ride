using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Automated Speech Recognition system that records microphone audio in Unity and submits
    /// short utterances to a FasterWhisper HTTP service for transcription.
    /// </summary>
    public class SpeechRecognitionSystemFasterWhisper : SpeechRecognitionSystemUnity
    {
        [Header("Endpoint")]
        [SerializeField] private string m_endpoint = "http://127.0.0.1:9001/transcribe";
        [SerializeField] private bool m_sendAuthorizationHeader = false;
        [SerializeField] private string m_authorizationToken = string.Empty;

        [Header("Recognition Settings")]
        [SerializeField] private string m_language = "en";
        [SerializeField] private bool m_vadFilter = true;
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 30;

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
        private Coroutine m_requestCoroutine;

        public override bool IsSupported => !RideUtils.IsWebGL();
        public override bool SupportsContinuousRecognition => true;

        void OnDisable()
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
            if (!IsSupported)
                return;

            if (!IsRecognizing)
                return;

            if (string.IsNullOrEmpty(m_microphoneDevice))
                m_microphoneDevice = SelectedMicrophone;

            if (string.IsNullOrEmpty(m_microphoneDevice))
            {
                Debug.LogWarning("[FasterWhisper ASR] No microphone is selected.");
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
                Debug.LogError("[FasterWhisper ASR] Failed to start microphone capture.");
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
                Debug.Log("[FasterWhisper ASR] Initial silence timeout reached. Restarting capture.");
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
            {
                float amplitude = Mathf.Abs(sampleBuffer[i]);
                if (amplitude > maxAmplitude)
                    maxAmplitude = amplitude;
            }

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

            m_requestCoroutine = StartCoroutine(SubmitUtteranceCoroutine(utteranceClip));
        }

        private AudioClip CreateUtteranceClip(int sampleFrames)
        {
            if (m_recordingClip == null || sampleFrames <= 0)
                return null;

            int channels = m_recordingClip.channels;
            float[] samples = new float[sampleFrames * channels];
            m_recordingClip.GetData(samples, 0);

            AudioClip utteranceClip = AudioClip.Create(
                "FasterWhisperUtterance",
                sampleFrames,
                channels,
                m_sampleRate,
                false);

            utteranceClip.SetData(samples, 0);
            return utteranceClip;
        }

        private IEnumerator SubmitUtteranceCoroutine(AudioClip utteranceClip)
        {
            m_requestInFlight = true;

            byte[] wavBytes = BuildWavBytes(utteranceClip);
            Destroy(utteranceClip);

            string requestUrl = BuildRequestUrl();
            using var webRequest = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(wavBytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "audio/wav");

            if (m_sendAuthorizationHeader && !string.IsNullOrWhiteSpace(m_authorizationToken))
                webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationToken}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                FasterWhisperResponse response =
                    RideIO.JsonDeserialize<FasterWhisperResponse>(webRequest.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(response.text))
                    OnSpeechRecognized(response.text, Mathf.Clamp01(response.confidence));
            }
            else
            {
                Debug.LogWarning(
                    $"[FasterWhisper ASR] Transcription request failed: {webRequest.result} - {webRequest.error}");
            }

            m_requestInFlight = false;
            m_requestCoroutine = null;

            if (IsRecognizing)
                StartCapture();
        }

        private string BuildRequestUrl()
        {
            StringBuilder builder = new StringBuilder(m_endpoint);
            builder.Append(m_endpoint.Contains("?") ? "&" : "?");
            builder.Append("vad_filter=").Append(m_vadFilter ? "true" : "false");

            if (!string.IsNullOrWhiteSpace(m_language))
            {
                builder.Append("&language=");
                builder.Append(UnityWebRequest.EscapeURL(m_language));
            }

            return builder.ToString();
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

            byte[] wavBytes = new byte[44 + pcmBytes.Length];
            using var memoryStream = new System.IO.MemoryStream(wavBytes);
            using var writer = new System.IO.BinaryWriter(memoryStream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + pcmBytes.Length);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(pcmBytes.Length);
            writer.Write(pcmBytes);

            return wavBytes;
        }

        [Serializable]
        private struct FasterWhisperResponse
        {
            public string text;
            public float confidence;
            public string language;
            public float duration_seconds;
        }
    }
}
