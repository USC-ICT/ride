using System.Collections;
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
    /// SpeechRecognitionSystem utilzing Microsofts Cognitive services Speech SDK
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

        public override bool IsSupported => true;
        public override float AutoSilenceTimeoutSeconds { get; set; } = 3;
        public override float InitialSilenceTimeoutSeconds { get; set; } = 10;

        public override bool SupportsContinuousRecognition => true;

#if PLATFORM_ANDROID || PLATFORM_IOS
        // Required to manifest microphone permission, cf.
        // https://docs.unity3d.com/Manual/android-manifest.html
#endif

        //bool m_micPermissionGranted = false;

#if !UNITY_WEBGL
        string m_apiKey;
        string m_region;
        SpeechConfig m_speechConfig;
        AudioConfig m_audioConfig;
        SpeechRecognizer m_speechRecognizer;
#endif

        private string m_recognizedSpeech;
        private string m_recognizedSpeechPartial;
        private SpeechRecognitionType m_currentInputSource = SpeechRecognitionType.MICROPHONE;
        private string m_filePath;

        public SpeechRecognitionType CurrentInputSource => m_currentInputSource;
        public string FilePath => m_filePath;

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

#if !UNITY_WEBGL
            ConfigurationSystemUnity configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_apiKey = configSystem.config.azureSpeech.apiKey;
            m_region = configSystem.config.azureSpeech.region;
            m_speechConfig = SpeechConfig.FromSubscription(m_apiKey, m_region);
            m_audioConfig = AudioConfig.FromDefaultMicrophoneInput();
#endif

            CheckMicrophonePermissions();
        }

        /// <inheritdoc/>
        public override void SetMicrophone(string deviceName)
        {
            base.SetMicrophone(deviceName);
            SetInputSource(SpeechRecognitionType.MICROPHONE);
        }

        /// <inheritdoc/>
        public void SetInputSource(SpeechRecognitionType sourceType, string filePath = null)
        {
            m_currentInputSource = sourceType;
            m_filePath = filePath;
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            StartCoroutine(CheckRecognizedSpeechRoutine());
            StartSpeechRecognitionAsync();
            base.OnRecognizingStarted();
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            StopSpeechRecognitionAsync();
            base.OnRecognizingStopped();
        }

#if !UNITY_WEBGL
        /// <summary>
        /// Event handler for receiving partial recognition results.
        /// </summary>
        protected void OnSpeechPartialRecognizedEvent(object sender, SpeechRecognitionEventArgs e)
        {
            m_recognizedSpeechPartial = e.Result.Text;
        }

        /// <summary>
        /// Event handler for receiving final recognition results.
        /// </summary>
        protected void OnSpeechRecognizedEvent(object sender, SpeechRecognitionEventArgs e)
        {
            m_recognizedSpeech = e.Result.Text;
        }
#endif

        /// <summary>
        /// Starts the Azure SDK speech recognition process using the current input source.
        /// </summary>
        protected async void StartSpeechRecognitionAsync()
        {
#if !UNITY_WEBGL
            try
            {
                m_speechConfig = SpeechConfig.FromSubscription(m_apiKey, m_region);

                m_speechConfig.SetProperty(
                    PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs,
                    ((int)(InitialSilenceTimeoutSeconds * 1000)).ToString());
                m_speechConfig.SetProperty(
                    PropertyId.Speech_SegmentationSilenceTimeoutMs,
                    ((int)(AutoSilenceTimeoutSeconds * 1000)).ToString());

                m_audioConfig = BuildAudioConfigWithFallback();

                m_speechRecognizer = new SpeechRecognizer(m_speechConfig, m_audioConfig);

                m_speechRecognizer.Recognizing += OnSpeechPartialRecognizedEvent;
                m_speechRecognizer.Recognized += OnSpeechRecognizedEvent;

                Confidence = 1;
                await m_speechRecognizer.StartContinuousRecognitionAsync();
            }
            catch (System.Exception ex)
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
                StopSpeechRecognitionAsync();
        }

#if !UNITY_WEBGL
        private AudioConfig BuildAudioConfigWithFallback()
        {
            switch (CurrentInputSource)
            {
                case SpeechRecognitionType.FILE:
                    if (!string.IsNullOrEmpty(FilePath))
                        return AudioConfig.FromWavFileInput(FilePath);
                    Debug.LogWarning("[Azure ASR] FILE mode selected but FilePath is empty. Falling back to microphone.");
                    goto case SpeechRecognitionType.MICROPHONE;

                case SpeechRecognitionType.SPEAKER:
                    // Not all platforms support this; if it fails, fall back to mic.
                    try { return AudioConfig.FromDefaultSpeakerOutput(); }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Azure ASR] Default speaker output not available: {e.Message}. Falling back to microphone.");
                        goto case SpeechRecognitionType.MICROPHONE;
                    }

                case SpeechRecognitionType.MICROPHONE:
                default:
                    // IMPORTANT: Unity’s Microphone.devices names don’t always match what Azure expects.
                    // Prefer default mic to avoid SPXERR_MIC_NOT_AVAILABLE from mismatched device names.
                    try
                    {
                        if (!string.IsNullOrEmpty(SelectedMicrophone))
                            return AudioConfig.FromMicrophoneInput(SelectedMicrophone);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Azure ASR] Named microphone '{SelectedMicrophone}' not available: {e.Message}. Using default mic.");
                    }
                    return AudioConfig.FromDefaultMicrophoneInput();
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
                    await m_speechRecognizer.StopContinuousRecognitionAsync();
                    m_speechRecognizer.Recognizing -= OnSpeechPartialRecognizedEvent;
                    m_speechRecognizer.Recognized -= OnSpeechRecognizedEvent;
                    m_speechRecognizer.Dispose();
                    m_speechRecognizer = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Azure ASR] Stop failed (already closed?): {ex.GetType().Name}: {ex.Message}");
            }

            // If you explicitly created m_audioConfig and want to release it:
            try { m_audioConfig?.Dispose(); } catch { }
            m_audioConfig = null;

#else
            await Task.Delay(0);
#endif
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
