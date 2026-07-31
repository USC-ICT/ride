using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ride.Conversation;
using Ride.Vendor.NativeWebSocket.NativeWebSocket;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// OpenAI Realtime API speech recognition implementation for microphone-driven streaming transcription.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation adapts OpenAI's realtime websocket session into the shared
    /// <see cref="SpeechRecognitionSystemUnity"/> contract used by the rest of the package. It streams
    /// microphone audio to the configured OpenAI realtime endpoint, forwards partial and final transcript
    /// events through the normal RIDE speech-recognition callbacks, and can optionally request assistant
    /// audio output when <c>transcriptOnly</c> is disabled.
    /// </para>
    /// <para>
    /// In transcript-only mode, the class behaves as a speech-recognition backend. In transcript-plus-audio
    /// mode, it also exercises the assistant response path so the same websocket session can return both
    /// transcription updates and synthesized audio playback. That makes this class useful both as an ASR
    /// provider and as a lightweight reference for the broader realtime conversation flow.
    /// </para>
    /// <para>
    /// Runtime configuration is resolved from <see cref="ConfigurationSystemUnity"/> when needed rather than
    /// cached locally, so changes to the active RIDE configuration remain the source of truth for the
    /// endpoint and API key used by connection setup.
    /// </para>
    /// <para>
    /// External references:
    /// <see href="https://developers.openai.com/api/docs/guides/realtime">OpenAI Realtime guide</see>,
    /// <see href="https://developers.openai.com/api/reference/resources/realtime">OpenAI Realtime API reference</see>,
    /// <see href="https://docs.unity3d.com/Manual/class-Microphone.html">Unity Manual: Microphone</see>.
    /// Related RIDE implementations:
    /// <see cref="SpeechRecognitionSystemUnity"/>,
    /// <see cref="SpeechRecognitionSystemWindows"/>,
    /// <see cref="SpeechRecognitionSystemAzure"/>,
    /// <see cref="SpeechRecognitionSystemElevenLabs"/>,
    /// <see cref="Ride.Conversation.UnifiedConversationSystemOpenAIRealtime"/>.
    /// </para>
    /// </remarks>
    public class SpeechRecognitionSystemOpenAI : SpeechRecognitionSystemUnity
    {
        [Serializable]
        private class WhisperResponse
        {
            public string text;
        }


        public override bool IsSupported => true;
        public override bool SupportsContinuousRecognition => true;

#if PLATFORM_ANDROID || PLATFORM_IOS
        // Required to manifest microphone permission, cf.
        // https://docs.unity3d.com/Manual/android-manifest.html
#endif        

        [Header("API Settings")]
#pragma warning disable 0414  // (WebGL) The field '' is assigned but its value is never used
        [SerializeField] private OpenAIRealtimeModel model = OpenAIRealtimeModel.Realtime21Mini;
        [SerializeField] private OpenAIRealtimeTranscriptionModel transcriptionModel =
            OpenAIRealtimeTranscriptionModel.RealtimeWhisper;
#pragma warning restore 0414

        [Header("Audio Settings")]
        [SerializeField] private int sampleRate = 24000; // Realtime API uses 24kHz
#pragma warning disable 0414  // (WebGL) The field '' is assigned but its value is never used
        [SerializeField] private int chunkSize = 4800; // 200ms at 24kHz
        [SerializeField] private string microphoneDevice = null;
#pragma warning restore 0414

        [Header("Response Settings")]
        [SerializeField] private bool transcriptOnly = true; // Toggle: true = text only, false = text + audio
        [SerializeField] private string voice = "alloy"; // alloy, echo, shimmer (used when transcriptOnly is false)

        [Header("Conversation Flow")]
        [SerializeField] private float postPlaybackDelay = 0.3f; // Delay before re-enabling recording after playback
        [SerializeField] private float streamingStartBufferSeconds = 1.0f; // Start playing after buffering this much audio
        [SerializeField] private float streamingEndBufferSeconds = 3.0f; // Wait this long after queue empty before stopping
        [SerializeField] private int maxStreamingClipSeconds = 300; // Maximum AudioClip size in seconds (5 minutes)

        [Header("Voice Activity Detection")]
        [SerializeField] private float vadThreshold = 0.7f; // VAD sensitivity (0.0-1.0, higher = less sensitive)
        [SerializeField] private int vadPrefixPaddingMs = 300; // Audio captured before speech detected (ms)
        // How long a pause ends the utterance. Server-side VAD takes milliseconds, while the shared
        // setting is in seconds, so this converts rather than duplicating the knob: a second field
        // would let the Inspector show one value while the service is sent another.
        private int VadSilenceDurationMs => Mathf.RoundToInt(AutoSilenceTimeoutSeconds * 1000f);


        private const float MinCommitAudioSeconds = 0.1f;
        private const float StreamingPlaybackMonitorIntervalSeconds = 0.1f;
        private const float RestartRecordingPlaybackSettleSeconds = 0.2f;
        private WebSocket websocket;
        private AudioClip recordingClip;
        private bool isRecording = false;
        private bool isConnected = false;
        private bool isPlayingResponse = false;
        private bool isSystemActive = false;
        private bool audioStreamComplete = false;
        private int lastSamplePosition = 0;
        private int inputAudioBufferSamples = 0;
        private AudioSource audioSource;
        private Queue<float> streamingAudioQueue = new Queue<float>();
        private bool isStreamingAudio = false;
        private bool isAudioCallbackReady = false;
        private int streamingPlaybackPosition = 0;
        private int currentClipSamples = 0;
        private string currentTranscriptionBuffer = "";
        private object audioQueueLock = new object();
        private float m_streamingPlaybackMonitorTimer = 0f;
        private int m_streamingPlaybackEmptyFrames = 0;
        private bool m_restartRecordingPending = false;
        private bool m_restartRecordingAwaitingPlaybackStop = false;
        private float m_restartRecordingReadyTime = 0f;

        // Events
        public event Action<string> OnTranscriptionReceived;
        public event Action<string> OnTranscriptionDeltaReceived;
        public event Action OnAudioResponseStarted;
        public event Action OnAudioResponseFinished;
        public event Action OnConnectionEstablished;
        public event Action<string> OnError;
        public event Action OnUserSpeechStarted;
        public event Action OnUserSpeechEnded;
        public event Action OnSystemStarted;
        public event Action OnSystemStopped;

        private string m_recognizedSpeech;
        private string m_recognizedSpeechPartial;
        private string m_recognizedLanguage = string.Empty;
        private string m_recognizedLanguagePartial = string.Empty;


        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            CheckMicrophonePermissions();

#if !UNITY_WEBGL
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected.");
                return;
            }

            Debug.Log($"[SpeechRecognitionSystemOpenAI] Available microphones: {string.Join(", ", Microphone.devices)}");
