using Ride.Entities;

namespace Ride.Terrain
{
    /// <summary>
    /// The data for terrain 
    /// </summary>
    public interface ITerrain : IEntity // TODO: Remove this .9inheritance
    {
        int dataFolderIndex { get; set; }
        string dataFolder { get; set; }
        //ITerrainSystem terrainSystem { get; set; }
    }
}
