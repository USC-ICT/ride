using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ride.Vendor.NativeWebSocket.NativeWebSocket;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>Selectable OpenAI realtime models. Model ids live in code (not RideConfig) -
    /// see <see cref="OpenAIRealtimeModels"/>; mirrors the ChatGPT NLP pattern. Shared by the
    /// realtime conversation system and the realtime-backed speech-recognition system.</summary>
    public enum OpenAIRealtimeModel
    {
        Realtime21     = 10,
        Realtime21Mini = 20,
    }

    /// <summary>
    /// Selectable models for transcribing the user's speech within a realtime session. This is
    /// a different choice from <see cref="OpenAIRealtimeModel"/>: the realtime model understands
    /// the conversation and produces the spoken reply, while this model only turns the user's
    /// audio into text for transcripts and downstream logic.
    /// </summary>
    public enum OpenAIRealtimeTranscriptionModel
    {
        RealtimeWhisper     = 10,
        GPT4oMiniTranscribe = 20,
    }

    /// <summary>Maps realtime model selections to their API model ids.</summary>
    public static class OpenAIRealtimeModels
    {
        private static readonly Dictionary<OpenAIRealtimeModel, string> s_ids = new()
        {
            { OpenAIRealtimeModel.Realtime21,     "gpt-realtime-2.1"      },
            { OpenAIRealtimeModel.Realtime21Mini, "gpt-realtime-2.1-mini" },
        };

        private static readonly Dictionary<OpenAIRealtimeTranscriptionModel, string> s_transcriptionIds = new()
        {
            { OpenAIRealtimeTranscriptionModel.RealtimeWhisper,     "gpt-realtime-whisper"   },
            { OpenAIRealtimeTranscriptionModel.GPT4oMiniTranscribe, "gpt-4o-mini-transcribe" },
        };

        /// <summary>The API model id for the given realtime conversation model.</summary>
        public static string Id(OpenAIRealtimeModel model) => s_ids[model];

        /// <summary>The API model id for the given input-transcription model.</summary>
        public static string Id(OpenAIRealtimeTranscriptionModel model) => s_transcriptionIds[model];
    }

    /// <summary>
    /// End-to-end OpenAI realtime conversation system for voice-driven characters.
    /// Owns the websocket session, user audio input, assistant transcript stream, and assistant audio stream.
    /// </summary>
    public class UnifiedConversationSystemOpenAIRealtime : Ride.SpeechRecognition.SpeechRecognitionSystemUnity
    {
        public override bool IsSupported => true;
        public override bool SupportsContinuousRecognition => true;

        [Header("API Settings")]
        [SerializeField] private OpenAIRealtimeModel m_model = OpenAIRealtimeModel.Realtime21Mini;
        [SerializeField] private OpenAIRealtimeTranscriptionModel m_transcriptionModel =
            OpenAIRealtimeTranscriptionModel.RealtimeWhisper;

        /// <summary>The realtime model id currently selected, e.g. for UI display.</summary>
        public string ModelId => OpenAIRealtimeModels.Id(m_model);

        /// <summary>The input-transcription model id currently selected.</summary>
        public string TranscriptionModelId => OpenAIRealtimeModels.Id(m_transcriptionModel);

        [Header("Audio Input")]
        [SerializeField] private int m_sampleRate = 24000;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField] private int m_chunkSize = 4800;
#pragma warning restore 0414
        [SerializeField] private string m_microphoneDevice = null;
#pragma warning disable 0414 // The field '' is assigned but its value is never used
        [SerializeField] private int m_recordingBufferSeconds = 10;
