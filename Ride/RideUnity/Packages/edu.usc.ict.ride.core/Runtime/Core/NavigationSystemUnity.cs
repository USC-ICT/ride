using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ride.Terrain.Navigation
{
    public class NavigationSystemUnity : RideSystemMonoBehaviour, INavigationSystem
    {
        List<INavigation> navMeshDataList = new List<INavigation>();

        #region INavigationSystem funcs
        /// <summary>
        /// Loads a nav mesh based on the parameters given
        /// </summary>
        /// <param name="parameters">Navigation loading parameters</param>
        /// <returns>The navigation mesh data</returns>
        public INavigation LoadNavMesh(LoadNavigationParams parameters)
        {
            if (parameters.navMeshGenType == NavigationMeshGenerationType.Custom)
                return LoadCustomNavMesh(parameters.navMeshPath, parameters.loadNavigationProgress);
            else
                return GenerateNavMeshFromTerrain(parameters.navMeshGenType, parameters.terrain, parameters.loadNavigationProgress);
        }

        /// <summary>
        /// Builds a nav mesh based on custom user data (such as a render mesh to build from)
        /// </summary>
        /// <param name="customMeshPath">Path (string) to the custom mesh used for building a nav mesh</param>
        /// <param name="loadNavigationProgress">Progress var</param>
        /// <returns>The navigation mesh data</returns>
        public INavigation LoadCustomNavMesh(string customMeshPath, IProgress<LoadNavigationProgress> loadNavigationProgress = null)
        {
            LoadNavigationProgress progress = new LoadNavigationProgress();
            loadNavigationProgress?.Report(progress);

            NavigationMono navMeshAsset = GetAssetFromPath(customMeshPath);
            if (navMeshAsset != null)
                navMeshAsset.StartCoroutine(GenerateNavMeshCustom(navMeshAsset, progress));
            else
                NavMeshFailMessage("No custom nav mesh asset found");

            navMeshDataList.Add(navMeshAsset);

            return navMeshAsset;
        }

        /// <summary>
        /// Builds a nav mesh based on terrain data passed in
        /// </summary>
        /// <param name="navMeshGenType">The type of nav mesh generation (tiled or combined)</param>
        /// <param name="terrain">Terrain data to build the nav mesh data from</param>
        /// <param name="loadNavigationProgress">Progress var</param>
        /// <returns>The navigation mesh data</returns>
        public INavigation GenerateNavMeshFromTerrain(NavigationMeshGenerationType navMeshGenType, ITerrain terrain, IProgress<LoadNavigationProgress> loadNavigationProgress = null)
        {
            if (terrain == null)
                NavMeshFailMessage("No terrain found to build nav mesh from"); ;


            //if (terrain is TerrainMono terrainMono)
            {
                LoadNavigationProgress progress = new LoadNavigationProgress();
                loadNavigationProgress?.Report(progress);

                switch (navMeshGenType)
                {
                    case NavigationMeshGenerationType.Tiled:
#if false
                        NavigationMono navMono = (terrainMono.terrainRoot.GetComponentInChildren<NavigationMono>() == null) ? terrainMono.terrainRoot.AddComponent<NavigationMono>() : terrainMono.terrainRoot.GetComponentInChildren<NavigationMono>();
                        navMono.StartCoroutine(GenerateNavMeshTiled(terrainMono.terrainRoot, progress));
                        navMeshDataList.Add(navMono);
                        return navMono;
#else
                        Debug.LogError($"NavigationSystemMono.GenerateNavMeshFromTerrain() - TODO - RIDE Modularization - needs to be refactored");
                        return null;
#endif
                    case NavigationMeshGenerationType.Combined:
#if false
                        GameObject newObject = new GameObject("CombinedTerrain");
                        newObject.transform.parent = terrainMono.terrainRoot.transform;
                        navMono = newObject.AddComponent<NavigationMono>();
                        navMono.StartCoroutine(GenerateNavMeshCombined(terrainMono.terrainRoot, progress, navMono));
                        navMeshDataList.Add(navMono);
                        return navMono;
#else
                        Debug.LogError($"NavigationSystemMono.GenerateNavMeshFromTerrain() - TODO - RIDE Modularization - needs to be refactored");
                        return null;
#endif
                    default:
                        progress.overallProgress = 1.0f;
                        break;
                };
            }

            return null;
        }

        /// <summary>
        /// Rebuilds navigation mesh data for an INavigation
        /// </summary>
        /// <param name="rideID">TSSID of the navigation mesh data to be cleared</param>
        /// <returns>True if navigation mesh data rebuilds successfully</returns>
        public bool RebuildNavMesh(RideID rideID)
        {
            return RebuildNavMesh(navMeshDataList.Find(i => i.id == rideID));
        }

        /// <summary>
        /// Rebuilds navigation mesh data for an INavigation
        /// </summary>
        /// <param name="navMeshData">Navigation mesh data to be rebuilt</param>
        /// <returns>True if navigation mesh data rebuilds successfully</returns>
        public bool RebuildNavMesh(INavigation navMeshData)
        {
            if (navMeshData != null && navMeshData is NavigationMono navMeshDataMono)
            {
                navMeshDataMono.StartCoroutine(RebuildNavMesh_Internal(navMeshDataMono));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears navigation mesh data for an INavigation
        /// </summary>
        /// <param name="tssId">TSSID of the navigation mesh data to be cleared</param>
        /// <returns>True if there was navigation mesh data to clear</returns>
        public bool ClearNavMeshData(RideID tssId)
        {
            return ClearNavMeshData(navMeshDataList.Find(i => i.id == tssId));
        }

        /// <summary>
        /// Clears navigation mesh data for an INavigation
        /// </summary>
        /// <param name="navMeshData">Navigation mesh data to be cleared</param>
        /// <returns>True if there was navigation mesh data to clear</returns>
        public bool ClearNavMeshData(INavigation navMeshData)
        {
            if (navMeshData != null && navMeshData is NavigationMono navMeshDataMono)
            {
                foreach (NavMeshSurface surface in navMeshDataMono.GetComponentsInChildren<NavMeshSurface>())
                    surface.RemoveData();

                var surfaceSelf = navMeshDataMono.GetComponent<NavMeshSurface>();
                if (surfaceSelf != null)
                    surfaceSelf.RemoveData();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Places a capsule obstacle in the navigation mesh so that a specific radius becomes non-navigable.
        /// </summary>
        /// <param name="position">The world position of the navigation mesh obstacle</param>
        /// <param name="radius">Radius of space that is non-navigable</param>
        /// <param name="height">Height of space that is non-navigable</param>
        public void PlaceNavMeshObstacle_Capsule(RideVector3 position, float radius, float height)
        {
            GameObject navMeshObstacleObj = new GameObject("NavMeshObstacle");
            NavMeshObstacle navMeshObstacle = navMeshObstacleObj.AddComponent<NavMeshObstacle>();
            navMeshObstacle.transform.position = position.ToVector3();
            navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
            navMeshObstacle.radius = radius;
            navMeshObstacle.height = height;
            navMeshObstacle.carving = true;
        }

        /// <summary>
        /// Places a box obstacle in the navigation mesh so that a specific radius becomes non-navigable.
        /// </summary>
        /// <param name="position">The world position of the navigation mesh obstacle</param>
        /// <param name="size">Size of the box that carves out the navigation mesh as non-navigable</param>
        public void PlaceNavMeshObstacle_Box(RideVector3 position, RideVector3 size)
        {
            GameObject navMeshObstacleObj = new GameObject("NavMeshObstacle");
            NavMeshObstacle navMeshObstacle = navMeshObstacleObj.AddComponent<NavMeshObstacle>();
            navMeshObstacle.transform.position = position.ToVector3();
            navMeshObstacle.shape = NavMeshObstacleShape.Box;
            navMeshObstacle.size = size.ToVector3();
            navMeshObstacle.carving = true;
        }

        /// <summary>
        /// Places a custom obstacle in the navigation mesh so that the area around it becomes non-navigable.
        /// </summary>
        /// <param name="customMeshPath">Path (string) to the custom mesh used for creating the obstacle</param>
        /// <param name="position">Position of the obstacle</param>
        /// <param name="rotation">Rotation of the obstacle</param>
        /// <param name="localScale">Local scale of the obstacle</param>
        public void PlaceNavMeshObstacle_Custom(string customMeshPath, RideVector3 position, RideVector3 rotation, RideVector3 localScale)
        {
#if UNITY_EDITOR
            GameObject assetObj = AssetDatabase.LoadAssetAtPath<GameObject>(customMeshPath);
            MeshFilter meshAsset = assetObj.GetComponentInChildren<MeshFilter>();
            MeshFilter clonedAsset = (meshAsset != null) ? UnityEngine.Object.Instantiate(meshAsset) : null;
            if (clonedAsset != null)
            {
                NavigationMono navMonoAsset = clonedAsset.gameObject.AddComponent<NavigationMono>();
                navMonoAsset.StartCoroutine(PlaceNavMeshObstacle_Custom_Internal(navMonoAsset, position.ToVector3(), rotation.ToVector3(), localScale.ToVector3()));
            }
#endif
        }
        #endregion


        #region NavigationSystemMono funcs
        IEnumerator GenerateNavMeshCustom(NavigationMono customNavMesh, LoadNavigationProgress progress)
        {
            Debug.Log("Building Nav Mesh (Custom)");
            float startTime = Time.realtimeSinceStartup;

            MeshFilter terrainMeshFilter = customNavMesh.GetComponent<MeshFilter>();
            MeshRenderer navMeshRenderer = terrainMeshFilter.GetComponent<MeshRenderer>();
            if (terrainMeshFilter != null && navMeshRenderer != null)
            {
                NavMeshSurface navMeshTerrainSurface = terrainMeshFilter.gameObject.AddComponent<NavMeshSurface>();
                SetEnableOtherMeshRenderers(false, new MeshRenderer[] { navMeshRenderer });
                navMeshRenderer.enabled = true;
                navMeshTerrainSurface.BuildNavMesh();
                yield return new WaitForEndOfFrame();
                Debug.Log("Nav mesh built in: " + (Time.realtimeSinceStartup - startTime).ToString("F10"));
                navMeshRenderer.enabled = false;
                SetEnableOtherMeshRenderers(true, new MeshRenderer[] { navMeshRenderer });
                progress.overallProgress = 1.0f;
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator GenerateNavMeshCombined(GameObject terrainRoot, LoadNavigationProgress progress, NavigationMono navMono = null)
        {
            Debug.Log("Building Nav Mesh (Combined)");
            float startTime = Time.realtimeSinceStartup;
            GameObject navMeshTerrain = CombineTerrain(terrainRoot, navMono);
            navMeshTerrain.AddComponent<MeshCollider>();
            progress.overallProgress = 0.5f;
            yield return new WaitForEndOfFrame();

            NavMeshSurface navMeshTerrainSurface = navMeshTerrain.AddComponent<NavMeshSurface>();
            SetEnableOtherMeshRenderers(false, null);
            navMeshTerrain.GetComponent<MeshRenderer>().enabled = true;
            navMeshTerrainSurface.BuildNavMesh();
            Debug.Log("Nav mesh built in: " + (Time.realtimeSinceStartup - startTime).ToString("F10"));
            SetEnableOtherMeshRenderers(true, null);
            navMeshTerrain.GetComponent<MeshRenderer>().enabled = false;
            progress.overallProgress = 1.0f;
            yield return new WaitForEndOfFrame();
        }

        IEnumerator GenerateNavMeshTiled(GameObject terrainRoot, LoadNavigationProgress progress)
        {
            Debug.Log("Building Nav Mesh (Tiled)");
            float startTime = Time.realtimeSinceStartup;
            float lastStartTime = startTime;
            List<MeshFilter> meshFilterList = new List<MeshFilter>(terrainRoot.transform.GetComponentsInChildren<MeshFilter>());
            meshFilterList.RemoveAll(x => x.GetComponent<MeshRenderer>() == null);

            List<MeshRenderer> meshRendererList = new List<MeshRenderer>(terrainRoot.transform.GetComponentsInChildren<MeshRenderer>());
            SetEnableMeshRenderers(false, meshRendererList.ToArray());

            int i = 0;
            foreach (MeshFilter mesh in meshFilterList)
            {
                i++;
                mesh.GetComponent<MeshRenderer>().enabled = true;
                mesh.gameObject.AddComponent<NavMeshSurface>();
                mesh.GetComponent<NavMeshSurface>().BuildNavMesh();
                Debug.Log("Piece " + i.ToString() + " out of " + meshFilterList.Count + " built in " + (Time.realtimeSinceStartup - lastStartTime).ToString("F10"));
                lastStartTime = Time.realtimeSinceStartup;

                float navMeshProgress = i / meshFilterList.Count;
                progress.overallProgress = navMeshProgress;
                mesh.GetComponent<MeshRenderer>().enabled = false;
                yield return new WaitForEndOfFrame();
            }

            SetEnableMeshRenderers(true, meshRendererList.ToArray());
            progress.overallProgress = 1.0f;
            yield return new WaitForEndOfFrame();
            Debug.Log("Nav mesh built in: " + (Time.realtimeSinceStartup - startTime).ToString("F10"));
        }

        private GameObject CombineTerrain(GameObject terrainRoot, NavigationMono navMono = null)
        {
            List<MeshFilter> meshFilterList = new List<MeshFilter>(terrainRoot.transform.GetComponentsInChildren<MeshFilter>());
            meshFilterList.RemoveAll(x => x.GetComponent<MeshRenderer>() == null);

            CombineInstance[] combine = new CombineInstance[meshFilterList.Count];
            int i = 0;
            int combinedVertexCount = 0;
            while (i < meshFilterList.Count)
            {
                combine[i].mesh = meshFilterList[i].sharedMesh;
                combine[i].transform = meshFilterList[i].transform.localToWorldMatrix;
                combinedVertexCount += meshFilterList[i].sharedMesh.vertexCount;

                i++;
            }

            GameObject combinedTerrain = (navMono != null) ? navMono.gameObject : new GameObject("CombinedTerrain");
            combinedTerrain.AddComponent<MeshFilter>();
            combinedTerrain.AddComponent<MeshRenderer>();
            combinedTerrain.transform.GetComponent<MeshFilter>().mesh = new Mesh();

            // if the vertex count in the combined mesh is larger than UInt16, changes the vertex index format to UInt32
            if (combinedVertexCount >= 65535)
                combinedTerrain.transform.GetComponent<MeshFilter>().mesh.indexFormat = IndexFormat.UInt32;
            combinedTerrain.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);

            return combinedTerrain;
        }

        private NavigationMono GetAssetFromPath(string path)
        {
#if UNITY_EDITOR
            GameObject assetObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshFilter meshAsset = assetObj.GetComponentInChildren<MeshFilter>();
            MeshFilter clonedAsset = (meshAsset != null) ? UnityEngine.Object.Instantiate(meshAsset) : null;
            return (clonedAsset != null) ? clonedAsset.gameObject.AddComponent<NavigationMono>() : null;
#else
            return null; // TODO: Implement alternative method of retrieving mesh asset
#endif
        }

        private void SetEnableMeshRenderers(bool enable, MeshRenderer []meshRenderers)
        {
            foreach (MeshRenderer meshRenderer in meshRenderers)
                meshRenderer.enabled = enable;
        }

        private void SetEnableOtherMeshRenderers(bool enable, MeshRenderer []ignoreMeshRenderers)
        {
            List<MeshRenderer> ignoreMeshRendererList = (ignoreMeshRenderers != null) ? new List<MeshRenderer>(ignoreMeshRenderers) : new List<MeshRenderer>();
            foreach(MeshRenderer meshRenderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (ignoreMeshRendererList.Count == 0 || !ignoreMeshRendererList.Contains(meshRenderer))
                    meshRenderer.enabled = enable;
            }
        }

        private void NavMeshFailMessage(string msg)
        {
            Debug.LogWarning("NAV MESH FAILED TO BUILD: " + msg);
        }

        IEnumerator PlaceNavMeshObstacle_Custom_Internal(INavigation obstacleData, Vector3 position, Vector3 rotation, Vector3 localScale)
        {
            if (obstacleData is NavigationMono obstacleDataMono)
            {
                NavMeshModifier navMeshModifier = obstacleDataMono.gameObject.AddComponent<NavMeshModifier>();
                navMeshModifier.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
                navMeshModifier.transform.localScale = localScale;
                navMeshModifier.overrideArea = true;
                navMeshModifier.area = NavMesh.GetAreaFromName("Not Walkable");

                foreach (INavigation navMeshData in navMeshDataList)
                {
                    if (navMeshData is NavigationMono navMeshDataMono)
                        yield return navMeshDataMono.StartCoroutine(RebuildNavMesh_Internal(navMeshDataMono));
                }

                if (obstacleDataMono.GetComponent<MeshRenderer>() != null)
                    obstacleDataMono.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        IEnumerator RebuildNavMesh_Internal(NavigationMono navMeshDataMono)
        {
            if (navMeshDataMono != null)
            {
                NavMeshSurface navMeshSurface = navMeshDataMono.GetComponent<NavMeshSurface>();
                if (navMeshSurface != null)
                    navMeshSurface.BuildNavMesh();
                yield return new WaitForEndOfFrame();

                foreach (NavMeshSurface surface in navMeshDataMono.GetComponentsInChildren<NavMeshSurface>())
                {
                    if (surface != navMeshSurface)
                    {
                        surface.BuildNavMesh();
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }

        public bool SamplePosition(RideVector3 sourcePosition, out RideNavMeshHit hit, float maxDistance, int areaMask)
        {
            if (NavMesh.SamplePosition(sourcePosition, out var navHit, maxDistance, areaMask))
            {
                hit = new RideNavMeshHit(navHit);
                return true;
            }

            hit = default;
            return false;
        }

        #endregion
    }
}
