using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Ride
{
    public class RideAddressablesBuildWindow : EditorWindow
    {
        private string m_accessKey = "";
        private string m_secretKey = "";
        private string m_region = "us-west-2";
        private string m_s3BucketPrefix = "s3://dummy-bucket/addressables";
        private string m_statusMessage = "";
        
        [MenuItem("Ride/Addressables/Build and Sync")]
        public static void ShowWindow()
        {
            GetWindow<RideAddressablesBuildWindow>("Build and Sync Addressables");
        }

        private void OnGUI()
        {
            GUILayout.Label("Build Addressables", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Addressables"))
                BuildAddressables();

            GUILayout.Space(10);
            GUILayout.Label("AWS Configuration", EditorStyles.boldLabel);
            m_accessKey = EditorGUILayout.TextField("Access Key", m_accessKey);
            m_secretKey = EditorGUILayout.PasswordField("Secret Key", m_secretKey);
            m_region = EditorGUILayout.TextField("Region", m_region);
            m_s3BucketPrefix = EditorGUILayout.TextField("S3 Prefix", m_s3BucketPrefix);

            GUI.enabled = !string.IsNullOrEmpty(m_accessKey) && !string.IsNullOrEmpty(m_secretKey);
            if (GUILayout.Button("Sync Addressables to S3"))
                SyncAddressables();
            GUI.enabled = true;

            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(m_statusMessage))
                EditorGUILayout.HelpBox(m_statusMessage, m_statusMessage.Contains("Error") || m_statusMessage.Contains("Exception")
                    ? MessageType.Error : MessageType.Info);
        }

        private void BuildAddressables()
        {
            try
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    m_statusMessage = "Could not load Addressable Asset Settings.";
                    return;
                }

                UnityEngine.Debug.Log("Building Addressables...");
                AddressableAssetSettings.BuildPlayerContent();

                AddressableSystem system = FindAnyObjectByType<AddressableSystem>();
                if (system != null)
                {
                    UnityEngine.Debug.Log("RideAddressableSystem: Refreshing asset labels after build.");
                    system.EditorLoadCatalogs();
                }
            
                m_statusMessage = "Addressables built successfully!";
            }
            catch (System.Exception ex)
            {
                m_statusMessage = "Error building Addressables: " + ex.Message;
                UnityEngine.Debug.LogError(m_statusMessage);
            }
        }

        private void SyncAddressables()
        {
            try
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    m_statusMessage = "Could not load Addressable Asset Settings.";
                    return;
                }

                var profileID = settings.activeProfileId;
                string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                string renderPipeline = settings.profileSettings.GetValueByName(profileID, "RenderPipeline");
                string unityVersion = Application.unityVersion;
                string buildPath = Path.Combine(unityVersion, renderPipeline, buildTarget);

                SyncToS3(buildPath);
            }
            catch (System.Exception ex)
            {
                m_statusMessage = "Error syncing Addressables: " + ex.Message;
                UnityEngine.Debug.LogError(m_statusMessage);
            }
        }

        private void SyncToS3(string addressableBuildPath)
        {
            try
            {
                if (!ConfigureAws(m_accessKey, m_secretKey, m_region))
                {
                    m_statusMessage = "AWS configuration failed. Check credentials.";
                    return;
                }

                string buildPath = Path.Combine(UnityEngine.Application.persistentDataPath, "Addressables", addressableBuildPath);
                string s3Bucket = $"{m_s3BucketPrefix}/{addressableBuildPath.Replace("\\", "/")}/";
                string awsCli = "aws";
                string syncCommand = $"s3 sync \"{buildPath}\" \"{s3Bucket}\" --delete";
                UnityEngine.Debug.Log($"Syncing Addressables from {buildPath} to {s3Bucket}");

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = awsCli,
                        Arguments = syncCommand,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    m_statusMessage = $"Error syncing Addressables: {process.StandardError.ReadToEnd()}";
                    UnityEngine.Debug.LogError(m_statusMessage);
                }
                else
                {
                    m_statusMessage = $"Addressables successfully synced to S3.";
                    UnityEngine.Debug.Log(m_statusMessage);
                }
            }
            catch (System.Exception ex)
            {
                m_statusMessage = "Exception occurred during S3 sync: " + ex.Message;
                UnityEngine.Debug.LogError(m_statusMessage);
            }
        }

        private bool ConfigureAws(string accessKey, string secretKey, string region)
        {
            try
            {
                UnityEngine.Debug.Log("Configuring AWS CLI...");
                string awsCli = "aws";

                string[] configureCommands = new string[]
                {
                    $"configure set aws_access_key_id {accessKey}",
                    $"configure set aws_secret_access_key {secretKey}",
                    $"configure set region {region}"
                };

                foreach (var command in configureCommands)
                {
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = awsCli,
                            Arguments = command,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        UnityEngine.Debug.LogError("Error configuring AWS CLI: " + error);
                        return false;
                    }
                }

                UnityEngine.Debug.Log("AWS CLI configured successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Exception during AWS configuration: " + ex.Message);
                return false;
            }
        }
    }
}
