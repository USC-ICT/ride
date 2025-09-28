using UnityEngine;
using VHAssets;

namespace Ride.SpeechRecognition
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    /// <summary>
    /// Windows-only Speech Recognition implementation using UnityEngine.Windows.Speech.DictationRecognizer
    /// </summary>
    public class SpeechRecognitionSystemWindows : SpeechRecognitionSystemUnity
    {
        UnityEngine.Windows.Speech.DictationRecognizer m_dictationRecognizer;

        public override bool IsSupported => VHUtils.IsWindows10OrGreater() && m_dictationRecognizer != null && m_dictationRecognizer.Status != UnityEngine.Windows.Speech.SpeechSystemStatus.Failed;
        public override float AutoSilenceTimeoutSeconds { get => m_dictationRecognizer.AutoSilenceTimeoutSeconds; set => m_dictationRecognizer.AutoSilenceTimeoutSeconds = value; }
        public override float InitialSilenceTimeoutSeconds { get => m_dictationRecognizer.InitialSilenceTimeoutSeconds; set => m_dictationRecognizer.InitialSilenceTimeoutSeconds = value; }
        public override bool SupportsContinuousRecognition => true;

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            m_dictationRecognizer = new UnityEngine.Windows.Speech.DictationRecognizer();

            m_dictationRecognizer.DictationHypothesis += (text) =>
            {
                Debug.Log($"Dictation hypothesis: {text}");

                OnPartialSpeechRecognized(text);
            };

            m_dictationRecognizer.DictationResult += (text, confidence) =>
            {
                Debug.Log($"Dictation result: {text}");

                OnSpeechRecognized(text, confidence switch
                {
                    UnityEngine.Windows.Speech.ConfidenceLevel.High => 1,
                    UnityEngine.Windows.Speech.ConfidenceLevel.Medium => 0.66f,
                    UnityEngine.Windows.Speech.ConfidenceLevel.Low => 0.33f,
                    UnityEngine.Windows.Speech.ConfidenceLevel.Rejected => 0,
                    _ => 0
                });
            };

            m_dictationRecognizer.DictationComplete += (cause) =>
            {
                if (cause != UnityEngine.Windows.Speech.DictationCompletionCause.Complete)
                {
                    Debug.LogError($"Dictation completed unsuccessfully: {cause}.");

                    StopRecognizing();
                }
            };

            m_dictationRecognizer.DictationError += (error, hresult) =>
            {
                Debug.LogError($"Dictation error: {error}; HResult = {hresult}.");

                StopRecognizing();
            };

            m_dictationRecognizer.InitialSilenceTimeoutSeconds = 999;
        }

        /// <inheritdoc/>
        public override void SystemShutdown()
        {
            base.SystemShutdown();

            m_dictationRecognizer?.Dispose();
            m_dictationRecognizer = null;
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            base.OnRecognizingStarted();

            if (IsSupported)
                m_dictationRecognizer?.Start();
            else
                OnSpeechRecognized("This system is not configured properly to use Speech Recognition", 0);
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            base.OnRecognizingStopped();

            m_dictationRecognizer?.Stop();
        }
        /// <summary>
        /// Indicates if Unity’s phrase recognition system is supported on this machine.
        /// </summary>
        public bool PhraseRecognitionSystemIsSupported =>
            VHUtils.IsWindows10OrGreater() && UnityEngine.Windows.Speech.PhraseRecognitionSystem.isSupported;

        /// <summary>
        /// Returns the current status of the phrase recognition system.
        /// </summary>
        public string PhraseRecognitionSystemStatus =>
            VHUtils.IsWindows10OrGreater()
                ? UnityEngine.Windows.Speech.PhraseRecognitionSystem.Status.ToString()
                : "Not Supported.";

        /// <summary>
        /// Generalized status string for external usage (e.g. UI/debug).
        /// </summary>
        public string Status => PhraseRecognitionSystemStatus;
    }
#else
    // Non-Windows stub
    public class SpeechRecognitionSystemWindows : SpeechRecognitionSystemUnity
    {
        public override bool IsSupported => false;
        public override float AutoSilenceTimeoutSeconds { get => 0; set => _ = value; }
        public override float InitialSilenceTimeoutSeconds { get => 0; set => _ = value; }
        public override bool SupportsContinuousRecognition => false;
        public bool PhraseRecognitionSystemIsSupported => false;
        public string PhraseRecognitionSystemStatus => "Not Supported.";
        public string Status => PhraseRecognitionSystemStatus;
    }
#endif
}
