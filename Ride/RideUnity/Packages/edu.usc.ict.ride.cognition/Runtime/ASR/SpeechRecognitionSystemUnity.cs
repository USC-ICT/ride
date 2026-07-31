using System;
using UnityEngine;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Abstract base class for Unity-facing speech recognition system implementations in RIDE.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class centralizes the common runtime contract shared by provider-specific implementations
    /// such as <see cref="SpeechRecognitionSystemWindows"/>, <see cref="SpeechRecognitionSystemAzure"/>,
    /// <see cref="SpeechRecognitionSystemAzureWebGL"/>, and <see cref="SpeechRecognitionSystemOpenAI"/>.
    /// </para>
    /// <para>
    /// It is responsible for microphone selection bookkeeping, recognition lifecycle state, shared
    /// recognized-text/confidence storage, and the common event pipeline that higher-level RIDE systems
    /// listen to regardless of which recognition backend is active.
    /// </para>
    /// <para>
    /// Concrete subclasses are expected to bridge their provider-specific SDK callbacks into
    /// <see cref="OnRecognizingStarted"/>, <see cref="OnRecognizingStopped"/>,
    /// <see cref="OnPartialSpeechRecognized(string, float)"/>, and
    /// <see cref="OnSpeechRecognized(string, float)"/> so the rest of the application can consume a
    /// consistent speech-recognition interface.
    /// </para>
    /// </remarks>
    public abstract class SpeechRecognitionSystemUnity : RideSystemMonoBehaviour, ISpeechRecognitionSystem
    {
        [SerializeField, Min(0f), Tooltip(
            "Auto Silence Timeout (seconds).\n" +
            "After speech has started, recognition will stop if no sound is detected for this duration.\n" +
            "Controls how long pauses between words or sentences are allowed.")]
        float m_autoSilenceTimeoutSeconds = 0.7f;

        [SerializeField, Min(0f), Tooltip(
            "Initial Silence Timeout (seconds).\n" +
            "Maximum time to wait for the user to begin speaking after recognition starts.\n" +
            "If no speech is detected within this time, recognition aborts.")]
        float m_initialSilenceTimeoutSeconds = 10f;

        public abstract bool IsSupported { get; }

        /// <summary>
        /// Gets or sets the amount of silence, in seconds, that is allowed after speech has already
        /// started before the active recognition session is considered complete.
        /// </summary>
        /// <remarks>
        /// This value typically controls how tolerant a provider is of pauses between words or sentences.
        /// Provider implementations may forward this value directly into an underlying SDK, clamp it to a
        /// supported range, or ignore it entirely if the backend does not expose an equivalent setting.
        /// </remarks>
        public virtual float AutoSilenceTimeoutSeconds
        {
            get => m_autoSilenceTimeoutSeconds;
            set => m_autoSilenceTimeoutSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the amount of silence, in seconds, that is allowed immediately after recognition
        /// starts before the session gives up waiting for the speaker to begin talking.
        /// </summary>
        /// <remarks>
        /// This timeout applies to the initial wait-for-speech phase rather than pauses after speech has
        /// already begun. Provider implementations may map this value onto their native SDK configuration
        /// when such a setting exists.
        /// </remarks>
        public virtual float InitialSilenceTimeoutSeconds
        {
            get => m_initialSilenceTimeoutSeconds;
            set => m_initialSilenceTimeoutSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets a value indicating whether this recognition backend is designed to keep listening for
        /// additional utterances after a final result is produced.
        /// </summary>
        /// <remarks>
        /// When this value is <see langword="false"/>, the shared base implementation will stop the
        /// recognition session automatically after <see cref="OnSpeechRecognized(string, float)"/> raises
        /// a final result. Backends that stream or continuously dictate input should return
        /// <see langword="true"/>.
        /// </remarks>
        public abstract bool SupportsContinuousRecognition { get; }

        public virtual bool IsRecognizing { get; protected set; }
        public virtual string RecognizedSpeech { get; protected set; }
        public virtual float Confidence { get; protected set; }

        public string SelectedMicrophone { get; protected set; }

        public event EventHandler RecognizingStarted;
        public event EventHandler RecognizingStopped;
        public event EventHandler<SpeechRecognizedEventArgs> PartialSpeechRecognized;
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            if (string.IsNullOrEmpty(SelectedMicrophone))
                SetMicrophone(null);
        }

        /// <inheritdoc/>
        public virtual void SetMicrophone(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
#if UNITY_WEBGL
                SelectedMicrophone = string.Empty;
#else
                if (Microphone.devices.Length > 0)
                    SelectedMicrophone = Microphone.devices[0];
                else
                    SelectedMicrophone = string.Empty;
#endif
            }
            else
            {
                SelectedMicrophone = deviceName;
            }
        }

        /// <inheritdoc/>
        public void StartRecognizing()
        {
            if (!IsSupported || IsRecognizing)
                return;

            IsRecognizing = true;

            OnRecognizingStarted();
        }

        /// <inheritdoc/>
        public void StopRecognizing()
        {
            if (!IsRecognizing)
                return;

            IsRecognizing = false;

            OnRecognizingStopped();
        }

        /// <summary>
        /// Triggers the <see cref="RecognizingStarted"/> event and clears old recognition data.
        /// </summary>
        public virtual void OnRecognizingStarted()
        {
            RecognizedSpeech = string.Empty;
            Confidence = 0;
            RecognizingStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Triggers the <see cref="RecognizingStopped"/> event.
        /// </summary>
        public virtual void OnRecognizingStopped()
        {
            RecognizingStopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when a partial recognition result is available.
        /// Updates internal state and invokes the <see cref="PartialSpeechRecognized"/> event.
        /// </summary>
        /// <param name="result">The partial recognized text.</param>
        /// <param name="confidence">Confidence score.</param>
        public virtual void OnPartialSpeechRecognized(string result, float confidence = 1, string language = "")
        {
            if (!string.IsNullOrEmpty(result))
            {
                RecognizedSpeech = result;
                Confidence = confidence;
            }

            PartialSpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(result, confidence, language));
        }

        /// <summary>
        /// Called when a final recognition result is available.
        /// Updates internal state and invokes the <see cref="SpeechRecognized"/> event.
        /// </summary>
        /// <param name="result">The final recognized text.</param>
        /// <param name="confidence">Confidence score (default is 1).</param>
        public virtual void OnSpeechRecognized(string result, float confidence = 1, string language = "")
        {
            if (!string.IsNullOrEmpty(result))
            {
                RecognizedSpeech = result;
                Confidence = confidence;
            }

            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(result, confidence, language));

            if (!SupportsContinuousRecognition)
                StopRecognizing();
        }
    }
}
