using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;
using Ride.AWS;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ride
{
    /// <summary>
    /// A class to group together information needed to load addressables from diff projects
    /// </summary>
    [Serializable]
    public class AddressableCatalogInfo
    {
        public string bucketName;
        public string filePrefixPath;
        public string catalogKey;
        public bool isLocal;

        private bool isCatalogLoaded = false;
        public bool IsCatalogLoaded { get => isCatalogLoaded; set => isCatalogLoaded = value; }

        private bool assetBundlesReady = false;
        public bool AssetBundlesReady { get => assetBundlesReady; set => assetBundlesReady = value; }

        private List<string> assetBundleKeys = new();
        public List<string> AssetBundleKeys { get => assetBundleKeys; }

        public AddressableCatalogInfo(string bucketName, string filePrefixPath, string catalogKey)
        {
            this.bucketName = bucketName;
            this.filePrefixPath = filePrefixPath;
            this.catalogKey = catalogKey;
            this.isLocal = string.IsNullOrEmpty(this.bucketName);
            assetBundleKeys = new();
        }

        public AddressableCatalogInfo(string bucketName, string filePrefixPath, string catalogKey, bool isLocal)
        {
            this.bucketName = bucketName;
            this.filePrefixPath = filePrefixPath;
            this.catalogKey = catalogKey;
            this.isLocal = isLocal;
            assetBundleKeys = new();
        }

        public string GetCatalogAssetKey()
        {
            return Path.Combine(filePrefixPath, catalogKey).Replace("\\", "/");
        }

        public void AddAssetBundleKey(string key)
        {
            assetBundleKeys ??= new List<string>();
            assetBundleKeys.Add(key);
        }
    }

    /// <summary>
    /// A class to group together information about an asset loaded in using the RideAddressableSystem
    /// </summary>
    public struct LoadedAssetInfo
    {
        public GameObject assetObject;
        public string assetName;
        public List<string> labels;
        public string catalog;

        public LoadedAssetInfo(GameObject assetObject, string assetName, List<string> labels /*string catalog*/)
        {
            this.assetObject = assetObject;
            this.assetName = assetName;
            this.labels = labels;
            this.catalog = "";
        }
    }


    /// <summary>
    /// System that handles loading of Addressables
    /// </summary>
    public class AddressableSystem : RideSystemMonoBehaviour
    {
        [SerializeField] private List<AddressableCatalogInfo> m_addressableInfos = new();
        [SerializeField] private bool m_clearLoadedAssets = false;

        private AWSFileStorageS3System m_fileStorageSystemAWS;
        private ConfigurationSystemUnity m_rideConfigSystem;
        private bool m_allCatalogsLoaded = false;
        public bool AllCatalogsLoaded => m_allCatalogsLoaded;
        private bool m_allAssetBundlesReady = false;

        private List<AsyncOperationHandle> m_loadedAssets = new();
        private Dictionary<string, string> m_signedURLCache = new();

        private Dictionary<string, List<string>> m_allAvailableAssets = new();
        public Dictionary<string, List<string>> AllAvailableAssets => m_allAvailableAssets;

        [SerializeField, HideInInspector]
        private List<string> m_availableLabels = new();
        public List<string> AvailableLabels => m_availableLabels;

        private List<LoadedAssetInfo> m_loadedAssetsList = new();
        public List<LoadedAssetInfo> LoadedAssets => m_loadedAssetsList;

        private List<AsyncOperationHandle<IResourceLocator>> m_pendingCatalogLoads = new();

        public enum HandleType
        {
            Catalog = 0,
            Bundle = 10,
        }

        public override void SystemAwake()
        {
            //    if (Instance != null && Instance != this)
            //    {
            //        Destroy(gameObject);
            //        return;
            //    }
            //    Instance = this;
            base.SystemAwake();
        }

        public override void SystemInit()
        {
            Addressables.WebRequestOverride = EditWebRequestURL;

            m_rideConfigSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_fileStorageSystemAWS = Globals.api.GetSystem<AWSFileStorageS3System>();
            m_fileStorageSystemAWS.m_cognitoIdentityPoolId = m_rideConfigSystem.GetTerrainKey();
            m_fileStorageSystemAWS.m_regionName = m_rideConfigSystem.GetTerrainKeyRegion();

            if (Application.isPlaying)
            {
                m_allCatalogsLoaded = false;
                LoadCatalogs();
            }
            
            base.SystemInit();
        }

        private void EditWebRequestURL(UnityWebRequest request)
        {
            string originalUrl = request.url;
            string assetBundleKey = ExtractAssetBundleKeyFromURL(originalUrl);

            if (IsLocalAsset(assetBundleKey))
            {
                string localPath = Path.Combine(Application.persistentDataPath, "Addressables", Path.GetFileName(originalUrl)).Replace("\\", "/");
                request.url = localPath;
                return;
            }

            if (originalUrl.Contains(".bundle"))
            {
                Debug.Log("EditWebRequestURL(): original URL contains .bundle");

                if (m_signedURLCache.TryGetValue(assetBundleKey, out string signedURL))
                {
                    request.url = signedURL;
                    Debug.Log($"Asset bundle request URL overridden with signed URL: {signedURL}");
                }
                else
                    Debug.LogError($"No signed URL available for asset bundle: {assetBundleKey}");
            }
        }

        private string ExtractAssetBundleKeyFromURL(string originalUrl)
        {
            Uri uri = new(originalUrl);
            string[] segments = uri.Segments;
            string assetBundleKey = segments[^1];
            return assetBundleKey;
        }

        private void LoadCatalogs()
        {
            foreach (var addressableInfo in m_addressableInfos)
                ProcessCatalogLoading(addressableInfo);
        }

        private void ProcessCatalogLoading(AddressableCatalogInfo addressableInfo)
        {
            if (addressableInfo.isLocal)
                LoadLocalCatalog(addressableInfo);
            else
                RequestSignedURL(addressableInfo, HandleType.Catalog);
        }

        private void LoadLocalCatalog(AddressableCatalogInfo addressableInfo)
        {
            string localCatalogPath = Path.Combine(Application.persistentDataPath, "Addressables", addressableInfo.GetCatalogAssetKey()).Replace("\\", "/");

            if (!System.IO.File.Exists(localCatalogPath))
            {
                Debug.LogError($"Local catalog not found at {localCatalogPath}. Ensure Addressables are built and stored correctly.");
                return;
            }
            Debug.Log($"Loading local Addressables catalog from {localCatalogPath}");

            Addressables.LoadContentCatalogAsync(localCatalogPath).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    addressableInfo.IsCatalogLoaded = true;
                    m_loadedAssets.Add(handle);

                    string remoteLoadPath = Addressables.RuntimePath;
                    string localLoadPath = Path.Combine(Application.persistentDataPath, "Addressables").Replace("\\", "/");

                    foreach (var key in handle.Result.Keys)
                    {
                        string assetPath = key.ToString();
                        if (assetPath.StartsWith(remoteLoadPath) && assetPath.EndsWith(".bundle"))
                            assetPath = assetPath.Replace(remoteLoadPath, localLoadPath);
                        addressableInfo.AddAssetBundleKey(assetPath);
                    }
                    addressableInfo.AssetBundlesReady = true;
                    ValidateCatalogLoadStatus();
                    ValidateAssetBundleLoadStatus();
                    PopulateAssetLabels();
                }
                else
                    Debug.LogError($"Failed to load local catalog from {localCatalogPath}.");
            };
        }

        private void RequestSignedURL(AddressableCatalogInfo addressableInfo, HandleType handleType)
        {
            string assetKey = addressableInfo.GetCatalogAssetKey();
            m_fileStorageSystemAWS.GetSignedURL(addressableInfo.bucketName, assetKey, signedURL =>
            {
                if (!string.IsNullOrEmpty(signedURL))
                {
                    Debug.Log($"Successfully received signed URL for {assetKey}");

                    switch (handleType)
                    {
                        case HandleType.Catalog: LoadCatalog(addressableInfo, signedURL); break;
                        case HandleType.Bundle: m_signedURLCache[assetKey] = signedURL; break;
                    }
                }
                else
                    Debug.LogError($"Failed to get signed URL for {assetKey}.");
            });
        }

        public void AddAndLoadAddressableInfo(AddressableCatalogInfo addressableInfo)
        {
            m_addressableInfos.Add(addressableInfo);
            ProcessCatalogLoading(addressableInfo);
        }

        private void LoadCatalog(AddressableCatalogInfo addressableInfo, string catalogUrl)
        {
            Addressables.LoadContentCatalogAsync(catalogUrl).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("Catalog loaded successfully!");
                    addressableInfo.IsCatalogLoaded = true;
                    m_loadedAssets.Add(handle);

                    foreach (var key in handle.Result.Keys)
                        if (key.ToString().Contains(".bundle"))
                            addressableInfo.AddAssetBundleKey(key.ToString());

                    StartCoroutine(PreFetchSignedURLs(addressableInfo));
                    ValidateCatalogLoadStatus();
                    PopulateAssetLabels();
                }
                else
                    Debug.LogError($"Failed to load catalog from {catalogUrl}.");
            };
        }

        private IEnumerator PreFetchSignedURLs(AddressableCatalogInfo addressableInfo)
        {
            if (addressableInfo.isLocal)
            {
                addressableInfo.AssetBundlesReady = true;
                ValidateAssetBundleLoadStatus();
                yield break; //Skip AWS S3 signed URL fetching for local bundles
            }

            foreach (string assetBundleKey in addressableInfo.AssetBundleKeys)
            {
                bool signedURLReady = false;
                string fullAssetKey = addressableInfo.filePrefixPath + assetBundleKey;

                m_fileStorageSystemAWS.GetSignedURL(addressableInfo.bucketName, fullAssetKey, signedURL =>
                {
                    if (!string.IsNullOrEmpty(signedURL))
                    {
                        m_signedURLCache[assetBundleKey] = signedURL;
                        signedURLReady = true;
                        Debug.Log($"Successfully fetched and cached signed URL for bundle: {assetBundleKey}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to fetch signed URL for bundle: {assetBundleKey}");
                        signedURLReady = true;
                    }
                });
                yield return new WaitUntil(() => signedURLReady);
            }
            addressableInfo.AssetBundlesReady = true;
            ValidateAssetBundleLoadStatus();
        }

        private void ValidateCatalogLoadStatus()
        {
            m_allCatalogsLoaded = m_addressableInfos.All(addressableInfo => addressableInfo.IsCatalogLoaded);
        }

        private void ValidateAssetBundleLoadStatus()
        {
            bool allBundlesReady = m_addressableInfos.All(addressableInfo =>
                !addressableInfo.isLocal || (addressableInfo.AssetBundlesReady && addressableInfo.AssetBundleKeys.Count > 0));

            m_allAssetBundlesReady = allBundlesReady;
        }

        public IEnumerator RequestAssetLoadCoroutine(string label, RideAddressable parent)
        {
            yield return new WaitUntil(() => m_allCatalogsLoaded && m_allAssetBundlesReady);

            Addressables.LoadAssetAsync<GameObject>(label).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    m_loadedAssets.Add(handle);
                    GameObject asset = handle.Result;

                    if (asset != null)
                    {
                        GameObject instantiatedAsset = Instantiate(asset, parent.transform);
                        instantiatedAsset.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                        //string catalogSource = m_allAvailableAssets.ContainsKey(asset.name) ? m_allAvailableAssets[asset.name].catalog : "Unknown Catalog";
                        List<string> assetLabels = GetAssetLabelsByAssetName(asset.name);
                        m_loadedAssetsList.Add(new LoadedAssetInfo(instantiatedAsset, asset.name, assetLabels /*catalogSource*/));
                        Debug.Log($"Asset {asset.name} successfully loaded and parented to {parent.transform.name}.");
                        parent.OnAssetLoaded(instantiatedAsset);
                    }
                    else
                        Debug.LogError($"Failed to instantiate asset: {label}");
                }
                else
                {
                    if (!IsLocalAsset(label) && handle.OperationException != null && handle.OperationException.Message.Contains("403"))
                    {
                        Debug.LogWarning("Received 403 Forbidden error, re-signing URLs and retrying...");
                        StartCoroutine(ReSignAndRetryLoad(label, parent));
                    }
                    else
                        Debug.LogError($"Failed to load asset with name: {label}. Error: {handle.OperationException?.Message}");
                }
            };
        }

        public List<string> GetAssetLabelsByAssetName(string assetName)
        {
            List<string> assetLabels = m_allAvailableAssets.ContainsKey(assetName) 
                ? new List<string>(m_allAvailableAssets[assetName])
                : new List<string>();
            return assetLabels;
        }

        private bool IsLocalAsset(string assetLabel)
        {
            if (m_addressableInfos == null || m_addressableInfos.Count == 0)
                return false;

            foreach (var info in m_addressableInfos)
            {
                if (info == null)
                    continue;

                if (info.isLocal)
                {
                    if (info.AssetBundleKeys == null || info.AssetBundleKeys.Count == 0)
                        continue;

                    if (info.AssetBundleKeys.Any(bundle => bundle.Contains(assetLabel)))
                        return true;
                }
            }
            return false;
        }

        private IEnumerator ReSignAndRetryLoad(string assetLabel, RideAddressable parent)
        {
            m_allAssetBundlesReady = false;
            Debug.Log($"Re-signing asset bundle URLs ");
            foreach (var addressableInfo in m_addressableInfos)
                yield return StartCoroutine(PreFetchSignedURLs(addressableInfo));
            StartCoroutine(RequestAssetLoadCoroutine(assetLabel, parent));
        }

        public List<string> GetAddressableLabels()
        {
            HashSet<string> validLabels = new();
            HashSet<string> assetNames = new();

            if (!m_allCatalogsLoaded)
            {
                Debug.LogWarning("Addressable catalogs are not yet loaded!");
                return validLabels.ToList();
            }

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string potentialLabel && !IsLikelyHash(potentialLabel))
                    {
                        if (locator.Locate(potentialLabel, null, out IList<IResourceLocation> locations) && locations != null && locations.Count > 0)
                        {
                            foreach (var location in locations)
                            {
                                if (location.PrimaryKey == potentialLabel)
                                    assetNames.Add(potentialLabel);
                                else
                                    validLabels.Add(potentialLabel);
                            }
                        }
                    }
                }
            }
            validLabels.ExceptWith(assetNames);
            return validLabels.ToList();
        }

        public void PopulateAssetLabels(/*AddressableCatalogInfo catalogInfo*/)
        {
            if (!m_allCatalogsLoaded)
            {
                Debug.LogWarning("Cannot populate asset labels: Addressable catalogs are not fully loaded.");
                return;
            }

            Debug.Log("Fetching available labels from loaded Addressable catalogs...");
            m_allAvailableAssets.Clear();
            HashSet<string> uniqueLabels = new();

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string assetName && !IsLikelyHash(assetName) && !IsBundleFile(assetName) && !IsLabel(assetName))
                    {
                        if (locator.Locate(assetName, null, out IList<IResourceLocation> locations) && locations != null && locations.Count > 0)
                        {
                            if (!m_allAvailableAssets.ContainsKey(assetName))
                                m_allAvailableAssets[assetName] = (new()/*, catalogInfo.GetCatalogAssetKey()*/);

                            List<string> assignedLabels = GetLabelsForAsset(assetName);
                            foreach (var label in assignedLabels)
                            {
                                if (!m_allAvailableAssets[assetName]/*.labels*/.Contains(label))
                                    m_allAvailableAssets[assetName]/*.labels*/.Add(label);
                                uniqueLabels.Add(label);
                            }

                        }
                    }
                }
            }
            m_availableLabels = uniqueLabels.OrderBy(label => label).ToList();
            Debug.Log($"Found {m_availableLabels.Count} unique labels.");
            LogAllAvailableAssets();
        }

        private bool IsBundleFile(string assetName)
        {
            return assetName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLabel(string key)
        {
            bool foundAsAsset = false;
            bool foundAsLabel = false;

            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(key, null, out IList<IResourceLocation> locations) && locations != null)
                {
                    foreach (var location in locations)
                    {
                        //If we find an exact match where the PrimaryKey is the key, it's an asset
                        if (location.PrimaryKey == key)
                            foundAsAsset = true;
                        else
                            foundAsLabel = true;
                    }
                }
            }
            //If the key exists as both a label and an asset, treat it as an asset.
            return foundAsLabel && !foundAsAsset;
        }

        private List<string> GetLabelsForAsset(string assetName)
        {
            HashSet<string> labels = new();
            HashSet<string> allDefinedLabels = GetAllDefinedLabels(); 
            bool assetNameIsARealLabel = allDefinedLabels.Contains(assetName);

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string labelName && !IsLikelyHash(labelName) && !IsBundleFile(labelName))
                    {
                        if (locator.Locate(labelName, null, out IList<IResourceLocation> locations) && locations != null)
                            foreach (var location in locations)
                                if (location.PrimaryKey == assetName)
                                    labels.Add(labelName);
                    }
                }
            }
            //Remove asset name from labels list unless it was explicitly assigned as a label
            if (!assetNameIsARealLabel)
                labels.Remove(assetName);

            return labels.ToList();
        }

        private HashSet<string> GetAllDefinedLabels()
        {
            HashSet<string> definedLabels = new();

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string potentialLabel && !IsLikelyHash(potentialLabel) && !IsBundleFile(potentialLabel))
                    {
                        if (locator.Locate(potentialLabel, null, out IList<IResourceLocation> locations) && locations != null && locations.Count > 1)
                            definedLabels.Add(potentialLabel);
                    }
                }
            }
            return definedLabels;
        }

        public void LogAllAvailableAssets()
        {
            if (m_allAvailableAssets == null || m_allAvailableAssets.Count == 0)
            {
                Debug.Log("No available addressable assets.");
                return;
            }

            Debug.Log("==== Available Addressable Assets ====");

            foreach (var entry in m_allAvailableAssets)
            {
                string assetName = entry.Key;
                string labels = entry.Value/*.labels*/.Count > 0 ? string.Join(", ", entry.Value/*.labels*/) : "No Labels";

                if (!IsLabel(assetName) && !IsBundleFile(assetName))
                    Debug.Log($"Asset: {assetName} | Labels: {labels}");
            }
        }

        public string GetAssetLabelString(string assetName)
        {
            string labels = m_allAvailableAssets[assetName]/*.labels*/.Count > 0 ? string.Join(", ", m_allAvailableAssets[assetName]/*.labels*/) : "No Labels";
            return $"Asset: {assetName} | Labels: {labels}";
        }

        private bool IsLikelyHash(string label)
        {
            //Check if hex string (like a GUID or hash)
            return label.Length > 30 && System.Text.RegularExpressions.Regex.IsMatch(label, @"^[a-fA-F0-9]{30,}$");
        }

        private void ReleaseAllAssets()
        {
            foreach (var asset in m_loadedAssets)
                if (asset.IsValid())
                    Addressables.Release(asset);
        }

        public override void SystemShutdown()
        {
            base.SystemShutdown();
            if (m_clearLoadedAssets)
                ReleaseAllAssets();
            m_loadedAssets.Clear();
        }

        public void EditorLoadCatalogs()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Cannot load Addressable catalogs in Play Mode.");
                return;
            }

            Debug.Log("Starting editor Addressable catalog loading...");

            m_allCatalogsLoaded = false;
            m_allAvailableAssets.Clear();
            m_availableLabels.Clear();
            m_pendingCatalogLoads.Clear();

            foreach (var addressableInfo in m_addressableInfos)
            {
                string catalogPath = addressableInfo.isLocal 
                    ? Path.Combine(Application.persistentDataPath, "Addressables", addressableInfo.GetCatalogAssetKey()).Replace("\\", "/")
                    : addressableInfo.GetCatalogAssetKey();

                var handle = Addressables.LoadContentCatalogAsync(catalogPath);
                m_pendingCatalogLoads.Add(handle);
                
                handle.Completed += catalogHandle =>
                {
                    if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        addressableInfo.IsCatalogLoaded = true;
                        Debug.Log($"Successfully loaded catalog: {catalogPath}");
                    }
                    else
                        Debug.LogError($"Failed to load catalog: {catalogPath}");
                };
            }
            #if UNITY_EDITOR
            EditorApplication.update += MonitorCatalogLoading;
            #endif
        }

        private void MonitorCatalogLoading()
        {
            bool allLoaded = true;

            foreach (var handle in m_pendingCatalogLoads)
            {
                if (!handle.IsDone)
                {
                    allLoaded = false;
                    break;
                }
            }

            if (allLoaded)
            {
                Debug.Log("All Addressable catalogs finished loading. Populating asset labels...");
                #if UNITY_EDITOR
                EditorApplication.update -= MonitorCatalogLoading; 
                #endif
                m_allCatalogsLoaded = true;
                PopulateAssetLabels();
                
                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                #endif
            }
        }
    }
}

