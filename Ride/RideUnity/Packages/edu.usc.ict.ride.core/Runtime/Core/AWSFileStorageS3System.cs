using System;
using System.Collections;
using System.IO;
using System.Text;
using Ride.Networking;
using UnityEngine.Networking;

namespace Ride.AWS
{
    /// <summary>
    /// Provides an implementation of <see cref="IFileStorageSystem"/> that uploads, downloads, and manages files via AWS S3,
    /// using signed URLs returned by the configured signer endpoint.
    ///
    /// This system supports both absolute S3-style paths and relative paths resolved against <see cref="preferredPath"/>.
    /// File transfer happens directly between the client and S3 after a short signer request obtains a temporary URL.
    ///
    /// If internet connectivity is unavailable, the system generates fallback responses to maintain flow without exceptions,
    /// allowing the caller to handle offline scenarios gracefully.
    /// </summary>
    public class AWSFileStorageS3System : RideSystemMonoBehaviour, IFileStorageSystem
    {
        const string SIGN_METHOD_GET = "GET";
        const string SIGN_METHOD_PUT = "PUT";
        const string DEFAULT_UPLOAD_CONTENT_TYPE = "text/plain; charset=utf-8";

        public string m_cognitoIdentityPoolId = "";
        public string m_regionName = "";

        /// <inheritdoc/>
        public string preferredPath { get; set; }

        /// <inheritdoc/>
        public void Load(string filepath, Action<StorageLoadResponse> onComplete)
        {
            string location = NormalizeLocation(filepath);
            if (string.IsNullOrEmpty(location))
            {
                onComplete?.Invoke(CreateLoadErrorResponse(filepath, "Storage location is empty"));
                return;
            }

            if (!RideIO.IsInternetConnectionAvailable())
            {
                onComplete?.Invoke(CreateOfflineLoadResponse(location));
                return;
            }

            GetSignedUrlForMethod(location, SIGN_METHOD_GET, null, signedUrl =>
            {
                if (string.IsNullOrEmpty(signedUrl))
                {
                    onComplete?.Invoke(CreateLoadErrorResponse(location, "Unable to get signed URL"));
                    return;
                }

                StartCoroutine(LoadFromSignedUrlCoroutine(location, signedUrl, onComplete));
            });
        }

        /// <inheritdoc/>
        public void LoadUsingPreferredPath(string filename, Action<StorageLoadResponse> onComplete) =>
            Load(RideIO.PathCombine(preferredPath, filename), onComplete);