#endif

            InitializeAgentResponseAudio();  // Not needed for ASR, but included as unified real-time ASR + NLP + TTS example
            StartCoroutine(StartSystem());
        }

        void OnDisable()
        {
            Cleanup();
        }

        public override void SystemShutdown()
        {
            Cleanup();

            base.SystemShutdown();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            StartRecording();

            base.OnRecognizingStarted();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            StopRecording();

            base.OnRecognizingStopped();
        }

        /// <inheritdoc/>
        public override void SystemUpdate(float dt)
        {
            // Dispatch WebSocket messages on Unity main thread
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif

            ProcessRecognizedSpeech();
            PumpMicrophoneAudio();
            UpdateStreamingPlaybackMonitor(dt);
            UpdatePendingRecordingRestart();

            base.SystemUpdate(dt);
        }

        /// <summary>
        /// Processes the most recent partial and final transcription results gathered from the
        /// realtime websocket callbacks and forwards them through the shared speech-recognition
        /// event flow exposed by <see cref="SpeechRecognitionSystemUnity"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called from <see cref="SystemUpdate(float)"/> after websocket message
        /// dispatch has populated the staged recognition fields.
        /// </para>
        /// <para>
        /// Keeping transcript promotion here centralizes the handoff between the OpenAI Realtime
        /// transport layer and the package's existing partial/final recognition callbacks, so
        /// downstream systems continue to observe the same behavior as other
        /// <see cref="ISpeechRecognitionSystem"/> implementations.
        /// </para>
        /// </remarks>
        private void ProcessRecognizedSpeech()
        {            
            if (!IsRecognizing)
                return;

            if (!string.IsNullOrEmpty(m_recognizedSpeechPartial))
            {
                OnPartialSpeechRecognized(m_recognizedSpeechPartial, Confidence, m_recognizedLanguagePartial);
                m_recognizedSpeechPartial = null;
                m_recognizedLanguagePartial = string.Empty;
            }

            if (!string.IsNullOrEmpty(m_recognizedSpeech))
            {
                OnSpeechRecognized(m_recognizedSpeech, Confidence, m_recognizedLanguage);
                m_recognizedSpeech = null;
                m_recognizedLanguage = string.Empty;
            }
        }

        /// <summary>
        /// Reads newly recorded microphone samples and appends them to the active OpenAI Realtime
        /// input-audio stream while recognition is running.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called from <see cref="SystemUpdate(float)"/> so microphone capture can
        /// be advanced as part of the normal <see cref="RideSystemMonoBehaviour"/> frame lifecycle.
        /// </para>
        /// <para>
        /// The method compares the current microphone write position against the last transmitted
        /// offset, extracts only the newly available PCM data, and forwards that chunk to
        /// <see cref="SendAudioChunk(float[])"/> when enough samples have accumulated to justify
        /// another websocket append.
        /// </para>
        /// </remarks>
        private void PumpMicrophoneAudio()
        {
            if (!isSystemActive || !IsRecognizing || recordingClip == null)
                return;

#if !UNITY_WEBGL
            int currentPosition = Microphone.GetPosition(microphoneDevice);
            if (currentPosition >= 0)
            {
                int samplesToSend = currentPosition - lastSamplePosition;
                if (samplesToSend < 0)
                    samplesToSend += recordingClip.samples;

                if (samplesToSend >= chunkSize)
                {
                    SendAudioChunk(samplesToSend);
                    lastSamplePosition = currentPosition;
                }
            }
#endif
        }

        /// <summary>
        /// Verifies and requests microphone permission on mobile platforms.
        /// TODO: move to central location; copied from SpeechRecognitionSystemAzure
        /// </summary>
        protected void CheckMicrophonePermissions()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#else
            // Desktop: ensure OS privacy allows mic; no runtime prompt here.
