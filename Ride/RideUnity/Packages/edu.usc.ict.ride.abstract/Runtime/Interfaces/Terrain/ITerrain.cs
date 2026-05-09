namespace Ride.Terrain
{
    /// <summary>
    /// Represents a loaded terrain dataset that can be passed between terrain,
    /// navigation, and scenario systems.
    /// </summary>
    /// <remarks>
    /// Implementations identify the terrain source folder used to load the dataset.
    /// <see cref="ITerrainSystem"/> returns this interface from loading methods such as
    /// <see cref="ITerrainSystem.LoadTerrain(LoadTerrainParams)"/>, and accepts it when
    /// unloading, checking, or hiding a terrain instance. <see cref="INavigationSystem"/>
    /// also accepts an <see cref="ITerrain"/> when generating navigation data.
    /// </remarks>
    public interface ITerrain
    {
        /// <summary>Gets or sets the index of the configured terrain data folder used by this terrain.</summary>
        int dataFolderIndex { get; set; }

        /// <summary>Gets or sets the terrain data folder path or identifier used to load this terrain.</summary>
        string dataFolder { get; set; }

        //ITerrainSystem terrainSystem { get; set; }
    }
}
