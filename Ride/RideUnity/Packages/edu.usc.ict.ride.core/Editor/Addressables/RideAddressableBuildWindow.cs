using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Ride
{
    /// <summary>Provides editor controls for building Addressables content and synchronizing it to Amazon S3.</summary>
    /// <remarks>
    /// The window is opened from <c>Ride/Addressables/Build and Sync</c>. It builds the active
    /// Addressables settings, refreshes labels on a scene <see cref="AddressableSystem"/>, and can
    /// invoke the locally installed AWS CLI to upload the platform-specific build output.
    /// See <a href="https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/Builds.html">Unity Addressables build documentation</a>
    /// and <a href="https://docs.aws.amazon.com/cli/latest/reference/s3/sync.html">AWS CLI <c>s3 sync</c> documentation</a>.
    /// </remarks>
    public class RideAddressablesBuildWindow : EditorWindow
    {
        /// <summary>AWS access-key identifier entered for the sync operation.</summary>
        private string m_accessKey = "";

        /// <summary>AWS secret access key entered for the sync operation.</summary>
        private string m_secretKey = "";

        /// <summary>AWS region written to the active CLI configuration.</summary>
        private string m_region = "us-west-2";

        /// <summary>Destination S3 URI prefix for Addressables content.</summary>
        private string m_s3BucketPrefix = "s3://dummy-bucket/addressables";

        /// <summary>Most recent build, configuration, or synchronization result shown in the window.</summary>
        private string m_statusMessage = "";

        /// <summary>Opens the Addressables build and synchronization window.</summary>
        [MenuItem("Ride/Addressables/Build and Sync")]
        public static void ShowWindow()
        {
            GetWindow<RideAddressablesBuildWindow>("Build and Sync Addressables");
        }

        /// <summary>Draws the Addressables build, AWS configuration, synchronization, and status controls.</summary>
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

        /// <summary>Builds content from the active Addressables settings and refreshes scene catalog labels.</summary>
        /// <remarks>
        /// Uses <see cref="AddressableAssetSettings.BuildPlayerContent()"/>. If an
        /// <see cref="AddressableSystem"/> exists in the open scenes, its editor catalog cache is
        /// refreshed after the build completes.
        /// </remarks>
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

        /// <summary>Derives the current version, render-pipeline, and build-target path and starts an S3 sync.</summary>
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

        /// <summary>Synchronizes a local Addressables build directory to its corresponding S3 prefix.</summary>
        /// <remarks>
        /// The AWS CLI command uses <c>--delete</c>, so objects under the destination prefix that
        /// are absent from the local build directory are removed during synchronization.
        /// </remarks>
        /// <param name="addressableBuildPath">Path segment composed from Unity version, render pipeline, and build target.</param>
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

        /// <summary>Writes credentials and region to the active AWS CLI configuration.</summary>
        /// <remarks>
        /// This invokes <c>aws configure set</c>; the AWS CLI persists the access and secret keys in
        /// the user's shared credentials file. See
        /// <a href="https://docs.aws.amazon.com/cli/latest/reference/configure/set.html">AWS CLI configuration documentation</a>.
        /// </remarks>
        /// <param name="accessKey">AWS access-key identifier.</param>
        /// <param name="secretKey">AWS secret access key.</param>
        /// <param name="region">Default AWS region.</param>
        /// <returns><see langword="true"/> when every AWS CLI configuration command succeeds; otherwise <see langword="false"/>.</returns>
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
