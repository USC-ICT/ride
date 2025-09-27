using System;

namespace Ride.NLP
{
    /// <summary>
    /// Holds NLP Sentiment text analytics request to be sent, typically a block of user input text.
    /// </summary>
    public class NlpSentimentRequest : NlpRequest
    {
        public string text;

        public NlpSentimentRequest(string text) : base(text)
        {
            this.text = text;
        }
    }

    /// <summary>
    /// Holds NLP Sentiment text analytics response, including main sentiment
    /// and scores for positive, neutral, and negative sentiments.
    /// </summary>
    public class NlpSentimentResponse : NlpResponse
    {
        public string mainSentiment;
        public double positiveScore;
        public double neutralScore;
        public double negativeScore;

        public NlpSentimentResponse(string response) : base(response) { }

        public NlpSentimentResponse(string response, string mainSentiment) : base(response)
        {
            this.mainSentiment = mainSentiment;
        }

        public NlpSentimentResponse(string response, string mainSentiment, double positiveScore, double neutralScore, double negativeScore) : base(response)
        {
            this.mainSentiment = mainSentiment;
            this.positiveScore = positiveScore;
            this.neutralScore = neutralScore;
            this.negativeScore = negativeScore;
        }
    }

    /// <summary>
    /// Interface for calling NLP Sentiment text analytics service.
    /// </summary>
    public interface INlpSentimentSystem : INlpSystem
    {
        /// <summary>
        /// Analyses sentiment of a given text.
        /// </summary>
        /// <param name="text">Text input to be analyzed</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        void AnalyzeSentiment(string text, Action<NlpResponse> onComplete);
    }
}
