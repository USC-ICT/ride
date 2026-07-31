using System;

namespace Ride
{
    /// <summary>Global settings for text-based language detection fallback.</summary>
    [Serializable]
    public struct LanguageDetectionSettings
    {
        /// <summary>Enables fallback language detection when no provider language is available.</summary>
        public bool enabled;

        /// <summary>Provider used by the fallback language detector.</summary>
        public string provider;

        /// <summary>Model used by the configured provider.</summary>
        public string model;

        /// <summary>Minimum confidence required before using the detected language.</summary>
        public float minimumConfidence;

        public static LanguageDetectionSettings Default => new LanguageDetectionSettings
        {
            enabled = false,
            provider = "OpenAI",
            model = "gpt-4o-mini",
            minimumConfidence = 0.45f,
        };
    }
}
