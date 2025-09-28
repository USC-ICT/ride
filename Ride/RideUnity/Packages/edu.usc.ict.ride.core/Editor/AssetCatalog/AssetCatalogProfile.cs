using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ScriptableObject that stores the configuration for asset groups and their global labels.
/// Used as the main editable asset in the Asset Catalog Editor.
/// </summary>
[CreateAssetMenu(menuName = "AssetCatalog/Asset Catalog Profile")]
public class AssetCatalogProfile : ScriptableObject
{
    /// <summary>
    /// Global list of all defined labels used for categorizing assets.
    /// </summary>
    public List<string> allLabels = new();

    /// <summary>
    /// List of all asset groups configured in the catalog.
    /// </summary>
    public List<AssetCatalogGroup> groups = new();
}
