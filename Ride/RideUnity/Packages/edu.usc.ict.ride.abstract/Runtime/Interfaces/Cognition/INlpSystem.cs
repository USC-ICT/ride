using System;

namespace Ride.NLP
{
    /// <summary>
    /// Base class for holing content to be send to NLP service.
    /// </summary>
    public class NlpRequest : ServiceRequest
    {
        public string content;
        public NlpRequest(string request)
        {
            this.content = request;
        }
    }

    /// <summary>
    /// Base class for storing NLP service response.
    /// </summary>
    public class NlpResponse : SystemResponse
    {
        public string[] content;

        public NlpResponse (string response)
        {
            content = new string[1];
            this.content[0] = response;
        }
        public NlpResponse(string[] response)
        {
            this.content = response;
        }
    }

    /// <summary>
    /// Struct intended storing the history of interaction between a user and a NLP service.
    /// </summary>
    public struct NlpInteraction
    {
        public string input;
        public string response;
        public DateTime inputTimestamp;
        public DateTime responseTimestamp;
    }

    /// <summary>
    /// Base interface for natural language processing system interfaces.
    /// </summary>
    public interface INlpSystem : IRideSystem
    {
        /// <summary>
        /// Requests NLP response based on provided input.
        /// </summary>
        /// <param name="uri">URL to send request to</param>
        /// <param name="content">Content input</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        void Request(NlpRequest request, Action<NlpResponse> onComplete);

        /// <summary>
        /// Set system prompt
        /// </summary>
        /// <param name="prompt"></param>
        void SetSystemPrompt(string prompt);
    }
}
