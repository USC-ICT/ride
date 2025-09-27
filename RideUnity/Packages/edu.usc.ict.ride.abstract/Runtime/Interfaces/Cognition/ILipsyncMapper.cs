namespace Ride.TextToSpeech
{
    /// <summary>
    /// Interface that maps visemes and word timings from generated speech
    /// </summary>
    public interface ILipsyncMapper
    {
        /// <summary>
        /// Generates a speech map with timing and viseme data.
        /// </summary>
        /// <param name="voice">The voice identifier.</param>
        /// <param name="text">The text to convert.</param>
        /// <param name="resultCallback">Callback to return generated map.< see cref = "AudioSpeechMap" /></param>
        void GenerateAudioSpeechMap(string voice, string text, System.Action<AudioSpeechMap> resultCallback);
    }
}