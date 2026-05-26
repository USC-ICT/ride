using UnityEngine;
using VHAssets;

namespace Ride.SpeechRecognition
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    /// <summary>
    /// Windows-specific RIDE speech recognition implementation built on Unity's
    /// <see href="https://docs.unity3d.com/ScriptReference/Windows.Speech.DictationRecognizer.html">DictationRecognizer</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation adapts the Windows dictation API into the shared
    /// <see cref="SpeechRecognitionSystemUnity"/> contract used by the rest of the package.
    /// It listens to <c>DictationHypothesis</c>, <c>DictationResult</c>, <c>DictationComplete</c>,
    /// and <c>DictationError</c> callbacks, then forwards those events through the common RIDE speech
    /// recognition pipeline.
    /// </para>
    /// <para>
    /// Compared with the other provider implementations in this package, this class is the built-in
    /// Windows option for local dictation.
    /// </para>
    /// <para>
    /// Support still depends on the host OS and Unity's Windows speech subsystem being available at
    /// runtime. Timeout values exposed by <see cref="SpeechRecognitionSystemUnity.AutoSilenceTimeoutSeconds"/>
    /// and <see cref="SpeechRecognitionSystemUnity.InitialSilenceTimeoutSeconds"/> are forwarded directly
    /// into the underlying recognizer after initialization and whenever they are changed.
    /// </para>
    /// <para>
    /// On Windows 11, speech recognition also needs to be enabled in OS settings under
    /// Privacy &amp; security -&gt; Speech by turning Online speech recognition on.
    /// </para>
    /// <para>
    /// External references:
    /// <see href="https://docs.unity3d.com/ScriptReference/Windows.Speech.DictationRecognizer.html">Unity Scripting API: DictationRecognizer</see>,
    /// <see href="https://docs.unity3d.com/ScriptReference/Windows.Speech.DictationRecognizer.AutoSilenceTimeoutSeconds.html">AutoSilenceTimeoutSeconds</see>,
    /// <see href="https://docs.unity3d.com/ScriptReference/Windows.Speech.DictationRecognizer.InitialSilenceTimeoutSeconds.html">InitialSilenceTimeoutSeconds</see>.
    /// Related RIDE implementations:
    /// <see cref="SpeechRecognitionSystemAzure"/>,
    /// <see cref="SpeechRecognitionSystemAzureWebGL"/>,
    /// <see cref="SpeechRecognitionSystemOpenAI"/>.
    /// </para>
    /// </remarks>
    public class SpeechRecognitionSystemWindows : SpeechRecognitionSystemUnity
    {
        UnityEngine.Windows.Speech.DictationRecognizer m_dictationRecognizer;

        public override bool IsSupported => 
            VHUtils.IsWindows10OrGreater() && 
            m_dictationRecognizer != null && 
            m_dictationRecognizer.Status != UnityEngine.Windows.Speech.SpeechSystemStatus.Failed;

        /// <inheritdoc cref="SpeechRecognitionSystemUnity.AutoSilenceTimeoutSeconds"/>
        public override float AutoSilenceTimeoutSeconds
        {
            get => base.AutoSilenceTimeoutSeconds;
            set
            {
                base.AutoSilenceTimeoutSeconds = value;
                if (m_dictationRecognizer != null)
                    m_dictationRecognizer.AutoSilenceTimeoutSeconds = base.AutoSilenceTimeoutSeconds;
            }
        }

        /// <inheritdoc cref="SpeechRecognitionSystemUnity.InitialSilenceTimeoutSeconds"/>
        public override float InitialSilenceTimeoutSeconds
        {
            get => base.InitialSilenceTimeoutSeconds;
            set
            {
                base.InitialSilenceTimeoutSeconds = value;
                if (m_dictationRecognizer != null)
                    m_dictationRecognizer.InitialSilenceTimeoutSeconds = base.InitialSilenceTimeoutSeconds;
            }
        }

        /// <inheritdoc cref="SpeechRecognitionSystemUnity.SupportsContinuousRecognition"/>
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

            // Apply inspector-configured timeouts from base class.
            m_dictationRecognizer.AutoSilenceTimeoutSeconds = AutoSilenceTimeoutSeconds;
            m_dictationRecognizer.InitialSilenceTimeoutSeconds = InitialSilenceTimeoutSeconds;
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

        /// <summary>Indicates if Unity’s phrase recognition system is supported on this machine.</summary>
        public bool PhraseRecognitionSystemIsSupported =>
            VHUtils.IsWindows10OrGreater() && UnityEngine.Windows.Speech.PhraseRecognitionSystem.isSupported;

        /// <summary>Returns the current status of the phrase recognition system.</summary>
        public string PhraseRecognitionSystemStatus =>
            VHUtils.IsWindows10OrGreater()
                ? UnityEngine.Windows.Speech.PhraseRecognitionSystem.Status.ToString()
                : "Not Supported.";

        /// <summary>Generalized status string for external usage (e.g. UI/debug).</summary>
        public string Status => PhraseRecognitionSystemStatus;
    }
#else
    /// <summary>
    /// Non-Windows stub for the Windows speech recognition system.
    /// </summary>
    /// <remarks>
    /// This placeholder keeps the shared type available on platforms where Unity's Windows dictation
    /// APIs do not exist. It reports unsupported behavior and does not perform any real recognition.
    /// </remarks>
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
