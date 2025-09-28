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
        /// Load the configuration data
        /// </summary>
        /// <returns>The configuration data</returns>
        RideConfig Load();

        /// <summary>
        /// Save the configuration data
        /// </summary>
        void Save();
    }
}
