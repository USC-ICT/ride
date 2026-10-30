using System.Collections;
using System.Collections.Generic;

namespace Ride.Networking
{
    /// <summary>Describes the completion state of a RIDE web request.</summary>
    /// <remarks>Values mirror Unity's <c>UnityWebRequest.Result</c> values for adapter compatibility.</remarks>
    public enum WebRequestResult
    {
        // Ref: UnityWebRequest.Result

        /// <summary>The request is still running.</summary>
        InProgress = 0,           // UnityWebRequest.Result.InProgress

        /// <summary>The request completed successfully.</summary>
        Success = 1,              // UnityWebRequest.Result.Success

        /// <summary>The request could not establish or maintain a connection.</summary>
        ConnectionError = 2,      // UnityWebRequest.Result.ConnectionError

        /// <summary>The server returned an HTTP error response.</summary>
        ProtocolError = 3,        // UnityWebRequest.Result.ProtocolError

        /// <summary>The received data could not be processed.</summary>
        DataProcessingError = 4,  // UnityWebRequest.Result.DataProcessingError
    }

    /// <summary>Represents the callback invoked when an asynchronous web request completes.</summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    /// <param name="result">The request completion state.</param>
    /// <param name="error">The transport or processing error, or an empty value when successful.</param>
    /// <param name="response">The response payload, when available.</param>
    public delegate void OnWebRequestReceived<T>(WebRequestResult result, string error, T response);

    /// <summary>Provides JSON PUT requests used by RIDE services such as AWS file storage.</summary>
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
