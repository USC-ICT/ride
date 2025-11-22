namespace Ride
{
    /// <summary>
    /// System for handling configuration files
    /// </summary>
    public interface IConfigurationSystem
    {
        /// <summary>
        /// The configuration data
        /// </summary>
        RideConfig Config { get; }

        /// <summary>
        /// Loads configuration from the default config path on disk and sets it as active.
        /// 
        /// The default path is returned by <see cref="ConfigurationSystemUnity.GetDefaultPath"/>.
        /// </summary>
        /// <returns>The loaded configuration.</returns>
        RideConfig Load();

        /// <summary>
        /// Loads a configuration from an in-memory JSON string and sets it as the active config.
        /// 
        /// This is useful for loading configuration from sources other than disk
        /// (e.g., network, embedded resources, or temporary overrides).
        /// </summary>
        /// <param name="json">JSON contents representing a <see cref="RideConfig"/>.</param>
        /// <returns>The loaded configuration (or defaults if parsing fails).</returns>
        RideConfig LoadFromJson(string json);

        /// <summary>
        /// Save the configuration data
        /// </summary>
        void Save();

        /// <summary>
        /// Replaces the currently loaded configuration with the provided one.
        /// 
        /// This does not automatically persist the config to disk. Call <see cref="Save"/>
        /// if you want the new configuration to be written to <see cref="ConfigPath"/>.
        /// </summary>
        /// <param name="config">The configuration to become active.</param>
        void SetConfig(RideConfig config);
    }
}
