using System.Collections;
using System.Collections.Generic;

namespace Ride.Networking
{
    public enum WebRequestResult
    {
        // Ref: UnityWebRequest.Result

        InProgress = 0,           // UnityWebRequest.Result.InProgress
        Success = 1,              // UnityWebRequest.Result.Success
        ConnectionError = 2,      // UnityWebRequest.Result.ConnectionError
        ProtocolError = 3,        // UnityWebRequest.Result.ProtocolError
        DataProcessingError = 4,  // UnityWebRequest.Result.DataProcessingError
    }

    public delegate void OnWebRequestReceived<T>(WebRequestResult result, string error, T response);

    /// <summary>
    /// System that facilitates making common web requests (e.g., PUT with JSON payloads).
    /// </summary>
    public interface IWebRequesterSystem : IRideSystem
    {
        /// <summary>
        /// Sends a PUT request with a raw string body and invokes a callback when complete.
        /// </summary>
        /// <param name="url">The endpoint URL.</param>
        /// <param name="headers">Optional headers to include in the request.</param>
        /// <param name="bodyData">The body payload as a JSON-formatted string.</param>
        /// <param name="cb">Callback invoked upon request completion with the response as a string.</param>
        void Put(string url, Dictionary<string, string> headers, string bodyData, OnWebRequestReceived<string> cb = null);

        /// <summary>
        /// Sends a PUT request with a serializable input object, and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="Input">Type of the input body object (serialized as JSON).</typeparam>
        /// <typeparam name="Output">Type of the expected output object (deserialized from JSON).</typeparam>
        /// <param name="url">The endpoint URL.</param>
        /// <param name="headers">Optional headers to include in the request.</param>
        /// <param name="bodyData">The object to serialize into the request body.</param>
        /// <param name="cb">Callback invoked upon request completion with a deserialized output object.</param>
        void Put<Input, Output>(string url, Dictionary<string, string> headers, Input bodyData, OnWebRequestReceived<Output> cb = null);
    }
}
