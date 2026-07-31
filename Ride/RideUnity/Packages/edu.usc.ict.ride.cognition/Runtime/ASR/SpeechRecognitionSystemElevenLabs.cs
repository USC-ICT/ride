using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ride.Vendor.NativeWebSocket.NativeWebSocket;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// RIDE automatic speech recognition (ASR) system that streams microphone audio to the
    /// ElevenLabs Scribe realtime speech-to-text API over a WebSocket connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation adapts the ElevenLabs realtime transcription protocol into the shared
    /// <see cref="SpeechRecognitionSystemUnity"/> contract used by the rest of the package.
    /// It captures microphone audio in Unity, converts it to 16-bit PCM chunks, sends those chunks
    /// to the provider as base-64 encoded websocket messages, and forwards partial and committed
    /// transcripts through the common RIDE speech recognition pipeline.
    /// </para>
    /// <para>
    /// Configuration is sourced from <see cref="ConfigurationSystemUnity"/> and the active
    /// <c>RideConfig</c> instance rather than hardcoded credentials. The serialized fields on this
    /// component primarily control realtime session behavior such as model selection, language hinting,
    /// chunking cadence, and voice-activity-detection thresholds used when committing audio to the
    /// remote recognizer.
    /// </para>
    /// <para>
    /// Compared with request/response recognizers in this package, this class is a continuous
    /// streaming provider. It keeps a websocket session open while recognition is active, dispatches
    /// inbound provider messages on the Unity thread, and emits both partial and final text updates
    /// as the provider advances through the session lifecycle.
    /// </para>
    /// <para>
    /// Platform note: this provider is intended for non-WebGL runtimes. Unity's microphone API and
    /// the current implementation path used by this class are not available for WebGL builds, so
    /// WebGL microphone capture is intentionally blocked out here. For browser-based deployments,
    /// prefer a WebGL-specific implementation such as <see cref="SpeechRecognitionSystemAzureWebGL"/>.
    /// </para>
    /// <para>
    /// External references:
    /// <see href="https://elevenlabs.io/docs/capabilities/speech-to-text">ElevenLabs Speech to Text</see>,
    /// <see href="https://elevenlabs.io/docs/api-reference/speech-to-text/websocket">ElevenLabs Speech-to-Text WebSocket API</see>.
    /// Related RIDE implementations:
    /// <see cref="SpeechRecognitionSystemOpenAI"/>,
    /// <see cref="SpeechRecognitionSystemAzure"/>,
    /// <see cref="SpeechRecognitionSystemFasterWhisper"/>.
    /// </para>
    /// </remarks>
    public class SpeechRecognitionSystemElevenLabs : SpeechRecognitionSystemUnity
    {
        [Header("Endpoint")]
        [SerializeField] private string m_endpointOverride = string.Empty;

        [Header("Recognition Settings")]
        [SerializeField] private string m_modelId = "scribe_v2_realtime";
        [SerializeField] private string m_languageCodeHint = string.Empty;
        [SerializeField] private bool m_includeTimestamps = true;
        [SerializeField] private bool m_includeLanguageDetection = true;
        [SerializeField] private bool m_noVerbatim = false;
        [SerializeField] private int m_requestTimeoutSeconds = 20;
        [SerializeField] private bool m_enableProtocolDebugLogging = true;

        [Header("Audio Capture")]
        [SerializeField] private int m_sampleRate = 16000;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField] private int m_chunkSize = 6400;
#pragma warning restore 0414
        [SerializeField] private string m_microphoneDevice = null;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField] private int m_recordingBufferSeconds = 10;
#pragma warning restore 0414

        [Header("Voice Activity Detection")]
        [SerializeField] private float m_vadSilenceThresholdSeconds = 0.7f;
        [SerializeField] private float m_vadThreshold = 0.4f;
        [SerializeField] private int m_minSpeechDurationMs = 100;
        [SerializeField] private int m_minSilenceDurationMs = 100;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField] private float m_minCommittedAudioSeconds = 0.35f;