#pragma warning restore 0414

        [Header("Assistant Audio Output")]
        [SerializeField] private string m_voice = "cedar";
        [SerializeField] private float m_streamingStartBufferSeconds = 0.20f;
        [SerializeField] private float m_streamingEndBufferSeconds = 0.30f;
        [SerializeField] private float m_assistantAudioStartEventDelaySeconds = 0.50f;
        [SerializeField] private int m_maxStreamingClipSeconds = 300;

        [Header("Voice Activity Detection")]
        [SerializeField] private float m_vadThreshold = 0.7f;
        [SerializeField] private int m_vadPrefixPaddingMs = 300;
        [SerializeField] private int m_vadSilenceDurationMs = 700;

        [Header("Transcript Chunking")]
        [SerializeField] private int m_minStableChunkCharacters = 24;
        [SerializeField] private bool m_enableProtocolDebugLogging = false;

        private WebSocket m_websocket;
        private AudioClip m_recordingClip;
        private AudioSource m_outputAudioSource;
        private Coroutine m_sendAudioCoroutine;
        private Coroutine m_monitorStreamingPlaybackCoroutine;

        private readonly Queue<float> m_streamingAudioQueue = new Queue<float>();
        private readonly object m_audioQueueLock = new object();
        private readonly Dictionary<string, List<ConversationTextTurn>> m_historyByCharacter = new Dictionary<string, List<ConversationTextTurn>>(StringComparer.Ordinal);

        private bool m_isConnected;
        private bool m_isRecording;
        private bool m_isStreamingAudio;
        private bool m_isAudioCallbackReady;
        private bool m_audioStreamComplete;
        private bool m_isAssistantSpeaking;
        private bool m_sessionConfigured;
        private bool m_reconnectInProgress;
        private bool m_assistantResponseFinalized;
        private bool m_responseInProgress;
        private bool m_receivedAssistantAudioDeltaThisResponse;
        private bool m_receivedAssistantTranscriptDeltaThisResponse;
        private bool m_loggedFirstAudioDeltaThisResponse;
        private bool m_assistantAudioStartedEventSent;
        private bool m_pendingAssistantAudioStartedEvent;
        private bool m_assistantAudioStartEventDelayArmed;
        private int m_lastSamplePosition;
        private int m_streamingPlaybackPosition;
        private int m_currentClipSamples;
        private int m_receivedAssistantAudioSamples;
        private float m_pendingAssistantAudioStartedEventTime;
        private float m_lastUserFinalTranscriptTime;
        private float m_lastResponseCreatedTime;
        private float m_lastFirstAudioDeltaTime;
        private volatile float m_currentAssistantAudioLevel;
        private string m_apiKey;
        private string m_endpoint;
        private string m_currentCharacterId = string.Empty;
        private string m_currentPrompt = string.Empty;
        private string m_currentAssistantTranscript = string.Empty;
        private string m_currentUserTranscript = string.Empty;
        private string m_activeResponseId = string.Empty;
        private string m_activeAudioResponseId = string.Empty;
        private StableTranscriptAccumulator m_assistantChunkAccumulator;

        public event Action<string> UserTranscriptDeltaReceived;
        public event Action<string> UserTranscriptFinalReceived;
        public event Action<string, string> UserTranscriptFinalReceivedWithLanguage;
        public event Action<string> AssistantTranscriptDeltaReceived;
        public event Action<string> AssistantTranscriptChunkReceived;
        public event Action<string> AssistantTranscriptFinalReceived;
        public event Action AssistantAudioStarted;
        public event Action AssistantAudioFinished;
        public event Action UserSpeechStarted;
        public event Action UserSpeechEnded;
        public event Action<string> ErrorReceived;

        public string CurrentCharacterId => m_currentCharacterId;
        public string CurrentAssistantTranscript => m_currentAssistantTranscript;
        public string CurrentUserTranscript => m_currentUserTranscript;
        public bool IsAssistantSpeaking => m_isAssistantSpeaking;
        public float AssistantPlaybackSeconds => m_streamingPlaybackPosition / (float)Mathf.Max(1, m_sampleRate);
        public float AssistantReceivedAudioSeconds => m_receivedAssistantAudioSamples / (float)Mathf.Max(1, m_sampleRate);
        public float AssistantOutputLevel => m_currentAssistantAudioLevel;

        private const string DefaultInstructions = "You are a helpful conversational assistant.";

        private void LogDebug(string message)
        {
            Debug.Log($"[OpenAI Realtime Unified] {message}");
        }

        private sealed class ConversationTextTurn
        {
            public string Role;
            public string Text;
        }

        private sealed class StableTranscriptAccumulator
        {
            private readonly int m_minStableChunkCharacters;
            private int m_committedLength;
            private string m_buffer = string.Empty;

            public StableTranscriptAccumulator(int minStableChunkCharacters)
            {
                m_minStableChunkCharacters = Math.Max(1, minStableChunkCharacters);
            }

            public void Reset()
            {
                m_committedLength = 0;
                m_buffer = string.Empty;
            }

            public List<string> Update(string transcript, bool flush)
            {
                var committedChunks = new List<string>();

                if (string.IsNullOrEmpty(transcript))
                    return committedChunks;

                if (!string.Equals(transcript, m_buffer, StringComparison.Ordinal))
                    m_buffer = transcript;

                while (m_committedLength < m_buffer.Length)
                {
                    int candidateLength = flush
                        ? m_buffer.Length
                        : FindStableBoundary(m_buffer, m_committedLength, m_minStableChunkCharacters);

                    if (candidateLength <= m_committedLength)
                        break;

                    string chunk = m_buffer.Substring(m_committedLength, candidateLength - m_committedLength);
                    m_committedLength = candidateLength;

                    if (!string.IsNullOrWhiteSpace(chunk))
                        committedChunks.Add(chunk);
                }

                return committedChunks;
            }

            private static int FindStableBoundary(string text, int startIndex, int minStableChunkCharacters)
            {
                if (string.IsNullOrEmpty(text) || startIndex >= text.Length)
                    return startIndex;

                int searchStart = Math.Min(text.Length - 1, startIndex + minStableChunkCharacters - 1);
                int punctuationBoundary = -1;
                int whitespaceBoundary = -1;

                for (int i = searchStart; i < text.Length; i++)
                {
                    char c = text[i];
                    if (char.IsWhiteSpace(c))
                        whitespaceBoundary = i + 1;

                    if (IsCommitPunctuation(c))
                    {
                        punctuationBoundary = i + 1;
                        break;
                    }
                }

                if (punctuationBoundary > startIndex)
                    return punctuationBoundary;

                return whitespaceBoundary > startIndex ? whitespaceBoundary : startIndex;
            }

            private static bool IsCommitPunctuation(char c) => c == '.' || c == '!' || c == '?' || c == ';' || c == ':';
        }

        /// <summary>
        /// Initializes OpenAI realtime configuration and prepares transcript chunking state.
        /// </summary>
        public override void SystemInit()
        {
            base.SystemInit();

            ConfigurationSystemUnity configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_apiKey = configSystem.config.openAIRealtime.endpointKey;
            m_endpoint = configSystem.config.openAIRealtime.endpoint;

            if (string.IsNullOrWhiteSpace(m_apiKey))
            {
                Debug.LogError("UnifiedConversationSystemOpenAIRealtime requires an OpenAI realtime API key.");
                return;
            }

            m_assistantChunkAccumulator = new StableTranscriptAccumulator(m_minStableChunkCharacters);

            if (string.IsNullOrWhiteSpace(SelectedMicrophone))
                SetMicrophone(null);
        }

        /// <summary>
        /// Releases websocket, microphone, and streaming audio resources during system shutdown.
        /// </summary>
        public override void SystemShutdown()
        {
            Cleanup();
            base.SystemShutdown();
        }

        /// <summary>
        /// Selects the microphone device used for realtime audio input.
        /// </summary>
        /// <param name="deviceName">Unity microphone device name, or null to use the default selection.</param>
        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);
            m_microphoneDevice = SelectedMicrophone;
        }

        /// <summary>
        /// Applies character-specific prompt, voice, and audio output routing for the realtime session.
        /// </summary>
        /// <param name="characterId">Stable character identifier used for per-character history.</param>
        /// <param name="prompt">System prompt/persona instructions for the character.</param>
        /// <param name="voice">OpenAI realtime voice name.</param>
        /// <param name="outputAudioSource">Audio source that should play streamed assistant audio.</param>
        public void ConfigureCharacter(string characterId, string prompt, string voice, AudioSource outputAudioSource)
        {
            characterId = string.IsNullOrWhiteSpace(characterId) ? "Character" : characterId;
            prompt = string.IsNullOrWhiteSpace(prompt) ? DefaultInstructions : prompt;
            voice = NormalizeVoiceName(voice);

            bool characterChanged = !string.Equals(m_currentCharacterId, characterId, StringComparison.Ordinal);
            bool promptChanged = !string.Equals(m_currentPrompt, prompt, StringComparison.Ordinal);
            bool voiceChanged = !string.Equals(m_voice, voice, StringComparison.Ordinal);
            bool outputChanged = m_outputAudioSource != outputAudioSource;

            m_currentCharacterId = characterId;
            m_currentPrompt = prompt;
            m_voice = voice;
            m_outputAudioSource = outputAudioSource;

            LogDebug($"ConfigureCharacter character='{m_currentCharacterId}' voice='{m_voice}' promptLength={m_currentPrompt?.Length ?? 0} usingDefaultPrompt={string.Equals(m_currentPrompt, DefaultInstructions, StringComparison.Ordinal)} outputSource='{m_outputAudioSource?.name ?? "null"}'");

            if (outputChanged && m_outputAudioSource != null)
                ConfigureOutputAudioSource(m_outputAudioSource);

            if ((characterChanged || promptChanged || voiceChanged) && m_isConnected)
            {
                StartCoroutine(ReconnectForCharacterChange());
            }
        }

        private static string NormalizeVoiceName(string voice)
        {
            if (string.IsNullOrWhiteSpace(voice))
                return "cedar";

            voice = voice.Trim();
            return string.Equals(voice, "aloy", StringComparison.OrdinalIgnoreCase) ? "alloy" : voice;
        }

        /// <summary>
        /// Sends a text-only user turn through the realtime conversation and requests an assistant response.
        /// </summary>
        /// <param name="text">User text to submit.</param>
        public void SubmitText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            LogDebug($"SubmitText connected={m_isConnected} sessionConfigured={m_sessionConfigured} text='{text}'");
            StartCoroutine(SubmitTextWhenReady(text));
        }

        /// <summary>
        /// Cancels any active assistant response and stops streamed audio playback.
        /// </summary>
        public void InterruptAssistant()
        {
            LogDebug($"InterruptAssistant websocketOpen={m_websocket?.State == WebSocketState.Open} speaking={m_isAssistantSpeaking}");
            if (m_websocket?.State == WebSocketState.Open)
                SendEvent(new { type = "response.cancel" });

            StopStreamingPlayback(invokeFinishedEvent: true);
            ResetAssistantResponseState();
        }

        /// <summary>
        /// Stops recognition, audio playback, microphone capture, and closes the realtime websocket.
        /// </summary>
        public void DeactivateConversation()
        {
            StopRecognizing();
            StopStreamingPlayback(invokeFinishedEvent: false);
            StopRecording();
            StartCoroutine(CloseWebSocketRoutine());
        }

        /// <summary>
        /// Starts realtime recognition by connecting to OpenAI and beginning microphone capture.
        /// </summary>
        public override void OnRecognizingStarted()
        {
            base.OnRecognizingStarted();
            LogDebug($"OnRecognizingStarted microphone='{m_microphoneDevice}' connected={m_isConnected}");
            StartCoroutine(EnsureConnectedAndStartRecording());
        }

        /// <summary>
        /// Stops realtime microphone capture when recognition is disabled.
        /// </summary>
        public override void OnRecognizingStopped()
        {
            LogDebug("OnRecognizingStopped");
            StopRecording();
            base.OnRecognizingStopped();
        }

        protected override void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            m_websocket?.DispatchMessageQueue();
