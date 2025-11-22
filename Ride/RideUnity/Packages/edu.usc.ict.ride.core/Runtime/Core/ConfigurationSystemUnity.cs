using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Ride
{
    /// <summary>
    /// Unity MonoBehaviour system for loading, accessing, and persisting the RIDE configuration.
    /// Wraps access to <see cref="RideConfig"/>, and provides convenience methods for validation,
    /// AWS terrain configuration, and other cloud services setup. Automatically loads the configuration 
    /// at runtime from persistent storage.
    /// 
    /// Use <see cref="Load"/> and <see cref="Save"/> to manage configuration lifecycle, or call 
    /// <see cref="ResetConfig"/> to restore defaults. Supports checking for version compatibility 
    /// and validating AWS keys.
    ///
    /// The actual configuration format is defined in <see cref="RideConfig"/>.
    /// </summary>
    public class ConfigurationSystemUnity : RideSystemMonoBehaviour, IConfigurationSystem
    {
        const string FileName = "ride.json";

        private RideConfig m_config;

        public RideConfig Config => m_config;
        public string ConfigPath { get; private set; }  // initialized in ctor since it uses persistantDataPath

        // backwards compatibility
        public RideConfig config => Config;
        public string path => ConfigPath;

        protected override void Awake()
        {
            base.Awake();

            ConfigPath = GetDefaultPath();
            Load();
        }

        /// <inheritdoc/>
        public RideConfig Load() => m_config = Load(ConfigPath);

        /// <inheritdoc/>
        public RideConfig LoadFromJson(string json) => m_config = LoadFromJsonContent(json);

        /// <inheritdoc/>
        public void Save() => Save(Config, ConfigPath);

        /// <inheritdoc/>
        public void SetConfig(RideConfig config) => m_config = config;

        public RideConfig ResetConfig() => m_config = RideConfig.Default;

        /// <summary>
        /// Checks whether the loaded configuration matches the expected version.
        /// 
        /// If this returns <c>false</c>, the config file version is outdated or mismatched.
        /// Projects should give the user or developer an opportunity to back up the existing configuration
        /// before calling <see cref="ResetConfig"/> and <see cref="Save"/> to regenerate a valid one.
        /// </summary>
        /// <returns><c>true</c> if the loaded configuration version matches the default; otherwise, <c>false</c>.</returns>
        public bool IsCorrectVersion() => IsCorrectVersion(Config);

        /// <summary>
        /// Validates the format of a Cognito Identity Pool ID used for AWS terrain loading.
        /// 
        /// This does not check whether the key itself is valid — only that it appears to be in the correct format,
        /// which is typically <c>"region:key"</c>. It also rejects known dummy values such as <see cref="RideConfig.awsTerrainDefault"/>.
        /// </summary>
        /// <returns><c>true</c> if the format appears valid; otherwise, <c>false</c>.</returns>
        /// <remarks>See: https://docs.aws.amazon.com/cognitoidentity/latest/APIReference/API_DescribeIdentityPool.html</remarks>
        public bool IsTerrainKeyFormatValid() => IsTerrainKeyFormatValid(Config.awsTerrain.cognitoIdentityPoolId);

        public string GetTerrainKey() => GetTerrainKey(Config);

        public string GetTerrainKeyRegion() => GetTerrainKeyRegion(Config);

        public void SetTerrainKey(string cognitoIdentityPoolId) => m_config.awsTerrain.cognitoIdentityPoolId = cognitoIdentityPoolId;

        public void SetQnAMaker(string endpoint, string endpointKey, string kbId)
        {
            m_config.azureQnA.endpoint = endpoint;
            m_config.azureQnA.endpointKey = endpointKey;
            m_config.azureQnA.kbId = kbId;
        }

        #region Public Static Functions

        public static string GetDefaultPath() => $"{RideIO.ApplicationPersistentDataPath()}/config/{FileName}";

        /// <summary>
        /// Loads configuration from a file on disk.
        /// 
        /// If the file does not exist, a default configuration is generated and saved.
        /// If the file exists but cannot be parsed, defaults are returned.
        /// </summary>
        /// <param name="path">Absolute path to the config file.</param>
        /// <returns>The loaded configuration (or defaults if unavailable/invalid).</returns>
        public static RideConfig Load(string path)
        {
            var config = RideConfig.Default;

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                config = LoadFromJsonContent(json);
            }
            else
            {
                Save(config, path);
            }

            return config;
        }

        /// <summary>
        /// Loads a configuration from an in-memory JSON string.
        /// 
        /// If parsing fails, defaults are returned.
        /// </summary>
        /// <param name="json">JSON contents representing a <see cref="RideConfig"/>.</param>
        /// <returns>The parsed configuration, or <see cref="RideConfig.Default"/> on failure.</returns>
        public static RideConfig LoadFromJsonContent(string json)
        {
            var config = RideConfig.Default;

            if (string.IsNullOrWhiteSpace(json))
                return config;

            try
            {
                config = RideIO.JsonDeserialize<RideConfig>(json);
            }
            catch (JsonReaderException e)
            {
                Debug.LogWarning($"ConfigurationSystemUnity.LoadFromJson() - error reading config contents. Is it out of date? Exception: {e}");
                config = RideConfig.Default;
            }

            return config;
        }

        public static void Save(RideConfig config, string path)
        {
            // make sure dest folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string json = RideIO.JsonSerialize(config);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Checks whether the loaded configuration matches the expected version.
        /// 
        /// If this returns <c>false</c>, the config file version is outdated or mismatched.
        /// Projects should give the user or developer an opportunity to back up the existing configuration
        /// before calling <see cref="ResetConfig"/> and <see cref="Save"/> to regenerate a valid one.
        /// </summary>
        /// <param name="config">The config to test.</param>
        /// <returns><c>true</c> if the loaded configuration version matches the default; otherwise, <c>false</c>.</returns>
        public static bool IsCorrectVersion(RideConfig config) => config.version == RideConfig.Default.version;

        /// <summary>
        /// Validates the format of a Cognito Identity Pool ID used for AWS terrain loading.
        /// 
        /// This does not check whether the key itself is valid — only that it appears to be in the correct format,
        /// which is typically <c>"region:key"</c>. It also rejects known dummy values such as <see cref="RideConfig.awsTerrainDefault"/>.
        /// </summary>
        /// <param name="cognitoIdentityPoolId">The AWS Cognito Identity Pool ID string to validate.</param>
        /// <returns><c>true</c> if the format appears valid; otherwise, <c>false</c>.</returns>
        /// <remarks>See: https://docs.aws.amazon.com/cognitoidentity/latest/APIReference/API_DescribeIdentityPool.html</remarks>
        public static bool IsTerrainKeyFormatValid(string cognitoIdentityPoolId)
        {
            /// this function checks to see if the given cognito ID is in a valid format, eg, "region:key"
            /// It does *not* check only to see if it is a valid key.
            /// ref: https://docs.aws.amazon.com/cognitoidentity/latest/APIReference/API_DescribeIdentityPool.html

            var split = cognitoIdentityPoolId.Split(':');
            if (split.Length != 2)
                return false;

            if (string.IsNullOrWhiteSpace(split[0]) ||
                string.IsNullOrWhiteSpace(split[1]))
                return false;

            // If we are using the dummy default key, return false since we know it won't work, and it'll force notify the user that it needs to change
            if (cognitoIdentityPoolId == RideConfig.AWSTerrain.Default.cognitoIdentityPoolId)
                return false;

            // TODO - do further checking on the format (eg, valid regions, proper format, etc).
            //        ref aws link above

            return true;
        }

        public static string GetTerrainKey(RideConfig config) => config.awsTerrain.cognitoIdentityPoolId;

        public static string GetTerrainKeyRegion(RideConfig config) => GetTerrainKeyRegion(config.awsTerrain.cognitoIdentityPoolId);

        public static string GetTerrainKeyRegion(string cognitoIdentityPoolId)
        {
            if (!IsTerrainKeyFormatValid(cognitoIdentityPoolId))
                return null;

            string [] split = cognitoIdentityPoolId.Split(':');
            if (split.Length != 2)
                return null;

            return split[0];
        }

        public static string GetRideRestApi(string functionName)
        {
            const string RideProductionApi = "https://e5kjenv7gc.execute-api.us-west-2.amazonaws.com/Prod";

            string url = "";
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            if (configSystem == null)
                url = $"{RideProductionApi}/{functionName}";
            else
                url = $"{configSystem.Config.restApi.url}/{configSystem.Config.restApi.stage}/{functionName}";

            return url;
        }

        #endregion
    }
}