#endif
        }

        /// <inheritdoc/>
        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);
            microphoneDevice = SelectedMicrophone;
        }

        /// <summary>
        /// Start the speech recognition system
        /// </summary>
        public IEnumerator StartSystem()
        {
            if (isSystemActive)
            {
                Debug.LogWarning("System is already active");
                yield break;
            }

            isSystemActive = true;
            // Wait for connection coroutine to complete
            yield return StartCoroutine(ConnectToRealtimeAPI());

            if (!isSystemActive || !isConnected)
            {
                Debug.LogWarning("[SpeechRecognitionSystemOpenAI] StartSystem aborted before connection was established.");
                yield break;
            }

            OnSystemStarted?.Invoke();
            Debug.Log("[SpeechRecognitionSystemOpenAI] Speech Recognition System Started");
        }

        /// <summary>
        /// Stop the speech recognition system (disconnect and stop recording)
        /// </summary>
        public IEnumerator StopSystem()
        {
            if (!isSystemActive)
            {
                Debug.LogWarning("System is not active");
                yield break;
            }

            isSystemActive = false;
            StopRecording();

            // Stop streaming audio
            if (isStreamingAudio)
            {
                StopStreamingPlayback();
            }

            // Stop any audio playback
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Clear audio data
            lock (audioQueueLock)
            {
                streamingAudioQueue.Clear();
            }
            audioStreamComplete = false;
            isPlayingResponse = false;

            if (websocket?.State == WebSocketState.Open)
            {
                // Close with coroutine-style waiting
                Task closeTask = null;
                try
                {
                    closeTask = websocket.Close();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception initiating websocket close: {e.Message}");
                }

                float timeout = 10f;
                float startTime = Time.time;

                // Wait until the task completes or state changes, with timeout
                while ((closeTask != null && !closeTask.IsCompleted) && (Time.time - startTime) < timeout)
                {
                    yield return null;
                }

                if (closeTask != null && closeTask.IsFaulted)
                {
                    Debug.LogError($"Error closing websocket: {closeTask.Exception?.Flatten().Message}");
                }
            }

            isConnected = false;
            OnSystemStopped?.Invoke();
            Debug.Log("Speech Recognition System Stopped");
        }

        private void InitializeAgentResponseAudio()
        {
            // Get or create AudioSource for playing responses
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for optimal playback
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;  // 2D sound
            audioSource.priority = 0;       // Highest priority
            audioSource.volume = 1.0f;
            audioSource.pitch = 1.0f;
            audioSource.dopplerLevel = 0f;
            audioSource.enabled = true;

            Debug.Log($"[SpeechRecognitionSystemOpenAI] AudioSource configured: enabled={audioSource.enabled}, volume={audioSource.volume}");
        }
        private IEnumerator ConnectToRealtimeAPI()
        {
            if (isConnected)
            {
                Debug.LogWarning("Already connected to Realtime API");
                yield break;
            }

#if !UNITY_WEBGL
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            string endpoint = configSystem != null ? configSystem.config.openAIRealtime.endpoint : string.Empty;
            string apiKey = configSystem != null ? configSystem.config.openAIRealtime.endpointKey : string.Empty;

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("Please set the OpenAI Realtime endpoint and API key in the RIDE configuration file.");
                OnError?.Invoke("OpenAI Realtime endpoint or API key is missing.");
                isSystemActive = false;
                yield break;
            }

            string url = $"{endpoint}?model={OpenAIRealtimeModels.Id(model)}";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiKey}" }
            };

            websocket = new WebSocket(url, headers);

            websocket.OnOpen += OnWebSocketOpen;
            websocket.OnMessage += OnWebSocketMessage;
            websocket.OnError += OnWebSocketError;
            websocket.OnClose += OnWebSocketClose;

            Task connectTask = null;
            Exception connectException = null;

            try
            {
                connectTask = websocket.Connect();
            }
            catch (Exception e)
            {
                connectException = e;
            }

            if (connectException != null)
            {
                Debug.LogError($"Failed to start websocket connect: {connectException.Message}");
                OnError?.Invoke(connectException.Message);
                isSystemActive = false;
                yield break;
            }

            // Poll connection status outside of try/catch
            float timeout = 10f;
            float startTime = Time.time;

            while (websocket != null && websocket.State != WebSocketState.Open && (connectTask != null && !connectTask.IsCompleted))
            {
                if (Time.time - startTime > timeout)
                {
                    Debug.LogError("Timed out while connecting to Realtime API.");
                    OnError?.Invoke("Timed out while connecting to Realtime API.");
                    isSystemActive = false;
                    yield break;
                }
                yield return null;
            }

            // Wait briefly after task completes for connection to finalize
            float postWaitStart = Time.time;
            while (websocket != null && websocket.State != WebSocketState.Open && (Time.time - postWaitStart) < 2f)
            {
                yield return null;
            }

            if (websocket == null)
            {
                isSystemActive = false;
                yield break;
            }

            if (websocket.State == WebSocketState.Open)
            {
                Debug.Log("[SpeechRecognitionSystemOpenAI] Connected successfully to Realtime API (coroutine version).");
            }
            else
            {
                Debug.LogError("WebSocket connection did not open properly.");
                OnError?.Invoke("WebSocket connection failed.");
                isSystemActive = false;
            }

