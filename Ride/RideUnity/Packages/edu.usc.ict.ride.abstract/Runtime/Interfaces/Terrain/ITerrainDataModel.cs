namespace Ride.Terrain
{
    /// <summary>Represents typed feature data returned by terrain point queries.</summary>
    public interface ITerrainDataModel
    {
        /// <summary>Gets or sets the terrain feature category represented by the model.</summary>
        DataModelTypes type { get; set; }
    }


    /// <summary>Identifies the category represented by a terrain data model.</summary>
    public enum DataModelTypes
    {
        /// <summary>An exterior building representation.</summary>
        BuildingExterior,
        /// <summary>An interior building representation.</summary>
        BuildingInterior,
        /// <summary>A vegetation representation.</summary>
        Vegetation,
        /// <summary>A ground-surface representation.</summary>
        Ground,
        /// <summary>A civil-infrastructure representation.</summary>
        Civil
    }
}
