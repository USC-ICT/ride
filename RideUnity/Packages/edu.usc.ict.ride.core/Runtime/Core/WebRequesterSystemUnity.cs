using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.Networking
{
    /// <summary>
    /// Unity-specific implementation of <see cref="WebRequesterSystem"/> using UnityWebRequest and coroutines.
    /// </summary>
    public class WebRequesterSystemUnity : WebRequesterSystem
    {
        /// <inheritdoc/>
        public override void Put(string url, Dictionary<string, string> headers, string bodyData, OnWebRequestReceived<string> cb = null) => 
            StartCoroutine(PutCoroutine(url, headers, bodyData, cb));

        /// <inheritdoc/>
        public override void Put<Input, Output>(string url, Dictionary<string, string> headers, Input bodyData, OnWebRequestReceived<Output> cb = null) =>
            StartCoroutine(PutCoroutine(url, headers, ConvertToJson(bodyData), cb));

        /// <summary>
        /// Executes a UnityWebRequest PUT request with the specified body and headers, then invokes the callback.
        /// </summary>
        /// <typeparam name="T">Expected return type (string or JSON-deserialized object).</typeparam>
        /// <param name="url">The URL to send the request to.</param>
        /// <param name="headers">Optional request headers.</param>
        /// <param name="body">The request body as a JSON-formatted string.</param>
        /// <param name="cb">Callback to invoke when the request completes.</param>
        private static IEnumerator PutCoroutine<T>(string url, Dictionary<string, string> headers, string body, OnWebRequestReceived<T> cb = null)
        {
            using (var request = GetRequest(url, headers, body))
            {
                yield return request.SendWebRequest();

                //Debug.Log(request.downloadHandler.text);

                if (typeof(T) == typeof(string))
                {
                    object result = request.downloadHandler.text;
                    cb?.Invoke(MapResult(request.result), request.error, (T)result);
                }
                else
                {
                    T result = RideIO.JsonDeserialize<T>(request.downloadHandler.text);
                    cb?.Invoke(MapResult(request.result), request.error, result);
                }
            }
        }

        /// <summary>
        /// Creates a UnityWebRequest PUT request with the specified headers and body.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="headers">Optional HTTP headers to include.</param>
        /// <param name="body">Request body in JSON format.</param>
        /// <returns>A configured UnityWebRequest ready for sending.</returns>
        private static UnityWebRequest GetRequest(string url, Dictionary<string, string> headers, string body)
        {
            var request = UnityWebRequest.Put(url, body);
            if (headers != null)
            {
                foreach (var header in headers)
                    request.SetRequestHeader(header.Key, header.Value);
            }

            return request;
        }

        private static WebRequestResult MapResult(UnityWebRequest.Result unityResult) =>
            unityResult switch
            {
                UnityWebRequest.Result.InProgress => WebRequestResult.InProgress,
                UnityWebRequest.Result.Success => WebRequestResult.Success,
                UnityWebRequest.Result.ConnectionError => WebRequestResult.ConnectionError,
                UnityWebRequest.Result.ProtocolError => WebRequestResult.ProtocolError,
                UnityWebRequest.Result.DataProcessingError => WebRequestResult.DataProcessingError,
                _ => MapResultHandleUnknown(unityResult),
            };

        private static WebRequestResult MapResultHandleUnknown(UnityWebRequest.Result result)
        {
            Debug.LogError($"WebRequesterSystemUnity.MapResult() - unknown result: {result}");
            return WebRequestResult.DataProcessingError;
        }
    }
}