#endif
        }

        public void SetTranscriptOnlyMode(bool transcriptOnly)
        {
            this.transcriptOnly = transcriptOnly;

            // If already connected, update the session
            if (isConnected)
            {
                SendSessionUpdate();
                Debug.Log($"Mode changed to: {(transcriptOnly ? "Transcript Only" : "Transcript + Audio")}");
            }
        }

        private void OnWebSocketOpen()
        {
            Debug.Log("[SpeechRecognitionSystemOpenAI] Connected to OpenAI Realtime API");
            isConnected = true;

            // Send session configuration
            SendSessionUpdate();

            OnConnectionEstablished?.Invoke();

            if (IsRecognizing && !isRecording)
                StartRecording();
        }

        private void SendSessionUpdate()
        {
            SendEvent(transcriptOnly ? BuildTranscriptionSessionUpdate() : BuildRealtimeSessionUpdate());
        }

        private object BuildTranscriptionSessionUpdate()
        {
            return new
            {
                type = "session.update",
                session = new
                {
                    type = "realtime",
                    instructions = "You are a helpful assistant. Transcribe speech accurately and do not produce spoken responses.",
                    output_modalities = new[] { "text" },
                    audio = new
                    {
                        input = new
                        {
                            format = new
                            {
                                type = "audio/pcm",
                                rate = sampleRate
                            },
                            transcription = new
                            {
                                model = OpenAIRealtimeModels.Id(transcriptionModel)
                            },
                            turn_detection = new
                            {
                                type = "server_vad",
                                threshold = vadThreshold,
                                prefix_padding_ms = vadPrefixPaddingMs,
                                silence_duration_ms = VadSilenceDurationMs
                            }
                        }
                    }
                }
            };
        }

        private object BuildRealtimeSessionUpdate()
        {
            return new
            {
                type = "session.update",
                session = new
                {
                    type = "realtime",
                    instructions = "You are a helpful assistant. Transcribe speech and respond naturally.",
                    output_modalities = new[] { "audio" },
                    audio = new
                    {
                        input = new
                        {
                            format = new
                            {
                                type = "audio/pcm",
                                rate = sampleRate
                            },
                            transcription = new
                            {
                                model = OpenAIRealtimeModels.Id(transcriptionModel)
                            },
                            turn_detection = new
                            {
                                type = "server_vad",
                                threshold = vadThreshold,
                                prefix_padding_ms = vadPrefixPaddingMs,
                                silence_duration_ms = VadSilenceDurationMs
                            }
                        },
                        output = new
                        {
                            format = new
                            {
                                type = "audio/pcm",
                                rate = sampleRate
                            },
                            voice
                        }
                    }
                }
            };
        }

        private void OnWebSocketMessage(byte[] data)
        {
            try
            {
                string jsonMessage = Encoding.UTF8.GetString(data);
                JObject message = JObject.Parse(jsonMessage);
                string eventType = message["type"]?.ToString();

                switch (eventType)
                {
                    case "session.created":
                    case "session.updated":
                        Debug.Log($"[SpeechRecognitionSystemOpenAI] Session {eventType}: {message["session"]?["id"]}");
                        break;

                    case "input_audio_buffer.speech_started":
                        Debug.Log("Speech started");
                        OnUserSpeechStarted?.Invoke();

                        // Clear transcription buffer for new speech
                        currentTranscriptionBuffer = "";

                        // If user speech is detected while agent audio is playing, stop the agent audio
                        if (isPlayingResponse && audioSource != null && audioSource.isPlaying)
                        {
                            Debug.LogWarning("User started speaking while agent was talking - stopping agent audio");
                            audioSource.Stop();
                        }
                        break;

                    case "input_audio_buffer.speech_stopped":
                        Debug.Log("Speech stopped");
                        OnUserSpeechEnded?.Invoke();

                        // Auto-stop recording when user stops speaking
                        if (isSystemActive && IsRecognizing && !transcriptOnly)
                        {
                            StopRecording();
                        }
                        break;

                    case "conversation.item.input_audio_transcription.delta":
                        string transcriptStreamDelta = message["delta"]?.ToString();
                        if (!string.IsNullOrEmpty(transcriptStreamDelta))
                        {
                            // Accumulate the delta into the buffer
                            currentTranscriptionBuffer += transcriptStreamDelta;

                            // Fire delta event with the chunk
                            OnTranscriptionDeltaReceived?.Invoke(transcriptStreamDelta);                            

                            // Also fire the main transcription event with accumulated text so far
                            OnTranscriptionReceived?.Invoke(currentTranscriptionBuffer);

                            m_recognizedSpeechPartial = transcriptStreamDelta;
                            m_recognizedLanguagePartial = message["language"]?.ToString() ?? m_recognizedLanguagePartial;

                            Debug.Log($"Transcription delta: {transcriptStreamDelta} (Total so far: {currentTranscriptionBuffer})");
                        }
                        break;

                    case "conversation.item.input_audio_transcription.completed":
                        string completedTranscript = message["transcript"]?.ToString();
                        if (!string.IsNullOrEmpty(completedTranscript))
                        {
                            string detectedLanguage = message["language"]?.ToString() ?? string.Empty;
                            Debug.Log($"Transcription completed: {completedTranscript}");

                            // Update buffer with final version (in case deltas were incomplete)
                            currentTranscriptionBuffer = completedTranscript;

                            // Fire final transcription
                            OnTranscriptionReceived?.Invoke(completedTranscript);

                            m_recognizedSpeech = completedTranscript;
                            m_recognizedLanguage = detectedLanguage;
                        }
                        break;

                    case "response.audio.delta":
                        // Only process audio if not in transcript-only mode
                        if (!transcriptOnly)
                        {
                            string audioStreamDelta = message["delta"]?.ToString();
                            if (!string.IsNullOrEmpty(audioStreamDelta))
                            {
                                // Mark that we're receiving audio response
                                if (!isPlayingResponse)
                                {
                                    isPlayingResponse = true;
                                    audioStreamComplete = false;
                                    OnAudioResponseStarted?.Invoke();
                                    Debug.Log("Agent started responding with audio");
                                }
                                ProcessAudioResponseStreaming(audioStreamDelta);
                            }
                        }
                        break;

                    case "response.audio.done":
                        // Audio response finished streaming
                        Debug.Log($"Agent audio stream complete. Queued samples: {streamingAudioQueue.Count}");
                        if (!transcriptOnly && isPlayingResponse)
                        {
                            audioStreamComplete = true;
                        }
                        break;

                    case "response.audio_transcript.delta":
                        string aiTextStreamDelta = message["delta"]?.ToString();
                        if (!string.IsNullOrEmpty(aiTextStreamDelta))
                        {
                            Debug.Log($"Agent Response: {aiTextStreamDelta}");
                        }
                        break;

                    case "error":
                        string apiErrorMessage = message["error"]?["message"]?.ToString();
                        Debug.LogError($"API Error: {apiErrorMessage}");
                        OnError?.Invoke(apiErrorMessage);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing message: {e.Message}");
            }
        }

        private void OnWebSocketError(string error)
        {
            Debug.LogError($"WebSocket Error: {error}");
            OnError?.Invoke(error);
        }

        private void OnWebSocketClose(WebSocketCloseCode code)
        {
            Debug.Log($"WebSocket Closed: {code}");
            isConnected = false;
        }

        private void StartRecording()
        {
            if (isRecording)
                return;

            if (!isConnected)
            {
                Debug.LogError("Not connected to Realtime API");
                return;
            }

            if (!isSystemActive)
            {
                Debug.LogWarning("System is not active");
                return;
            }

            if (isPlayingResponse)
            {
                Debug.LogWarning("Cannot record while agent is speaking");
                return;
            }

#if !UNITY_WEBGL
            recordingClip = Microphone.Start(microphoneDevice, true, 10, sampleRate);
#endif

            isRecording = true;
            lastSamplePosition = 0;
            inputAudioBufferSamples = 0;

            Debug.Log("Recording started");
        }

        private void StopRecording()
        {
            if (!isRecording) return;

            FlushPendingAudioChunk();

#if !UNITY_WEBGL
            Microphone.End(microphoneDevice);
#endif

            recordingClip = null;
            isRecording = false;

            inputAudioBufferSamples = 0;

            Debug.Log("Recording stopped");
        }

        private void FlushPendingAudioChunk()
        {
#if !UNITY_WEBGL
            if (recordingClip == null)
                return;

            int currentPosition = Microphone.GetPosition(microphoneDevice);
            if (currentPosition < 0)
                return;

            int samplesToSend = currentPosition - lastSamplePosition;
            if (samplesToSend < 0)
                samplesToSend += recordingClip.samples;

            int minCommitSamples = Mathf.CeilToInt(MinCommitAudioSeconds * sampleRate);
            if (samplesToSend >= minCommitSamples)
            {
                SendAudioChunk(samplesToSend);
                lastSamplePosition = currentPosition;
            }
#endif
        }

        private void SendAudioChunk(int sampleCount)
        {
            if (recordingClip == null || sampleCount <= 0)
                return;

            float[] samples = new float[sampleCount * recordingClip.channels];
            recordingClip.GetData(samples, lastSamplePosition);

            // Convert to PCM16
            byte[] pcm16Data = ConvertToPCM16(samples);

            // Encode to base64
            string base64Audio = Convert.ToBase64String(pcm16Data);

            // Send audio append event
            var audioEvent = new
            {
                type = "input_audio_buffer.append",
                audio = base64Audio
            };

            SendEvent(audioEvent);
            inputAudioBufferSamples += sampleCount;
        }

        //private void CommitOrClearInputAudioBuffer()
        //{
        //    int minCommitSamples = Mathf.CeilToInt(MinCommitAudioSeconds * sampleRate);

        //    if (inputAudioBufferSamples >= minCommitSamples)
        //    {
        //        SendInputAudioBufferCommit();
        //    }
        //    else
        //    {
        //        if (inputAudioBufferSamples > 0)
        //            SendInputAudioBufferClear();

        //        float bufferMs = inputAudioBufferSamples * 1000f / sampleRate;
        //        Debug.Log($"Skipping OpenAI audio commit because the input buffer is too small ({bufferMs:F2}ms).");
        //    }

        //    inputAudioBufferSamples = 0;
        //}

        //private void SendInputAudioBufferCommit()
        //{
        //    var commitEvent = new
        //    {
        //        type = "input_audio_buffer.commit"
        //    };

        //    SendEvent(commitEvent);
        //}

        //private void SendInputAudioBufferClear()
        //{
        //    var clearEvent = new
        //    {
        //        type = "input_audio_buffer.clear"
        //    };

        //    SendEvent(clearEvent);
        //}

        private byte[] ConvertToPCM16(float[] samples)
        {
            byte[] pcm16 = new byte[samples.Length * 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * rescaleFactor);
                byte[] bytes = BitConverter.GetBytes(value);
                pcm16[i * 2] = bytes[0];
                pcm16[i * 2 + 1] = bytes[1];
            }

            return pcm16;
        }

        private void ProcessAudioResponseStreaming(string base64Audio)
        {
            try
            {
                byte[] audioData = Convert.FromBase64String(base64Audio);

                // Convert PCM16 bytes to float samples and add to queue
                lock (audioQueueLock)
                {
                    for (int i = 0; i < audioData.Length; i += 2)
                    {
                        if (i + 1 < audioData.Length)
                        {
                            short value = BitConverter.ToInt16(audioData, i);
                            float sample = value / 32768f;
                            streamingAudioQueue.Enqueue(sample);
                        }
                    }
                }

                // Check if we should start streaming playback
                if (!isStreamingAudio)
                {
                    int requiredSamples = (int)(streamingStartBufferSeconds * sampleRate);
                    if (streamingAudioQueue.Count >= requiredSamples)
                    {
                        Debug.Log($"Starting streaming playback with {streamingAudioQueue.Count} samples buffered ({streamingAudioQueue.Count / (float)sampleRate:F2}s)");
                        StartStreamingPlayback();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing streaming audio: {e.Message}");
            }
        }

        /// <summary>
        /// Starts playback of the assistant audio stream after enough PCM samples have been buffered.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called from <see cref="ProcessAudioResponseStreaming(string)"/> once the queued
        /// assistant audio reaches the configured <see cref="streamingStartBufferSeconds"/> threshold.
        /// It creates a streaming <see cref="AudioClip"/> backed by <see cref="OnAudioRead(float[])"/>,
        /// resets playback counters, and begins draining the queued samples through Unity's audio pipeline.
        /// </para>
        /// <para>
        /// The resulting clip uses Unity's streaming clip callback path described by
        /// <see href="https://docs.unity3d.com/ScriptReference/AudioClip.Create.html">AudioClip.Create</see>.
        /// Completion is not determined here; that responsibility is delegated to
        /// <see cref="UpdateStreamingPlaybackMonitor(float)"/>.
        /// </para>
        /// </remarks>
        private void StartStreamingPlayback()
        {
            isStreamingAudio = true;
            isAudioCallbackReady = false;
            streamingPlaybackPosition = 0;

            // Create a streaming AudioClip with proper sample rate
            if (audioSource != null)
            {
                audioSource.Stop();

                // Create large clip upfront to handle any response length
                currentClipSamples = sampleRate * maxStreamingClipSeconds;
                AudioClip streamingClip = AudioClip.Create("StreamingResponse", currentClipSamples, 1, sampleRate, true, OnAudioRead);

                audioSource.clip = streamingClip;
                audioSource.loop = false;

                Debug.Log($"Streaming audio clip created with {sampleRate}Hz, {maxStreamingClipSeconds}s capacity ({currentClipSamples} samples). Queue size: {streamingAudioQueue.Count}");

                // Set callback ready and start playing immediately
                isAudioCallbackReady = true;
                audioSource.Play();
                m_streamingPlaybackMonitorTimer = 0f;
                m_streamingPlaybackEmptyFrames = 0;

                Debug.Log("Streaming audio playback started");
                Debug.Log($"Monitoring playback. Required empty frames: {Mathf.CeilToInt(streamingEndBufferSeconds / StreamingPlaybackMonitorIntervalSeconds)}");
            }
        }

        // This is called by Unity's audio system to fill the audio buffer for streaming AudioClip
        void OnAudioRead(float[] data)
        {
            // Wait until we're ready to provide audio
            if (!isAudioCallbackReady || !isStreamingAudio)
            {
                // Fill with silence if not ready
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0f;
                }
                return;
            }

            lock (audioQueueLock)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (streamingAudioQueue.Count > 0)
                    {
                        data[i] = streamingAudioQueue.Dequeue();
                        streamingPlaybackPosition++;
                    }
                    else if (audioStreamComplete)
                    {
                        // Stream is complete and queue is empty
                        data[i] = 0f;
                    }
                    else
                    {
                        // Buffering - output silence but keep playing
                        data[i] = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Advances the assistant-audio playback completion monitor during <see cref="SystemUpdate(float)"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called once per frame from <see cref="SystemUpdate(float)"/>. It converts the
        /// frame delta time into fixed monitor ticks at <see cref="StreamingPlaybackMonitorIntervalSeconds"/>
        /// and invokes <see cref="EvaluateStreamingPlaybackMonitor()"/> for each elapsed tick while
        /// assistant audio is actively streaming.
        /// </para>
        /// <para>
        /// Its purpose is to keep playback-completion logic on the shared RIDE update path rather than
        /// tying that lifetime behavior to a standalone polling loop.
        /// </para>
        /// </remarks>
        private void UpdateStreamingPlaybackMonitor(float dt)
        {
            if (!isStreamingAudio)
            {
                m_streamingPlaybackMonitorTimer = 0f;
                m_streamingPlaybackEmptyFrames = 0;
                return;
            }

            m_streamingPlaybackMonitorTimer += dt;
            while (m_streamingPlaybackMonitorTimer >= StreamingPlaybackMonitorIntervalSeconds)
            {
                m_streamingPlaybackMonitorTimer -= StreamingPlaybackMonitorIntervalSeconds;
                EvaluateStreamingPlaybackMonitor();

                if (!isStreamingAudio)
                    break;
            }
        }

        /// <summary>
        /// Evaluates whether the active assistant audio stream has fully drained and can be stopped.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by <see cref="UpdateStreamingPlaybackMonitor(float)"/> at a fixed polling
        /// interval. It examines the queued audio sample count together with <see cref="audioStreamComplete"/>
        /// to determine whether the stream has both finished arriving from OpenAI and finished draining
        /// through Unity playback.
        /// </para>
        /// <para>
        /// Once the queue has remained empty for the configured end-buffer window, it calls
        /// <see cref="StopStreamingPlayback()"/> to finalize playback and transition into the post-playback
        /// state.
        /// </para>
        /// </remarks>
        private void EvaluateStreamingPlaybackMonitor()
        {
            int currentQueueSize;
            lock (audioQueueLock)
            {
                currentQueueSize = streamingAudioQueue.Count;
            }

            int requiredEmptyFrames = Mathf.CeilToInt(streamingEndBufferSeconds / StreamingPlaybackMonitorIntervalSeconds);

            if (audioStreamComplete && currentQueueSize == 0)
            {
                m_streamingPlaybackEmptyFrames++;

                if (m_streamingPlaybackEmptyFrames % 10 == 0)
                {
                    float playbackTimeSeconds = streamingPlaybackPosition / (float)sampleRate;
                    Debug.Log($"Stream complete and queue empty. Empty frames: {m_streamingPlaybackEmptyFrames}/{requiredEmptyFrames}, Samples played: {streamingPlaybackPosition} ({playbackTimeSeconds:F1}s)");
                }

                if (m_streamingPlaybackEmptyFrames > requiredEmptyFrames)
                {
                    float playbackTimeSeconds = streamingPlaybackPosition / (float)sampleRate;
                    Debug.Log($"Streaming playback complete. Total samples played: {streamingPlaybackPosition} ({playbackTimeSeconds:F1}s)");
                    StopStreamingPlayback();
                }
            }
            else
            {
                if (m_streamingPlaybackEmptyFrames > 0)
                {
                    Debug.Log($"Conditions changed. Queue: {currentQueueSize}, StreamComplete: {audioStreamComplete}. Resetting counter.");
                }

                m_streamingPlaybackEmptyFrames = 0;
            }
        }

        private void StopStreamingPlayback()
        {
            isStreamingAudio = false;
            isAudioCallbackReady = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            lock (audioQueueLock)
            {
                streamingAudioQueue.Clear();
            }

            streamingPlaybackPosition = 0;
            m_streamingPlaybackMonitorTimer = 0f;
            m_streamingPlaybackEmptyFrames = 0;

            OnAudioFinishedPlaying();
        }

        private void SendEvent(object eventData)
        {
            if (websocket?.State != WebSocketState.Open) return;

            string json = JsonConvert.SerializeObject(eventData);
            websocket.SendText(json);
        }

        /// <summary>
        /// Finalizes assistant-audio playback state and schedules microphone restart when appropriate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called from <see cref="StopStreamingPlayback()"/> after queued assistant audio has
        /// been fully drained. It clears the assistant-speaking state, raises
        /// <see cref="OnAudioResponseFinished"/>, and, when the realtime system is still active, schedules
        /// recognition to resume through <see cref="QueueRecordingRestart()"/>.
        /// </para>
        /// <para>
        /// This is the handoff point between assistant-output playback and the next user-input capture
        /// cycle in transcript-plus-audio mode.
        /// </para>
        /// </remarks>
        private void OnAudioFinishedPlaying()
        {
            Debug.Log($"OnAudioFinishedPlaying called. isPlayingResponse: {isPlayingResponse}, isSystemActive: {isSystemActive}");

            isPlayingResponse = false;
            audioStreamComplete = false;
            isStreamingAudio = false;

            OnAudioResponseFinished?.Invoke();

            Debug.Log("Audio response finished playing - resetting state");

            // Auto-restart recording after agent finishes speaking
            if (isSystemActive)
            {
                Debug.Log($"Queueing recording restart in {postPlaybackDelay}s");
                QueueRecordingRestart();
            }
            else
            {
                Debug.Log("System not active, skipping recording restart");
            }
        }

        /// <summary>
        /// Schedules microphone recording to restart after assistant playback has finished.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called from <see cref="OnAudioFinishedPlaying()"/> when the system should return
        /// from assistant output back to user capture. It does not restart recording immediately; instead,
        /// it records the target resume time derived from <see cref="postPlaybackDelay"/> so the actual
        /// transition can be evaluated safely from <see cref="UpdatePendingRecordingRestart()"/>.
        /// </para>
        /// </remarks>
        private void QueueRecordingRestart()
        {
            m_restartRecordingPending = true;
            m_restartRecordingAwaitingPlaybackStop = false;
            m_restartRecordingReadyTime = Time.time + postPlaybackDelay;
        }

        /// <summary>
        /// Processes any pending microphone-restart request during <see cref="SystemUpdate(float)"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called once per frame from <see cref="SystemUpdate(float)"/> after
        /// <see cref="QueueRecordingRestart()"/> has scheduled a resume time. It waits for the configured
        /// post-playback delay, optionally waits for the <see cref="AudioSource"/> to stop fully, applies a
        /// short settle window, and then restarts capture through <see cref="StartRecording()"/> if the
        /// realtime session is still active and ready to listen.
        /// </para>
        /// <para>
        /// Its purpose is to coordinate the transition back into user speech capture without cutting off
        /// trailing playback or resuming too early.
        /// </para>
        /// </remarks>
        private void UpdatePendingRecordingRestart()
        {
            if (!m_restartRecordingPending || Time.time < m_restartRecordingReadyTime)
                return;

            if (audioSource != null && audioSource.isPlaying)
            {
                if (!m_restartRecordingAwaitingPlaybackStop)
                {
                    Debug.LogWarning("AudioSource is still playing during restart delay - waiting for it to finish");
                }

                m_restartRecordingAwaitingPlaybackStop = true;
                return;
            }

            if (m_restartRecordingAwaitingPlaybackStop)
            {
                m_restartRecordingAwaitingPlaybackStop = false;
                m_restartRecordingReadyTime = Time.time + RestartRecordingPlaybackSettleSeconds;
                return;
            }

            m_restartRecordingPending = false;
            if (!isRecording && !isPlayingResponse && isSystemActive && IsRecognizing)
            {
                Debug.Log("Restarting recording after agent response");
                StartRecording();
            }
            else
            {
                Debug.Log($"Skipping restart: IsRecognizing={IsRecognizing}, isPlayingResponse={isPlayingResponse}, isSystemActive={isSystemActive}");
            }
        }

        void OnApplicationQuit()
        {
            Cleanup();
        }

        void OnDestroy()
        {
            Cleanup();
        }

        void Cleanup()
        {
            StopRecording();

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            lock (audioQueueLock)
            {
                streamingAudioQueue.Clear();
            }

            audioStreamComplete = false;
            isPlayingResponse = false;
            isStreamingAudio = false;
            isAudioCallbackReady = false;
            m_streamingPlaybackMonitorTimer = 0f;
            m_streamingPlaybackEmptyFrames = 0;
            m_restartRecordingPending = false;
            m_restartRecordingAwaitingPlaybackStop = false;
            m_restartRecordingReadyTime = 0f;
            isSystemActive = false;
            IsRecognizing = false;
            m_recognizedSpeech = null;
            m_recognizedSpeechPartial = null;
            currentTranscriptionBuffer = "";

#if !UNITY_WEBGL
            if (websocket != null)
            {
                websocket.OnOpen -= OnWebSocketOpen;
                websocket.OnMessage -= OnWebSocketMessage;
                websocket.OnError -= OnWebSocketError;
                websocket.OnClose -= OnWebSocketClose;

                try { _ = websocket.Close(); } catch (Exception) { }
                websocket = null;
            }
#endif

            isConnected = false;
        }
    }
}
