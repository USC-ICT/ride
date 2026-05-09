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
    /// Provides asynchronous loading of Unity assets using RIDE asset catalogs and
    /// asset bundles, with optional local-first access and secure remote fallback.
    ///
    /// This system is designed as a <b>loader</b>, not a cache. It resolves
    /// <see cref="AssetCatalogData"/> entries, loads the corresponding asset bundle
    /// (either from a local path or a signed remote URL), extracts the requested asset,
    /// and immediately unloads the bundle. The returned asset remains valid for as long
    /// as the caller holds references to it, and the caller is responsible for managing
    /// the asset's lifetime.
    ///
    /// <para>
    /// This behavior allows large bundles - such as Virtual Human characters, terrain
    /// chunks, or high-resolution textures - to be streamed on demand without permanently
    /// increasing memory usage. The system does not retain references to loaded bundles
    /// or assets; once the load operation completes, the asset bundle is unloaded and
    /// only the returned asset remains in memory.
    /// </para>
    ///
    /// <para><b>Usage Model</b></para>
    /// <para>
    /// Callers typically load an asset once, instantiate as many GameObjects as needed,
    /// and then release it when finished:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// Invoke <see cref="LoadAssetByName(string)"/> or <see cref="LoadAnyAssetByLabels(System.Collections.Generic.List{string})"/>
    /// to obtain a prefab or other Unity asset.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Instantiate the asset using <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// When the asset is no longer required:
    /// destroy all instantiated GameObjects,
    /// clear all strong C# references to the loaded asset,
    /// and run <see cref="UnloadUnusedAssetsCoroutine"/> (or call
    /// <see cref="UnityEngine.Resources.UnloadUnusedAssets"/> directly)
    /// to allow Unity to reclaim the underlying resources.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// <para><b>Memory Behavior</b></para>
    /// <para>
    /// Unity loads the textures, meshes, animations, and other dependencies of an asset
    /// when the asset is extracted from its bundle. These resources remain resident
    /// until:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All strong references to the asset are released, <i>and</i></description></item>
    /// <item><description>
    /// Unity performs an unload pass via <see cref="UnityEngine.Resources.UnloadUnusedAssets"/>
    /// or a scene change.
    /// </description></item>
    /// </list>
    /// <para>
    /// Because the system does not retain bundles or asset references, callers control
    /// resource lifetime explicitly. This allows applications to load many large assets
    /// sequentially (e.g., previewing 50 Virtual Humans) without accumulating memory.
    /// </para>
    ///
    /// <para><b>Catalog Management</b></para>
    /// <para>
    /// The system may load multiple catalogs at runtime. All asset loading operations
    /// wait for catalog initialization to complete. Each <see cref="AssetCatalogData"/>
    /// provides mappings between logical asset names and the bundle files that contain
    /// them, along with labels for category-based retrieval.
    /// </para>
    ///
    /// <para><b>Local and Remote Bundle Paths</b></para>
    /// <para>
    /// When resolving a bundle, the system will:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Attempt to load from the catalog's configured <c>localPrefixPath</c> if the bundle
    /// file exists on disk.
    /// </description></item>
    /// <item><description>
    /// If not found locally, request a signed URL for secure remote access and retrieve
    /// the bundle using <see cref="UnityEngine.Networking.UnityWebRequestAssetBundle"/>.
    /// </description></item>
    /// </list>
    ///
    /// <para><b>Threading and Execution</b></para>
    /// <para>
    /// All load operations are performed asynchronously using Unity coroutines.
    /// Operations report progress via <see cref="LoadOperation{T}"/> and complete
    /// with either a Unity asset or an error message.
    /// </para>
    ///
    /// <para><b>Intended Use</b></para>
    /// <para>
    /// This system is intended for on-demand loading of assets in memory-sensitive
    /// applications such as RIDE scenarios, Virtual Human selection interfaces,
    /// or terrain streaming tools. It provides the minimum necessary caching and
    /// leaves responsibility for asset reuse and memory cleanup to the caller.
    /// </para>
    /// </summary>
    public class AssetLoadingSystemAssetBundles : RideSystemMonoBehaviour, IAssetLoadingSystem
    {
        public List<CatalogLoadInfoUnity> m_catalogsToLoad = new();
        [SerializeField] private bool m_verboseLogging = false;
        private List<AssetCatalogData> m_catalogs = new();

        public bool CatalogCurrentlyLoading { get; private set; }
        public int NumCatalogsLoaded => m_catalogs.Count;
        public bool RemoteBundleDownloadActive { get; private set; }
        public string RemoteBundleAssetName { get; private set; }
        public string RemoteBundleName { get; private set; }
        public string RemoteBundleUrl { get; private set; }
        public float RemoteBundleRequestProgress { get; private set; }
        public float RemoteBundleOverallProgress { get; private set; }
        public ulong RemoteBundleDownloadedBytes { get; private set; }
        public float RemoteBundleElapsedSeconds { get; private set; }
        public float RemoteBundleAverageBytesPerSecond { get; private set; }
        public string RemoteBundleLastResult { get; private set; }
        public string RemoteBundleLastError { get; private set; }


        /// <inheritdoc/>
        public override void SystemInit()
        {
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

                    CheckRideBundleVersion(catalog);

                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;

                    Debug.Log(
                        $"[AssetLoadingSystemAssetBundles] Catalog loaded (TextAsset). " +
                        $"rideBundleVersion='{catalog.rideBundleVersion ?? "(none)"}', " +
                        $"artAssetSvnRevision={catalog.artAssetVersion}."
                    );

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

                    {
                        float startWait = Time.realtimeSinceStartup;
                        float lastLog = 0f;
                        const float TimeoutSeconds = 15f;

                        while (!done)
                        {
                            float now = Time.realtimeSinceStartup;

                            if ((now - startWait) >= TimeoutSeconds)
                            {
                                Debug.LogWarning($"[AssetLoadingSystemAssetBundles] SignedURL TIMEOUT (catalog) after {TimeoutSeconds}s path='{catalogFilePath}'");
                                break;
                            }

                            if ((now - lastLog) >= 3f)
                            {
                                Debug.Log($"[AssetLoadingSystemAssetBundles] SignedURL WAIT (catalog) elapsed={(now - startWait):0.000}s path='{catalogFilePath}'");
                                lastLog = now;
                            }

                            yield return null;
                        }
                    }

                    if (string.IsNullOrEmpty(signedCatalogUrl))
                    {
                        Debug.LogWarning($"[AssetLoadingSystemAssetBundles] Failed to get signed URL for catalog: {catalogFilePath}");
                        op.SetFailed($"Failed to get signed URL. {catalogFilePath}");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    using var www = UnityWebRequest.Get(signedCatalogUrl);
                    www.timeout = 60;

                    {
                        var request = www.SendWebRequest();

                        float dlStart = Time.realtimeSinceStartup;
                        float lastProgressChange = dlStart;
                        float lastLog = 0f;
                        float lastProgress = -1f;
                        ulong lastBytes = 0;

                        while (!request.isDone)
                        {
                            op.SetProgress(0.3f * request.progress);

                            float now = Time.realtimeSinceStartup;
                            bool progressed = (request.progress > lastProgress + 0.0001f) || (www.downloadedBytes > lastBytes);

                            if (progressed)
                            {
                                lastProgress = request.progress;
                                lastBytes = www.downloadedBytes;
                                lastProgressChange = now;
                            }
                            else if ((now - lastProgressChange) >= 3f && (now - lastLog) >= 3f)
                            {
                                Debug.LogWarning(
                                    $"[AssetLoadDiag] CatalogDownload STALL? elapsed={(now - dlStart):0.000}s " +
                                    $"progress={request.progress:0.000} bytes={www.downloadedBytes} url='{signedCatalogUrl}'");
                                lastLog = now;
                            }

                            yield return null;
                        }
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Failed to download remote catalog: code={www.responseCode} url={signedCatalogUrl} error={www.error}");
                        op.SetFailed($"Remote catalog download failed. {signedCatalogUrl} : {www.error}");
                        CatalogCurrentlyLoading = false;
                        yield break;
                    }

                    string json = www.downloadHandler.text;
                    var catalog = JsonUtility.FromJson<AssetCatalogData>(json);
                    catalog.isRemoteCatalog = true;

                    CheckRideBundleVersion(catalog);

                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;

                    Debug.Log(
                        $"[AssetLoadingSystemAssetBundles] Catalog loaded (Remote). " +
                        $"'{catalog.catalogName ?? "(unknown)"}'," +
                        $"rideBundleVersion={catalog.rideBundleVersion ?? "(none)"} " +
                        $"(cur={AssetCatalogData.RIDE_VERSION})," +
                        $"artAssetSvnRevision={catalog.artAssetVersion ?? "(unknown)"}."
                    );

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

                    CheckRideBundleVersion(catalog);

                    m_catalogs.Add(catalog);
                    newCatalogLoaded = true;

                    Debug.Log(
                        $"[AssetLoadingSystemAssetBundles] Catalog loaded (Local). " +
                        $"rideBundleVersion='{catalog.rideBundleVersion ?? "(none)"}', " +
                        $"artAssetSvnRevision={catalog.artAssetVersion}."
                    );

                    LogCatalogContents(catalog, "[AssetLoadingSystemAssetBundles] Loaded local catalog.");
                }
            }

            if (newCatalogLoaded)
                op.SetCompleted(true);
            else
                op.SetFailed("Catalog was not loaded");

            CatalogCurrentlyLoading = false;
        }

        private static string GetBuildPostfixPath()
        {
            string version = AssetCatalogUtility.GetCompatibleUnityVersionName();
            string pipeline = AssetCatalogUtility.GetRenderPipelineName();
            string target = AssetCatalogUtility.GetPlatformName();
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

        /// <summary>Internal method for loading a list of core catalog entries.</summary>
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

            if (catalog == null)
            {
                Debug.LogWarning("[AssetLoadingSystemAssetBundles] LogCatalogContents called with null catalog.");
                return;
            }

            string source = catalog.isRemoteCatalog ? "Remote" : "Local";
            string rideVersion = catalog.rideBundleVersion ?? "(none)";
            string artVersion = catalog.artAssetVersion ?? "0";
            string unityVersion = catalog.unityVersion ?? "(unknown)";
            string platform = catalog.platform ?? "(unknown)";
            string renderPipeline = catalog.renderPipeline ?? "(unknown)";
            string rpVersion = catalog.renderPipelineVersion ?? "(unknown)";
            int entryCount = catalog.entries?.Count ?? 0;

            string summaryLine =
                $"{source} | " +
                $"Ver={rideVersion} | " +
                $"Art={artVersion} | " +
                $"Unity={unityVersion} | " +
                $"{platform} | {renderPipeline} | " +
                $"Entries={entryCount}";

            string catalogText =
                $"{headerMessage} {summaryLine}\n" +
                $"AssetLoadingSystemAssetBundles - [RIDE Asset Catalog]\n" +
                $"  Source:                       {source}\n" +
                $"  Local Prefix Path:            {catalog.localPrefixPath}\n" +
                $"  Remote Prefix Path:           {catalog.remotePrefixPath}\n" +
                $"  Ride Bundle Version:          {rideVersion}\n" +
                $"  Art Version:                  {artVersion}\n" +
                $"  Unity Version (Built With):   {unityVersion}\n" +
                $"  Platform:                     {platform}\n" +
                $"  Render Pipeline:              {renderPipeline}\n" +
                $"  Render Pipeline Version:      {rpVersion}\n" +
                $"  Entry Count:                  {entryCount}";

            Debug.Log(catalogText);

            if (catalog.entries != null)
            {
                foreach (var entry in catalog.entries)
                {
                    string asset = entry.assetName ?? "(null)";
                    string bundle = entry.bundleFileName ?? "(null)";
                    string hash = entry.bundleHash128 ?? "(null)";
                    string labels = entry.labels != null && entry.labels.Count > 0 ? string.Join(",", entry.labels) : "(none)";
                    string shortHash = hash.Length > 8 ? hash.Substring(0, 8) : hash;  // Shorten hash (first 8 chars is enough for human scan)
                    string entryText =
                        $"[AssetLoadingSystemAssetBundles] {asset} | " +
                        $"bundle={bundle} | " +
                        $"hash={shortHash} | " +
                        $"labels={labels}";
                    Debug.Log(entryText);
                }
            }
        }

        /// <summary>
        /// Asynchronously loads a single asset by name from the loaded asset catalogs.
        /// </summary>
        /// <param name="assetName">
        /// Logical asset name as defined in the loaded <see cref="AssetCatalogData"/> entries
        /// (for example, <c>entry.assetName</c>), not a file system path.
        /// </param>
        /// <returns>
        /// A <see cref="LoadOperation{T}"/> that completes with the loaded asset as a
        /// <see cref="UnityEngine.Object"/> on success, or a failure state if the asset
        /// cannot be found or its bundle fails to load.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method looks up <paramref name="assetName"/> in the currently loaded catalogs,
        /// resolves the corresponding <see cref="AssetCatalogEntry"/>, loads the associated
        /// asset bundle (from local storage or remote storage such as S3, depending on the
        /// catalog configuration), and then loads the asset from that bundle.
        /// </para>
        /// <para>
        /// The operation reports progress via the returned <see cref="LoadOperation{T}"/>.
        /// The call is non-blocking; the underlying work is performed by a Unity coroutine
        /// started on this system.
        /// </para>
        /// <para>
        /// Memory management:
        /// </para>
        /// <para>
        /// The asset returned by this method is a live Unity object. This system is intended
        /// to behave as a loader rather than a long-term cache: once the asset has been
        /// loaded and returned, callers are expected to keep any references they need and
        /// are also responsible for releasing those references when the asset is no longer
        /// required. Destroy all instantiated GameObjects created from the loaded asset,
        /// clear any C# references to the asset itself, and then run
        /// <c>UnloadUnusedAssetsCoroutine()</c> (or call
        /// <see cref="UnityEngine.Resources.UnloadUnusedAssets"/> directly) to allow Unity to
        /// reclaim the underlying textures, meshes, and other resources.
        /// </para>
        /// <para>
        /// For repeated use of the same asset (for example, instantiating 100 copies of a
        /// barrel prefab), the recommended pattern is:
        /// </para>
        /// <list type="number">
        /// <item>
        /// <description>Call <see cref="LoadAssetByName"/> once to obtain the prefab.</description>
        /// </item>
        /// <item>
        /// <description>Instantiate as many copies as needed using <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.</description>
        /// </item>
        /// <item>
        /// <description>When finished, destroy the instances and clear your references to the prefab, then run <c>UnloadUnusedAssetsCoroutine()</c> to free memory.</description>
        /// </item>
        /// </list>
        /// <para>
        /// Calling <see cref="LoadAssetByName"/> multiple times for the same asset name is
        /// allowed and will load the asset each time according to the current implementation,
        /// but is not recommended when you can reuse an existing reference.
        /// </para>
        /// </remarks>
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

            while (CatalogCurrentlyLoading)
                yield return null;

            foreach (var catalog in m_catalogs)
            {
                var entry = catalog.entries.FirstOrDefault(e => e.assetName == assetName);
                if (entry == null)
                    continue;

                string bundleName = entry.bundleFileName;

                AssetBundle bundle = null;

                // Check if bundle already loaded
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

                        string signedUrl = null;
                        bool done = false;
                        string remoteBundlePath = Path.Combine(catalog.remotePrefixPath, bundleName).Replace("\\", "/");
                        var urlOp = RequestSignedURL(remoteBundlePath);
                        urlOp.Then(url => { signedUrl = url; done = true; })
                            .Catch(err => { Debug.LogError(err); done = true; });

                        float startWait = Time.realtimeSinceStartup;
                        float lastLogSignedUrl = 0f;
                        const float TimeoutSeconds = 15f;

                        while (!done)
                        {
                            float now = Time.realtimeSinceStartup;

                            if ((now - startWait) >= TimeoutSeconds)
                            {
                                Debug.LogWarning($"[AssetLoadDiag] SignedURL TIMEOUT (bundle) after {TimeoutSeconds}s path='{remoteBundlePath}' asset='{entry.assetName}'");
                                break;
                            }

                            if ((now - lastLogSignedUrl) >= 3f)
                            {
                                Debug.Log($"[AssetLoadDiag] SignedURL WAIT (bundle) elapsed={(now - startWait):0.000}s path='{remoteBundlePath}' asset='{entry.assetName}'");
                                lastLogSignedUrl = now;
                            }

                            yield return null;
                        }

                        if (string.IsNullOrEmpty(signedUrl))
                        {
                            op.SetFailed("Failed to get signed bundle URL.");
                            yield break;
                        }

                        //Debug.Log($"signedUrl: {signedUrl}");

                        // report if this bundle is locally cached on the machine
                        //if (!string.IsNullOrEmpty(entry.bundleHash128) && Caching.IsVersionCached(new CachedAssetBundle(bundleName, Hash128.Parse(entry.bundleHash128))))
                        //    Debug.Log($"[AssetLoad] Using cached version of '{bundleName}'");

                        BeginRemoteBundleDownloadStatus(entry.assetName, bundleName, signedUrl);

                        using var www = string.IsNullOrEmpty(entry.bundleHash128)
                            ? UnityWebRequestAssetBundle.GetAssetBundle(signedUrl)
                            : UnityWebRequestAssetBundle.GetAssetBundle(signedUrl, Hash128.Parse(entry.bundleHash128));

                        www.timeout = 180; // bundles can be larger; tune later
                        var request = www.SendWebRequest();

                        float dlStart = Time.realtimeSinceStartup;
                        float lastProgressChange = dlStart;
                        float lastLog = 0f;
                        float lastProgress = -1f;
                        ulong lastBytes = 0;

                        while (!request.isDone)
                        {
                            float overallProgress = 0.2f + 0.5f * request.progress;
                            op.SetProgress(overallProgress);

                            float now = Time.realtimeSinceStartup;
                            ulong currentBytes = www.downloadedBytes;
                            bool progressed = (request.progress > lastProgress + 0.0001f) || (currentBytes > lastBytes);

                            if (progressed)
                            {
                                lastProgress = request.progress;
                                lastBytes = currentBytes;
                                lastProgressChange = now;
                            }

                            float elapsed = now - dlStart;
                            UpdateRemoteBundleDownloadStatus(request.progress, overallProgress, currentBytes, elapsed);

                            bool stalled = (now - lastProgressChange) >= 3f;
                            if ((now - lastLog) >= 3f && (m_verboseLogging || stalled))
                            {
                                if (m_verboseLogging)
                                {
                                    string speedText = FormatBytesPerSecond(RemoteBundleAverageBytesPerSecond);
                                    string bytesText = FormatBytes(currentBytes);
                                    string lastBytesText = FormatBytes(lastBytes);
                                    string diagPrefix = stalled ? "STALL?" : "PROGRESS";
                                    string message =
                                        $"[AssetLoadDiag] BundleDownload {diagPrefix} asset='{entry.assetName}' bundle='{bundleName}' " +
                                        $"elapsed={elapsed:0.000}s requestProgress={request.progress:0.000} overallProgress={overallProgress:0.000} " +
                                        $"bytes={currentBytes} ({bytesText}) lastObservedBytes={lastBytes} ({lastBytesText}) avgSpeed={speedText} url='{signedUrl}'";

                                    if (stalled) Debug.LogWarning(message);
                                    else Debug.Log(message);
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        $"[AssetLoadDiag] BundleDownload STALL? asset='{entry.assetName}' bundle='{bundleName}' " +
                                        $"elapsed={elapsed:0.000}s progress={request.progress:0.000} bytes={currentBytes} url='{signedUrl}'");
                                }

                                lastLog = now;
                            }

                            yield return null;
                        }

                        float finalElapsed = Time.realtimeSinceStartup - dlStart;
                        UpdateRemoteBundleDownloadStatus(
                            request.progress,
                            0.2f + 0.5f * request.progress,
                            www.downloadedBytes,
                            finalElapsed);

                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            CompleteRemoteBundleDownloadStatus("Failed", www.error);
                            Debug.LogWarning($"Failed to download asset bundle: code={www.responseCode} url={signedUrl} error={www.error}");
                            op.SetFailed($"Bundle download failed. {signedUrl} : {www.error}");
                            yield break;
                        }

                        CompleteRemoteBundleDownloadStatus("Success");
                        if (m_verboseLogging)
                        {
                            Debug.Log(
                                $"[AssetLoadDiag] BundleDownload COMPLETE asset='{entry.assetName}' bundle='{bundleName}' " +
                                $"elapsed={RemoteBundleElapsedSeconds:0.000}s bytes={www.downloadedBytes} ({FormatBytes(www.downloadedBytes)}) " +
                                $"avgSpeed={FormatBytesPerSecond(RemoteBundleAverageBytesPerSecond)} url='{signedUrl}'");
                        }

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
                    bundle.Unload(false);
                    yield break;
                }

                bundle.Unload(false);
                op.SetCompleted(asset);
                yield break;
            }

            op.SetFailed($"Asset '{assetName}' not found in any loaded catalog.");
        }


        /// <summary>
        /// Asynchronously loads the first asset whose catalog entry contains at least one of the specified labels.
        /// </summary>
        /// <param name="labels">
        /// List of labels to match against catalog entries. An entry is considered a match if its
        /// <c>labels</c> collection contains any one of the provided label values.
        /// </param>
        /// <returns>
        /// A <see cref="LoadOperation{T}"/> that completes with the loaded asset as a
        /// <see cref="UnityEngine.Object"/> on success, or a failure state if no matching
        /// entry is found or the underlying bundle fails to load.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method waits for any in-progress catalog loads to complete, then scans the
        /// currently loaded <see cref="AssetCatalogData"/> instances in order and selects
        /// the first <see cref="AssetCatalogEntry"/> whose <c>labels</c> collection contains
        /// at least one of the labels in <paramref name="labels"/>. It then delegates to
        /// <see cref="LoadAssetByName(string)"/> to perform the actual bundle and asset load.
        /// </para>
        /// <para>
        /// Label matching is an "any-match" search: if an entry has labels
        /// <c>["vh", "civilian", "male"]</c> and the caller passes
        /// <c>["civilian", "tank"]</c>, that entry is considered a match because it
        /// contains <c>"civilian"</c>.
        /// </para>
        /// <para>
        /// Memory management:
        /// </para>
        /// <para>
        /// As with <see cref="LoadAssetByName(string)"/>, this system functions as a loader,
        /// not a long-term cache. Once the asset has been loaded and returned, the caller is
        /// responsible for managing its lifetime. Destroy any instantiated GameObjects created
        /// from the loaded asset, clear strong C# references to the asset when it is no longer
        /// needed, and then invoke <c>UnloadUnusedAssetsCoroutine()</c> (or call
        /// <see cref="UnityEngine.Resources.UnloadUnusedAssets"/> directly) to allow Unity to
        /// unload the underlying resources.
        /// </para>
        /// <para>
        /// If you intend to create many instances of a single asset chosen by label (for
        /// example, selecting a random barrel prefab from a group of labeled barrels),
        /// the recommended pattern is to:
        /// </para>
        /// <list type="number">
        /// <item>
        /// <description>Call <see cref="LoadAnyAssetByLabels"/> once to obtain the prefab.</description>
        /// </item>
        /// <item>
        /// <description>Instantiate as many copies as needed.</description>
        /// </item>
        /// <item>
        /// <description>When finished, destroy the instances, clear references to the prefab, and run <c>UnloadUnusedAssetsCoroutine()</c> to free memory.</description>
        /// </item>
        /// </list>
        /// </remarks>
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

            var rideConfigSystem = Systems.Get<ConfigurationSystemUnity>();
            var fileStorageSystemAWS = Systems.Get<AWSFileStorageS3System>();
            fileStorageSystemAWS.m_cognitoIdentityPoolId = rideConfigSystem.GetTerrainKey();
            fileStorageSystemAWS.m_regionName = rideConfigSystem.GetTerrainKeyRegion();

            string bucket = parts[0];
            string key = string.Join("/", parts.Skip(1));

            if (!ConfigurationSystemUnity.IsTerrainKeyFormatValid(fileStorageSystemAWS.m_cognitoIdentityPoolId))
            {
                op.SetCompleted(AWSFileStorageS3System.GetLocation(bucket, key));
                return op;
            }

            //Debug.Log($"RequestSignedURL() - {bucket} {key}");

            fileStorageSystemAWS.GetSignedURL(bucket, key, signedURL =>
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

        /// <summary>Returns the total number of assets across all catalogs.</summary>
        public int GetEntryCount() => m_catalogs.SelectMany(c => c.entries).Count();

        /// <summary>
        /// Runs Unity's <see cref="UnityEngine.Resources.UnloadUnusedAssets"/> and yields
        /// until the unload operation has completed.
        /// </summary>
        /// <returns>
        /// An <see cref="System.Collections.IEnumerator"/> that yields while Unity scans
        /// for assets that are no longer referenced and releases their associated memory.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use this helper after destroying all instances of assets loaded through this
        /// system and clearing any strong C# references to those assets. Unity only
        /// releases the underlying textures, meshes, and other resources when there are
        /// no remaining references and an unload pass is executed.
        /// </para>
        /// <para>
        /// Centralizing the call to <see cref="UnityEngine.Resources.UnloadUnusedAssets"/>
        /// in this coroutine makes the intended lifetime model explicit for callers of
        /// the asset loading system and avoids scattering unload calls throughout the codebase.
        /// </para>
        /// <para>
        /// Typical usage:
        /// </para>
        /// <code>
        /// // Destroy instances and clear references
        /// GameObject.Destroy(characterInstance);
        /// characterPrefab = null;
        ///
        /// // Then run the unload pass through this system
        /// StartCoroutine(assetLoadingSystem.UnloadUnusedAssetsCoroutine());
        /// </code>
        /// </remarks>
        public IEnumerator UnloadUnusedAssetsCoroutine()
        {
            yield return Resources.UnloadUnusedAssets();
        }

        /// <summary>Returns a flat list of all asset name/label pairs in loaded catalogs.</summary>
        /// <returns>List of (assetName, labels).</returns>
        public List<(string assetName, List<string> labels)> GetEntryInfoList()
        {
            return m_catalogs
                .SelectMany(c => c.entries)
                .Select(e => (e.assetName, e.labels))
                .ToList();
        }

        private static void CheckRideBundleVersion(AssetCatalogData catalog)
        {
            // Null or empty means older catalogs that didn't contain this field.
            if (string.IsNullOrEmpty(catalog.rideBundleVersion))
            {
                Debug.LogWarning(
                    $"[RIDE Asset Catalog] Catalog '{catalog.localPrefixPath}' " +
                    $"does not specify a rideBundleVersion. It may be outdated."
                );
            }
            // Compare against the editor-defined version.
            else if (!string.Equals(catalog.rideBundleVersion, AssetCatalogData.RIDE_VERSION, StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"AssetLoadingSystemAssetBundles - [RIDE Asset Catalog] Version mismatch detected! " +
                    $"Catalog version: {catalog.rideBundleVersion}, " +
                    $"Expected: {AssetCatalogData.RIDE_VERSION}. " +
                    $"Loading will continue, but issues may occur."
                );
            }
        }

        private void BeginRemoteBundleDownloadStatus(string assetName, string bundleName, string url)
        {
            RemoteBundleDownloadActive = true;
            RemoteBundleAssetName = assetName;
            RemoteBundleName = bundleName;
            RemoteBundleUrl = url;
            RemoteBundleRequestProgress = 0f;
            RemoteBundleOverallProgress = 0f;
            RemoteBundleDownloadedBytes = 0;
            RemoteBundleElapsedSeconds = 0f;
            RemoteBundleAverageBytesPerSecond = 0f;
            RemoteBundleLastResult = "InProgress";
            RemoteBundleLastError = null;
        }

        private void UpdateRemoteBundleDownloadStatus(
            float requestProgress,
            float overallProgress,
            ulong downloadedBytes,
            float elapsedSeconds)
        {
            RemoteBundleRequestProgress = Mathf.Clamp01(requestProgress);
            RemoteBundleOverallProgress = Mathf.Clamp01(overallProgress);
            RemoteBundleDownloadedBytes = downloadedBytes;
            RemoteBundleElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            RemoteBundleAverageBytesPerSecond = elapsedSeconds > 0.0001f
                ? (float)(downloadedBytes / elapsedSeconds)
                : 0f;
        }

        private void CompleteRemoteBundleDownloadStatus(string result, string error = null)
        {
            RemoteBundleDownloadActive = false;
            RemoteBundleLastResult = result;
            RemoteBundleLastError = error;
        }

        private static string FormatBytes(ulong bytes)
        {
            const float KB = 1024f;
            const float MB = KB * 1024f;
            const float GB = MB * 1024f;

            if (bytes >= GB) return $"{bytes / GB:0.00} GB";
            if (bytes >= MB) return $"{bytes / MB:0.00} MB";
            if (bytes >= KB) return $"{bytes / KB:0.0} KB";
            return $"{bytes} B";
        }

        private static string FormatBytesPerSecond(float bytesPerSecond)
        {
            if (bytesPerSecond <= 0f) return "0 B/s";

            const float KB = 1024f;
            const float MB = KB * 1024f;
            const float GB = MB * 1024f;

            if (bytesPerSecond >= GB) return $"{bytesPerSecond / GB:0.00} GB/s";
            if (bytesPerSecond >= MB) return $"{bytesPerSecond / MB:0.00} MB/s";
            if (bytesPerSecond >= KB) return $"{bytesPerSecond / KB:0.0} KB/s";
            return $"{bytesPerSecond:0} B/s";
        }
    }
}
