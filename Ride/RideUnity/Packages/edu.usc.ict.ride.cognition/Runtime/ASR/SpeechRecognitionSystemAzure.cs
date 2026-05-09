using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
#if !UNITY_WEBGL
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Interface to support input source switching (e.g., microphone, speaker, file) for Azure speech recognition.
    /// </summary>
    public interface IInputSourceConfigurableSpeechRecognition
    {
        void SetInputSource(SpeechRecognitionSystemAzure.SpeechRecognitionType sourceType, string filePath = null);
        SpeechRecognitionSystemAzure.SpeechRecognitionType CurrentInputSource { get; }
        string FilePath { get; }
    }

    /// <summary>
    /// SpeechRecognitionSystem utilizing Microsofts Cognitive services Speech SDK
    /// </summary>
    public class SpeechRecognitionSystemAzure : SpeechRecognitionSystemUnity, IInputSourceConfigurableSpeechRecognition
    {
        public enum SpeechRecognitionType
        {
            MICROPHONE,
            SPEAKER,
            FILE
        }

        const int SAMPLE_RATE = 24000;
        const string DEFAULT_MICROPHONE_NAME = "AzureDefault";

        public override bool IsSupported => true;

        public override bool SupportsContinuousRecognition => true;

#if PLATFORM_ANDROID || PLATFORM_IOS
        // Required to manifest microphone permission, cf.
        // https://docs.unity3d.com/Manual/android-manifest.html
#endif

        //bool m_micPermissionGranted = false;

#if !UNITY_WEBGL
        SpeechRecognizer m_speechRecognizer;
#endif

        private string m_recognizedSpeech;
        private string m_recognizedSpeechPartial;
        private SpeechRecognitionType m_currentInputSource = SpeechRecognitionType.MICROPHONE;
        private string m_filePath;

        public SpeechRecognitionType CurrentInputSource => m_currentInputSource;
        public string FilePath => m_filePath;


        void OnDisable()
        {
            Cleanup();
        }


        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            CheckMicrophonePermissions();
        }

        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            if (IsRecognizing)
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
            }
        }

        public override void SystemShutdown()
        {
            Cleanup();

            base.SystemShutdown();
        }

        /// <inheritdoc/>
        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);

            // the method in the base class for finding default is not valid on this platform (unity mic names differ from azure expected names)
            // see: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-select-audio-input-devices

            // On most platforms, Unity's microphone names do not match Azure's device names.
            // We prefer default mic, but we still track the selected Unity mic name for logging.
            if (string.IsNullOrEmpty(deviceName))
            {
                SelectedMicrophone = DEFAULT_MICROPHONE_NAME;

                Debug.Log("[Azure ASR] SetMicrophone called with empty deviceName; using default microphone.");
            }
            else
            {
                SelectedMicrophone = deviceName;

                Debug.Log($"[Azure ASR] SetMicrophone called with deviceName='{deviceName}'.");
            }

            SetInputSource(SpeechRecognitionType.MICROPHONE);
        }

        /// <inheritdoc/>
        public void SetInputSource(SpeechRecognitionType sourceType, string filePath = null)
        {
            m_currentInputSource = sourceType;
            m_filePath = filePath;

            //Debug.Log($"[Azure ASR] Input source set to {m_currentInputSource}, filePath='{m_filePath}'.");
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            Debug.Log("[Azure ASR] OnRecognizingStarted - starting Azure recognizer.");

            StartSpeechRecognitionAsync();

            base.OnRecognizingStarted();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            Debug.Log("[Azure ASR] OnRecognizingStopped - stopping Azure recognizer.");

            StopSpeechRecognitionAsync();

            base.OnRecognizingStopped();
        }

