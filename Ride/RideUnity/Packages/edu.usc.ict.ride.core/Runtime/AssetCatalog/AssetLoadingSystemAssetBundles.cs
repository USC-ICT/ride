using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Ride.AWS;

namespace Ride
{
    /// <summary>
    /// Unity runtime system responsible for loading asset catalogs and asset bundles locally or from remote storage.
    /// Supports AWS S3 signed URL integration, local caching, progress tracking, and async loading by name or label.
    /// </summary>
    public class AssetLoadingSystemAssetBundles : RideSystemMonoBehaviour, IAssetLoadingSystem
    {
        public List<CatalogLoadInfoUnity> m_catalogsToLoad = new();
        [SerializeField] private bool m_verboseLogging = false;
        private List<AssetCatalogData> m_catalogs = new();
        private Dictionary<string, UnityEngine.Object> m_loadedAssetsByName = new();
        private Dictionary<string, AssetBundle> m_loadedBundles = new();
        private AWSFileStorageS3System m_fileStorageSystemAWS;
        private ConfigurationSystemUnity m_rideConfigSystem;

        public bool CatalogCurrentlyLoading { get; private set; }
        public int NumCatalogsLoaded => m_catalogs.Count;


#if UNITY_EDITOR
        /// <summary>
        /// Ensures that all AssetBundles loaded in memory are unloaded when entering Play Mode in the Unity Editor.
        /// <para>
        /// This prevents the following error that can occur if the same AssetBundle is loaded again during a new Play session:
        /// </para>
        /// <code>
        /// "AssetBundle '...' can't be loaded because another AssetBundle with the same files is already loaded."
        /// </code>
        /// <para>
        /// This issue arises because Unity does not automatically unload AssetBundles when exiting Play Mode. If an AssetBundle was
        /// previously loaded and not explicitly unloaded (via <see cref="AssetBundle.Unload"/>), it can remain in memory and trigger a
        /// conflict when reloading the same bundle (even from a different URL) in a subsequent Play session.
        /// </para>
        /// <para>
        /// Calling <see cref="AssetBundle.UnloadAllAssetBundles(bool)"/> with <c>true</c> ensures both the bundles and their loaded assets
        /// are fully released from memory.
        /// </para>
        /// <para>
        /// Related documentation and references:
        /// <list type="bullet">
        /// <item><see href="https://docs.unity3d.com/ScriptReference/AssetBundle.UnloadAllAssetBundles.html">Unity docs: AssetBundle.UnloadAllAssetBundles</see></item>
        /// <item><see href="https://forum.unity.com/threads/assetbundle-cant-be-loaded-because-another-assetbundle-with-the-same-files-is-already-loaded.1201760/">Unity Forum: Duplicate bundle load error</see></item>
        /// <item><see href="https://issuetracker.unity3d.com/issues/assetbundles-are-not-unloaded-on-exiting-play-mode">Unity Issue Tracker: Bundles not unloaded on Play exit</see></item>
        /// </list>
        /// </para>
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode()
        {
            //Debug.Log("[Ride] Clearing cached AssetBundles before entering Play Mode.");
            AssetBundle.UnloadAllAssetBundles(true);
        }
#endif

        /// <inheritdoc/>
        public override void SystemInit()
        {
            m_rideConfigSystem = Systems.Get<ConfigurationSystemUnity>();
            m_fileStorageSystemAWS = Systems.Get<AWSFileStorageS3System>();
            m_fileStorageSystemAWS.m_cognitoIdentityPoolId = m_rideConfigSystem.GetTerrainKey();
            m_fileStorageSystemAWS.m_regionName = m_rideConfigSystem.GetTerrainKeyRegion();

            // if (Application.isPlaying)
            //     await LoadCatalogs(m_catalogsToLoad);

            base.SystemInit();
        }

        /// <summary>Loads a single catalog from in-memory string, local path, or remote path.</summary>
        /// <param name="catalogInfo">Catalog load configuration.</param>
        /// <returns>True if the catalog was successfully loaded.</returns>
        public LoadOperation<bool> LoadCatalog(CatalogLoadInfo catalogInfo)
        {
            var op = new LoadOperation<bool>();
            StartCoroutine(LoadCatalogCoroutine(catalogInfo, op));
            return op;
        }

