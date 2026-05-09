using System.IO;
using System;
using System.Reflection;
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
            if (string.IsNullOrWhiteSpace(json))
                return RideConfig.Default;

            try
            {
                if (IsJsonCurrentVersion(json))
                    return RideIO.JsonDeserialize<RideConfig>(json);

                return LoadMergedOntoDefaults(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ConfigurationSystemUnity.LoadFromJson() - error reading config contents. Is it out of date? Exception: {e}");
                return RideConfig.Default;
            }
        }

        public static void Save(RideConfig config, string path)
        {
            // make sure dest folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string json = RideIO.JsonSerialize(config);
            File.WriteAllText(path, json);
        }

        [Serializable]
        private class VersionNumber
        {
            public int Major;
            public int Minor;
            public int Build;
            public int Revision;
        }

        [Serializable]
        private class VersionProbe
        {
            public VersionNumber version;
        }

        private static bool IsJsonCurrentVersion(string json)
        {
            try
            {
                var probe = JsonConvert.DeserializeObject<VersionProbe>(json);
                if (probe.version == null)
                    return false;

                return probe.version.Major == RideConfig.Default.version.Major &&
                       probe.version.Minor == RideConfig.Default.version.Minor &&
                       probe.version.Build == RideConfig.Default.version.Build &&
                       probe.version.Revision == RideConfig.Default.version.Revision;
            }
            catch
            {
                return false;
            }
        }

        private static RideConfig LoadMergedOntoDefaults(string json)
        {
            var result = RideConfig.Default;

            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Populate,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Error = (sender, args) => { args.ErrorContext.Handled = true; }
            };

            RideConfig temp;
            try
            {
                temp = JsonConvert.DeserializeObject<RideConfig>(json, settings);
            }
            catch
            {
                temp = default;
            }

            object boxedDst = result;
            OverlayRecursive(temp, boxedDst, typeof(RideConfig));
            return (RideConfig)boxedDst;
        }

        private static void OverlayRecursive(object src, object dst, Type t)
        {
            if (src == null || dst == null || IsLeaf(t))
                return;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;
            var fields = t.GetFields(Flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldType = field.FieldType;
                var srcVal = field.GetValue(src);
                var dstVal = field.GetValue(dst);

                if (IsLeaf(fieldType))
                {
                    if (LooksPresent(srcVal, fieldType))
                        field.SetValue(dst, CoerceIfNeeded(srcVal, fieldType));
                }
                else if (srcVal != null)
                {
                    if (dstVal == null && fieldType.IsClass)
                    {
                        dstVal = Activator.CreateInstance(fieldType);
                        field.SetValue(dst, dstVal);
                    }

                    OverlayRecursive(srcVal, dstVal, fieldType);
                    field.SetValue(dst, dstVal);
                }
            }
        }

        private static bool IsLeaf(Type t)
        {
            if (t.IsPrimitive) return true;
            if (t == typeof(string)) return true;
            if (t.IsEnum) return true;
            if (t == typeof(Version)) return true;

            return false;
        }

        private static bool LooksPresent(object val, Type t)
        {
            if (val == null) return false;
            if (t == typeof(string)) return !string.IsNullOrEmpty((string)val);
            if (t.IsEnum) return true;
            if (t == typeof(bool)) return true;
            if (t == typeof(ushort)) return (ushort)val != default(ushort);
            if (t == typeof(int)) return (int)val != default;
            if (t == typeof(float)) return Math.Abs((float)val - default(float)) > float.Epsilon;
            if (t == typeof(double)) return Math.Abs((double)val - default(double)) > double.Epsilon;
            if (t == typeof(Version)) return true;

            return true;
        }

        private static object CoerceIfNeeded(object val, Type t)
        {
            if (t == typeof(ushort))
            {
                int asInt = Convert.ToInt32(val);
                if (asInt < 0) asInt = 0;
                if (asInt > 65535) asInt = 65535;
                return (ushort)asInt;
            }

            return val;
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

        /// <summary>
        /// Builds the configured RIDE REST API endpoint URL for the specified backend function.
        /// </summary>
        /// <param name="functionName">The REST API function path to append to the configured base URL and stage.</param>
        /// <returns>
        /// The full RIDE REST API endpoint URL, or <c>null</c> if the configuration system is unavailable
        /// or the REST API configuration is incomplete.
        /// </returns>
        public static string GetRideRestApi(string functionName)
        {
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            if (configSystem == null)
            {
                RideLog.LogError($"{nameof(ConfigurationSystemUnity)}.{nameof(GetRideRestApi)}() failed. ConfigurationSystemUnity is not available.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(configSystem.Config.restApi.url) ||
                string.IsNullOrWhiteSpace(configSystem.Config.restApi.stage) ||
                string.IsNullOrWhiteSpace(functionName))
            {
                RideLog.LogError($"{nameof(ConfigurationSystemUnity)}.{nameof(GetRideRestApi)}() failed. REST API configuration is incomplete.");
                return null;
            }

            return $"{configSystem.Config.restApi.url}/{configSystem.Config.restApi.stage}/{functionName}";
        }

        /// <summary>
        /// Gets the configured RIDE endpoint that creates signed storage URLs.
        /// </summary>
        /// <returns>
        /// The configured signed URL endpoint, or <c>null</c> if the configuration system is unavailable
        /// or the endpoint is not configured.
        /// </returns>
        public static string GetStorageSignedUrlEndpoint()
        {
            if (!TryGetRestApiSettings(nameof(GetStorageSignedUrlEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.signedUrlEndpoint, nameof(RideConfig.RestServerApiSettings.signedUrlEndpoint), nameof(GetStorageSignedUrlEndpoint));
        }

        /// <summary>Gets the configured RIDE WebGL proxy endpoint for Anthropic chat requests.</summary>
        /// <returns>The configured Anthropic proxy endpoint, or <c>null</c> if it is not configured.</returns>
        public static string GetAnthropicProxyEndpoint()
        {
            if (!TryGetRestApiSettings(nameof(GetAnthropicProxyEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.anthropicProxyEndpoint, nameof(RideConfig.RestServerApiSettings.anthropicProxyEndpoint), nameof(GetAnthropicProxyEndpoint));
        }

        /// <summary>Gets the configured RIDE WebGL proxy endpoint for OpenAI chat requests.</summary>
        /// <returns>The configured OpenAI proxy endpoint, or <c>null</c> if it is not configured.</returns>
        public static string GetOpenAIProxyEndpoint()
        {
            if (!TryGetRestApiSettings(nameof(GetOpenAIProxyEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.openAIProxyEndpoint, nameof(RideConfig.RestServerApiSettings.openAIProxyEndpoint), nameof(GetOpenAIProxyEndpoint));
        }

        /// <summary>Builds a configured RIDE WebGL proxy endpoint URL for Azure Text-To-Speech requests.</summary>
        /// <param name="route">The proxy route to append to the configured Azure Text-To-Speech proxy endpoint.</param>
        /// <returns>The configured Azure Text-To-Speech proxy URL, or <c>null</c> if it is not configured.</returns>
        public static string GetAzureTtsProxyEndpoint(string route)
        {
            if (!TryGetRestApiSettings(nameof(GetAzureTtsProxyEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.azureTtsProxyEndpoint, nameof(RideConfig.RestServerApiSettings.azureTtsProxyEndpoint), nameof(GetAzureTtsProxyEndpoint), route);
        }

        /// <summary>Builds a configured RIDE WebGL proxy endpoint URL for ElevenLabs Text-To-Speech requests.</summary>
        /// <param name="route">The proxy route to append to the configured ElevenLabs Text-To-Speech proxy endpoint.</param>
        /// <returns>The configured ElevenLabs Text-To-Speech proxy URL, or <c>null</c> if it is not configured.</returns>
        public static string GetElevenLabsTtsProxyEndpoint(string route)
        {
            if (!TryGetRestApiSettings(nameof(GetElevenLabsTtsProxyEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.elevenLabsTtsProxyEndpoint, nameof(RideConfig.RestServerApiSettings.elevenLabsTtsProxyEndpoint), nameof(GetElevenLabsTtsProxyEndpoint), route);
        }

        /// <summary>Builds a configured RIDE WebGL proxy endpoint URL for AWS Polly Text-To-Speech requests.</summary>
        /// <param name="route">The proxy route to append to the configured AWS Polly Text-To-Speech proxy endpoint.</param>
        /// <returns>The configured AWS Polly Text-To-Speech proxy URL, or <c>null</c> if it is not configured.</returns>
        public static string GetPollyTtsProxyEndpoint(string route)
        {
            if (!TryGetRestApiSettings(nameof(GetPollyTtsProxyEndpoint), out var restApi))
                return null;

            return GetConfiguredEndpoint(restApi.pollyTtsProxyEndpoint, nameof(RideConfig.RestServerApiSettings.pollyTtsProxyEndpoint), nameof(GetPollyTtsProxyEndpoint), route);
        }

        private static bool TryGetRestApiSettings(string callerName, out RideConfig.RestServerApiSettings restApi)
        {
            restApi = default;

            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            if (configSystem == null)
            {
                RideLog.LogError($"{nameof(ConfigurationSystemUnity)}.{callerName}() failed. ConfigurationSystemUnity is not available.");
                return false;
            }

            restApi = configSystem.Config.restApi;
            return true;
        }

        private static string GetConfiguredEndpoint(string endpoint, string endpointName, string callerName, string route = null)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                RideLog.LogError($"{nameof(ConfigurationSystemUnity)}.{callerName}() failed. REST API endpoint '{endpointName}' is not configured.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(route))
                return endpoint;

            return $"{endpoint.TrimEnd('/')}/{route.TrimStart('/')}";
        }

        #endregion
    }
}
