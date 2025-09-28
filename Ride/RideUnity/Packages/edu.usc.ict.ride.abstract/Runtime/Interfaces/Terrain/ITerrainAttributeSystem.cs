using System.Collections;
using System.Collections.Generic;

namespace Ride.Terrain
{
    /// <summary>
    /// Describes if destruction should deform, or fragment.
    /// </summary>
    public enum TerrainType
    {
        NoType,
        Ground,     // A terrain tile made of the ground.
        Building,   // A building mesh cut from a ground terrain tile.
        LODTree,    // A SpeedTree that hasn't been broken ever, so it stil has LODs.
        TreeWood,   // A SpeedTree that has broken, so it only uses 1 LOD. This type represents a tree fragment that is either a trunk or branch.
        TreeLeaf    // A SpeedTree that has broken, so it only uses 1 LOD. This type represents a tree fragment that is a leaf or frond.
    }

    /// <summary>
    /// Describes the appearance of destructive fragmentation.
    /// </summary>
    public enum FragmentType
    {
        NoType,
        Concrete,   // Uses a typical 3D Voronoi appearance.
        Wood        // Uses a 2D Voronoi appearance stretched into 3D to create long thin cells.
    };

    public class UnityTerrainAttribute
    {
        public TerrainType terrainType { get; set; } = TerrainType.NoType;
        public FragmentType fragmentType { get; set; } = FragmentType.NoType;

        /// <summary>
        /// Terrain loaded via AssetBundle are seperated into collider and renderer objects, to allow those to use different LODS.
        /// This refers to the renderer object.
        /// </summary>
        public RideID RendererObject = RideID.Null;

        /// <summary>
        /// Terrain loaded via AssetBundle are seperated into collider and renderer objects, to allow those to use different LODS.
        /// This refers to the collider object.
        /// </summary>
        public RideID ColliderObject = RideID.Null;
    }


    /// <summary>
    /// System that stores attribute information about loaded terrain, including ground tiles, building tiles, and speed trees.
    /// Attributes include physical properties of the terrain, such as its composition (concrete, wood, dirt, etc).
    /// </summary>
    public interface ITerrainAttributeSystem : IRideSystem
    {
        /// <summary>
        /// Clears all attributes from the system.
        /// </summary>
        void ClearSystem();

        /// <summary>
        /// Creates a new empty TerrainAttribute, which stores a terrain's attribute information.
        /// </summary>
        /// <returns>The RideID of the new terrain attribute</returns>
        RideID CreateTerrainAttribute();

        /// <summary>
        /// Returns UnityTerrainAttribute associated with given RideID.
        /// </summary>
        /// <param name="attribute">RideID</param>
        /// <returns>UnityTerrainAttribute</returns>
        UnityTerrainAttribute GetTerrainAttribute(RideID attribute);

        /// <summary>
        /// Returns RideID of a terrain's attribute information using its collider object ID.
        /// </summary>
        /// <param name="colliderID">RideID of the terrain collider object.</param>
        /// <returns>RideID of a terrain's attribute information</returns>
        RideID GetTerrainAttributeFromCollider(RideID colliderID);

        /// <summary>
        /// Returns terrain type of the terrain object.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute.</param>
        /// <returns>terrain type of the terrain object</returns>
        TerrainType GetTerrainType(RideID terrainAttribute);

        /// <summary>
        /// Returns fragment type of the terrain object.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute.</param>
        /// <returns>fragment type of the terrain object</returns>
        FragmentType GetFragmentType(RideID terrainAttribute);

        /// <summary>
        /// Sets the TerrainType and FragmentType of the TerrainAttribute.
        /// FragmentType is automatically set based on the TerrainType.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute.</param>
        /// <param name="terrainType">TerrainType to be set.</param>
        void SetType(RideID terrainAttribute, TerrainType terrainType);

        /// <summary>
        /// Returns RideID of the object that has the tile's renderer.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute</param>
        /// <returns>RideID of the object that has the tile's renderer</returns>
        RideID GetRendererObject(RideID terrainAttribute);

        /// <summary>
        /// Returns RideID of the object that has the tile's collider.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute</param>
        /// <returns>RideID of the object that has the tile's collider</returns>
        RideID GetColliderObject(RideID terrainAttribute);

        /// <summary>
        /// Terrains loaded via AssetBundle have their collider and renderer separated into different
        /// objects to allow for different LODs for each. This sets the renderer object.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute</param>
        /// <param name="rendererObject">RideID of the object containing the tile's renderer</param>
        void SetRendererObject(RideID terrainAttribute, RideID rendererObject);

        /// <summary>
        /// Terrains loaded via AssetBundle have their collider and renderer separated into different
        /// objects to allow for different LODs for each. This sets the collider object.
        /// </summary>
        /// <param name="terrainAttribute">RideID of the terrain attribute</param>
        /// <param name="colliderObject">RideID of the object containing the tile's collider</param>
        void SetColliderObject(RideID terrainAttribute, RideID colliderObject);

        /// <summary>
        /// Returns all attribute RideIDs in use by the scene.
        /// </summary>
        /// <returns>Collection of attribute RideIDs</returns>
        IEnumerable<RideID> GetAllAttributes();
    }
}