#pragma warning restore 0414

        private string m_apiKey;
        private WebSocket m_webSocket;
        private AudioClip m_recordingClip;
        private bool m_isConnected;
        private bool m_isSystemActive;
        private bool m_sessionStarted;
        private bool m_loggedFirstChunk;
        private int m_lastSamplePosition;
        private Coroutine m_sendCoroutine;
        private Coroutine m_checkRecognizedSpeechCoroutine;
        private string m_recognizedSpeech;
        private string m_recognizedSpeechPartial;
        private string m_recognizedLanguage = string.Empty;
        private string m_recognizedLanguagePartial = string.Empty;

        public override bool IsSupported => !RideUtils.IsWebGL();
        public override bool SupportsContinuousRecognition => true;

        public event Action<string> OnTranscriptionReceived;
        public event Action<string> OnTranscriptionDeltaReceived;
        public event Action<string> OnError;

        public override void SystemInit()
        {
            base.SystemInit();

            ConfigurationSystemUnity configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_apiKey = configSystem.config.elevenLabs.apiKey;

            if (m_includeLanguageDetection)
                m_includeTimestamps = true;

            if (string.Equals(m_modelId, "scribe_v2", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[ElevenLabs ASR] Upgrading legacy realtime model_id 'scribe_v2' to 'scribe_v2_realtime'. Update the serialized component value.");
                m_modelId = "scribe_v2_realtime";
            }

            CheckMicrophonePermissions();
        }

        public override void SystemShutdown()
        {
            Cleanup();
            base.SystemShutdown();
        }

        void OnDisable()
        {
            Cleanup();
        }

        protected override void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            m_webSocket?.DispatchMessageQueue();
#endif
            base.Update();
        }

        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);
            m_microphoneDevice = SelectedMicrophone;
        }

        public override void OnRecognizingStarted()
        {
            if (m_checkRecognizedSpeechCoroutine == null)
                m_checkRecognizedSpeechCoroutine = StartCoroutine(CheckRecognizedSpeechRoutine());

            if (!m_isSystemActive)
            {
                StartCoroutine(StartSystem());
            }
            else if (m_isConnected)
            {
                StartRecording();
            }

            base.OnRecognizingStarted();
        }

        public override void OnRecognizingStopped()
        {
            StopRecording();

            if (m_checkRecognizedSpeechCoroutine != null)
            {
                StopCoroutine(m_checkRecognizedSpeechCoroutine);
                m_checkRecognizedSpeechCoroutine = null;
            }

            if (m_sendCoroutine != null)
            {
                StopCoroutine(m_sendCoroutine);
                m_sendCoroutine = null;
            }

            m_recognizedSpeech = null;
            m_recognizedSpeechPartial = null;
            m_recognizedLanguage = string.Empty;
            m_recognizedLanguagePartial = string.Empty;
            CloseWebSocket();
            m_isSystemActive = false;

            base.OnRecognizingStopped();
        }

        private IEnumerator StartSystem()
        {
            if (m_isSystemActive)
                yield break;

            if (string.IsNullOrWhiteSpace(m_apiKey))
            {
                Debug.LogError("[ElevenLabs ASR] Missing API key.");
                yield break;
            }

            m_isSystemActive = true;
            yield return StartCoroutine(ConnectToRealtimeApi());

            if (m_isConnected && IsRecognizing)
                StartRecording();
        }

        private IEnumerator ConnectToRealtimeApi()
        {
            if (m_isConnected)
                yield break;

            string url = BuildWebSocketUrl();
            if (m_enableProtocolDebugLogging)
                Debug.Log($"[ElevenLabs ASR] Connecting to '{url}'.");
            var headers = new Dictionary<string, string>
            {
                { "xi-api-key", m_apiKey }
            };

            m_webSocket = new WebSocket(url, headers);
            m_webSocket.OnOpen += OnWebSocketOpen;
            m_webSocket.OnMessage += OnWebSocketMessage;
            m_webSocket.OnError += OnWebSocketError;
            m_webSocket.OnClose += OnWebSocketClose;

            Task connectTask = null;
            Exception connectException = null;

            try
            {
                connectTask = m_webSocket.Connect();
            }
            catch (Exception ex)
            {
                connectException = ex;
            }

            if (connectException != null)
            {
                Debug.LogError($"[ElevenLabs ASR] Failed to start websocket connect: {connectException.Message}");
                OnError?.Invoke(connectException.Message);
                m_isSystemActive = false;
                yield break;
            }

            float startTime = Time.time;
            while (m_webSocket.State != WebSocketState.Open && connectTask != null && !connectTask.IsCompleted)
            {
                if (Time.time - startTime > m_requestTimeoutSeconds)
                {
                    Debug.LogError("[ElevenLabs ASR] Timed out while connecting to realtime API.");
                    OnError?.Invoke("Timed out while connecting to realtime API.");
                    yield break;
                }
                yield return null;
            }

            float postWaitStart = Time.time;
            while (m_webSocket.State != WebSocketState.Open && (Time.time - postWaitStart) < 2f)
                yield return null;

            if (m_webSocket.State != WebSocketState.Open)
            {
                Debug.LogError("[ElevenLabs ASR] WebSocket connection did not open properly.");
                OnError?.Invoke("WebSocket connection failed.");
                yield break;
            }

            if (m_sendCoroutine == null)
                m_sendCoroutine = StartCoroutine(SendWebSocketMessages());
        }

        private string BuildWebSocketUrl()
        {
            string baseUrl = string.IsNullOrWhiteSpace(m_endpointOverride)
                ? "wss://api.elevenlabs.io/v1/speech-to-text/realtime"
                : m_endpointOverride.Trim();

            if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                baseUrl = "wss://" + baseUrl.Substring("https://".Length);
            else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                baseUrl = "ws://" + baseUrl.Substring("http://".Length);

            if (!baseUrl.Contains("/speech-to-text/realtime", StringComparison.OrdinalIgnoreCase))
                baseUrl = $"{baseUrl.TrimEnd('/')}/speech-to-text/realtime";

            List<string> query = new List<string>
            {
                $"model_id={Uri.EscapeDataString(m_modelId)}",
                $"include_timestamps={m_includeTimestamps.ToString().ToLowerInvariant()}",
                $"include_language_detection={m_includeLanguageDetection.ToString().ToLowerInvariant()}",
                "timestamps_granularity=word",
                $"audio_format={Uri.EscapeDataString($"pcm_{m_sampleRate}")}",
                "commit_strategy=vad",
                $"vad_silence_threshold_secs={m_vadSilenceThresholdSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                $"vad_threshold={m_vadThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                $"min_speech_duration_ms={m_minSpeechDurationMs}",
                $"min_silence_duration_ms={m_minSilenceDurationMs}",
                $"no_verbatim={m_noVerbatim.ToString().ToLowerInvariant()}"
            };

            if (!string.IsNullOrWhiteSpace(m_languageCodeHint))
                query.Add($"language_code={Uri.EscapeDataString(m_languageCodeHint)}");

            return $"{baseUrl}?{string.Join("&", query)}";
        }

        private void OnWebSocketOpen()
        {
            Debug.Log("[ElevenLabs ASR] Connected to realtime API.");
            m_isConnected = true;
        }

        private void OnWebSocketMessage(byte[] data)
        {
            if (!IsRecognizing)
                return;

            try
            {
                string json = Encoding.UTF8.GetString(data);
                if (m_enableProtocolDebugLogging)
                    Debug.Log($"[ElevenLabs ASR] Raw inbound message: {json}");
                JObject message = JObject.Parse(json);
                string messageType = message["message_type"]?.ToString() ?? string.Empty;

                switch (messageType)
                {
                    case "session_started":
                        Debug.Log($"[ElevenLabs ASR] Session started. config={message["config"]?.ToString(Formatting.None)}");
                        m_sessionStarted = true;
                        if (IsRecognizing && m_recordingClip == null)
                            StartRecording();
                        break;

                    case "partial_transcript":
                        {
                            string partial = message["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(partial))
                            {
                                m_recognizedSpeechPartial = partial;
                                OnTranscriptionDeltaReceived?.Invoke(partial);
                            }
                            break;
                        }

                    case "committed_transcript":
                        {
                            string text = message["text"]?.ToString();
                            string languageCode = message["language_code"]?.ToString() ?? string.Empty;
                            Debug.Log($"[ElevenLabs ASR] committed_transcript language_code='{(string.IsNullOrWhiteSpace(languageCode) ? "missing" : languageCode)}' text='{text}'");
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                m_recognizedSpeech = text;
                                m_recognizedLanguage = languageCode;
                                OnTranscriptionReceived?.Invoke(text);
                            }
                            break;
                        }

                    case "committed_transcript_with_timestamps":
                        {
                            string text = message["text"]?.ToString();
                            string languageCode = message["language_code"]?.ToString() ?? string.Empty;
                            Debug.Log($"[ElevenLabs ASR] committed_transcript_with_timestamps language_code='{(string.IsNullOrWhiteSpace(languageCode) ? "missing" : languageCode)}' text='{text}'");
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                m_recognizedSpeech = text;
                                m_recognizedLanguage = languageCode;
                                OnTranscriptionReceived?.Invoke(text);
                            }
                            break;
                        }

                    default:
                        if ((!string.IsNullOrEmpty(messageType) && messageType.Contains("error", StringComparison.OrdinalIgnoreCase)) ||
                            message["error"] != null)
                        {
                            string error = message["message"]?.ToString() ?? message["error"]?.ToString();
                            if (string.IsNullOrWhiteSpace(error))
                                error = message.ToString(Formatting.None);
                            Debug.LogError($"[ElevenLabs ASR] API Error: {error}");
                            OnError?.Invoke(error);
                        }
                        else if (m_enableProtocolDebugLogging)
                        {
                            Debug.LogWarning($"[ElevenLabs ASR] Unhandled inbound message_type='{messageType}'.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevenLabs ASR] Error processing message: {ex.Message}");
            }
        }

        private void OnWebSocketError(string error)
        {
            Debug.LogError($"[ElevenLabs ASR] WebSocket Error: {error}");
            OnError?.Invoke(error);
        }

        private void OnWebSocketClose(WebSocketCloseCode code)
        {
            Debug.Log($"[ElevenLabs ASR] WebSocket Closed: {code}");
            m_isConnected = false;
        }

        private IEnumerator CheckRecognizedSpeechRoutine()
        {
            while (IsRecognizing)
            {
                if (!string.IsNullOrEmpty(m_recognizedSpeechPartial))
                {
                    OnPartialSpeechRecognized(m_recognizedSpeechPartial, Confidence, m_recognizedLanguagePartial);
                    m_recognizedSpeechPartial = null;
                    m_recognizedLanguagePartial = string.Empty;
                }

                if (!string.IsNullOrEmpty(m_recognizedSpeech))
                {
                    Debug.Log($"[ElevenLabs ASR] Recognized language='{(string.IsNullOrWhiteSpace(m_recognizedLanguage) ? "unknown" : m_recognizedLanguage)}' text='{m_recognizedSpeech}'");
                    OnSpeechRecognized(m_recognizedSpeech, Confidence, m_recognizedLanguage);
                    m_recognizedSpeech = null;
                    m_recognizedLanguage = string.Empty;
                }

                yield return null;
            }

            m_checkRecognizedSpeechCoroutine = null;
        }

        private IEnumerator SendWebSocketMessages()
        {
            while (m_isSystemActive)
            {
                if (!IsRecognizing || m_recordingClip == null || m_webSocket == null || m_webSocket.State != WebSocketState.Open)
                {
                    yield return null;
                    continue;
                }

                if (!m_sessionStarted)
                {
                    yield return null;
                    continue;
                }

#if !UNITY_WEBGL
                int currentPosition = Microphone.GetPosition(m_microphoneDevice);
                if (currentPosition >= 0)
                {
                    int samplesToSend = currentPosition - m_lastSamplePosition;
                    if (samplesToSend < 0)
                        samplesToSend += m_recordingClip.samples;

                    int minCommitSamples = Mathf.Max(m_chunkSize, Mathf.CeilToInt(m_minCommittedAudioSeconds * m_sampleRate));
                    if (samplesToSend >= minCommitSamples)
                    {
                        SendAudioChunk(minCommitSamples);
                        m_lastSamplePosition = (m_lastSamplePosition + minCommitSamples) % m_recordingClip.samples;
                    }
                }
#endif

                yield return null;
            }

            m_sendCoroutine = null;
        }

        private void StartRecording()
        {
            if (m_recordingClip != null || !m_isConnected || !m_isSystemActive || !m_sessionStarted)
                return;

            if (string.IsNullOrEmpty(m_microphoneDevice))
                m_microphoneDevice = SelectedMicrophone;

            if (string.IsNullOrEmpty(m_microphoneDevice))
            {
                Debug.LogWarning("[ElevenLabs ASR] No microphone selected.");
                return;
            }

#if !UNITY_WEBGL
            m_recordingClip = Microphone.Start(m_microphoneDevice, true, m_recordingBufferSeconds, m_sampleRate);
#else
            Debug.LogWarning("[ElevenLabs ASR] Realtime microphone capture is not supported on WebGL.");
#endif
            m_lastSamplePosition = 0;
            Confidence = 1f;
            Debug.Log("[ElevenLabs ASR] Recording started.");
        }

        private void StopRecording()
        {
            if (m_recordingClip == null)
                return;

            FlushPendingAudioChunk();
#if !UNITY_WEBGL
            Microphone.End(m_microphoneDevice);
#endif
            m_recordingClip = null;
            m_lastSamplePosition = 0;
            Debug.Log("[ElevenLabs ASR] Recording stopped.");
        }

        private void FlushPendingAudioChunk()
        {
            if (m_recordingClip == null || m_webSocket == null || m_webSocket.State != WebSocketState.Open)
                return;

#if !UNITY_WEBGL
            int currentPosition = Microphone.GetPosition(m_microphoneDevice);
            if (currentPosition < 0)
                return;

            int samplesToSend = currentPosition - m_lastSamplePosition;
            if (samplesToSend < 0)
                samplesToSend += m_recordingClip.samples;

            int minCommitSamples = Mathf.CeilToInt(m_minCommittedAudioSeconds * m_sampleRate);
            if (samplesToSend >= minCommitSamples)
            {
                SendAudioChunk(samplesToSend, commitImmediately: true);
                m_lastSamplePosition = currentPosition % m_recordingClip.samples;
            }
#endif
        }

        private void SendAudioChunk(int sampleCount, bool commitImmediately = false)
        {
            if (m_recordingClip == null || sampleCount <= 0)
                return;

            float[] samples = ReadCircularMonoSamples(sampleCount);
            byte[] pcm16Data = ConvertToPcm16(samples);
            string base64Audio = Convert.ToBase64String(pcm16Data);

            var audioEvent = new
            {
                message_type = "input_audio_chunk",
                audio_base_64 = base64Audio,
                commit = commitImmediately,
                sample_rate = m_sampleRate
            };

            if (m_enableProtocolDebugLogging && !m_loggedFirstChunk)
            {
                Debug.Log($"[ElevenLabs ASR] Sending first audio chunk. samples={sampleCount} pcmBytes={pcm16Data.Length} channels=1 sampleRate={m_sampleRate}");
                m_loggedFirstChunk = true;
            }

            SendEvent(audioEvent);
        }

        private float[] ReadCircularMonoSamples(int sampleCount)
        {
            int channels = m_recordingClip.channels;
            int totalSamples = m_recordingClip.samples;

            if (m_lastSamplePosition + sampleCount <= totalSamples)
            {
                float[] interleaved = new float[sampleCount * channels];
                m_recordingClip.GetData(interleaved, m_lastSamplePosition);
                return DownmixToMono(interleaved, channels);
            }

            int firstSegmentFrames = totalSamples - m_lastSamplePosition;
            int secondSegmentFrames = sampleCount - firstSegmentFrames;
            float[] firstSegment = new float[firstSegmentFrames * channels];
            float[] secondSegment = new float[secondSegmentFrames * channels];
            float[] wrappedInterleaved = new float[sampleCount * channels];

            m_recordingClip.GetData(firstSegment, m_lastSamplePosition);
            m_recordingClip.GetData(secondSegment, 0);

            Buffer.BlockCopy(firstSegment, 0, wrappedInterleaved, 0, firstSegment.Length * sizeof(float));
            Buffer.BlockCopy(secondSegment, 0, wrappedInterleaved, firstSegment.Length * sizeof(float), secondSegment.Length * sizeof(float));
            return DownmixToMono(wrappedInterleaved, channels);
        }

        private static float[] DownmixToMono(float[] interleavedSamples, int channels)
        {
            if (channels <= 1)
                return interleavedSamples;

            int frameCount = interleavedSamples.Length / channels;
            float[] mono = new float[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                float sum = 0f;
                int baseIndex = frame * channels;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleavedSamples[baseIndex + channel];

                mono[frame] = sum / channels;
            }

            return mono;
        }

        private static byte[] ConvertToPcm16(float[] samples)
        {
            byte[] pcm16 = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(value);
                pcm16[i * 2] = bytes[0];
                pcm16[i * 2 + 1] = bytes[1];
            }
            return pcm16;
        }

        private void SendEvent(object eventData)
        {
            if (m_webSocket?.State != WebSocketState.Open)
                return;

            string json = JsonConvert.SerializeObject(eventData);
            m_webSocket.SendText(json);
        }

        private void CheckMicrophonePermissions()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
#elif UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
        }

        private void Cleanup()
        {
            if (m_checkRecognizedSpeechCoroutine != null)
            {
                StopCoroutine(m_checkRecognizedSpeechCoroutine);
                m_checkRecognizedSpeechCoroutine = null;
            }

            if (m_sendCoroutine != null)
            {
                StopCoroutine(m_sendCoroutine);
                m_sendCoroutine = null;
            }

            StopRecording();

            m_isSystemActive = false;
            m_isConnected = false;
            m_sessionStarted = false;
            m_loggedFirstChunk = false;
            m_recognizedSpeech = null;
            m_recognizedSpeechPartial = null;
            m_recognizedLanguage = string.Empty;
            m_recognizedLanguagePartial = string.Empty;

            CloseWebSocket();
        }

        private void CloseWebSocket()
        {
            m_isConnected = false;
            m_sessionStarted = false;
            m_loggedFirstChunk = false;

            if (m_webSocket != null)
            {
                m_webSocket.OnOpen -= OnWebSocketOpen;
                m_webSocket.OnMessage -= OnWebSocketMessage;
                m_webSocket.OnError -= OnWebSocketError;
                m_webSocket.OnClose -= OnWebSocketClose;

                try { _ = m_webSocket.Close(); } catch (Exception) { }
                m_webSocket = null;
            }
        }
    }
}
