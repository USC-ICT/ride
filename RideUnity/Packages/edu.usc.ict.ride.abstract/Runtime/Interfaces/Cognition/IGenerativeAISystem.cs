using System;
using Ride;

namespace Ride.GenerativeAI
{
    /// <summary>
    /// Holds request content to be send to Generative AI service, typically JSON string.
    /// </summary>
    public class GenerativeAIRequest : ServiceRequest
    {
        public string request;
    }

    /// <summary>
    /// Holds Generative AI service response, typically JSON string.
    /// </summary>
    public class GenerativeAIResponse : SystemResponse
    {
        public string response;

        public GenerativeAIResponse (string response)
        {
            this.response = response;
        }
    }

    /// <summary>
    /// Interface for calling NLP services
    /// </summary>
    public interface IGenerativeAISystem : IRideSystem
    {        
        /// <summary>
        /// Requests Generative AI response based on provided input.
        /// </summary>
        /// <param name="uri">URL to send request to</param>
        /// <param name="content">Content input</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        void Request(string uri, string content, Action<GenerativeAIResponse> onComplete);
    }
}
