using System;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Interface for an automatic speech recognition system, providing functionality
    /// to start and stop recognition, receive partial and final results, and configure microphone input.
    /// </summary>
    public interface ISpeechRecognitionSystem : IRideSystem
    {
        /// <summary>
        /// Indicates whether the speech recognition system is supported on the current platform.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Gets or sets the timeout (in seconds) after which recognition stops if no speech is detected.
        /// </summary>
        float AutoSilenceTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout (in seconds) to wait for the first spoken word before aborting recognition.
        /// </summary>
        float InitialSilenceTimeoutSeconds { get; set; }

        /// <summary>
        /// Indicates whether the system supports continuous recognition.
        /// </summary>
        bool SupportsContinuousRecognition { get; }

        /// <summary>
        /// Gets whether recognition is currently active.
        /// </summary>
        bool IsRecognizing { get; }

         /// <summary>
        /// Gets the most recently recognized speech.
        /// </summary>
        string RecognizedSpeech { get; }

        /// <summary>
        /// Gets the confidence score of the most recently recognized speech result.
        /// </summary>
        float Confidence { get; }

        /// <summary>
        /// Gets the name of the currently selected microphone.
        /// </summary>
        string SelectedMicrophone { get; }

        /// <summary>
        /// Event triggered when speech recognition has started.
        /// </summary>
        event EventHandler RecognizingStarted;

        /// <summary>
        /// Event triggered when speech recognition has stopped.
        /// </summary>
        event EventHandler RecognizingStopped;

        /// <summary>
        /// Event triggered when a partial speech result is available.
        /// </summary>
        event EventHandler<SpeechRecognizedEventArgs> PartialSpeechRecognized;

        /// <summary>
        /// Event triggered when a final speech result has been recognized.
        /// </summary>
        event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        /// <summary>
        /// Sets the microphone to be used for input, by name.
        /// </summary>
        /// <param name="deviceName">The name of the microphone device to use. If null or empty, the default microphone is selected.</param>
        void SetMicrophone(string deviceName);

         /// <summary>
        /// Starts speech recognition. May trigger the <see cref="RecognizingStarted"/> event.
        /// </summary>
        void StartRecognizing();

        /// <summary>
        /// Stops speech recognition. May trigger the <see cref="RecognizingStopped"/> event.
        /// </summary>
        void StopRecognizing();
    }

    /// <summary>
    /// Contains information about a recognized speech result, including the text and confidence score.
    /// </summary>
    public class SpeechRecognizedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the recognized text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the confidence level of the recognition result (0 to 1).
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Gets the detected or configured language for the recognition result when available.
        /// Empty means the provider did not supply a language.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognizedEventArgs"/> class.
        /// </summary>
        /// <param name="text">The recognized speech text.</param>
        /// <param name="confidence">The confidence score (0-1) of the recognition result.</param>
        /// <param name="language">The detected or configured language tag, if available.</param>
        public SpeechRecognizedEventArgs(string text, float confidence, string language = "")
        {
            Text = text;
            Confidence = confidence;
            Language = language ?? string.Empty;
        }

        /// Initializes a new instance of the <see cref="SpeechRecognizedEventArgs"/> class.
        /// </summary>
        /// <param name="text">The recognized speech text.</param>
        /// <param name="confidence">The confidence score (0-1) of the recognition result.</param>
        public SpeechRecognizedEventArgs(string text, float confidence)
        {
            Text = text;
            Confidence = confidence;
        }
    }
}
