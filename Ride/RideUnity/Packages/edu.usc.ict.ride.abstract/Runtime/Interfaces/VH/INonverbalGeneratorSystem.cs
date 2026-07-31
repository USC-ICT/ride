namespace Ride
{
    /// <summary>
    /// Provides nonverbal-behavior generation services for a character from a block of text.
    /// Implementations may generate behavior through different backends, such as an external process
    /// or a service call, but all expose the same high-level request surface to Ride systems.
    /// </summary>
    public interface INonverbalGeneratorSystem : IRideSystem, IExternalProcess
    {
        // TODO: Refactor this interface so it no longer inherits IExternalProcess.
        // Only ExternalProcessNVBG should expose IExternalProcess behavior.
        // RestfulNonverbalBehaviorGeneratorSystem and other in-process implementations should remain plain IRideSystem-based systems.


        /// <summary>
        /// Represents a callback invoked when nonverbal-behavior generation completes.
        /// </summary>
        /// <param name="result">The generated nonverbal behavior payload, typically as XML or BML text.</param>
        public delegate void NonverbalBehaviorResult(string result);

        /// <summary>
        /// Requests generated nonverbal behavior for the supplied character and utterance text.
        /// </summary>
        /// <param name="characterName">The character whose nonverbal behavior should be generated.</param>
        /// <param name="text">The speech text to enrich with nonverbal behavior.</param>
        /// <param name="resultCallback">Callback invoked when generation finishes.</param>
        void GetNonverbalBehavior(string characterName, string text, NonverbalBehaviorResult resultCallback);

        /// <summary>
        /// Requests generated nonverbal behavior for the supplied character and utterance text, using a language tag
        /// when the implementation supports language-specific rule routing.
        /// </summary>
        /// <param name="characterName">The character whose nonverbal behavior should be generated.</param>
        /// <param name="text">The speech text to enrich with nonverbal behavior.</param>
        /// <param name="languageTag">Detected or configured language tag for the utterance. Empty falls back to English.</param>
        /// <param name="resultCallback">Callback invoked when generation finishes.</param>
        void GetNonverbalBehavior(string characterName, string text, string languageTag, NonverbalBehaviorResult resultCallback);

        /// <summary>
        /// Starts or warms up generation resources for the supplied character when supported by the implementation.
        /// </summary>
        /// <param name="characterName">The character to initialize.</param>
        void StartProcess(string characterName);
    }
}
