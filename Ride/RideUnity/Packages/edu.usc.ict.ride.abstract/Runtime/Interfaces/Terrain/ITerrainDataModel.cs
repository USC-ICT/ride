namespace Ride.Terrain
{
    /// <summary>
    /// The data model for terrain sets
    /// </summary>
    public interface ITerrainDataModel
    {
        DataModelTypes type { get; set; }
    }


    public enum DataModelTypes
    {
        BuildingExterior,
        BuildingInterior,
        Vegetation,
        Ground,
        Civil
    }
}
