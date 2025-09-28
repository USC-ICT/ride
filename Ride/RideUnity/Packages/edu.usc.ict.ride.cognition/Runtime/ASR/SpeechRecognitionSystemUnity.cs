using System;
using UnityEngine;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Abstract base class for Unity-based speech recognition systems.
    /// Handles microphone selection, state management, and event triggering.
    /// </summary>
    public abstract class SpeechRecognitionSystemUnity : RideSystemMonoBehaviour, ISpeechRecognitionSystem
    {
        public abstract bool IsSupported { get; }

        public abstract float AutoSilenceTimeoutSeconds { get; set; }
        public abstract float InitialSilenceTimeoutSeconds { get; set; }
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
        public virtual void OnPartialSpeechRecognized(string result, float confidence = 1)
        {
            if (!string.IsNullOrEmpty(result))
            {
                RecognizedSpeech = result;
                Confidence = confidence;
            }

            PartialSpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(result, confidence));
        }

        /// <summary>
        /// Called when a final recognition result is available.
        /// Updates internal state and invokes the <see cref="SpeechRecognized"/> event.
        /// </summary>
        /// <param name="result">The final recognized text.</param>
        /// <param name="confidence">Confidence score (default is 1).</param>
        public virtual void OnSpeechRecognized(string result, float confidence = 1)
        {
            if (!string.IsNullOrEmpty(result))
            {
                RecognizedSpeech = result;
                Confidence = confidence;
            }

            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(result, confidence));

            if (!SupportsContinuousRecognition)
                StopRecognizing();
        }
    }
}
