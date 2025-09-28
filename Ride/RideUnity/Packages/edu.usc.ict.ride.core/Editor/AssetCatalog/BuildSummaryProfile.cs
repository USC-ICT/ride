using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ScriptableObject that stores the complete history of asset catalog build summaries.
/// Used to persist and inspect build data across Unity Editor sessions.
/// </summary>
[CreateAssetMenu(menuName = "AssetCatalog/Build Summary Profile")]
public class BuildSummaryProfile : ScriptableObject
{
    /// <summary>
    /// A list of entries, each representing a catalog and bundle build process.
    /// </summary>
    public List<BuildSummaryEntry> builds = new();
}