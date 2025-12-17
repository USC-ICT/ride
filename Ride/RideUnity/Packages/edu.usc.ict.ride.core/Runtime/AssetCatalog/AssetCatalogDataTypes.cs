using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ride
{
    #region Asset Catalog Related Data

    /// <summary>
    /// Represents an asset that can be included in a catalog, along with its associated labels.
    /// Used in the Unity Editor for organizing assets before building.
    /// </summary>
    [Serializable]
    public class LoadableAsset
    {
        public UnityEngine.Object asset;
        public List<string> labels = new();

        [NonSerialized] public string assetPath;  // cache the asset path, to avoid calling AssetDatabase.GetAssetPath() every frame
    }

    /// <summary>
    /// Represents a single catalog entry written into a built catalog.json file.
    /// Maps asset name to the bundle it resides in, along with its labels.
    /// </summary>
    [Serializable]
    public class AssetCatalogEntry
    {
        public string assetName;
        public string bundleFileName;
        public string bundleHash128;
        public List<string> labels = new();
    }

    /// <summary>
    /// Editor-time representation of a group of assets for use with the Asset Catalog Editor Window.
    /// Defines group-specific prefix paths and inclusion flags for build processing.
    /// </summary>
    [Serializable]
    public class AssetCatalogGroup
    {
        public string groupName;
        public string localPrefixPath;
        public string remotePrefixPath;
        public List<LoadableAsset> assets = new();
        public bool includeInBuild = true;
    }

    /// <summary>
    /// Runtime/build-time descriptor representing the built catalog.
    /// This is what gets serialized into the catalog.json files.
    /// </summary>
    [Serializable]
    public class AssetCatalogData
    {
        public const string RIDE_VERSION = "1.0";

        public string rideBundleVersion;  // ride custom version number for this data type
        public string unityVersion;  // Full Unity version used when the bundles and catalog were built.  Example: "2023.2.8f1"
        public string platform;
        public string renderPipeline;
        public string renderPipelineVersion;
        public string localPrefixPath;
        public string remotePrefixPath;
        public List<AssetCatalogEntry> entries = new();

        [NonSerialized] public bool isRemoteCatalog;  // populated at runtime
    }

    #endregion

    #region Catalog Loading Info

    /// <summary>
    /// Unity-specific wrapper for catalog loading, used at runtime to specify where to load a catalog.json file from.
    /// </summary>
    [Serializable]
    public class CatalogLoadInfoUnity
    {
        [Tooltip("Optional reference to a prebuilt catalog.json TextAsset.")]
        public TextAsset catalogJsonFile;
        [Tooltip("Optional manual path to the catalog.json file.")]
        public string catalogPath;
        [Tooltip("Is the manual path referring to a remote file?")]
        public bool isRemote;

        /// <summary>
        /// Converts the Unity-side loading info into the core data structure used by the catalog loading system.
        /// </summary>
        /// <returns>A CatalogLoadInfo instance initialized with the appropriate values.</returns>
        public CatalogLoadInfo ToCoreInfo()
        {
            return new CatalogLoadInfo
            {
                catalogJson = catalogJsonFile != null ? catalogJsonFile.text : null,
                catalogPath = catalogPath,
                isRemote = isRemote
            };
        }
    }
    #endregion
}