        private IEnumerator LoadCatalogCoroutine(CatalogLoadInfo catalogInfo, LoadOperation<bool> op)
        {
            CatalogCurrentlyLoading = true;

            op.SetProgress(0f);

            if (catalogInfo == null)
            {
                Debug.LogWarning("[AssetLoadingSystemAssetBundles] Skipping null catalog info entry.");
                op.SetFailed("Catalog info was null.");
                CatalogCurrentlyLoading = false;
                yield break;
            }

            if (catalogInfo.catalogJson == null && string.IsNullOrEmpty(catalogInfo.catalogPath))
            {
                Debug.LogWarning("[AssetLoadingSystemAssetBundles] Skipping catalog entry: no file or path assigned.");
                op.SetFailed("No catalog JSON or path provided.");
                CatalogCurrentlyLoading = false;
                yield break;
            }

            bool newCatalogLoaded = false;

            if (!string.IsNullOrEmpty(catalogInfo.catalogJson))
            {
                try
                {
                    var catalog = JsonUtility.FromJson<AssetCatalogData>(catalogInfo.catalogJson);
                    if (catalog == null)
                    {
                        Debug.LogError("[AssetLoadingSystemAssetBundles] Failed to parse catalog from TextAsset.");
                        op.SetFailed("Catalog JSON parse failed.");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    catalog.isRemoteCatalog = false;
                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;
                    LogCatalogContents(catalog, "[AssetLoadingSystemAssetBundles] Loaded catalog from TextAsset.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetLoadingSystemAssetBundles] Error loading catalog from TextAsset: {ex.Message}");
                    op.SetFailed("Exception while loading catalog JSON.");
                    CatalogCurrentlyLoading = false;
                    yield break;
                }
            }
            else
            {
                var postfix = GetBuildPostfixPath();
                string basePath = catalogInfo.catalogPath.TrimEnd('/', '\\');
                string fullFolder = Path.Combine(basePath, postfix).Replace("\\", "/");
                string catalogFilePath = Path.Combine(fullFolder, "catalog.json").Replace("\\", "/");

                if (catalogInfo.isRemote)
                {
                    bool done = false;
                    string signedCatalogUrl = null;

                    //Debug.Log($"Loading catalog: {catalogFilePath}");

                    var signedUrlOp = RequestSignedURL(catalogFilePath);
                    signedUrlOp.Then(url => { signedCatalogUrl = url; done = true; })
                               .Catch(error => { Debug.LogError(error); done = true; });

                    while (!done)
                        yield return null;

                    if (string.IsNullOrEmpty(signedCatalogUrl))
                    {
                        Debug.LogWarning($"[AssetLoadingSystemAssetBundles] Failed to get signed URL for catalog: {catalogFilePath}");
                        op.SetFailed($"Failed to get signed URL. {catalogFilePath}");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    using var www = UnityWebRequest.Get(signedCatalogUrl);
                    var request = www.SendWebRequest();

                    while (!request.isDone)
                    {
                        op.SetProgress(0.3f * request.progress);
                        yield return null;
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Failed to download remote catalog: {signedCatalogUrl} : {www.error}");
                        op.SetFailed($"Remote catalog download failed. {signedCatalogUrl} : {www.error}");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    string json = www.downloadHandler.text;
                    var catalog = JsonUtility.FromJson<AssetCatalogData>(json);
                    catalog.isRemoteCatalog = true;
                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;
                    LogCatalogContents(catalog, "[AssetLoadingSystemAssetBundles] Loaded remote catalog.");
                }
                else
                {
                    if (!File.Exists(catalogFilePath))
                    {
                        Debug.LogWarning($"[AssetLoadingSystemAssetBundles] Local catalog not found at {catalogFilePath}");
                        op.SetFailed($"Local catalog file not found. {catalogFilePath}");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    string json = File.ReadAllText(catalogFilePath);
                    var catalog = JsonUtility.FromJson<AssetCatalogData>(json);
                    catalog.isRemoteCatalog = false;
                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;
                    LogCatalogContents(catalog, "[AssetLoadingSystemAssetBundles] Loaded local catalog.");
                }
            }

            if (newCatalogLoaded)
                op.SetCompleted(true);
            else
                op.SetFailed("Catalog was not loaded");

            CatalogCurrentlyLoading = false;
        }

        private static string GetAssetBundlePlatformName()
        {
            string platform = "";
            if (RideUtils.IsAndroid()) platform = "Android";
            else if (RideUtils.IsIOS()) platform = "iOS";
            else if (RideUtils.IsWebGL()) platform = "WebGL";
            else if (RideUtils.IsLinux()) platform = "StandaloneLinux64";
            else if (RideUtils.IsOSX()) platform = "StandaloneOSXUniversal";  // OSX and Windows currently placed last in the list to prevent early match when in editor
            else if (RideUtils.IsWindows()) platform = "StandaloneWindows64";
            else Debug.LogWarning("GetAssetBundlePlatformName() - platform not found.");
            return platform;
        }

        private static string GetBuildPostfixPath()
        {
            string[] v = Application.unityVersion.Split('.');
            string version = v.Length >= 2 ? $"{v[0]}.{v[1]}.x" : Application.unityVersion;
            string pipeline = GraphicsSettings.currentRenderPipeline == null
                ? "BuiltIn" : GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HD")
                ? "HDRP" : (GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal")
                ? "URP" : GraphicsSettings.currentRenderPipeline.GetType().Name);
            string target = GetAssetBundlePlatformName();
            return Path.Combine(version, pipeline, target).Replace("\\", "/");
        }

        /// <summary>Loads a list of catalogs, clearing existing entries first.</summary>
        /// <param name="catalogInfos">Catalog configurations to load.</param>
        /// <returns>True if at least one catalog was loaded successfully.</returns>
        public IEnumerator LoadCatalogs(List<CatalogLoadInfoUnity> catalogInfos)
        {
            m_catalogs.Clear();
            yield return StartCoroutine(InternalLoadCatalogs(catalogInfos));
        }

        /// <summary>Loads all catalogs listed in the inspector-serialized list.</summary>
        public IEnumerator LoadCachedCatalogs()
        {
            yield return StartCoroutine(LoadCatalogs(m_catalogsToLoad));
        }

        // <summary>Internal method for loading a list of core catalog entries.</summary>
        private IEnumerator InternalLoadCatalogs(List<CatalogLoadInfoUnity> catalogInfos)
        {
            CatalogCurrentlyLoading = true;

            foreach (var catalogInfo in catalogInfos)
            {
                bool done = false;
                var op = LoadCatalog(catalogInfo.ToCoreInfo());
                op.Then(url => { done = true; })
                  .Catch(err => { Debug.LogWarning(err); done = true; });

                while (!done)
                    yield return null;
            }

            CatalogCurrentlyLoading = false;
        }

        /// <summary>Logs the names of assets in a given catalog if verbose logging is enabled.</summary>
        private void LogCatalogContents(AssetCatalogData catalog, string headerMessage)
        {
            if (!m_verboseLogging)
                return;

            if (catalog == null || catalog.entries == null)
            {
                Debug.LogWarning("[AssetLoadingSystemAssetBundles] Catalog loaded but no entries found.");
                return;
            }

            Debug.Log($"{headerMessage}");
            Debug.Log($"Contains {catalog.entries.Count} entries:");
            foreach (var entry in catalog.entries)
                Debug.Log($"- {entry.assetName}");
        }

        /// <summary>Loads an asset using its name.</summary>
        /// <param name="assetName">The unique asset name.</param>
        /// <returns>The loaded asset or null.</returns>
        public LoadOperation<object> LoadAssetByName(string assetName)
        {
            var op = new LoadOperation<object>();
            StartCoroutine(LoadAssetByNameCoroutine(assetName, op));
            return op;
        }

        private IEnumerator LoadAssetByNameCoroutine(string assetName, LoadOperation<object> op)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                op.SetFailed($"Asset name is null or empty. {assetName}");
                yield break;
            }

            if (m_loadedAssetsByName.TryGetValue(assetName, out var cached))
            {
                op.SetCompleted(cached);
                yield break;
            }

            while (CatalogCurrentlyLoading)
                yield return null;

            foreach (var catalog in m_catalogs)
            {
                var entry = catalog.entries.FirstOrDefault(e => e.assetName == assetName);
                if (entry == null)
                    continue;

                string bundleName = entry.bundleFileName;

                // Check if bundle already loaded
                if (!m_loadedBundles.TryGetValue(bundleName, out var bundle))
                {
                    string localBundlePath = Path.Combine(catalog.localPrefixPath, bundleName);
                    if (File.Exists(localBundlePath))
                    {
                        //Debug.Log($"LoadFromBundle() - attempting to load locally: {localBundlePath}");

                        var localRequest = AssetBundle.LoadFromFileAsync(localBundlePath);
                        while (!localRequest.isDone)
                        {
                            op.SetProgress(localRequest.progress * 0.9f);
                            yield return null;
                        }

                        bundle = localRequest.assetBundle;
                    }

                    if (bundle == null)
                    {
                        // Remote load

                        bool done = false;
                        string signedUrl = null;
                        string remoteBundlePath = Path.Combine(catalog.remotePrefixPath, bundleName).Replace("\\", "/");
                        var urlOp = RequestSignedURL(remoteBundlePath);
                        urlOp.Then(url => { signedUrl = url; done = true; })
                             .Catch(err => { Debug.LogError(err); done = true; });

                        while (!done)
                            yield return null;

                        if (string.IsNullOrEmpty(signedUrl))
                        {
                            op.SetFailed("Failed to get signed bundle URL.");
                            yield break;
                        }

                        //Debug.Log($"signedUrl: {signedUrl}");

                        // report if this bundle is locally cached on the machine
                        //if (!string.IsNullOrEmpty(entry.bundleHash128) && Caching.IsVersionCached(new CachedAssetBundle(bundleName, Hash128.Parse(entry.bundleHash128))))
                        //    Debug.Log($"[AssetLoad] Using cached version of '{bundleName}'");

                        using var www = string.IsNullOrEmpty(entry.bundleHash128)
                            ? UnityWebRequestAssetBundle.GetAssetBundle(signedUrl)
                            : UnityWebRequestAssetBundle.GetAssetBundle(signedUrl, Hash128.Parse(entry.bundleHash128));
                        var request = www.SendWebRequest();

                        while (!request.isDone)
                        {
                            op.SetProgress(0.2f + 0.5f * request.progress);
                            yield return null;
                        }

                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogWarning($"Failed to download asset bundle: {signedUrl} : {www.error}");
                            op.SetFailed($"Bundle download failed. {signedUrl} : {www.error}");
                            yield break;
                        }

                        //Debug.Log($"WWW result: {www.result} - downloaded {www.downloadedBytes} bytes");

                        bundle = DownloadHandlerAssetBundle.GetContent(www);
                        if (bundle == null)
                            Debug.LogWarning($"[AssetLoadingSystem] Downloaded bundle is null for {bundleName}. This usually means the AssetBundle was built for the wrong platform.");
                    }

                    if (bundle == null)
                    {
                        Debug.Log($"LoadAssetByNameCoroutine(): Bundle is null after load");
                        op.SetFailed("Bundle is null after load.");
                        yield break;
                    }

                    m_loadedBundles[bundleName] = bundle;
                }

                var loadRequest = bundle.LoadAssetAsync(entry.assetName, typeof(UnityEngine.Object));
                while (!loadRequest.isDone)
                {
                    op.SetProgress(0.8f + 0.2f * loadRequest.progress);
                    yield return null;
                }

                var asset = loadRequest.asset;

                if (asset == null)
                {
                    Debug.LogWarning($"Asset '{entry.assetName}' not found in bundle '{bundleName}'.");
                    op.SetFailed($"Asset '{entry.assetName}' not found in bundle '{bundleName}'.");
                    yield break;
                }

                m_loadedAssetsByName[entry.assetName] = asset;
                op.SetCompleted(asset);
                yield break;
            }

            op.SetFailed($"Asset '{assetName}' not found in any loaded catalog.");
        }


        /// <summary>Loads the first matching asset using one or more labels.</summary>
        /// <param name="labels">The list of labels to match.</param>
        /// <returns>The loaded asset or null.</returns>
        public LoadOperation<object> LoadAnyAssetByLabels(List<string> labels)
        {
            var op = new LoadOperation<object>();
            StartCoroutine(LoadAnyAssetByLabelsCoroutine(labels, op));
            return op;
        }

        private IEnumerator LoadAnyAssetByLabelsCoroutine(List<string> labels, LoadOperation<object> op)
        {
            if (labels == null || labels.Count == 0)
            {
                Debug.LogWarning("LoadAnyAssetByLabels: No labels provided.");
                op.SetFailed("Labels list is null or empty.");
                yield break;
            }

            while (CatalogCurrentlyLoading)
                yield return null;

            var entry = ChooseFirstEntryWithLabels(labels);
            if (entry == null)
            {
                Debug.LogWarning($"LoadAnyAssetByLabels: No entries found with labels [{string.Join(", ", labels)}]");
                op.SetFailed("No matching entry for labels.");
                yield break;
            }

            yield return LoadAssetByNameCoroutine(entry.assetName, op);

            // Completion is handled inside the coroutine already, but this ensures it's finished
            if (!op.IsCompleted && !op.IsCancelled)
                op.SetFailed("LoadAssetByNameCoroutine exited without completion.");
        }

        private AssetCatalogEntry ChooseFirstEntryWithLabels(List<string> labels)
        {
            if (labels == null || labels.Count == 0)
                return null;

            foreach (var catalog in m_catalogs)
            {
                foreach (var entry in catalog.entries)
                {
                    if (entry.labels == null)
                        continue;

                    foreach (var label in labels)
                        if (entry.labels.Contains(label))
                            return entry;
                }
            }

            return null;
        }

        /// <summary>Requests a signed AWS S3 URL for a remote bundle or catalog.</summary>
        /// <param name="fullPath">The S3 bucket/key path.</param>
        /// <returns>Signed URL if successful, otherwise null.</returns>
        private LoadOperation<string> RequestSignedURL(string fullPath)
        {
            var op = new LoadOperation<string>();

            if (string.IsNullOrEmpty(fullPath))
            {
                op.SetFailed("RequestSignedURL: path is null or empty.");
                return op;
            }

            string cleanPath = fullPath.Replace("\\", "/");
            string[] parts = cleanPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                op.SetFailed($"RequestSignedURL: invalid path: {fullPath}");
                return op;
            }

            string bucket = parts[0];
            string key = string.Join("/", parts.Skip(1));

            if (!ConfigurationSystemUnity.IsTerrainKeyFormatValid(m_fileStorageSystemAWS.m_cognitoIdentityPoolId))
            {
                op.SetCompleted(AWSFileStorageS3System.GetLocation(bucket, key));
                return op;
            }

            //Debug.Log($"RequestSignedURL() - {bucket} {key}");

            m_fileStorageSystemAWS.GetSignedURL(bucket, key, signedURL =>
            {
                if (!string.IsNullOrEmpty(signedURL))
                    op.SetCompleted(signedURL);
                else
                    op.SetFailed("GetSignedURL returned null or empty.");
            });

            return op;
        }

