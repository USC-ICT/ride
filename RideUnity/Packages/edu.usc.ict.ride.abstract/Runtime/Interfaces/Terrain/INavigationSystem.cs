using System;

namespace Ride.Terrain
{
    /// <summary>Navigation Mesh Generation Type upon loading a terrain</summary>
    public enum NavigationMeshGenerationType
    {
        /// <summary>No navigation mesh generation upon loading - this assumes the user already has an existing nav mesh in the scene or does not need one.</summary>
        None,

        /// <summary>Creates a nav mesh for each tile in the terrain and stitches them together - this allows for the modification of smaller mesh pieces at runtime.</summary>
        Tiled,

        /// <summary>Creates a combined mesh from all the tiles to create a singular nav mesh for the entire terrain.</summary>
        Combined,

        /// <summary>Creates a navigation mesh from a customized mesh designed for that purpose.</summary>
        Custom
    }

    public class LoadNavigationProgress
    {
        public float overallProgress;
    }

    public class LoadNavigationParams
    {
        public NavigationMeshGenerationType navMeshGenType;                 // Type of nav mesh generation
        public string navMeshPath;                                          // Path to custom nav mesh (only if using custom nav mesh type)
        public ITerrain terrain;                                            // Terrain data for generating nav meshes using terrain
        public IProgress<LoadNavigationProgress> loadNavigationProgress;    // Progress var
    }

    /// <summary>
    /// The functionality for loading, managing and altering the navigation system for agents
    /// </summary>
    public interface INavigationSystem : IRideSystem
    {
        /// <summary>
        /// Loads a nav mesh based on the parameters given
        /// </summary>
        /// <param name="parameters">Navigation loading parameters</param>
        /// <returns>The navigation mesh data</returns>
        INavigation LoadNavMesh(LoadNavigationParams parameters);

        /// <summary>
        /// Builds a nav mesh based on custom user data (such as a render mesh to build from)
        /// </summary>
        /// <param name="customMeshPath">Path (string) to the custom mesh used for building a nav mesh</param>
        /// <param name="loadNavigationProgress">Progress var</param>
        /// <returns>The navigation mesh data</returns>
        INavigation LoadCustomNavMesh(string customMeshPath, IProgress<LoadNavigationProgress> loadNavigationProgress = null);

        /// <summary>
        /// Builds a nav mesh based on terrain data passed in
        /// </summary>
        /// <param name="navMeshGenType">The type of nav mesh generation (tiled or combined)</param>
        /// <param name="terrain">Terrain data to build the nav mesh data from</param>
        /// <param name="loadNavigationProgress">Progress var</param>
        /// <returns>The navigation mesh data</returns>
        INavigation GenerateNavMeshFromTerrain(NavigationMeshGenerationType navMeshGenType, ITerrain terrain, IProgress<LoadNavigationProgress> loadNavigationProgress = null);

        /// <summary>
        /// Rebuilds navigation mesh data for an INavigation
        /// </summary>
        /// <param name="tssId">TSSID of the navigation mesh data to be cleared</param>
        /// <returns>True if navigation mesh data rebuilds successfully</returns>
        bool RebuildNavMesh(RideID tssId);

        /// <summary>
        /// Rebuilds navigation mesh data for an INavigation
        /// </summary>
        /// <param name="navMeshData">Navigation mesh data to be rebuilt</param>
        /// <returns>True if navigation mesh data rebuilds successfully</returns>
        bool RebuildNavMesh(INavigation navMeshData);

        /// <summary>
        /// Clears navigation mesh data for an INavigation
        /// </summary>
        /// <param name="tssId">TSSID of the navigation mesh data to be cleared</param>
        /// <returns>True if there was navigation mesh data to clear</returns>
        bool ClearNavMeshData(RideID tssId);

        /// <summary>
        /// Clears navigation mesh data for an INavigation
        /// </summary>
        /// <param name="navMeshData">Navigation mesh data to be cleared</param>
        /// <returns>True if there was navigation mesh data to clear</returns>
        bool ClearNavMeshData(INavigation navMeshData);

        /// <summary>
        /// Places a capsule obstacle in the navigation mesh so that a specific radius becomes non-navigable.
        /// </summary>
        /// <param name="position">The world position of the navigation mesh obstacle</param>
        /// <param name="radius">Radius of space that is non-navigable</param>
        /// <param name="height">Height of space that is non-navigable</param>
        void PlaceNavMeshObstacle_Capsule(RideVector3 position, float radius, float height);

        /// <summary>
        /// Places a box obstacle in the navigation mesh so that a specific radius becomes non-navigable.
        /// </summary>
        /// <param name="position">The world position of the navigation mesh obstacle</param>
        /// <param name="size">Size of the box that carves out the navigation mesh as non-navigable</param>
        void PlaceNavMeshObstacle_Box(RideVector3 position, RideVector3 size);

        /// <summary>
        /// Places a custom obstacle in the navigation mesh so that the area around it becomes non-navigable.
        /// </summary>
        /// <param name="customMeshPath">Path (string) to the custom mesh used for creating the obstacle</param>
        /// <param name="position">Position of the obstacle</param>
        /// <param name="rotation">Rotation of the obstacle</param>
        /// <param name="localScale">Local scale of the obstacle</param>
        void PlaceNavMeshObstacle_Custom(string customMeshPath, RideVector3 position, RideVector3 rotation, RideVector3 localScale);

        bool SamplePosition(RideVector3 sourcePosition, out RideNavMeshHit hit, float maxDistance, int areaMask);
    }
}
