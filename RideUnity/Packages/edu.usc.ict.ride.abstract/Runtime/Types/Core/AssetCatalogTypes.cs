using System;

/// <summary>
/// Represents the data required to load a catalog file during runtime.
/// Used by the asset loading system to determine where and how to load a catalog.
/// </summary>
[Serializable]
public class CatalogLoadInfo
{
    /// <summary>
    /// Raw JSON content of the catalog (used for in-memory loading).
    /// </summary>
    public string catalogJson;

    /// <summary>
    /// Optional file path to a catalog.json file, local or remote.
    /// </summary>
    public string catalogPath;
    
    /// <summary>
    /// Whether the catalog path refers to a remote URL.
    /// </summary>
    public bool isRemote;
}