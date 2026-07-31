using System;

/// <summary>
/// Shared serialized service contract types used by RIDE runtime systems when exchanging request and response data.
/// This file is the RIDE-side source of truth for these DTOs.
/// </summary>
namespace Ride
{
    /// <summary>
    /// Base class for service request payloads.
    /// Used by shared abstract request models such as NLP, sensing, and other runtime request DTOs.
    /// </summary>
    [Serializable]
    public class ServiceRequest { }

    /// <summary>
    /// Base class for service responses that wrap a typed payload and a simple error string.
    /// Used by file-storage responses and older service DTOs that follow the same serialized shape.
    /// </summary>
    [Serializable]
    public class ServiceResponse<T>
    {
        /// <summary>If null or empty, no error occurred during the request.</summary>
        public string error;

        /// <summary>Convenience flag indicating whether <see cref="error"/> is empty.</summary>
        public bool success => string.IsNullOrEmpty(error);

        /// <summary>Typed response payload returned by the service.</summary>
        public T responseData;
    }

    #region Storage Save
    /// <summary>
    /// Save response returned by <see cref="IFileStorageSystem"/> implementations.
    /// Actively used by package and asset-side file-storage systems.
    /// </summary>
    [Serializable]
    public class StorageSaveResponse : ServiceResponse<StorageSaveResult> { }

    /// <summary>
    /// Result payload for a file save operation.
    /// The <see cref="location"/> is the resolved storage path or object key that was saved.
    /// </summary>
    [Serializable]
    public struct StorageSaveResult
    {
        public string location;
    }

    /// <summary>
    /// Legacy proxy request used by older storage-save web services.
    /// This is still referenced by the asset-side Azure blob implementation and is a migration candidate.
    /// </summary>
    [Serializable]
    public struct StorageSaveRequest
    {
        public string connectionString;
        public string location;
        public string content;
    }
    #endregion

    #region Storage Load
    /// <summary>
    /// Load response returned by <see cref="IFileStorageSystem"/> implementations.
    /// Actively used by package and asset-side file-storage systems.
    /// </summary>
    [Serializable]
    public class StorageLoadResponse : ServiceResponse<StorageLoadResult> { }

    /// <summary>
    /// Legacy proxy request used by older storage-load web services.
    /// This is still referenced by the asset-side Azure blob implementation and is a migration candidate.
    /// </summary>
    [Serializable]
    public struct StorageLoadRequest
    {
        public string connectionString;
        public string location;
    }

    /// <summary>
    /// Result payload for a file load operation.
    /// Callers may use either the text <see cref="content"/> or raw <see cref="contentBlob"/> depending on the storage use case.
    /// </summary>
    [Serializable]
    public struct StorageLoadResult
    {
        public string location;
        public string content;
        public byte[] contentBlob;
    }
    #endregion

    #region Get Signed Url
    /// <summary>Request payload for generating a temporary signed URL to a storage object.</summary>
    [Serializable]
    public struct GetPreSignedUrlRequest
    {
        /// <summary>Full storage path or object key to sign.</summary>
        public string location;

        /// <summary>Optional storage credential used by the legacy Azure path; not used by the current AWS path.</summary>
        public string storageKey;

        /// <summary>Optional provider-specific account hint for environments that support multiple storage backends.</summary>
        public string authenticationKey;
    }

    /// <summary>Response wrapper for a signed URL request.</summary>
    [Serializable]
    public class GetPreSignedUrlResponse : ServiceResponse<GetPreSignedUrlResult> { }

    /// <summary>Result payload containing the generated signed URL.</summary>
    [Serializable]
    public struct GetPreSignedUrlResult
    {
        public string url;
    }
    #endregion
}
