using System;


namespace Ride.Sensing
{
    /// <summary>
    /// Encapsulates a multimodal sensing request (e.g. sending an image or audio clip).
    /// </summary>
    public class SensingRequest : ServiceRequest
    {
        public object request;
    }

    /// <summary>
    /// Represents the response returned from a multimodal sensing service.
    /// Typically holds a JSON-formatted string as raw output.
    /// </summary>
    public class SensingResponse : SystemResponse
    {
        public string response;

        public SensingResponse(string response) { this.response = response; }
    }

    /// <summary>
    /// Interface for calling multimodal sensing services (e.g., face attributes, emotions, etc.)
    /// </summary>
    public interface ISensingSystem : IRideSystem
    {
        /// <summary>
        /// Sends a sensing request to the system and receives a processed result.
        /// </summary>
        /// <param name="uri">The endpoint to send the request to.</param>
        /// <param name="input">The input content, typically an image or audio byte array.</param>
        /// <param name="onComplete">Callback invoked upon completion with the parsed <see cref="SensingResponse"/>.</param>
        void Request(string uri, Object input, Action<SensingResponse> onComplete);
    }    
}