        /// <summary>
        /// Uploads UTF-8 text content to an AWS S3 bucket using a signed PUT URL.
        /// </summary>
        /// <param name="path">The bucket name, subfolder(s), and filename/extension to store the data inside S3.</param>
        /// <param name="data">The text content to upload.</param>
        /// <param name="onComplete">Called when the upload succeeds or fails.</param>
        public void Save(string path, string data, Action<StorageSaveResponse> onComplete)
        {
            string location = NormalizeLocation(path);
            if (string.IsNullOrEmpty(location))
            {
                onComplete?.Invoke(CreateSaveErrorResponse(path, "Storage location is empty"));
                return;
            }

            if (!RideIO.IsInternetConnectionAvailable())
            {
                onComplete?.Invoke(CreateOfflineSaveResponse(location));
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(data ?? string.Empty);
            GetSignedUrlForMethod(location, SIGN_METHOD_PUT, DEFAULT_UPLOAD_CONTENT_TYPE, signedUrl =>
            {
                if (string.IsNullOrEmpty(signedUrl))
                {
                    onComplete?.Invoke(CreateSaveErrorResponse(location, "Unable to get signed URL"));
                    return;
                }

                StartCoroutine(SaveToSignedUrlCoroutine(location, signedUrl, bytes, onComplete));
            });
        }

        /// <inheritdoc/>
        public void SaveUsingPreferredPath(string filename, string data, Action<StorageSaveResponse> onComplete) =>
            Save(RideIO.PathCombine(preferredPath, filename), data, onComplete);

        /// <summary>
        /// Uploads a local text file to an AWS S3 bucket using the same direct-upload path as <see cref="Save"/>.
        /// </summary>
        /// <param name="src">The full local path of the file to upload.</param>
        /// <param name="dst">The bucket name and destination key.</param>
        /// <param name="onComplete">Called when the upload succeeds or fails.</param>
        public void Copy(string src, string dst, Action<StorageSaveResponse> onComplete) =>
            Save(dst, File.ReadAllText(src), onComplete);

        /// <inheritdoc/>
        public void GetSignedURL(string bucketName, string objectKey, Action<string> onComplete)
        {
            string location = ComposeLocation(bucketName, objectKey);
            if (string.IsNullOrEmpty(location))
            {
                RideLog.LogError("[GetSignedURL] FAILED location is empty");
                onComplete?.Invoke(null);
                return;
            }

            GetSignedUrlForMethod(location, SIGN_METHOD_GET, null, onComplete);
        }

        /// <summary>
        /// Retrieves a signed PUT URL for a specific S3 object.
        /// </summary>
        /// <param name="bucketName">The S3 bucket name.</param>
        /// <param name="objectKey">The S3 object key inside the bucket.</param>
        /// <param name="contentType">The content type that the signed upload will require.</param>
        /// <param name="onComplete">Called with the signed URL, or <c>null</c> if signing fails.</param>
        public void GetSignedUploadURL(string bucketName, string objectKey, string contentType, Action<string> onComplete)
        {
            string location = ComposeLocation(bucketName, objectKey);
            if (string.IsNullOrEmpty(location))
            {
                RideLog.LogError("[GetSignedUploadURL] FAILED location is empty");
                onComplete?.Invoke(null);
                return;
            }

            GetSignedUrlForMethod(location, SIGN_METHOD_PUT, contentType, onComplete);
        }

        /// <inheritdoc/>
        public void GetSignedURLUsingPreferredPath(string filename, Action<string> onComplete) =>
            GetSignedURL(preferredPath, filename, onComplete);

        /// <summary>
        /// Builds the conventional virtual-hosted S3 URL for the given bucket and key.
        /// </summary>
        /// <param name="bucket">The S3 bucket name.</param>
        /// <param name="key">The S3 object key.</param>
        /// <returns>The unsigned HTTPS S3 object URL.</returns>
        public static string GetLocation(string bucket, string key) => $"https://{bucket}.s3.amazonaws.com/{key.Replace("\\", "/")}";

        /// <summary>
        /// Requests a signed URL from the configured signer endpoint for the specified S3 operation.
        /// </summary>
        /// <param name="location">The normalized <c>bucket/key</c> location to sign.</param>
        /// <param name="method">The HTTP method to sign, such as <c>GET</c> or <c>PUT</c>.</param>
        /// <param name="contentType">Optional upload content type required by the signer for PUT operations.</param>
        /// <param name="onComplete">Called with the signed URL, or <c>null</c> if signing fails.</param>
        private void GetSignedUrlForMethod(string location, string method, string contentType, Action<string> onComplete)
        {
            string uri = ConfigurationSystemUnity.GetStorageSignedUrlEndpoint();
            if (string.IsNullOrWhiteSpace(uri))
            {
                RideLog.LogError($"[GetSignedURL] FAILED signed URL endpoint is not configured location='{location}'");
                onComplete?.Invoke(null);
                return;
            }

            RideLog.Log($"[GetSignedURL] start - uri='{uri}' location='{location}' method='{method}'");

            Systems.Get<IWebRequesterSystem>().Put<object, GetPreSignedUrlResponse>(
                uri,
                null,
                CreateSignRequest(location, method, contentType),
                (result, error, response) =>
                {
                    try
                    {
                        if (result != WebRequestResult.Success)
                        {
                            RideLog.LogWarning($"[GetSignedURL] FAILED result={result} error='{error}' location='{location}' method='{method}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        if (response == null)
                        {
                            RideLog.LogWarning($"[GetSignedURL] FAILED response is null location='{location}' method='{method}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        string url = response.responseData.url;

                        if (string.IsNullOrEmpty(url))
                        {
                            RideLog.LogError($"[GetSignedURL] FAILED url empty location='{location}' method='{method}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        RideLog.Log($"[GetSignedURL] OK location='{location}' method='{method}'");
                        onComplete?.Invoke(url);
                    }
                    catch (Exception ex)
                    {
                        RideLog.LogWarning($"[GetSignedURL] EXCEPTION location='{location}' method='{method}': {ex}");
                        onComplete?.Invoke(null);
                    }
                });
        }

        /// <summary>
        /// Creates the signer request payload expected by the AWS signed-URL lambda.
        /// </summary>
        /// <param name="location">The normalized <c>bucket/key</c> target location.</param>
        /// <param name="method">The HTTP method to sign.</param>
        /// <param name="contentType">Optional upload content type.</param>
        /// <returns>An anonymous object that serializes into the signer request body.</returns>
        private object CreateSignRequest(string location, string method, string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return new { location, authenticationKey = m_cognitoIdentityPoolId, region = m_regionName, method };

            return new { location, authenticationKey = m_cognitoIdentityPoolId, region = m_regionName, method, contentType };
        }

        /// <summary>
        /// Downloads an object directly from S3 using a previously signed GET URL.
        /// </summary>
        /// <param name="location">The normalized <c>bucket/key</c> location being loaded.</param>
        /// <param name="signedUrl">The signed GET URL returned by the signer.</param>
        /// <param name="onComplete">Called with the populated load response.</param>
        private IEnumerator LoadFromSignedUrlCoroutine(string location, string signedUrl, Action<StorageLoadResponse> onComplete)
        {
            using var request = UnityWebRequest.Get(signedUrl);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = BuildUnityWebRequestError("Load", location, request);
                RideLog.LogWarning(error);
                onComplete?.Invoke(CreateLoadErrorResponse(location, error));
                yield break;
            }

            byte[] bytes = request.downloadHandler.data ?? Array.Empty<byte>();
            onComplete?.Invoke(new StorageLoadResponse
            {
                responseData = new StorageLoadResult { location = location, content = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty, contentBlob = bytes }
            });
        }

        /// <summary>
        /// Uploads bytes directly to S3 using a previously signed PUT URL.
        /// </summary>
        /// <param name="location">The normalized <c>bucket/key</c> location being saved.</param>
        /// <param name="signedUrl">The signed PUT URL returned by the signer.</param>
        /// <param name="bytes">The bytes to upload.</param>
        /// <param name="onComplete">Called with the populated save response.</param>
        private IEnumerator SaveToSignedUrlCoroutine(string location, string signedUrl, byte[] bytes, Action<StorageSaveResponse> onComplete)
        {
            using var request = new UnityWebRequest(signedUrl, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", DEFAULT_UPLOAD_CONTENT_TYPE);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = BuildUnityWebRequestError("Save", location, request);
                RideLog.LogWarning(error);
                onComplete?.Invoke(CreateSaveErrorResponse(location, error));
                yield break;
            }

            onComplete?.Invoke(new StorageSaveResponse
            {
                responseData = new StorageSaveResult { location = location }
            });
        }

        /// <summary>
        /// Combines bucket and key values into a normalized <c>bucket/key</c> location string.
        /// </summary>
        /// <param name="bucketName">The S3 bucket name.</param>
        /// <param name="objectKey">The S3 object key.</param>
        /// <returns>The normalized location string, or <c>null</c> if the bucket is empty.</returns>
        private static string ComposeLocation(string bucketName, string objectKey)
        {
            bucketName = bucketName?.Trim().Replace("\\", "/").Trim('/');
            objectKey = objectKey?.Trim().Replace("\\", "/").Trim('/');

            if (string.IsNullOrEmpty(bucketName))
                return null;

            if (string.IsNullOrEmpty(objectKey))
                return bucketName;

            return $"{bucketName}/{objectKey}";
        }

        /// <summary>
        /// Normalizes a caller-provided storage location into a bare <c>bucket/key</c> form.
        /// </summary>
        /// <param name="location">The input path, optionally including an <c>s3://</c> prefix.</param>
        /// <returns>The normalized storage location, or <c>null</c> if the input is empty.</returns>
        private static string NormalizeLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return null;

            location = location.Trim().Replace("\\", "/");
            if (location.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
                location = location.Substring(5);

            return location.Trim('/');
        }

        /// <summary>
        /// Formats a UnityWebRequest failure into a user-facing error message that includes the response body when available.
        /// </summary>
        /// <param name="operation">The high-level operation name, such as <c>Load</c> or <c>Save</c>.</param>
        /// <param name="location">The normalized storage location involved in the request.</param>
        /// <param name="request">The completed UnityWebRequest.</param>
        /// <returns>A formatted error string suitable for logs and callback responses.</returns>
        private static string BuildUnityWebRequestError(string operation, string location, UnityWebRequest request)
        {
            string responseText = request.downloadHandler?.text;
            if (!string.IsNullOrEmpty(responseText))
                return $"AWSFileStorageS3System.{operation}() failed for location '{location}': {request.error} | Response: {responseText}";

            return $"AWSFileStorageS3System.{operation}() failed for location '{location}': {request.error}";
        }

        /// <summary>
        /// Creates an offline load response that preserves the target location while reporting the connectivity issue.
        /// </summary>
        /// <param name="location">The requested storage location.</param>
        /// <returns>A load response populated with an offline error.</returns>
        private static StorageLoadResponse CreateOfflineLoadResponse(string location) => new StorageLoadResponse
        {
            responseData = new StorageLoadResult { location = location, content = string.Empty, contentBlob = Array.Empty<byte>() },
            error = "No internet connection available"
        };

        /// <summary>
        /// Creates a failed load response with the supplied error text.
        /// </summary>
        /// <param name="location">The requested storage location.</param>
        /// <param name="error">The error to report back to the caller.</param>
        /// <returns>A load response populated with the error.</returns>
        private static StorageLoadResponse CreateLoadErrorResponse(string location, string error) => new StorageLoadResponse
        {
            responseData = new StorageLoadResult { location = location, content = string.Empty, contentBlob = Array.Empty<byte>() },
            error = error
        };

        /// <summary>
        /// Creates an offline save response that preserves the target location while reporting the connectivity issue.
        /// </summary>
        /// <param name="location">The requested storage location.</param>
        /// <returns>A save response populated with an offline error.</returns>
        private static StorageSaveResponse CreateOfflineSaveResponse(string location) => new StorageSaveResponse
        {
            responseData = new StorageSaveResult { location = location },
            error = "No internet connection available"
        };

        /// <summary>
        /// Creates a failed save response with the supplied error text.
        /// </summary>
        /// <param name="location">The requested storage location.</param>
        /// <param name="error">The error to report back to the caller.</param>
        /// <returns>A save response populated with the error.</returns>
        private static StorageSaveResponse CreateSaveErrorResponse(string location, string error) => new StorageSaveResponse
        {
            responseData = new StorageSaveResult { location = location },
            error = error
        };
    }
}
