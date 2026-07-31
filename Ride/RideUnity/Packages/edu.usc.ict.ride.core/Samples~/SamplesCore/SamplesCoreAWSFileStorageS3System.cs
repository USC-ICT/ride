using System;
using System.Text;
using Ride;
using Ride.AWS;

namespace Ride.Samples
{
    public class SamplesCoreAWSFileStorageS3System : RideMonoBehaviour
    {
        const int MAX_PREVIEW_CHARACTERS = 512;

        DebugMenu m_debugMenu;
        AWSFileStorageS3System m_awsFileStorageS3;

        string m_cognitoId = RideConfig.AWSTerrain.Default.cognitoIdentityPoolId;
        string m_region = "us-west-2";
        string m_bucket = "ride-capabilities";
        string m_objectKey = "valid-key";
        string m_saveContent = "Hello from SamplesCoreAWSFileStorageS3System";

        string m_status;
        string m_saveLocation;
        string m_loadLocation;
        string m_loadedContent;
        string m_signedUrl;
        bool m_showSignedUrl;
        bool m_showLoadedContent;
        bool m_requestInFlight;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_awsFileStorageS3 = Globals.api.GetSystem<AWSFileStorageS3System>();
        }

        public void OnGUIAWSFileStorageS3()
        {
            m_debugMenu.Label("Cognito ID:");
            m_cognitoId = m_debugMenu.TextField(m_cognitoId);
            m_debugMenu.Label("Region:");
            m_region = m_debugMenu.TextField(m_region);
            m_debugMenu.Label("Bucket:");
            m_bucket = m_debugMenu.TextField(m_bucket);
            m_debugMenu.Label("Object Key:");
            m_objectKey = m_debugMenu.TextField(m_objectKey);

            if (m_debugMenu.Button("GetSignedURL"))
                RunGetSignedUrlTest();

            if (m_debugMenu.Button("Load"))
                RunLoadTest();

            m_debugMenu.Label("Content To Save:");
            m_saveContent = m_debugMenu.TextArea(m_saveContent);

            if (m_debugMenu.Button("Save"))
                RunSaveTest();

            if (m_requestInFlight)
                m_debugMenu.Label("<b>Status:</b> Request in flight");

            if (!string.IsNullOrEmpty(m_status))
                m_debugMenu.Label($"<b>Status:</b> {m_status}");

            if (!string.IsNullOrEmpty(m_signedUrl))
            {
                using (m_debugMenu.Horizontal())
                {
                    m_debugMenu.Label("<b>Signed URL:</b>", 140);
                    if (m_debugMenu.Button(m_showSignedUrl ? "Hide" : "Show", 70))
                        m_showSignedUrl = !m_showSignedUrl;
                }

                if (m_showSignedUrl)
                    m_debugMenu.Label(m_signedUrl);
            }

            if (!string.IsNullOrEmpty(m_saveLocation))
                m_debugMenu.Label($"<b>Saved To:</b> {m_saveLocation}");

            if (!string.IsNullOrEmpty(m_loadLocation))
                m_debugMenu.Label($"<b>Loaded From:</b> {m_loadLocation}");

            if (!string.IsNullOrEmpty(m_loadedContent))
            {
                using (m_debugMenu.Horizontal())
                {
                    m_debugMenu.Label("<b>Loaded Content:</b>", 140);
                    if (m_debugMenu.Button(m_showLoadedContent ? "Hide" : "Show", 70))
                        m_showLoadedContent = !m_showLoadedContent;
                }

                if (m_showLoadedContent)
                    m_debugMenu.Label(m_loadedContent);
            }
        }

        void RunGetSignedUrlTest()
        {
            if (!TryBeginRequest("Get signed URL"))
                return;

            ConfigureStorageSystem();
            m_signedUrl = null;
            m_loadedContent = null;

            m_awsFileStorageS3.GetSignedURL(m_bucket, m_objectKey, url =>
            {
                if (string.IsNullOrEmpty(url))
                {
                    m_signedUrl = "Error";
                    CompleteRequest("Get signed URL failed");
                    return;
                }

                m_signedUrl = url;
                CompleteRequest("Get signed URL succeeded");
            });
        }

        void RunSaveTest()
        {
            if (!TryBeginRequest("Save"))
                return;

            ConfigureStorageSystem();
            m_saveLocation = null;
            m_signedUrl = null;

            m_awsFileStorageS3.GetSignedUploadURL(m_bucket, m_objectKey, "text/plain; charset=utf-8", url =>
            {
                m_signedUrl = url;
                m_awsFileStorageS3.Save(GetStorageLocation(), m_saveContent, response =>
                {
                    m_saveLocation = response != null ? response.responseData.location : null;
                    CompleteRequest(DescribeSaveResponse("Save", response));
                });
            });
        }

        void RunLoadTest()
        {
            if (!TryBeginRequest("Load"))
                return;

            ConfigureStorageSystem();
            m_loadLocation = null;
            m_loadedContent = null;
            m_signedUrl = null;

            m_awsFileStorageS3.GetSignedURL(m_bucket, m_objectKey, url =>
            {
                m_signedUrl = url;
                m_awsFileStorageS3.Load(GetStorageLocation(), response =>
                {
                    if (response != null)
                    {
                        m_loadLocation = response.responseData.location;
                        m_loadedContent = CreatePreview(response.responseData.content, response.responseData.contentBlob);
                    }

                    CompleteRequest(DescribeLoadResponse("Load", response));
                });
            });
        }

        void ConfigureStorageSystem()
        {
            m_awsFileStorageS3.m_cognitoIdentityPoolId = m_cognitoId;
            m_awsFileStorageS3.m_regionName = m_region;
        }

        string GetStorageLocation() => RideIO.PathCombine(m_bucket, m_objectKey);

        bool TryBeginRequest(string operation)
        {
            if (m_requestInFlight)
            {
                m_status = $"Busy; skipped {operation}";
                return false;
            }

            m_requestInFlight = true;
            m_status = $"{operation} started";
            return true;
        }

        void CompleteRequest(string status)
        {
            m_requestInFlight = false;
            m_status = status;
        }

        static string DescribeSaveResponse(string operation, StorageSaveResponse response)
        {
            if (response == null)
                return $"{operation} failed: null response";

            if (!string.IsNullOrEmpty(response.error))
                return $"{operation} failed: {response.error}";

            string location = response.responseData.location;
            return string.IsNullOrEmpty(location)
                ? $"{operation} succeeded"
                : $"{operation} succeeded: {location}";
        }

        static string DescribeLoadResponse(string operation, StorageLoadResponse response)
        {
            if (response == null)
                return $"{operation} failed: null response";

            if (!string.IsNullOrEmpty(response.error))
                return $"{operation} failed: {response.error}";

            int contentLength = response.responseData.contentBlob?.Length ?? response.responseData.content?.Length ?? 0;
            return $"{operation} succeeded ({contentLength} bytes)";
        }

        static string CreatePreview(string textContent, byte[] binaryContent)
        {
            if (!string.IsNullOrEmpty(textContent))
                return LimitPreview(textContent);

            if (binaryContent == null || binaryContent.Length == 0)
                return "(empty)";

            string decoded = Encoding.UTF8.GetString(binaryContent);
            return LimitPreview(decoded);
        }

        static string LimitPreview(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "(empty)";

            if (value.Length <= MAX_PREVIEW_CHARACTERS)
                return value;

            return value.Substring(0, MAX_PREVIEW_CHARACTERS) + "...";
        }
    }
}
