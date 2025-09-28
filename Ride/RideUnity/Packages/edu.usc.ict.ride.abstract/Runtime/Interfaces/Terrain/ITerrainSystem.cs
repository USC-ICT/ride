using System;

namespace Ride.Terrain
{
    public static class TerrainConstants
    {
        public static string groundMeshName = "groundMesh.lmab";
        public static string interiorMeshName = "interiorMesh.lmab";
        public static string fullMeshName = "terrainMesh.lmab";
        public static string geoRefName = "metadata.xml";
        public static string latLngName = "zoneLatLng.json";
        public static string sandTableName = "SandTableMetadata.xml";
    }

    public static class TerrainDefaults
    {
        // If loading locally from the loader use these static strings
        public static string presetPath;
        public static string groundLod;
        public static string buildingLod;
        public static bool disableTrees;
    }

    public struct MeshData
    {
        public string textureName;
        public UInt16 numVerts;
        public float[] vertices;
        public float[] uvs;
        public UInt16 numFaces;
        public int[] faces;
    }

    public class LoadTerrainProgress
    {
        public float overallProgress;
        public bool renderLoadFinished;
        public bool physicsLoadFinished;
        public bool navMeshLoadFinished;
    }

    public class LoadTerrainParams
    {
        public bool localTrees;
        public string path;
        public string pathcrc; // this is the path to a .crc file containing the crc for the description file.  Only used if useTiles = true.  keep as null if you don't want to check the crc

        public string assetCatalogPath;

        public string lod;
        public string assetBundleColliderPath;
        public string assetBundleColliderLod;
        public bool loadPhysics = true;
        public bool loadTrees = true;
        public string gameObjectRootName;

        public RideVector3 startLoadingPriorityPosition; // prioritize loading of tiles based on the distance from this position

        public int renderRadius = -1;
        public bool useAssetCatalog = false;
        public bool useAssetBundles = true;
        public float cpuPriority = 0.2f;
        public LoadNavigationParams navParams;
        public IProgress<LoadTerrainProgress> loadTerrainProgress;
        public bool loadLocal;
        public bool loadBuildings = true;
    }

    /// <summary>
    /// Interface for the terrain system in RIDE.
    /// Provides functionality for loading terrain data, modifying it,
    /// performing coordinate conversion, and accessing terrain metadata
    /// such as navigation and attributes.
    /// </summary>
    public interface ITerrainSystem : IRideSystem
    {
        // --------------------------------------------------------------------
        // DATA ACCESS
        // --------------------------------------------------------------------

        /// <summary>
        /// Provides access to the navigation system tied to the terrain.
        /// </summary>
        INavigationSystem navigationSystem { get; }

        /// <summary>
        /// Provides access to the terrain attribute system for colliders, renderers, and metadata.
        /// </summary>
        ITerrainAttributeSystem attributeSystem { get; }


        // --------------------------------------------------------------------
        // TERRAIN LOADING & UNLOADING
        // --------------------------------------------------------------------

        /// <summary>
        /// Loads terrain from a given path with a simplified parameter list.
        /// </summary>
        /// <param name="path">The path to the terrain dataset or descriptor.</param>
        /// <param name="lod">The LOD to use when loading.</param>
        /// <param name="loadTerrainProgress">Optional progress reporting callback.</param>
        ITerrain LoadFromPath(string path, string lod, IProgress<LoadTerrainProgress> loadTerrainProgress = default);

        /// <summary>
        /// Loads a flat terrain plane instead of a mesh-based terrain.
        /// Useful for simplified test cases or fallback environments.
        /// </summary>
        /// <param name="loadTerrainProgress">Optional progress reporting callback.</param>
        ITerrain LoadPlane(IProgress<LoadTerrainProgress> loadTerrainProgress = default);

        /// <summary>
        /// Loads terrain using a detailed configuration object.
        /// </summary>
        /// <param name="parameters">The parameters used to drive terrain loading.</param>
        ITerrain LoadTerrain(LoadTerrainParams parameters);

        /// <summary>
        /// Destroys the specified terrain and removes it from the scene.
        /// </summary>
        /// <param name="terrain">The terrain to destroy.</param>
        /// <returns>True if the terrain was successfully destroyed.</returns>
        bool DestroyTerrain(ITerrain terrain);

        /// <summary>
        /// Loads tree data for the given terrain path.
        /// </summary>
        /// <param name="terrainPath">The path to the terrain.</param>
        void LoadTrees(string terrainPath);