#endif
            base.Update();

            if (m_pendingAssistantAudioStartedEvent && m_isStreamingAudio && !m_assistantAudioStartEventDelayArmed)
            {
                m_assistantAudioStartEventDelayArmed = true;
                m_pendingAssistantAudioStartedEventTime = Time.realtimeSinceStartup + Mathf.Max(0f, m_assistantAudioStartEventDelaySeconds);
            }

            if (m_pendingAssistantAudioStartedEvent && m_isStreamingAudio && Time.realtimeSinceStartup >= m_pendingAssistantAudioStartedEventTime)
            {
                m_pendingAssistantAudioStartedEvent = false;
                m_assistantAudioStartEventDelayArmed = false;
                LogDebug("Assistant audio stream started");
                AssistantAudioStarted?.Invoke();
            }
        }

        private IEnumerator EnsureConnectedAndStartRecording()
        {
            LogDebug($"EnsureConnectedAndStartRecording connected={m_isConnected} recognizing={IsRecognizing}");
            if (!m_isConnected)
                yield return StartCoroutine(ConnectToRealtimeAPI());

            float timeoutSeconds = 10f;
            float startTime = Time.time;
            while (!m_isConnected && (Time.time - startTime) < timeoutSeconds)
                yield return null;

            if (IsRecognizing)
            {
                LogDebug("EnsureConnectedAndStartRecording starting microphone capture");
                StartRecording();
            }
        }

        private IEnumerator ReconnectForCharacterChange()
        {
            bool shouldResumeRecognition = IsRecognizing;
            LogDebug($"ReconnectForCharacterChange character='{m_currentCharacterId}' shouldResumeRecognition={shouldResumeRecognition}");
            m_reconnectInProgress = true;
            m_isConnected = false;
            m_sessionConfigured = false;
            StopRecording();
            ResetAssistantResponseState();
            yield return StartCoroutine(CloseWebSocketRoutine());
            yield return StartCoroutine(ConnectToRealtimeAPI());

            if (shouldResumeRecognition)
                StartRecording();

            m_reconnectInProgress = false;
        }

        private IEnumerator ConnectToRealtimeAPI()
        {
            if (m_isConnected || string.IsNullOrWhiteSpace(m_endpoint) || string.IsNullOrWhiteSpace(m_apiKey))
                yield break;

            string url = $"{m_endpoint}?model={ModelId}";
            LogDebug($"ConnectToRealtimeAPI url='{url}'");

            if (IsRuntimeWebGL())
            {
                var subprotocols = new List<string>
                {
                    "realtime",
                    $"openai-insecure-api-key.{m_apiKey}"
                };

                m_websocket = new WebSocket(url, subprotocols);
            }
            else
            {
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {m_apiKey}" }
                };

                m_websocket = new WebSocket(url, headers);
            }

            m_websocket.OnOpen += OnWebSocketOpen;
            m_websocket.OnMessage += OnWebSocketMessage;
            m_websocket.OnError += OnWebSocketError;
            m_websocket.OnClose += OnWebSocketClose;

            Task connectTask = null;
            try
            {
                connectTask = m_websocket.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError($"OpenAI realtime connect failed: {e.Message}");
                ErrorReceived?.Invoke(e.Message);
                yield break;
            }

            float timeout = 10f;
            float startTime = Time.time;
            while (m_websocket.State != WebSocketState.Open && connectTask != null && !connectTask.IsCompleted)
            {
                if (Time.time - startTime > timeout)
                {
                    ErrorReceived?.Invoke("Timed out while connecting to OpenAI realtime.");
                    yield break;
                }

                yield return null;
            }

            while (m_websocket.State != WebSocketState.Open && (Time.time - startTime) < timeout)
                yield return null;

            if (m_websocket.State != WebSocketState.Open)
                ErrorReceived?.Invoke("OpenAI realtime websocket failed to open.");

            if (m_sendAudioCoroutine == null)
                m_sendAudioCoroutine = StartCoroutine(SendAudioLoop());
        }

        private IEnumerator CloseWebSocketRoutine()
        {
            if (m_sendAudioCoroutine != null)
            {
                StopCoroutine(m_sendAudioCoroutine);
                m_sendAudioCoroutine = null;
            }

            if (m_monitorStreamingPlaybackCoroutine != null)
            {
                StopCoroutine(m_monitorStreamingPlaybackCoroutine);
                m_monitorStreamingPlaybackCoroutine = null;
            }

            if (m_websocket == null)
            {
                m_isConnected = false;
                m_sessionConfigured = false;
                yield break;
            }

            WebSocket socket = m_websocket;
            m_websocket = null;

            socket.OnOpen -= OnWebSocketOpen;
            socket.OnMessage -= OnWebSocketMessage;
            socket.OnError -= OnWebSocketError;
            socket.OnClose -= OnWebSocketClose;

            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting)
            {
                Task closeTask = null;
                try
                {
                    closeTask = socket.Close();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"OpenAI realtime close failed: {e.Message}");
                }

                float timeout = 3f;
                float startTime = Time.time;
                while (closeTask != null && !closeTask.IsCompleted && (Time.time - startTime) < timeout)
                    yield return null;
            }

            m_isConnected = false;
            m_sessionConfigured = false;
        }

        private void OnWebSocketOpen()
        {
            m_isConnected = true;
            m_sessionConfigured = false;
            LogDebug("WebSocket opened");
            SendSessionUpdate();
        }

        private void SendSessionUpdate()
        {
            LogDebug($"SendSessionUpdate character='{m_currentCharacterId}' voice='{m_voice}' promptLength={m_currentPrompt?.Length ?? 0}");
            SendEvent(new
            {
                type = "session.update",
                session = new
                {
                    type = "realtime",
                    instructions = BuildRealtimeInstructions(),
                    output_modalities = new[] { "audio" },
                    audio = new
                    {
                        input = new
                        {
                            format = new
                            {
                                type = "audio/pcm",
                                rate = m_sampleRate
                            },
                            transcription = new
                            {
                                model = TranscriptionModelId
                            },
                            turn_detection = new
                            {
                                type = "server_vad",
                                threshold = m_vadThreshold,
                                prefix_padding_ms = m_vadPrefixPaddingMs,
                                silence_duration_ms = m_vadSilenceDurationMs,
                                create_response = false
                            }
                        },
                        output = new
                        {
                            format = new
                            {
                                type = "audio/pcm",
                                rate = m_sampleRate
                            },
                            voice = m_voice
                        }
                    }
                }
            });
        }

        private string BuildRealtimeInstructions() => string.IsNullOrWhiteSpace(m_currentPrompt) ? DefaultInstructions : m_currentPrompt;

        private void ReplayHistoryForCurrentCharacter()
        {
            if (string.IsNullOrWhiteSpace(m_currentCharacterId))
                return;

            if (!m_historyByCharacter.TryGetValue(m_currentCharacterId, out List<ConversationTextTurn> history) || history == null)
            {
                LogDebug($"ReplayHistoryForCurrentCharacter character='{m_currentCharacterId}' turns=0");
                return;
            }

            LogDebug($"ReplayHistoryForCurrentCharacter character='{m_currentCharacterId}' turns={history.Count}");

            for (int i = 0; i < history.Count; i++)
            {
                ConversationTextTurn turn = history[i];
                if (turn == null || string.IsNullOrWhiteSpace(turn.Text) || string.IsNullOrWhiteSpace(turn.Role))
                    continue;

                SendEvent(new
                {
                    type = "conversation.item.create",
                    item = new
                    {
                        type = "message",
                        role = turn.Role,
                        content = new[]
                        {
                            new
                            {
                                type = turn.Role == "assistant" ? "output_text" : "input_text",
                                text = turn.Text
                            }
                        }
                    }
                });
            }
        }

        private IEnumerator SendAudioLoop()
        {
            while (m_websocket != null)
            {
                if (!IsRecognizing || !m_isRecording || m_recordingClip == null)
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

                    if (samplesToSend >= m_chunkSize)
                    {
                        SendAudioChunk(samplesToSend);
                        m_lastSamplePosition = currentPosition;
                    }
                }
#endif
                yield return null;
            }

            m_sendAudioCoroutine = null;
        }

        private void OnWebSocketMessage(byte[] messageBytes)
        {
            string messageText = Encoding.UTF8.GetString(messageBytes);
            if (m_enableProtocolDebugLogging)
                Debug.Log($"[OpenAI Realtime Unified] {messageText}");

            JObject message = JObject.Parse(messageText);
            string eventType = message["type"]?.ToString();
            if (string.IsNullOrWhiteSpace(eventType))
                return;

            switch (eventType)
            {
                case "session.created":
                case "session.updated":
                    LogDebug($"Received {eventType}");
                    if (!m_sessionConfigured)
                    {
                        m_sessionConfigured = true;
                        LogDebug("Session configured");
                        ReplayHistoryForCurrentCharacter();
                    }
                    break;

                case "input_audio_buffer.speech_started":
                    LogDebug("User speech started");
                    m_currentUserTranscript = string.Empty;
                    UserSpeechStarted?.Invoke();
                    if (m_isAssistantSpeaking)
                        InterruptAssistant();
                    break;

                case "input_audio_buffer.speech_stopped":
                    LogDebug("User speech stopped");
                    UserSpeechEnded?.Invoke();
                    break;

                case "conversation.item.input_audio_transcription.delta":
                    string partialTranscript = message["delta"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(partialTranscript))
                    {
                        LogDebug($"User partial transcript='{partialTranscript}'");
                        UserTranscriptDeltaReceived?.Invoke(partialTranscript);
                        OnPartialSpeechRecognized(partialTranscript, Confidence);
                    }
                    break;

                case "conversation.item.input_audio_transcription.completed":
                    string finalTranscript = message["transcript"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(finalTranscript))
                    {
                        string detectedLanguage = message["language"]?.ToString() ?? string.Empty;
                        m_lastUserFinalTranscriptTime = Time.realtimeSinceStartup;
                        LogDebug($"User final transcript language='{(string.IsNullOrWhiteSpace(detectedLanguage) ? "unknown" : detectedLanguage)}' text='{finalTranscript}'");
                        m_currentUserTranscript = finalTranscript;
                        UserTranscriptFinalReceived?.Invoke(finalTranscript);
                        UserTranscriptFinalReceivedWithLanguage?.Invoke(finalTranscript, detectedLanguage);
                        OnSpeechRecognized(finalTranscript, Confidence, detectedLanguage);
                        AppendHistoryTurn("user", finalTranscript);
                        ResetAssistantResponseState();
                        SendResponseCreate();
                    }
                    break;

                case "response.created":
                    StopStreamingPlayback(invokeFinishedEvent: false);
                    if (m_outputAudioSource != null)
                    {
                        m_outputAudioSource.Stop();
                        m_outputAudioSource.clip = null;
                    }

                    m_responseInProgress = true;
                    m_activeResponseId = message["response"]?["id"]?.ToString() ?? string.Empty;
                    m_lastResponseCreatedTime = Time.realtimeSinceStartup;
                    m_activeAudioResponseId = string.Empty;
                    m_receivedAssistantAudioDeltaThisResponse = false;
                    m_receivedAssistantTranscriptDeltaThisResponse = false;
                    m_loggedFirstAudioDeltaThisResponse = false;
                    LogDebug($"Response created payload={messageText}");
                    break;

                case "response.audio.delta":
                case "response.output_audio.delta":
                    string audioDelta = message["delta"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(audioDelta))
                    {
                        string audioResponseId = message["response_id"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(m_activeResponseId) &&
                            !string.IsNullOrWhiteSpace(audioResponseId) &&
                            !string.Equals(m_activeResponseId, audioResponseId, StringComparison.Ordinal))
                        {
                            LogDebug($"Ignoring audio delta for stale responseId='{audioResponseId}' activeResponseId='{m_activeResponseId}'");
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace(audioResponseId) &&
                            !string.IsNullOrWhiteSpace(m_activeAudioResponseId) &&
                            !string.Equals(m_activeAudioResponseId, audioResponseId, StringComparison.Ordinal))
                        {
                            LogDebug($"Audio response switched from '{m_activeAudioResponseId}' to '{audioResponseId}', resetting playback state");
                            StopStreamingPlayback(invokeFinishedEvent: false);
                            ResetStreamingAudioBuffers();
                        }

                        if (!string.IsNullOrWhiteSpace(audioResponseId))
                            m_activeAudioResponseId = audioResponseId;

                        m_receivedAssistantAudioDeltaThisResponse = true;
                        if (!m_loggedFirstAudioDeltaThisResponse)
                        {
                            m_loggedFirstAudioDeltaThisResponse = true;
                            m_lastFirstAudioDeltaTime = Time.realtimeSinceStartup;
                            LogDebug($"First assistant audio delta responseId='{audioResponseId}' activeResponseId='{m_activeResponseId}' responseCreateToAudio={(m_lastFirstAudioDeltaTime - m_lastResponseCreatedTime):0.000}s userFinalToAudio={(m_lastFirstAudioDeltaTime - m_lastUserFinalTranscriptTime):0.000}s");
                        }

                        ProcessStreamingAudio(audioDelta);
                    }
                    break;

                case "response.audio.done":
                case "response.output_audio.done":
                    string audioDoneResponseId = message["response_id"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(m_activeResponseId) &&
                        !string.IsNullOrWhiteSpace(audioDoneResponseId) &&
                        !string.Equals(m_activeResponseId, audioDoneResponseId, StringComparison.Ordinal))
                    {
                        LogDebug($"Ignoring audio done for stale responseId='{audioDoneResponseId}' activeResponseId='{m_activeResponseId}'");
                        break;
                    }

                    LogDebug("Assistant audio stream marked complete");
                    m_audioStreamComplete = true;
                    if (IsRuntimeWebGL() && !m_isStreamingAudio && m_outputAudioSource != null)
                        StartStreamingPlayback();
                    break;

                case "response.audio_transcript.delta":
                case "response.output_audio_transcript.delta":
                    string assistantDelta = message["delta"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(assistantDelta))
                    {
                        string transcriptResponseId = message["response_id"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(m_activeResponseId) &&
                            !string.IsNullOrWhiteSpace(transcriptResponseId) &&
                            !string.Equals(m_activeResponseId, transcriptResponseId, StringComparison.Ordinal))
                        {
                            LogDebug($"Ignoring transcript delta for stale responseId='{transcriptResponseId}' activeResponseId='{m_activeResponseId}'");
                            break;
                        }

                        m_receivedAssistantTranscriptDeltaThisResponse = true;
                        m_currentAssistantTranscript += assistantDelta;
                        LogDebug($"Assistant transcript delta='{assistantDelta}'");
                        AssistantTranscriptDeltaReceived?.Invoke(assistantDelta);

                        List<string> committedChunks = m_assistantChunkAccumulator.Update(m_currentAssistantTranscript, flush: false);
                        for (int i = 0; i < committedChunks.Count; i++)
                            AssistantTranscriptChunkReceived?.Invoke(committedChunks[i]);
                    }
                    break;

                case "response.audio_transcript.done":
                case "response.output_audio_transcript.done":
                    string transcriptDoneResponseId = message["response_id"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(m_activeResponseId) &&
                        !string.IsNullOrWhiteSpace(transcriptDoneResponseId) &&
                        !string.Equals(m_activeResponseId, transcriptDoneResponseId, StringComparison.Ordinal))
                    {
                        LogDebug($"Ignoring transcript done for stale responseId='{transcriptDoneResponseId}' activeResponseId='{m_activeResponseId}'");
                        break;
                    }

                    string assistantFinalTranscript = message["transcript"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(assistantFinalTranscript) &&
                        string.IsNullOrWhiteSpace(m_currentAssistantTranscript))
                    {
                        m_currentAssistantTranscript = assistantFinalTranscript;
                    }

                    LogDebug("Assistant transcript stream finalized");
                    FlushAssistantTranscript(finalize: true);
                    break;

                case "response.content_part.done":
                    string partTranscript = message["part"]?["transcript"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(partTranscript) &&
                        string.IsNullOrWhiteSpace(m_currentAssistantTranscript))
                    {
                        m_currentAssistantTranscript = partTranscript;
                    }
                    break;

                case "response.output_item.done":
                    JToken firstContent = message["item"]?["content"]?.First;
                    string itemTranscript = firstContent?["transcript"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(itemTranscript) &&
                        string.IsNullOrWhiteSpace(m_currentAssistantTranscript))
                    {
                        m_currentAssistantTranscript = itemTranscript;
                    }
                    break;

                case "response.done":
                    LogDebug($"Response done audioDeltaReceived={m_receivedAssistantAudioDeltaThisResponse} transcriptDeltaReceived={m_receivedAssistantTranscriptDeltaThisResponse} payload={messageText}");
                    m_responseInProgress = false;
                    m_activeResponseId = string.Empty;
                    FlushAssistantTranscript(finalize: true);
                    break;

                case "error":
                    string error = message["error"]?["message"]?.ToString() ?? "Unknown OpenAI realtime error.";
                    if (error.IndexOf("Cancellation failed: no active response found", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        LogDebug(error);
                    }
                    else
                    {
                        Debug.LogError(error);
                        ErrorReceived?.Invoke(error);
                    }
                    break;

                default:
                    if (eventType.StartsWith("response.", StringComparison.Ordinal))
                        LogDebug($"Unhandled response event type='{eventType}' payload={messageText}");
                    break;
            }
        }

        private void FlushAssistantTranscript(bool finalize)
        {
            if (finalize && m_assistantResponseFinalized)
                return;

            List<string> committedChunks = m_assistantChunkAccumulator.Update(m_currentAssistantTranscript, flush: finalize);
            for (int i = 0; i < committedChunks.Count; i++)
                AssistantTranscriptChunkReceived?.Invoke(committedChunks[i]);

            if (finalize && !string.IsNullOrWhiteSpace(m_currentAssistantTranscript))
            {
                LogDebug($"Assistant final transcript='{m_currentAssistantTranscript}'");
                m_assistantResponseFinalized = true;
                AssistantTranscriptFinalReceived?.Invoke(m_currentAssistantTranscript);
                AppendHistoryTurn("assistant", m_currentAssistantTranscript);
            }
        }

        private void OnWebSocketError(string error)
        {
            Debug.LogError($"OpenAI realtime websocket error: {error}");
            ErrorReceived?.Invoke(error);
        }

        private void OnWebSocketClose(WebSocketCloseCode code)
        {
            m_isConnected = false;
            m_sessionConfigured = false;
            LogDebug($"WebSocket closed code={code}");
        }

        private void StartRecording()
        {
            if (m_isRecording || !m_isConnected)
            {
                LogDebug($"StartRecording skipped isRecording={m_isRecording} connected={m_isConnected}");
                return;
            }

#if !UNITY_WEBGL
            m_recordingClip = Microphone.Start(m_microphoneDevice, true, m_recordingBufferSeconds, m_sampleRate);
#endif
            m_isRecording = true;
            m_lastSamplePosition = 0;
            LogDebug($"Recording started microphone='{m_microphoneDevice}' sampleRate={m_sampleRate}");
        }

        private void StopRecording()
        {
            if (!m_isRecording)
                return;

#if !UNITY_WEBGL
            Microphone.End(m_microphoneDevice);
#endif
            m_recordingClip = null;
            m_isRecording = false;
            m_lastSamplePosition = 0;
            LogDebug("Recording stopped");
        }

        private void SendAudioChunk(int sampleCount)
        {
            if (m_recordingClip == null || sampleCount <= 0)
                return;

            float[] samples = ReadCircularClipSamplesMono(m_recordingClip, m_lastSamplePosition, sampleCount);

            byte[] pcm16Data = ConvertToPcm16(samples);
            string base64Audio = Convert.ToBase64String(pcm16Data);

            SendEvent(new
            {
                type = "input_audio_buffer.append",
                audio = base64Audio
            });
        }

        private static float[] ReadCircularClipSamplesMono(AudioClip clip, int startSample, int sampleCount)
        {
            int channels = clip.channels;
            float[] samples = new float[sampleCount];
            int firstSegmentSamples = Math.Min(sampleCount, clip.samples - startSample);
            if (firstSegmentSamples > 0)
            {
                float[] firstSegment = new float[firstSegmentSamples * channels];
                clip.GetData(firstSegment, startSample);
                DownmixToMono(firstSegment, channels, samples, 0, firstSegmentSamples);
            }

            int remainingSamples = sampleCount - firstSegmentSamples;
            if (remainingSamples > 0)
            {
                float[] secondSegment = new float[remainingSamples * channels];
                clip.GetData(secondSegment, 0);
                DownmixToMono(secondSegment, channels, samples, firstSegmentSamples, remainingSamples);
            }

            return samples;
        }

        private static void DownmixToMono(float[] interleavedSamples, int channels, float[] monoSamples, int monoOffset, int sampleFrames)
        {
            if (channels <= 1)
            {
                Array.Copy(interleavedSamples, 0, monoSamples, monoOffset, sampleFrames);
                return;
            }

            for (int frame = 0; frame < sampleFrames; frame++)
            {
                float sum = 0f;
                int frameOffset = frame * channels;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleavedSamples[frameOffset + channel];

                monoSamples[monoOffset + frame] = sum / channels;
            }
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

        private void ProcessStreamingAudio(string base64Audio)
        {
            try
            {
                byte[] audioData = Convert.FromBase64String(base64Audio);
                int samplesAdded = 0;

                lock (m_audioQueueLock)
                {
                    for (int i = 0; i < audioData.Length; i += 2)
                    {
                        if (i + 1 >= audioData.Length)
                            break;

                        short value = BitConverter.ToInt16(audioData, i);
                        m_streamingAudioQueue.Enqueue(value / 32768f);
                        samplesAdded++;
                    }
                }

                m_receivedAssistantAudioSamples += samplesAdded;

                if (!IsRuntimeWebGL() && !m_isStreamingAudio && m_outputAudioSource != null)
                {
                    int requiredSamples = Mathf.CeilToInt(m_streamingStartBufferSeconds * m_sampleRate);
                    if (m_streamingAudioQueue.Count >= requiredSamples)
                        StartStreamingPlayback();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to process OpenAI realtime audio: {e.Message}");
            }
        }

        private void ConfigureOutputAudioSource(AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = 1f;
            source.pitch = 1f;
            source.dopplerLevel = 0f;
            source.enabled = true;
        }

        private static bool IsRuntimeWebGL() => RideUtils.IsWebGL() && !RideUtils.IsEditor();

        private void StartStreamingPlayback()
        {
            if (m_outputAudioSource == null)
            {
                LogDebug("StartStreamingPlayback skipped: no output audio source");
                return;
            }

            m_isStreamingAudio = true;
            m_isAudioCallbackReady = false;
            m_streamingPlaybackPosition = 0;
            m_assistantAudioStartedEventSent = false;
            m_pendingAssistantAudioStartedEvent = false;
            m_assistantAudioStartEventDelayArmed = false;
            m_currentAssistantAudioLevel = 0f;
            LogDebug($"StartStreamingPlayback queuedSamples={m_streamingAudioQueue.Count} source='{m_outputAudioSource.name}'");
            if (m_lastFirstAudioDeltaTime > 0f)
                LogDebug($"StartStreamingPlayback timing firstAudioToPlayback={(Time.realtimeSinceStartup - m_lastFirstAudioDeltaTime):0.000}s");

            m_outputAudioSource.Stop();
            m_outputAudioSource.clip = null;

            if (IsRuntimeWebGL())
            {
                float[] queuedSamples;
                lock (m_audioQueueLock)
                {
                    queuedSamples = m_streamingAudioQueue.ToArray();
                    m_streamingAudioQueue.Clear();
                }

                if (queuedSamples.Length == 0)
                {
                    LogDebug("StartStreamingPlayback skipped: no queued audio samples");
                    m_isStreamingAudio = false;
                    return;
                }

                m_currentClipSamples = queuedSamples.Length;
                AudioClip playbackClip = AudioClip.Create("OpenAIRealtimeWebGL", m_currentClipSamples, 1, m_sampleRate, false);
                playbackClip.SetData(queuedSamples, 0);
                m_outputAudioSource.clip = playbackClip;
                m_assistantAudioStartedEventSent = true;
                m_pendingAssistantAudioStartedEvent = true;
            }
            else
            {
                m_currentClipSamples = m_sampleRate * m_maxStreamingClipSeconds;
                AudioClip streamingClip = AudioClip.Create("OpenAIRealtimeStreaming", m_currentClipSamples, 1, m_sampleRate, true, OnAudioRead);
                m_outputAudioSource.clip = streamingClip;
                m_isAudioCallbackReady = true;
            }

            m_outputAudioSource.Play();
            m_isAssistantSpeaking = true;

            if (m_monitorStreamingPlaybackCoroutine != null)
                StopCoroutine(m_monitorStreamingPlaybackCoroutine);

            m_monitorStreamingPlaybackCoroutine = StartCoroutine(MonitorStreamingPlayback());
        }

        private void OnAudioRead(float[] data)
        {
            if (!m_isAudioCallbackReady || !m_isStreamingAudio)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            lock (m_audioQueueLock)
            {
                float sumSquares = 0f;
                int realSampleCount = 0;

                for (int i = 0; i < data.Length; i++)
                {
                    if (m_streamingAudioQueue.Count > 0)
                    {
                        float sample = m_streamingAudioQueue.Dequeue();
                        data[i] = sample;
                        sumSquares += sample * sample;
                        realSampleCount++;
                        m_streamingPlaybackPosition++;
                        if (!m_assistantAudioStartedEventSent)
                        {
                            m_assistantAudioStartedEventSent = true;
                            m_pendingAssistantAudioStartedEvent = true;
                        }
                    }
                    else
                    {
                        data[i] = 0f;
                    }
                }

                if (realSampleCount > 0)
                    m_currentAssistantAudioLevel = Mathf.Sqrt(sumSquares / realSampleCount);
                else
                    m_currentAssistantAudioLevel = 0f;
            }
        }

        private IEnumerator MonitorStreamingPlayback()
        {
            if (IsRuntimeWebGL())
            {
                while (m_isStreamingAudio)
                {
                    yield return null;

                    if (m_outputAudioSource == null || !m_outputAudioSource.isPlaying)
                    {
                        StopStreamingPlayback(invokeFinishedEvent: true);
                        break;
                    }

                    m_streamingPlaybackPosition = m_outputAudioSource.timeSamples;
                }

                m_monitorStreamingPlaybackCoroutine = null;
                yield break;
            }

            int emptyFrames = 0;
            int requiredEmptyFrames = Mathf.CeilToInt(m_streamingEndBufferSeconds / 0.1f);

            while (m_isStreamingAudio)
            {
                yield return new WaitForSeconds(0.1f);

                int queuedSamples;
                lock (m_audioQueueLock)
                    queuedSamples = m_streamingAudioQueue.Count;

                if (m_audioStreamComplete && queuedSamples == 0)
                {
                    emptyFrames++;
                    if (emptyFrames > requiredEmptyFrames)
                    {
                        StopStreamingPlayback(invokeFinishedEvent: true);
                        break;
                    }
                }
                else
                {
                    emptyFrames = 0;
                }
            }

            m_monitorStreamingPlaybackCoroutine = null;
        }

        private void StopStreamingPlayback(bool invokeFinishedEvent)
        {
            if (m_outputAudioSource != null && m_outputAudioSource.isPlaying)
                m_outputAudioSource.Stop();

            if (m_outputAudioSource != null)
                m_outputAudioSource.clip = null;

            m_isStreamingAudio = false;
            m_isAudioCallbackReady = false;
            m_assistantAudioStartedEventSent = false;
            m_pendingAssistantAudioStartedEvent = false;
            m_assistantAudioStartEventDelayArmed = false;
            m_currentAssistantAudioLevel = 0f;
            m_audioStreamComplete = false;
            ResetStreamingAudioBuffers();

            if (invokeFinishedEvent && m_isAssistantSpeaking)
            {
                m_isAssistantSpeaking = false;
                LogDebug("Assistant audio finished");
                AssistantAudioFinished?.Invoke();
            }
        }

        private void ResetStreamingAudioBuffers()
        {
            lock (m_audioQueueLock)
                m_streamingAudioQueue.Clear();
        }

        private void SendEvent(object eventData)
        {
            if (m_websocket?.State != WebSocketState.Open)
                return;

            string json = JsonConvert.SerializeObject(eventData);
            m_websocket.SendText(json);
        }

        private void SendResponseCreate()
        {
            if (m_responseInProgress)
            {
                LogDebug("SendResponseCreate skipped because a response is already in progress");
                return;
            }

            LogDebug("SendResponseCreate");
            SendEvent(new
            {
                type = "response.create"
            });
        }

        private void ResetAssistantResponseState()
        {
            m_currentAssistantTranscript = string.Empty;
            m_assistantChunkAccumulator.Reset();
            m_audioStreamComplete = false;
            m_assistantResponseFinalized = false;
            m_receivedAssistantAudioDeltaThisResponse = false;
            m_receivedAssistantTranscriptDeltaThisResponse = false;
            m_loggedFirstAudioDeltaThisResponse = false;
            m_activeAudioResponseId = string.Empty;
            m_receivedAssistantAudioSamples = 0;
            StopStreamingPlayback(invokeFinishedEvent: false);
            m_isAssistantSpeaking = false;
        }

        private IEnumerator SubmitTextWhenReady(string text)
        {
            float timeoutSeconds = 10f;
            float startTime = Time.time;
            while (m_reconnectInProgress && (Time.time - startTime) < timeoutSeconds)
                yield return null;

            if (!m_isConnected)
                yield return StartCoroutine(ConnectToRealtimeAPI());

            startTime = Time.time;
            while ((m_reconnectInProgress || !m_sessionConfigured) && (Time.time - startTime) < timeoutSeconds)
                yield return null;

            if (m_reconnectInProgress || !m_isConnected || !m_sessionConfigured)
            {
                LogDebug($"SubmitTextWhenReady failed: session not ready reconnectInProgress={m_reconnectInProgress} connected={m_isConnected} sessionConfigured={m_sessionConfigured}");
                ErrorReceived?.Invoke("OpenAI realtime session is not ready for text submission.");
                yield break;
            }

            if (m_responseInProgress)
            {
                LogDebug("SubmitTextWhenReady skipped because a response is already in progress");
                yield break;
            }

            ResetAssistantResponseState();
            LogDebug($"SubmitTextWhenReady sending text='{text}'");

            SendEvent(new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text
                        }
                    }
                }
            });

            AppendHistoryTurn("user", text);
            SendResponseCreate();
        }

        private void AppendHistoryTurn(string role, string text)
        {
            if (string.IsNullOrWhiteSpace(m_currentCharacterId) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text))
                return;

            if (!m_historyByCharacter.TryGetValue(m_currentCharacterId, out List<ConversationTextTurn> history))
            {
                history = new List<ConversationTextTurn>();
                m_historyByCharacter[m_currentCharacterId] = history;
            }

            history.Add(new ConversationTextTurn
            {
                Role = role,
                Text = text
            });
        }

        private void Cleanup()
        {
            StopRecording();
            StopStreamingPlayback(invokeFinishedEvent: false);
            if (m_sendAudioCoroutine != null)
            {
                StopCoroutine(m_sendAudioCoroutine);
                m_sendAudioCoroutine = null;
            }

            if (m_monitorStreamingPlaybackCoroutine != null)
            {
                StopCoroutine(m_monitorStreamingPlaybackCoroutine);
                m_monitorStreamingPlaybackCoroutine = null;
            }

            if (m_websocket != null)
            {
                m_websocket.OnOpen -= OnWebSocketOpen;
                m_websocket.OnMessage -= OnWebSocketMessage;
                m_websocket.OnError -= OnWebSocketError;
                m_websocket.OnClose -= OnWebSocketClose;
                try { _ = m_websocket.Close(); } catch { }
                m_websocket = null;
            }

            m_isConnected = false;
            m_sessionConfigured = false;
        }
    }
}
