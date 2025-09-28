using System;

/*
 * WARNING!!!!!!
 * This file is copied over from the trunk/RideWebServices/RideServerUtils Project. Any changes made to this file have to be made
 * in that project, otherwise, you will lose them the next time that project is compiled.
 * */
namespace Ride
{
    /// <summary>
    /// Base class for making a service request
    /// </summary>
    [Serializable]
    public class ServiceRequest { }

    /// <summary>
    /// Base class for responses received from a service
    /// </summary>
    [Serializable]
    public class ServiceResponse<T>
    {
        /// <summary>
        /// if null or empty, no error occured during the request
        /// </summary>
        public string error;

        public bool success { get { return string.IsNullOrEmpty(error); } }

        public T responseData;
    }

    #region Hello World
    [Serializable]
    public class HelloWorldResponse : ServiceResponse<HelloWorldResult> { }

    public struct HelloWorldResult
    {
        public string result;
    }

    [Serializable]
    public struct HelloWorldRequest
    {
        public string origin;
    }
    #endregion

    #region Storage Save
    [Serializable]
    public class StorageSaveResponse : ServiceResponse<StorageSaveResult> { }

    [Serializable]
    public struct StorageSaveResult
    {
        public string location;
    }

    [Serializable]
    public struct StorageSaveRequest
    {
        public string connectionString;
        public string location;
        public string content;
    }
    #endregion

    #region Storage Load
    [Serializable]
    public class StorageLoadResponse : ServiceResponse<StorageLoadResult> { }

    [Serializable]
    public struct StorageLoadRequest
    {
        public string connectionString;
        public string location;
    }

    [Serializable]
    public struct StorageLoadResult
    {
        public string location;
        public string content;
        public byte[] contentBlob;
    }
    #endregion


    #region Get Signed Url
    [Serializable]
    public struct GetPreSignedUrlRequest
    {
        /// <summary>
        /// path to the file
        /// </summary>
        public string location;

        /// <summary>
        /// Used with azure for authentication, not aws
        /// </summary>
        public string storageKey;

        /// <summary>
        /// Used to specify which cloud account to use. Not required.
        /// </summary>
        public string authenticationKey;
    }

    [Serializable]
    public class GetPreSignedUrlResponse : ServiceResponse<GetPreSignedUrlResult> { }

    [Serializable]
    public struct GetPreSignedUrlResult
    {
        public string url;
    }
    #endregion
}
