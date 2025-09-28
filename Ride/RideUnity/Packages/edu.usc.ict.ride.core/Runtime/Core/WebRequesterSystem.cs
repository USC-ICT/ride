using System.Collections;
using System.Collections.Generic;

namespace Ride.Networking
{
    /// <summary>
    /// Base class that provides JSON serialization helpers and abstract PUT logic for implementing IWebRequesterSystem.
    /// </summary>
    /// <inheritdoc cref="IWebRequesterSystem"/>
    public abstract class WebRequesterSystem : RideSystemMonoBehaviour, IWebRequesterSystem
    {
        /// <summary>
        /// Serializes the given object to a JSON string.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <returns>JSON representation of the object.</returns>
        public static string ConvertToJson<T>(T data) => RideIO.JsonSerialize(data);

        /// <summary>
        /// Serializes the given object to a UTF-8 encoded JSON byte array.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <returns>UTF-8 encoded JSON byte array.</returns>
        public static byte[] ConvertToJsonBytes<T>(T data) => System.Text.Encoding.UTF8.GetBytes(ConvertToJson(data));

        /// <inheritdoc/>
        public abstract void Put(string url, Dictionary<string, string> headers, string bodyData, OnWebRequestReceived<string> cb = null);

        /// <inheritdoc/>
        public abstract void Put<Input, Output>(string url, Dictionary<string, string> headers, Input bodyData, OnWebRequestReceived<Output> cb = null);
    }
}
