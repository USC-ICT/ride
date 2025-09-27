namespace Ride.TextToSpeech
{
    /// <summary>
    /// Returns the index of the specified voice in the list.
    /// </summary>
    public delegate void LipsyncedTextToSpeechResult(string lipsyncXML, string audioFilePath);

    /// <summary>
    /// ITextToSpeechSystem variation that also provides a lipsync timing schedule
    /// Usually also want to implement ILipsyncMapper for ILipsyncedTextToSpeechSystem implementations
    /// </summary>
    public interface ILipsyncedTextToSpeechSystem : ITextToSpeechSystem
    {
        string lipsyncSchedule { get; }
        bool lipsyncProcessing { get; }

        /// <summary>
        /// Starts the full lipsynced TTS generation process for the given voice and text.
        /// </summary>
        /// <param name="voice">The selected TTS voice.</param>
        /// <param name="text">The input text to synthesize.</param>
        /// <param name="resultCallback">Callback to return the lipsync data and audio path.<see cref="LipsyncedTextToSpeechResult"/></param>
        void CreateTextToSpeech(string voice, string text, LipsyncedTextToSpeechResult resultCallback);

        /// <summary> Returns the index of the specified voice in the list. </summary>
        int GetVoiceIndex(string voice);
    }
}