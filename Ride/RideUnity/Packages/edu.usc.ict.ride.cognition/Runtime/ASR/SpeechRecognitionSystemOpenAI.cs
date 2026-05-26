using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// Class to support OpenAI speech recognition. Combination of Whisper and GPT models.
    /// While mainly used for speech recognition, it contains an optional ChatGPT text and audio response. 
    /// All of these are streaming using web sockets. 
    /// </summary>
    public class SpeechRecognitionSystemOpenAI : SpeechRecognitionSystemUnity
    {
        public override bool IsSupported => true;
        public override bool SupportsContinuousRecognition => true;

#if PLATFORM_ANDROID || PLATFORM_IOS
        // Required to manifest microphone permission, cf.
        // https://docs.unity3d.com/Manual/android-manifest.html
#endif        

#if !UNITY_WEBGL
        string m_apiKey;
        string m_endpoint;
#endif

        [Header("API Settings")]
#pragma warning disable 0414  // (WebGL) The field '' is assigned but its value is never used
        [SerializeField] private string model = "gpt-realtime-mini";
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
        [SerializeField] private int vadSilenceDurationMs = 700; // Silence duration before considering speech stopped (ms)

        private const float MinCommitAudioSeconds = 0.1f;
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
        private Coroutine checkRecognizedSpeechCoroutine;
        private Coroutine sendWebsocketMessagesCoroutine;
        private Coroutine monitorStreamingPlaybackCoroutine;
        private Coroutine restartRecordingCoroutine;

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

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

#if !UNITY_WEBGL
            ConfigurationSystemUnity configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_apiKey = configSystem.config.openAIRealtime.endpointKey;
            m_endpoint = configSystem.config.openAIRealtime.endpoint;

            if (string.IsNullOrEmpty(m_apiKey))
            {
                Debug.LogError("Please set your OpenAI API key in the RIDE configuration file.");
                return;
            }
#endif

            CheckMicrophonePermissions();

#if !UNITY_WEBGL
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected.");
                return;
            }

            Debug.Log($"Available microphones: {string.Join(", ", Microphone.devices)}");
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
            if (checkRecognizedSpeechCoroutine == null)
                checkRecognizedSpeechCoroutine = StartCoroutine(CheckRecognizedSpeechRoutine());

            StartRecording();

            base.OnRecognizingStarted();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            StopRecording();

            if (checkRecognizedSpeechCoroutine != null)
            {
                StopCoroutine(checkRecognizedSpeechCoroutine);
                checkRecognizedSpeechCoroutine = null;
            }

            base.OnRecognizingStopped();
        }

        protected override void Update()
        {
            // Dispatch WebSocket messages on Unity main thread
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue();
#endif
            base.Update();
        }

        /// <summary>
        /// Coroutine that polls partial and final recognition results each frame.
        /// </summary>
        protected IEnumerator CheckRecognizedSpeechRoutine()
        {            
            while (IsRecognizing)
            {
                if (!string.IsNullOrEmpty(m_recognizedSpeechPartial))
                {
                    OnPartialSpeechRecognized(m_recognizedSpeechPartial, Confidence);
                    m_recognizedSpeechPartial = null;
                }

                if (!string.IsNullOrEmpty(m_recognizedSpeech))
                {
                    OnSpeechRecognized(m_recognizedSpeech, Confidence);
                    m_recognizedSpeech = null;
                }

                yield return null;
            }

            checkRecognizedSpeechCoroutine = null;
        }

        /// <summary>
        /// Gathers microphone audio and prepares for sending.
        /// </summary>
        protected IEnumerator SendWebsocketMessages()
        {            
            while (isSystemActive)
            {
                if (!IsRecognizing || recordingClip == null)
                {
                    yield return null;
                    continue;
                }

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

                yield return null;
            }

            Debug.Log("SendWebsocketMessages coroutine stopped");
            sendWebsocketMessagesCoroutine = null;
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
            OnSystemStarted?.Invoke();
            Debug.Log("Speech Recognition System Started");
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

            Debug.Log($"AudioSource configured: enabled={audioSource.enabled}, volume={audioSource.volume}");
        }
        private IEnumerator ConnectToRealtimeAPI()
        {
            if (isConnected)
            {
                Debug.LogWarning("Already connected to Realtime API");
                yield break;
            }

#if !UNITY_WEBGL
            string url = $"{m_endpoint}?model={model}";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {m_apiKey}" }
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

            while (websocket.State != WebSocketState.Open && (connectTask != null && !connectTask.IsCompleted))
            {
                if (Time.time - startTime > timeout)
                {
                    Debug.LogError("Timed out while connecting to Realtime API.");
                    OnError?.Invoke("Timed out while connecting to Realtime API.");
                    yield break;
                }
                yield return null;
            }

            // Wait briefly after task completes for connection to finalize
            float postWaitStart = Time.time;
            while (websocket.State != WebSocketState.Open && (Time.time - postWaitStart) < 2f)
            {
                yield return null;
            }

            if (websocket.State == WebSocketState.Open)
            {
                Debug.Log("Connected successfully to Realtime API (coroutine version).");
            }
            else
            {
                Debug.LogError("WebSocket connection did not open properly.");
                OnError?.Invoke("WebSocket connection failed.");
            }

            if (sendWebsocketMessagesCoroutine == null)
                sendWebsocketMessagesCoroutine = StartCoroutine(SendWebsocketMessages());
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
            Debug.Log("Connected to OpenAI Realtime API");
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
                                model = "whisper-1"
                            },
                            turn_detection = new
                            {
                                type = "server_vad",
                                threshold = vadThreshold,
                                prefix_padding_ms = vadPrefixPaddingMs,
                                silence_duration_ms = vadSilenceDurationMs
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
                                model = "whisper-1"
                            },
                            turn_detection = new
                            {
                                type = "server_vad",
                                threshold = vadThreshold,
                                prefix_padding_ms = vadPrefixPaddingMs,
                                silence_duration_ms = vadSilenceDurationMs
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
                        Debug.Log($"Session {eventType}: {message["session"]?["id"]}");
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

                            Debug.Log($"Transcription delta: {transcriptStreamDelta} (Total so far: {currentTranscriptionBuffer})");
                        }
                        break;

                    case "conversation.item.input_audio_transcription.completed":
                        string completedTranscript = message["transcript"]?.ToString();
                        if (!string.IsNullOrEmpty(completedTranscript))
                        {
                            Debug.Log($"Transcription completed: {completedTranscript}");

                            // Update buffer with final version (in case deltas were incomplete)
                            currentTranscriptionBuffer = completedTranscript;

                            // Fire final transcription
                            OnTranscriptionReceived?.Invoke(completedTranscript);

                            m_recognizedSpeech = completedTranscript;
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

                Debug.Log("Streaming audio playback started");

                // Monitor streaming playback
                if (monitorStreamingPlaybackCoroutine != null)
                    StopCoroutine(monitorStreamingPlaybackCoroutine);

                monitorStreamingPlaybackCoroutine = StartCoroutine(MonitorStreamingPlayback());
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

        private IEnumerator MonitorStreamingPlayback()
        {
            int emptyFrames = 0;
            int requiredEmptyFrames = Mathf.CeilToInt(streamingEndBufferSeconds / 0.1f);

            Debug.Log($"Monitoring playback. Required empty frames: {requiredEmptyFrames}");

            while (isStreamingAudio)
            {
                yield return new WaitForSeconds(0.1f);

                int currentQueueSize;
                lock (audioQueueLock)
                {
                    currentQueueSize = streamingAudioQueue.Count;
                }

                // Check if stream is complete and queue is empty
                if (audioStreamComplete && currentQueueSize == 0)
                {
                    emptyFrames++;

                    if (emptyFrames % 10 == 0) // Log every second
                    {
                        float playbackTimeSeconds = streamingPlaybackPosition / (float)sampleRate;
                        Debug.Log($"Stream complete and queue empty. Empty frames: {emptyFrames}/{requiredEmptyFrames}, Samples played: {streamingPlaybackPosition} ({playbackTimeSeconds:F1}s)");
                    }

                    // Wait for the configured duration to make sure we're really done
                    if (emptyFrames > requiredEmptyFrames)
                    {
                        float playbackTimeSeconds = streamingPlaybackPosition / (float)sampleRate;
                        Debug.Log($"Streaming playback complete. Total samples played: {streamingPlaybackPosition} ({playbackTimeSeconds:F1}s)");
                        StopStreamingPlayback();
                        break;
                    }
                }
                else
                {
                    if (emptyFrames > 0)
                    {
                        Debug.Log($"Conditions changed. Queue: {currentQueueSize}, StreamComplete: {audioStreamComplete}. Resetting counter.");
                    }
                    emptyFrames = 0;
                }
            }

            monitorStreamingPlaybackCoroutine = null;
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
            monitorStreamingPlaybackCoroutine = null;

            OnAudioFinishedPlaying();
        }

        private void SendEvent(object eventData)
        {
            if (websocket?.State != WebSocketState.Open) return;

            string json = JsonConvert.SerializeObject(eventData);
            websocket.SendText(json);
        }

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
                Debug.Log("Starting restart recording coroutine");
                if (restartRecordingCoroutine != null)
                    StopCoroutine(restartRecordingCoroutine);

                restartRecordingCoroutine = StartCoroutine(RestartRecordingAfterDelay());
            }
            else
            {
                Debug.Log("System not active, skipping recording restart");
            }
        }

        private IEnumerator RestartRecordingAfterDelay()
        {
            Debug.Log($"Waiting {postPlaybackDelay}s before restarting recording...");
            yield return new WaitForSeconds(postPlaybackDelay);

            // Double-check audio is really finished
            if (audioSource != null && audioSource.isPlaying)
            {
                Debug.LogWarning("AudioSource is still playing during restart delay - waiting for it to finish");
                while (audioSource != null && audioSource.isPlaying)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(0.2f);
            }

            if (!isRecording && !isPlayingResponse && isSystemActive && IsRecognizing)
            {
                Debug.Log("Restarting recording after agent response");
                StartRecording();
            }
            else
            {
                Debug.Log($"Skipping restart: IsRecognizing={IsRecognizing}, isPlayingResponse={isPlayingResponse}, isSystemActive={isSystemActive}");
            }

            restartRecordingCoroutine = null;
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
            if (checkRecognizedSpeechCoroutine != null)
            {
                StopCoroutine(checkRecognizedSpeechCoroutine);
                checkRecognizedSpeechCoroutine = null;
            }

            if (sendWebsocketMessagesCoroutine != null)
            {
                StopCoroutine(sendWebsocketMessagesCoroutine);
                sendWebsocketMessagesCoroutine = null;
            }

            if (monitorStreamingPlaybackCoroutine != null)
            {
                StopCoroutine(monitorStreamingPlaybackCoroutine);
                monitorStreamingPlaybackCoroutine = null;
            }

            if (restartRecordingCoroutine != null)
            {
                StopCoroutine(restartRecordingCoroutine);
                restartRecordingCoroutine = null;
            }

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

    [Serializable]
    public class WhisperResponse
    {
        public string text;
    }
} 