        /// <summary>Gets a catalog entry for a specific asset name.</summary>
        /// <param name="assetName">The asset name to find.</param>
        /// <returns>Catalog entry if found, otherwise null.</returns>
        public AssetCatalogEntry GetCatalogEntry(string assetName) => m_catalogs.SelectMany(c => c.entries).FirstOrDefault(e => e.assetName == assetName);

        /// <summary>Unloads all loaded asset bundles.</summary>
        public void UnloadAll()
        {
            foreach (var bundle in m_loadedBundles.Values)
                bundle.Unload(false);

            m_loadedBundles.Clear();
        }

        /// <summary>Returns the total number of assets across all catalogs.</summary>
        public int GetEntryCount() => m_catalogs.SelectMany(c => c.entries).Count();

        /// <summary>Returns a flat list of all asset name/label pairs in loaded catalogs.</summary>
        /// <returns>List of (assetName, labels).</returns>
        public List<(string assetName, List<string> labels)> GetEntryInfoList()
        {
            return m_catalogs
                .SelectMany(c => c.entries)
                .Select(e => (e.assetName, e.labels))
                .ToList();
        }

        /// <summary>Checks if an asset is already loaded and cached.</summary>
        /// <param name="assetName">The name of the asset.</param>
        /// <returns>True if the asset's bundle is loaded.</returns>
        public bool IsCached(string assetName)
        {
            var entry = m_catalogs.SelectMany(c => c.entries).FirstOrDefault(e => e.assetName == assetName);
            if (entry == null) return false;
            return m_loadedBundles.ContainsKey(entry.bundleFileName);
        }

        /// <summary>Clears the internal asset bundle cache without unloading them.</summary>
        public void ClearCache() => m_loadedBundles.Clear();
    }
}
