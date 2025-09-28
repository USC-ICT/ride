using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;
using Ride.AWS;

namespace Ride.Samples
{
    public class SamplesCoreAWSFileStorageS3System : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        AWSFileStorageS3System m_awsFileStorageS3;

        string m_cognitoId = RideConfig.AWSTerrain.Default.cognitoIdentityPoolId;
        string m_region = "us-west-2";
        string m_bucket = "ride-capabilities";
        string m_objectKey = "valid-key";

        string m_signedUrl;

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
            {
                m_awsFileStorageS3.m_cognitoIdentityPoolId = m_cognitoId;
                m_awsFileStorageS3.m_regionName = m_region;
                m_awsFileStorageS3.GetSignedURL(m_bucket, m_objectKey, url => 
                {
                    if (string.IsNullOrEmpty(url))
                        m_signedUrl = "Error";
                    else
                        m_signedUrl = url;
                });
            }

            if (!string.IsNullOrEmpty(m_signedUrl))
                m_debugMenu.Label(m_signedUrl);
        }
    }
}
