using System;

namespace Ride.Terrain
{
    /// <summary>Defines the conventional filenames used by RIDE terrain datasets.</summary>
    public static class TerrainConstants
    {
        /// <summary>The ground-mesh asset-bundle filename.</summary>
        public static string groundMeshName = "groundMesh.lmab";
        /// <summary>The interior-mesh asset-bundle filename.</summary>
        public static string interiorMeshName = "interiorMesh.lmab";
        /// <summary>The combined terrain-mesh asset-bundle filename.</summary>
        public static string fullMeshName = "terrainMesh.lmab";
        /// <summary>The georeference metadata filename.</summary>
        public static string geoRefName = "metadata.xml";
        /// <summary>The latitude-and-longitude zone filename.</summary>
        public static string latLngName = "zoneLatLng.json";
        /// <summary>The sand-table metadata filename.</summary>
        public static string sandTableName = "SandTableMetadata.xml";
    }

    /// <summary>Stores optional default values used by terrain-loading tools.</summary>
    public static class TerrainDefaults
    {
        // If loading locally from the loader use these static strings
        /// <summary>The default local terrain preset path.</summary>
        public static string presetPath;
        /// <summary>The default ground level of detail.</summary>
        public static string groundLod;
        /// <summary>The default building level of detail.</summary>
        public static string buildingLod;
        /// <summary>Whether tree loading is disabled by default.</summary>
        public static bool disableTrees;
    }

    /// <summary>Stores serialized mesh geometry and texture metadata for a terrain mesh.</summary>
    public struct MeshData
    {
        /// <summary>The texture asset name.</summary>
        public string textureName;
        /// <summary>The number of vertices.</summary>
        public UInt16 numVerts;
        /// <summary>The packed vertex coordinates.</summary>
        public float[] vertices;
        /// <summary>The packed texture coordinates.</summary>
        public float[] uvs;
        /// <summary>The number of faces.</summary>
        public UInt16 numFaces;
        /// <summary>The vertex indices that define the faces.</summary>
        public int[] faces;
    }

    /// <summary>Reports aggregate and subsystem progress while terrain is loading.</summary>
    public class LoadTerrainProgress
    {
        /// <summary>The normalized overall loading completion value.</summary>
        public float overallProgress;
        /// <summary>Whether terrain rendering data has finished loading.</summary>
        public bool renderLoadFinished;
        /// <summary>Whether terrain physics data has finished loading.</summary>
        public bool physicsLoadFinished;
        /// <summary>Whether terrain navigation data has finished loading.</summary>
        public bool navMeshLoadFinished;
    }

    /// <summary>Collects detailed source, rendering, physics, navigation, and progress settings for terrain loading.</summary>
    public class LoadTerrainParams
    {
        /// <summary>Whether tree assets are resolved from local data.</summary>
        public bool localTrees;
        /// <summary>The primary terrain dataset or description path.</summary>
        public string path;
        /// <summary>The optional CRC-file path used to validate the terrain description.</summary>
        public string pathcrc; // this is the path to a .crc file containing the crc for the description file.  Only used if useTiles = true.  keep as null if you don't want to check the crc

        /// <summary>The optional asset-catalog path used when <see cref="useAssetCatalog"/> is enabled.</summary>
        public string assetCatalogPath;

        /// <summary>The terrain rendering level of detail to load.</summary>
        public string lod;
        /// <summary>The asset-bundle path containing collider geometry.</summary>
        public string assetBundleColliderPath;
        /// <summary>The collider level of detail to load.</summary>
        public string assetBundleColliderLod;
        /// <summary>Whether terrain collider and physics data should be loaded.</summary>
        public bool loadPhysics = true;
        /// <summary>Whether vegetation should be loaded.</summary>
        public bool loadTrees = true;
        /// <summary>The optional name assigned to the terrain's root scene object.</summary>
        public string gameObjectRootName;

        /// <summary>The world-space position used to prioritize nearby tile loading.</summary>
        public RideVector3 startLoadingPriorityPosition; // prioritize loading of tiles based on the distance from this position

        /// <summary>The maximum tile-loading radius, or a negative value for no radius limit.</summary>
        public int renderRadius = -1;
        /// <summary>Whether terrain assets should be resolved through an asset catalog.</summary>
        public bool useAssetCatalog = false;
        /// <summary>Whether terrain meshes should be loaded from asset bundles.</summary>
        public bool useAssetBundles = true;
        /// <summary>The CPU time-slice priority used by asynchronous asset-bundle loading.</summary>
        public float cpuPriority = 0.2f;
        /// <summary>The optional navigation loading or generation settings.</summary>
        public LoadNavigationParams navParams;
        /// <summary>The optional terrain-loading progress reporter.</summary>
        public IProgress<LoadTerrainProgress> loadTerrainProgress;
        /// <summary>Whether the terrain should be loaded through the local-data path.</summary>
        public bool loadLocal;
        /// <summary>Whether building geometry should be loaded.</summary>
        public bool loadBuildings = true;
    }

    /// <summary>Loads, modifies, samples, and converts coordinates for RIDE terrain datasets.</summary>
    /// <remarks>Implemented by the RIDE Terrain package and used by movement, scenarios, HMIF, REST services, and navigation generation.</remarks>
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
        /// <param name="precision">Optional precision level (0-5) for placement accuracy. Higher values yield finer placement.</param>
        /// <returns>The world-space position corresponding to the given latitude and longitude.</returns>
        RideVector3 ConvertToScenePosition(double lat, double lng, int precision = 0);

        /// <summary>
        /// Converts a scene/world position into a latitude/longitude coordinate.
        /// </summary>
        /// <param name="scenePosition">The world position to convert.</param>
        /// <param name="precision">Optional precision level (0-5) to round the result.</param>
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
