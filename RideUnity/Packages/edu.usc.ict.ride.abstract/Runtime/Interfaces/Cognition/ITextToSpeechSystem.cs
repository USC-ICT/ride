

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Delegate for basic TTS result with audio file path.
    /// </summary>
    public delegate void TextToSpeechResult(string audioFilePath);

    /// <summary>
    /// Interface to create text-to-speech audio files from text input.
    /// </summary>
    public interface ITextToSpeechSystem : IRideSystem
    {
        /// <summary> The path of the last generated audio file. </summary>
        string generatedAudioFilePath { get; }

        /// <summary> The length in seconds of the last generated audio. </summary>
        float generatedAudioLength { get; }

        /// <summary> Indicates whether TTS generation is in progress. </summary>
        bool textToSpeechProcessing { get; }

        /// <summary> Retrieves a list of all available voices. </summary>
        string[] GetAvailableVoices();

        /// <summary> Checks if a specific voice is supported. </summary>
        bool ContainsVoice(string voice);

        /// <summary>
        /// Starts the full lipsynced TTS generation process for the given voice and text.
        /// </summary>
        /// <param name="voice">The selected TTS voice.</param>
        /// <param name="text">The input text to synthesize.</param>
        /// <param name="resultCallback">Callback to return the lipsync data and audio path.<see cref="TextToSpeechResult"/></param>
        void CreateTextToSpeech(string voice, string text, TextToSpeechResult resultCallback);
    }
}
