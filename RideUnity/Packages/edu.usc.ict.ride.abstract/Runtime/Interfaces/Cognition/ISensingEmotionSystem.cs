using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Request wrapper for analyzing emotions from visual/audio input.
    /// </summary>
    [Serializable]
    public class SensingEmotionRequest : SensingRequest
    {
        public Object input;

        public SensingEmotionRequest(Object input)
        {
            this.input = input;
        }
    }

    /// <summary>
    /// Response containing intensity scores for multiple human emotions.
    /// </summary>
    [Serializable]
    public class SensingEmotionResponse : SensingResponse
    {
        public double anger;
        public double contempt;
        public double disgust;
        public double fear;
        public double happiness;
        public double neutral;
        public double sadness;
        public double surprise;

        public SensingEmotionResponse(string response) : base(response) { }

        public SensingEmotionResponse(string response, double anger, double contempt, double disgust, double fear, double happiness, double neutral, double sadness, double surprise) : base(response)
        {
            this.anger = anger;
            this.contempt = contempt;
            this.disgust = disgust;
            this.fear = fear;
            this.happiness = happiness;
            this.neutral = neutral;
            this.sadness = sadness;
            this.surprise = surprise;
        }
    }

    /// <summary>
    /// Interface for analyzing emotions based on facial or vocal expression.
    /// </summary>
    public interface ISensingEmotionSystem : ISensingSystem
    {
        /// <summary>
        /// Sends data for emotion analysis and invokes callback with emotional intensity scores.
        /// </summary>
        /// <param name="input">The image or audio to be analyzed.</param>
        /// <param name="onComplete">Callback to execute when analysis completes.</param>
        void AnalyzeEmotions(Object input, Action<SensingResponse> onComplete);
    }
}