        /// <summary>
        /// Returns whether a specific terrain is currently loaded in the scene.
        /// </summary>
        /// <param name="terrain">The terrain instance to check.</param>
        /// <returns>True if the terrain is loaded and active.</returns>
        bool IsTerrainLoaded(ITerrain terrain);


        // --------------------------------------------------------------------
        // VISUALIZATION CONTROL
        // --------------------------------------------------------------------

        /// <summary>
        /// Disables rendering for the specified terrain.
        /// Rendering components (e.g., mesh renderers) are turned off, but other systems like physics remain active.
        /// Useful for swapping terrains without unloading them entirely.
        /// </summary>
        /// <param name="terrain">The terrain to disable rendering on.</param>
        void DisableRendering(ITerrain terrain);

        // --------------------------------------------------------------------
        // POSITION SAMPLING & RAYCASTING
        // --------------------------------------------------------------------

        /// <summary>
        /// Returns the height (Y-value) of the terrain surface directly beneath a given position.
        /// Uses raycasting to determine surface height and is commonly used to place objects flush with the ground.
        /// </summary>
        /// <param name="position">The world position to sample from.</param>
        /// <param name="raycastOriginOffset">Optional offset to adjust the raycast start point above the target position.</param>
        /// <returns>The Y-value of the terrain at the given X,Z location.</returns>
        float GetTerrainHeight(RideVector3 position, RideVector3 raycastOriginOffset = default);

        /// <summary>
        /// Returns the vertical distance from a world position to the terrain surface below it.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <returns>Distance to the ground, or float.MaxValue if no terrain was detected below the position.</returns>
        float GetHeightAboveTerrain(RideVector3 position);

        /// <summary>
        /// Performs a raycast against the terrain to determine intersection points.
        /// </summary>
        /// <param name="ray">The ray to cast into the terrain.</param>
        /// <param name="hit">Outputs the details of the intersection if a hit occurs.</param>
        /// <returns>True if the ray intersects terrain geometry; false otherwise.</returns>
        bool RaycastTerrain(RideRay ray, out RideRaycastHit hit);

        /// <summary>
        /// Queries terrain data models associated with the specified world position.
        /// May include information about what features or metadata are present at that location.
        /// </summary>
        /// <param name="scenePosition">The world position to query.</param>
        /// <returns>Array of terrain data models found at the specified point.</returns>
        ITerrainDataModel[] QueryPoint(RideVector3 scenePosition);


        // --------------------------------------------------------------------
        // TERRAIN MODIFICATION
        // --------------------------------------------------------------------

        /// <summary>
        /// Modifies the vertices of the terrain mesh to simulate destruction or displacement.
        /// Typically used for visual damage effects like craters or explosions.
        /// </summary>
        /// <param name="point">Center world position of the modification.</param>
        /// <param name="radius">The radius around the point to affect.</param>
        /// <param name="power">The strength or depth of the terrain deformation.</param>
        void DestructTerrain(RideVector3 point, float radius, float power);


        // --------------------------------------------------------------------
        // COORDINATE CONVERSION
        // --------------------------------------------------------------------

        /// <summary>
        /// Converts a latitude/longitude coordinate into a world-space scene position.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lng">Longitude in decimal degrees.</param>
        /// <param name="precision">Optional precision level (0–5) for placement accuracy. Higher values yield finer placement.</param>
        /// <returns>The world-space position corresponding to the given latitude and longitude.</returns>
        RideVector3 ConvertToScenePosition(double lat, double lng, int precision = 0);

        /// <summary>
        /// Converts a scene/world position into a latitude/longitude coordinate.
        /// </summary>
        /// <param name="scenePosition">The world position to convert.</param>
        /// <param name="precision">Optional precision level (0–5) to round the result.</param>
        /// <param name="getCenter">If true, returns the lat/lng for the center of the terrain tile.</param>
        /// <returns>A tuple of (latitude, longitude) in decimal degrees.</returns>
        (double, double) ConvertToLatLng(RideVector3 scenePosition, int precision = 0, bool getCenter = false);


        // --------------------------------------------------------------------
        // TERRAIN LAYERS & MASKING
        // --------------------------------------------------------------------

        /// <summary>
        /// Returns the layer index used for terrain objects.
        /// </summary>
        /// <returns>The integer index of the terrain layer.</returns>
        int GetTerrainLayer();

        /// <summary>
        /// Returns a layer mask that includes only the terrain layer.
        /// Useful for filtering collisions or visibility checks.
        /// </summary>
        /// <returns>A mask that matches the terrain layer only.</returns>
        RideLayerMask GetTerrainMask();
    }
}