#if !UNITY_WEBGL
        /// <summary>
        /// Event handler for receiving partial recognition results.
        /// </summary>
        protected void OnAzureSpeechPartialRecognizedEvent(object sender, SpeechRecognitionEventArgs e)
        {
            if (e == null || e.Result == null)
                return;

            m_recognizedSpeechPartial = e.Result.Text;

            if (!string.IsNullOrEmpty(m_recognizedSpeechPartial))
                Debug.Log($"[Azure ASR] Recognizing (partial): '{m_recognizedSpeechPartial}'");
        }

        /// <summary>
        /// Event handler for receiving final recognition results.
        /// </summary>
        protected void OnAzureSpeechRecognizedEvent(object sender, SpeechRecognitionEventArgs e)
        {
            if (e == null || e.Result == null)
                return;

            m_recognizedSpeech = e.Result.Text;

            Debug.Log($"[Azure ASR] Recognized (final): Reason={e.Result.Reason}, Text='{m_recognizedSpeech}'");
        }

        /// <summary>
        /// Event handler for cancellation events (errors, end-of-session, etc.).
        /// </summary>
        protected void OnAzureSpeechCanceledEvent(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e == null)
                return;

            Debug.LogError(
                $"[Azure ASR] Canceled: Reason={e.Reason}, ErrorCode={e.ErrorCode}, Details='{e.ErrorDetails}'");
        }

        /// <summary>
        /// Event handler when the Azure session stops.
        /// </summary>
        protected void OnAzureSessionStoppedEvent(object sender, SessionEventArgs e)
        {
            Debug.Log("[Azure ASR] SessionStopped event received from Azure.");            
        }
#endif

        /// <summary>
        /// Starts the Azure SDK speech recognition process using the current input source.
        /// </summary>
        protected async void StartSpeechRecognitionAsync()
        {
#if !UNITY_WEBGL
            if (m_speechRecognizer != null)
            {
                Debug.LogWarning("[Azure ASR] StartSpeechRecognitionAsync called, but recognizer is already running.");
                return;
            }

            try
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.azureSpeech.apiKey;
                var region = configSystem.config.azureSpeech.region;

                Debug.Log($"[Azure ASR] SystemInit with region='{region}'.");

                var speechConfig = SpeechConfig.FromSubscription(apiKey, region);

                speechConfig.SetProperty(
                    PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs,
                    ((int)(InitialSilenceTimeoutSeconds * 1000)).ToString());
                speechConfig.SetProperty(
                    PropertyId.Speech_SegmentationSilenceTimeoutMs,
                    ((int)(AutoSilenceTimeoutSeconds * 1000)).ToString());

                Debug.Log($"[Azure ASR] StartSpeechRecognitionAsync - InputSource={CurrentInputSource}");

                var audioConfig = BuildAudioConfigWithFallback();
                if (audioConfig == null)
                {
                    Debug.LogError("[Azure ASR] Failed to build AudioConfig. Aborting start.");

                    StopRecognizing();
                    return;
                }

                m_speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                m_speechRecognizer.Recognizing += OnAzureSpeechPartialRecognizedEvent;
                m_speechRecognizer.Recognized += OnAzureSpeechRecognizedEvent;
                m_speechRecognizer.Canceled += OnAzureSpeechCanceledEvent;
                m_speechRecognizer.SessionStopped += OnAzureSessionStoppedEvent;

                Confidence = 1;

                Debug.Log("[Azure ASR] Calling StartContinuousRecognitionAsync...");

                await m_speechRecognizer.StartContinuousRecognitionAsync();

                Debug.Log("[Azure ASR] StartContinuousRecognitionAsync completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Azure ASR] Failed to start: {ex.GetType().Name}: {ex.Message}");

                //OnSpeechRecognized("Microphone not available. Check OS mic privacy, default input device, or close other apps using the mic.", 0);

                StopRecognizing(); 
                return;
            }
#else
            await Task.Delay(0);
#endif

            if (CurrentInputSource == SpeechRecognitionType.FILE)
            {
                Debug.Log("[Azure ASR] FILE input source used - stopping after initial recognition start.");

                StopSpeechRecognitionAsync();
            }
        }

