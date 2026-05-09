using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ride
{
    /// <summary>
    /// Provides utilities for path handling, Unity application paths, JSON serialization,
    /// UnityWebRequest-based networking, and generic HTTP requests. Also includes helpers for
    /// file system access and connectivity checks. Designed for cross-platform support in RIDE
    /// across editor and runtime contexts.
    /// </summary>
    public static class RideIO
    {
        #region Paths and Environment

        /// <summary>
        /// Combines path segments and normalizes to forward slashes for Unity compatibility.
        /// </summary>
        /// <param name="paths">the paths to combine via slashes</param>
        /// <returns>The combined path that uses the operating system appropriate slash</returns>
        public static string PathCombine(params string[] paths) => Path.Combine(paths).Replace("\\", "/");

        public static string ApplicationDataPath() => UnityEngine.Application.dataPath;
        public static string ApplicationPersistentDataPath() => UnityEngine.Application.persistentDataPath;

        #endregion

        #region JSON Serialization Helpers

        /// <summary>
        /// Default JSON settings used for Ride object graphs that may contain interface-typed values,
        /// derived runtime types, and shared object references.
        /// </summary>
        /// <remarks>
        /// This configuration is suited for internal Ride persistence scenarios where preserving
        /// object identity and limited type metadata is more important than producing minimal JSON.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/PreserveObjectReferences.htm
        /// https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
        /// </remarks>
        static readonly JsonSerializerSettings m_jsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,  // https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
            //TypeNameHandling = TypeNameHandling.All,  // https://skrift.io/articles/archive/bulletproof-interface-deserialization-in-jsonnet
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        /// <summary>
        /// JSON settings for serializing Ride data without Newtonsoft object-reference metadata.
        /// </summary>
        /// <remarks>
        /// Callers use this when they still need automatic type-name support for polymorphic values,
        /// but the payload must remain cleaner and easier for external services to consume.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/PreserveObjectReferences.htm
        /// https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
        /// </remarks>
        static readonly JsonSerializerSettings m_jsonSerializerSettingsNoObjRef = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,  // no object referencing
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,  // https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
            //TypeNameHandling = TypeNameHandling.All,  // https://skrift.io/articles/archive/bulletproof-interface-deserialization-in-jsonnet
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        /// <summary>
        /// JSON settings for plain payloads that should omit both object-reference metadata and type-name metadata.
        /// </summary>
        /// <remarks>
        /// This is primarily intended for simple DTO-style request and response bodies exchanged with
        /// external web services that expect conventional JSON rather than Ride-specific serialization hints.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
        /// </remarks>
        static readonly JsonSerializerSettings m_jsonSerializerSettingsNoNameHandling = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,  // no object referencing
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.None,  // https://www.newtonsoft.com/json/help/html/SerializeTypeNameHandling.htm
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        };

        /// <summary>Gets the default Ride JSON serialization settings.</summary>
        /// <remarks>See <see cref="m_jsonSerializerSettings"/> for the detailed behavior and external documentation links.</remarks>
        /// <returns>The shared serializer settings that preserve type metadata and object references.</returns>
        public static JsonSerializerSettings GetJsonConfig() => m_jsonSerializerSettings;

        /// <summary>Gets JSON serialization settings that omit object-reference metadata.</summary>
        /// <remarks>See <see cref="m_jsonSerializerSettingsNoObjRef"/> for the detailed behavior and external documentation links.</remarks>
        /// <returns>The shared serializer settings without object-reference preservation.</returns>
        public static JsonSerializerSettings GetJsonConfigNoObjRef() => m_jsonSerializerSettingsNoObjRef;

        /// <summary>Gets JSON serialization settings that omit both object-reference metadata and type-name metadata.</summary>
        /// <remarks>See <see cref="m_jsonSerializerSettingsNoNameHandling"/> for the detailed behavior and external documentation links.</remarks>
        /// <returns>The shared serializer settings intended for plain external-facing JSON payloads.</returns>
        public static JsonSerializerSettings GetJsonConfigNoNameHandling() => m_jsonSerializerSettingsNoNameHandling;

        /// <summary>Serializes a value using the default Ride JSON settings and writes the result to disk.</summary>
        /// <typeparam name="T">The type of value to serialize.</typeparam>
        /// <param name="data">The value to serialize.</param>
        /// <param name="path">The destination file path.</param>
        public static void JsonSerializeToFile<T>(T data, string path) => JsonSerializeToFile(JsonSerialize(data), path, GetJsonConfig());

        /// <summary>Serializes a value using the supplied JSON settings and writes the result to disk.</summary>
        /// <typeparam name="T">The type of value to serialize.</typeparam>
        /// <param name="data">The value to serialize.</param>
        /// <param name="path">The destination file path.</param>
        /// <param name="settings">The Newtonsoft settings to use during serialization.</param>
        public static void JsonSerializeToFile<T>(T data, string path, JsonSerializerSettings settings) => File.WriteAllText(path, JsonSerialize(data, settings));

        /// <summary>Serializes a value to JSON using the default Ride settings.</summary>
        /// <typeparam name="T">The type of value to serialize.</typeparam>
        /// <param name="data">The value to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string JsonSerialize<T>(T data) => JsonSerialize(data, GetJsonConfig());

        /// <summary>
        /// Serializes a value to JSON without emitting object-reference metadata.
        /// </summary>
        /// <typeparam name="T">The type of value to serialize.</typeparam>
        /// <param name="data">The value to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        /// <remarks>
        /// Callers commonly use this for service requests where Newtonsoft reference tokens such as
        /// <c>$id</c> and <c>$ref</c> would be noisy or unsupported, but type-name handling may still be useful.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/PreserveObjectReferences.htm
        /// </remarks>
        public static string JsonSerializeNoObjRef<T>(T data) => JsonSerialize(data, GetJsonConfigNoObjRef());

        /// <summary>
        /// Serializes a value to JSON using the supplied Newtonsoft settings.
        /// </summary>
        /// <typeparam name="T">The type of value to serialize.</typeparam>
        /// <param name="data">The value to serialize.</param>
        /// <param name="settings">The serializer settings to use.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string JsonSerialize<T>(T data, JsonSerializerSettings settings) => JsonConvert.SerializeObject(data, settings);

        /// <summary>
        /// Deserializes JSON into the requested type using Newtonsoft's default behavior.
        /// </summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized value.</returns>
        /// <remarks>
        /// This is the most common helper for parsing request and response DTOs returned by web services,
        /// as well as JSON previously serialized by Ride without special error-tolerance requirements.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/DeserializeObject.htm
        /// </remarks>
        public static T JsonDeserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        /// <summary>
        /// Deserializes JSON while ignoring explicit null values and unknown members in the payload.
        /// </summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized value.</returns>
        /// <remarks>
        /// Use this when ingesting partially compatible or forward-evolving payloads where callers prefer
        /// best-effort parsing over strict schema enforcement.
        /// Relevant docs:
        /// https://www.newtonsoft.com/json/help/html/NullValueHandlingIgnore.htm
        /// https://www.newtonsoft.com/json/help/html/DeserializeMissingMemberHandling.htm
        /// </remarks>
        public static T JsonDeserializeIgnoreNullAndMissing<T>(string json)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(json, jsonSettings);
        }

        #endregion

        #region Unity Asset and Cache Utilities

        /// <summary>
        /// Clears lcoal asset bundle cache
        /// </summary>
        /// <returns></returns>
        public static bool ClearAssetBundleCache()
        {
            // https://docs.unity3d.com/ScriptReference/Caching.ClearCache.html
            // https://forum.unity.com/threads/1-5-0-name-caching-does-not-exist.799077/
            // https://forum.unity.com/threads/webgl-build-cant-find-the-caching-library.1243648/
            bool succeeded = true;
#if !UNITY_WEBGL
            succeeded = UnityEngine.Caching.ClearCache();
#endif
            if (!succeeded)
                UnityEngine.Debug.LogWarning("ClearAssetBundleCache() - Caching.ClearCache() failed");

            return succeeded;
        }

        #endregion

        #region File System Utilities

        /// <summary>
        /// Computes the number of files and total bytes under a directory.
        /// </summary>
        /// <param name="path">The directory path to scan recursively.</param>
        /// <returns>The file count and total byte size.</returns>
        public static (int, Int64) ComputeDirectorySize(string path)
        {
            try
            {
                int fileCount = 0;
                Int64 totalBytes = 0;

                var directoryInfo = new DirectoryInfo(path);
                var files = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    fileCount++;
                    totalBytes += file.Length;
                }

                return (fileCount, totalBytes);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"RideIO.ComputeDirectorySize() - error reading directory '{path}': {ex}");
                return (0, 0);
            }
        }

        #endregion

        #region UnityWebRequest (Coroutine)

        /// <summary>
        /// Send a GET request using the given URI.
        /// </summary>
        /// <param name="uri">URI to be used in the request.</param>
        /// <param name="onComplete">Callback function, uses the request's downloaded text as the parameter.</param>
        /// <param name="startRange">Starting byte to download.</param>
        /// <param name="endRange">Ending byte to download.</param>
        /// <param name="hideErrorMessage">Hides the error message from displaying if there was a connection issue.</param>
        /// <returns></returns>
        public static IEnumerator Request(string uri, Action<string> onComplete, int startRange, int endRange, bool hideErrorMessage = false)
        {
            using var webRequest = UnityEngine.Networking.UnityWebRequest.Get(uri);

            if (startRange >= 0 && endRange >= 0)
                webRequest.SetRequestHeader("Range", $"bytes={startRange}-{endRange}");

            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                if (!hideErrorMessage)
                    RideLog.Log($"RideIO.Request() - Request Failed: {uri} - {webRequest.error}");

                onComplete?.Invoke(null);
            }
            else
            {
                onComplete?.Invoke(webRequest.downloadHandler.text);
            }
        }

        public static IEnumerator Request(string uri, Action<string> onComplete)
        {
            yield return Request(uri, onComplete, -1, -1);
        }

        /// <summary>
        /// Checks if a file exists in S3.
        /// </summary>
        /// <param name="path">The path of the file. It should start with 's3://'.</param>
        /// <param name="onComplete">Action that passes a bool. True if the input path exists in S3. False if it doesn't, or if there was an error.</param>
        /// <returns></returns>
        public static IEnumerator ExistsInS3(UnityEngine.MonoBehaviour behaviour, string path, Action<bool> onComplete)
        {
#if false
            var configSystem = Globals.api.systemAccessSystem.GetSystem<RideConfigSystem>();
            string cognitoIdentityPoolId = configSystem.GetTerrainKey();  // us-west-2:00x0xxx0-000x-000x-00x0-0000000xxxxx
            string region = configSystem.GetTerrainKeyRegion();

            string pathStripped = path.Replace("s3://", "");
            string bucketName = pathStripped.Substring(0, pathStripped.IndexOf("/"));
            string filePath = pathStripped.Remove(0, pathStripped.IndexOf("/") + 1);

            var aws = Globals.api.systemAccessSystem.GetSystem<AWS.AWSFileStorageS3System>();
            aws.m_cognitoIdentityPoolId = cognitoIdentityPoolId;
            aws.m_regionName = region;
            bool finished = false;
            string url = "";
            aws.GetSignedURL(bucketName, filePath, (returnedurl) =>
            {
                finished = true;
                url = returnedurl;
            });

            while (!finished)
                yield return new UnityEngine.WaitForEndOfFrame();

            if (string.IsNullOrEmpty(url))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            yield return behaviour.StartCoroutine(Request(url, (headValid) =>
            {
                if (!string.IsNullOrEmpty(headValid))
                {
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            },
            0, 0, true));
#else
            UnityEngine.Debug.LogError($"RideIO.ExistsInS3() - TODO - Ride Refactor - {path}");
            return null;
#endif
        }

        #endregion

        #region HTTP Synchronous (WebRequest, WebClient)

        /// <summary>
        /// Sends an http(s) GET request
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <returns>The result of the Get</returns>
        public static string Get(string uri) => 
            SendHttpAsync(HttpMethod.Get, uri).GetAwaiter().GetResult();

        /// <summary>
        /// Sends an http(s) GET request
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="headers">http headers</param>
        /// <returns>The result of the GET</returns>
        public static string Get(string uri, Dictionary<string, string> headers) => 
            SendHttpAsync(HttpMethod.Get, uri, null, headers).GetAwaiter().GetResult();

        /// <summary>
        /// Sends an http(s) PUT request
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <returns>The result of the PUT</returns>
        public static string Put(string uri, string data, Dictionary<string, string> headers) => 
            Put(uri, data, headers, "text/plain; charset=UTF-8");

        /// <summary>
        /// Sends an http(s) PUT request
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param>
        /// <returns>The result of the PUT</returns>
        public static string Put(string uri, string data, Dictionary<string, string> headers, string contentType) => 
            SendHttpAsync(HttpMethod.Put, uri, data, headers, contentType).GetAwaiter().GetResult();

        /// <summary>
        /// Sends a http(s) POST request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The url</param>
        /// <param name="payloadName">Json key name of the payload </param>
        /// <param name="payload">The payload to post</param>
        /// <returns>The result of the POST</returns>
        public static string Post<T>(string uri, string payloadName, T payload)
        {
            var formData = new Dictionary<string, string>
            {
                [payloadName] = JsonSerialize(payload)
            };

            return PostAsyncWithForm(uri, formData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends an http(s) PATCH request
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param>
        /// <returns>The result of the PATCH</returns>
        public static string Patch(string uri, string data, Dictionary<string, string> headers, string contentType) =>
            SendHttpAsync(new HttpMethod("PATCH"), uri, data, headers, contentType).GetAwaiter().GetResult();

        #endregion

        #region HTTP Async (HttpClient)

        /// <summary>
        /// Sends a http(s) JSON POST request asynchronously
        /// </summary>
        /// <param name="uri">The url to call</param>
        /// <param name="content">The content to send</param>
        /// <param name="authorizationType">Type of authorization</param>
        /// <param name="authorizationKey">Authorization key</param>
        /// <returns>String response</returns>
        public static Task<string> Post(string uri, string content, string authorizationType, string authorizationKey) =>
            SendHttpAsync(HttpMethod.Post, uri, content,
                new Dictionary<string, string> { [authorizationType] = authorizationKey });

        /// <summary>
        /// Sends a http(s) JSON POST request asynchronously
        /// </summary>
        /// <param name="uri">The url to call</param>
        /// <param name="content">The content to send</param>
        /// <param name="authorizationType">Type of authorization</param>
        /// <param name="authorizationKey">Authorization key</param>
        /// <param name="timeout">Timeout duration</param>
        /// <returns>String response</returns>
        public static Task<string> Post(string uri, string content, string authorizationType, string authorizationKey, double timeout) =>
            SendHttpAsync(HttpMethod.Post, uri, content,
                new Dictionary<string, string> { [authorizationType] = authorizationKey },
                "application/json", null, timeout);

        /// <summary>
        /// Sends an http(s) POST request asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param>
        /// <returns>Response from request</returns>
        public static Task<string> Post(string uri, string data, Dictionary<string, string> headers, string contentType) =>
            SendHttpAsync(HttpMethod.Post, uri, data, headers, contentType);

        /// <summary>
        /// Sends an http(s) POST request asynchronously (no headers)
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param></param>
        /// <returns></returns>
        public static Task<string> Post(string uri, string data, string contentType) =>
            SendHttpAsync(HttpMethod.Post, uri, data, null, contentType);

        /// <summary>
        /// Sends an http(s) POST request asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <returns>Response from request</returns>
        public static Task<string> Post(string uri, string data, Dictionary<string, string> headers) =>
            SendHttpAsync(HttpMethod.Post, uri, data, headers);

        /// <summary>
        /// Sends an http(s) POST request asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param>
        /// <param name="host">Host to be added in dedicated header</param>
        /// <returns>Response from request</returns>
        public static Task<string> Post(string uri, string data, Dictionary<string, string> headers, string contentType, string host) =>
            SendHttpAsync(HttpMethod.Post, uri, data, headers, contentType, host);

        /// <summary>
        /// Sends an http(s) POST request with Form Data asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="formData">A dictionary of data</param>
        /// <returns></returns>
        public static async Task<string> PostAsyncWithForm(string uri, Dictionary<string, string> formData)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(uri)))
            {
                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public static Task<string> PutAsync(string uri, string data, Dictionary<string, string> headers, string contentType, string host) =>
            SendHttpAsync(HttpMethod.Put, uri, data, headers, contentType, host);

        /// <summary>
        /// Sends an http(s) GET request asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="headers">http headers</param>
        /// <returns></returns>
        public static Task<string> GetAsync(string uri, Dictionary<string, string> headers) =>
            SendHttpAsync(HttpMethod.Get, uri, null, headers);

        /// <summary>
        /// Sends an http(s) GET request asynchronously without headers
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <returns></returns>
        public static Task<string> GetAsync(string uri) =>
            SendHttpAsync(HttpMethod.Get, uri);

        /// <summary>
        /// Sends an http(s) GET request asynchronously with a host
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="headers">http headers</param>
        /// <param name="host">the host url</param>
        /// <returns></returns>
        public static Task<string> GetAsyncHost(string uri, Dictionary<string, string> headers, string host) =>
            SendHttpAsync(HttpMethod.Get, uri, null, headers, "application/json", host);

        /// <summary>
        /// Sends an http(s) DELETE request asynchronously
        /// </summary>
        /// <param name="uri">The parameterized url</param>
        /// <param name="data">http body</param>
        /// <param name="headers">http headers</param>
        /// <param name="contentType">http content type i.e. text/plain; charset=UTF-8</param>
        /// <returns>Response from request</returns>
        public static Task<string> DeleteAsync(string uri, Dictionary<string, string> headers) =>
            SendHttpAsync(HttpMethod.Delete, uri, null, headers);

        public static Task<string> PatchAsync(string uri, string data, Dictionary<string, string> headers, string contentType) =>
            SendHttpAsync(new HttpMethod("PATCH"), uri, data, headers, contentType);

        private static async Task<string> SendHttpAsync(
            HttpMethod method,
            string uri,
            string content = null,
            Dictionary<string, string> headers = null,
            string contentType = "application/json",
            string host = null,
            double? timeoutSeconds = null)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(method, new Uri(uri)))
            {
                if (content != null)
                    request.Content = new StringContent(content, Encoding.UTF8, contentType);

                if (headers != null)
                {
                    foreach (var header in headers)
                        request.Headers.Add(header.Key, header.Value);
                }

                if (!string.IsNullOrEmpty(host))
                    request.Headers.Host = host;

                if (timeoutSeconds.HasValue)
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);

                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode(); // optional
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region System-Level Utilities

        /// <summary>
        /// Tests if the local machine has a connection to the internet
        /// </summary>
        /// <returns>True if a connection to the internet exists, otherwise false</returns>
        public static bool IsInternetConnectionAvailable()
        {
#if UNITY_WEBGL
            // In WebGL, if the app loaded, internet was already available.
            // Avoid blocking network calls which freeze the browser.
            return true;
#else
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
#endif
        }

        #endregion
    }
}
