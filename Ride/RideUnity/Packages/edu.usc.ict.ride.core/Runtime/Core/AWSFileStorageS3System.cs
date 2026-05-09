using System;
using System.IO;
using Ride.Networking;

namespace Ride.AWS
{
    /// <summary>
    /// Provides an implementation of <see cref="IFileStorageSystem"/> that uploads, downloads, and manages files via AWS S3,
    /// using Ride’s REST proxy endpoints defined in <see cref="ConfigurationSystemUnity"/>.
    /// 
    /// This system supports both absolute S3 paths and relative paths resolved against <see cref="preferredPath"/>.
    /// All file operations are routed through <see cref="IWebRequesterSystem"/> and operate asynchronously via callback.
    /// 
    /// If internet connectivity is unavailable, the system generates fallback responses to maintain flow without exceptions,
    /// allowing the caller to handle offline scenarios gracefully.
    /// 
    /// This class is used by calling <see cref="Save"/>, <see cref="Load"/>, <see cref="Copy"/>, and signed URL generation
    /// through <see cref="GetSignedURL"/> and <see cref="GetSignedURLUsingPreferredPath"/>.
    /// </summary>
    public class AWSFileStorageS3System : RideSystemMonoBehaviour, IFileStorageSystem
    {
        public string m_cognitoIdentityPoolId = "";
        public string m_regionName = "";

        /// <inheritdoc/>
        public string preferredPath { get; set; }

        /// <inheritdoc/>
        public void Load(string filepath, Action<StorageLoadResponse> onComplete)
        {
            string uri = ConfigurationSystemUnity.GetRideRestApi("StorageLoadAws");

            Systems.Get<IWebRequesterSystem>().Put<StorageLoadRequest, StorageLoadResponse>(
                uri,
                null,
                new StorageLoadRequest() { location = filepath },
                (result, error, response) => { onComplete?.Invoke(response); });
        }

        /// <inheritdoc/>
        public void LoadUsingPreferredPath(string filename, Action<StorageLoadResponse> onComplete) =>
            Load(RideIO.PathCombine(preferredPath, filename), onComplete);

        /// <summary>
        /// Uploads the data to an AWS S3 bucket
        /// </summary>
        /// <param name="path">the bucketname, subfolder(s), and filename/extension to store the data inside the s3</param>
        /// <param name="data">the data you want to store</param>
        /// <param name="onComplete">Called upon a successful upload and proves the url of where the data is stored on the s3</param>
        public void Save(string path, string data, Action<StorageSaveResponse> onComplete) =>
            UploadToStorage(path, data, onComplete);

        /// <inheritdoc/>
        public void SaveUsingPreferredPath(string filename, string data, Action<StorageSaveResponse> onComplete) =>
            Save(RideIO.PathCombine(preferredPath, filename), data, onComplete);

        /// <summary>
        /// Uploads the file to an AWS S3 bucket
        /// </summary>
        /// <param name="src">the full local path of the file to upload</param>
        /// <param name="dst">the bucketname and path of the file</param>
        /// <param name="onComplete">Called upon a successful upload and proves the url of where the data is stored on the s3</param>
        public void Copy(string src, string dst, Action<StorageSaveResponse> onComplete) =>
            UploadToStorage(dst, File.ReadAllText(src), onComplete);

        /// <inheritdoc/>
        public void GetSignedURL(string bucketName, string objectKey, Action<string> onComplete)
        {
            objectKey = objectKey.Replace("\\", "/");

            string location = $"{bucketName}/{objectKey}";
            string uri = ConfigurationSystemUnity.GetStorageSignedUrlEndpoint();
            if (string.IsNullOrWhiteSpace(uri))
            {
                RideLog.LogError($"[GetSignedURL] FAILED signed URL endpoint is not configured location='{location}'");
                onComplete?.Invoke(null);
                return;
            }

            RideLog.Log($"[GetSignedURL] start - uri='{uri}' location='{location}'");

            Systems.Get<IWebRequesterSystem>().Put<GetPreSignedUrlRequest, GetPreSignedUrlResponse>(
                uri,
                null,
                new GetPreSignedUrlRequest()
                {
                    location = location,
                    authenticationKey = m_cognitoIdentityPoolId
                },
                (result, error, response) =>
                {
                    try
                    {
                        if (result != WebRequestResult.Success)
                        {
                            RideLog.LogWarning($"[GetSignedURL] FAILED result={result} error='{error}' location='{location}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        if (response == null)
                        {
                            RideLog.LogWarning($"[GetSignedURL] FAILED response is null location='{location}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        string url = response.responseData.url;

                        if (string.IsNullOrEmpty(url))
                        {
                            RideLog.LogError($"[GetSignedURL] FAILED url empty location='{location}'");
                            onComplete?.Invoke(null);
                            return;
                        }

                        RideLog.Log($"[GetSignedURL] OK location='{location}' url='{url}'");
                        onComplete?.Invoke(url);
                    }
                    catch (Exception ex)
                    {
                        RideLog.LogWarning($"[GetSignedURL] EXCEPTION location='{location}': {ex}");
                        onComplete?.Invoke(null);
                    }
                });
        }

        /// <inheritdoc/>
        public void GetSignedURLUsingPreferredPath(string filename, Action<string> onComplete) =>
            GetSignedURL(preferredPath, filename, onComplete);

        public static string GetLocation(string bucket, string key) => $"https://{bucket}.s3.amazonaws.com/{key.Replace("\\", "/")}";

        /// <summary>
        /// Uploads text content to a remote AWS S3 bucket using REST proxy.
        /// This method will normalize slashes, check for internet connectivity,
        /// and fall back with a mock success response if offline.
        /// </summary>
        /// <param name="location">Full S3 path including bucket, subfolder, and filename.</param>
        /// <param name="data">The raw string content to upload.</param>
        /// <param name="onComplete">
        /// Callback invoked with the upload result. If offline, response will contain the intended location and an error message.
        /// </param>
        private static void UploadToStorage(string location, string data, Action<StorageSaveResponse> onComplete)
        {
            try
            {
                location = location.Replace("\\", "/");

                //RideLogSystem.Log("location " + location);

                if (!RideIO.IsInternetConnectionAvailable())
                {
                    onComplete?.Invoke(CreateOfflineSaveResponse(location));
                    return;
                }

                string uri = ConfigurationSystemUnity.GetRideRestApi("StorageSaveAws");
                Systems.Get<IWebRequesterSystem>().Put<StorageSaveRequest, StorageSaveResponse>(
                    uri,
                    null,
                    new StorageSaveRequest() { location = location, content = data },
                    (result, error, response) => { onComplete?.Invoke(response); });
            }
            catch (Exception e)
            {
                RideLog.LogError($"AWSFileStorageS3System.MakeSaveRequest() failed for location '{location}': {e}");
            }
        }

        /// <summary>
        /// Creates a simulated StorageSaveResponse when no internet connection is available.
        /// The response includes the original target location and an error message.
        /// </summary>
        /// <param name="location">The target location the upload would have used.</param>
        /// <returns>A fallback response indicating failure but preserving metadata.</returns>
        private static StorageSaveResponse CreateOfflineSaveResponse(string location) => new StorageSaveResponse
        {
            responseData = new StorageSaveResult { location = location },
            error = "No internet connection available"
        };
    }
}