#if !UNITY_WEBGL
        private AudioConfig BuildAudioConfigWithFallback()
        {
            Debug.Log($"[Azure ASR] BuildAudioConfigWithFallback - CurrentInputSource={CurrentInputSource}, SelectedMicrophone='{SelectedMicrophone}'");

            switch (CurrentInputSource)
            {
                case SpeechRecognitionType.FILE:
                    if (!string.IsNullOrEmpty(FilePath))
                    {
                        Debug.Log($"[Azure ASR] Using FILE input: '{FilePath}'");
                        return AudioConfig.FromWavFileInput(FilePath);
                    }
                    Debug.LogWarning("[Azure ASR] FILE mode selected but FilePath is empty. Falling back to microphone.");
                    goto case SpeechRecognitionType.MICROPHONE;

                case SpeechRecognitionType.SPEAKER:
                    // Not all platforms support this; if it fails, fall back to mic.
                    try
                    {
                        Debug.Log("[Azure ASR] Using default speaker output as input.");
                        return AudioConfig.FromDefaultSpeakerOutput();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Azure ASR] Default speaker output not available: {e.Message}. Falling back to microphone.");
                        goto case SpeechRecognitionType.MICROPHONE;
                    }

                case SpeechRecognitionType.MICROPHONE:
                default:
#if UNITY_ANDROID && !UNITY_EDITOR
                    Debug.Log("[Azure ASR] Forcing default microphone input on Android");
                    return AudioConfig.FromDefaultMicrophoneInput();
#else
                    // IMPORTANT: Unity’s Microphone.devices names don’t always match what Azure expects.
                    // Prefer default mic to avoid SPXERR_MIC_NOT_AVAILABLE from mismatched device names.
                    // See SetMicrophone()
                    try
                    {
                        if (!string.IsNullOrEmpty(SelectedMicrophone) && 
                            SelectedMicrophone != DEFAULT_MICROPHONE_NAME)
                        {
                            Debug.Log($"[Azure ASR] Using named microphone: '{SelectedMicrophone}'.");
                            return AudioConfig.FromMicrophoneInput(SelectedMicrophone);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Azure ASR] Named microphone '{SelectedMicrophone}' not available: {e.Message}. Using default mic.");
                    }

                    Debug.Log("[Azure ASR] Using default microphone input.");
                    return AudioConfig.FromDefaultMicrophoneInput();
#endif
            }
        }
#endif

        /// <summary>
        /// Stops the Azure SDK speech recognition and disposes internal recognizer state.
        /// </summary>
        protected async void StopSpeechRecognitionAsync()
        {
#if !UNITY_WEBGL
            try
            {
                if (m_speechRecognizer != null)
                {
                    Debug.Log("[Azure ASR] Calling StopContinuousRecognitionAsync...");

                    await m_speechRecognizer.StopContinuousRecognitionAsync();

                    Debug.Log("[Azure ASR] StopContinuousRecognitionAsync completed.");

                    RecognizerDispose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Azure ASR] Stop failed (already closed?): {ex.GetType().Name}: {ex.Message}");
                RecognizerDispose();
            }
#else
            await Task.Delay(0);
#endif
        }

        private void RecognizerDispose()
        {
#if !UNITY_WEBGL
            if (m_speechRecognizer == null)
                return;

            m_speechRecognizer.Recognizing -= OnAzureSpeechPartialRecognizedEvent;
            m_speechRecognizer.Recognized -= OnAzureSpeechRecognizedEvent;
            m_speechRecognizer.Canceled -= OnAzureSpeechCanceledEvent;
            m_speechRecognizer.SessionStopped -= OnAzureSessionStoppedEvent;

            m_speechRecognizer.Dispose();
            m_speechRecognizer = null;
#endif
        }

        void Cleanup()
        {
#if !UNITY_WEBGL
            try { m_speechRecognizer?.StopContinuousRecognitionAsync(); } catch (Exception) { }
#endif

            // we do not dispose here, since we need to wait for Stop above to finish.
            // but we can't reliably do that when Unity is shutting down
            //RecognizerDispose();

            m_recognizedSpeech = null;
            m_recognizedSpeechPartial = null;
        }

        /// <summary>
        /// Verifies and requests microphone permission on mobile platforms.
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
    }
}
