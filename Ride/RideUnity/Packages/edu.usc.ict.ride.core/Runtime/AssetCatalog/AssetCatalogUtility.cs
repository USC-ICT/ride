using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ride
{
/// <summary>
/// Provides a suite of utility methods for managing asset catalog groups via script, CLI or interactively in the Unity Editor.
/// Supports batch operations including group creation, asset assignment, label management, and catalog/bundle building.
/// </summary>
public static class AssetCatalogUtility
{
    /// <summary>
    /// Generates the default local prefix path for a given asset group.
    /// </summary>
    /// <param name="group">The asset group to generate the path for.</param>
    /// <returns>The default local prefix path.</returns>
    public static string GenerateDefaultLocalPath(string groupName) => Path.Combine(Application.persistentDataPath, "AssetCatalogs", SanitizeGroupName(groupName)).Replace("\\", "/");

    /// <summary>
    /// Generates the default local prefix path for a given asset group.
    /// </summary>
    /// <param name="group">The asset group to generate the path for.</param>
    /// <returns>The default local prefix path.</returns>
    public static string GenerateDefaultRemotePath(string groupName) => Path.Combine("ride", "AssetCatalogs", SanitizeGroupName(groupName)).Replace("\\", "/");

    /// <summary>
    /// Sanitizes a group name by replacing spaces and handling empty/null values.
    /// </summary>
    /// <param name="name">The original group name.</param>
    /// <returns>A safe, non-empty, space-free string.</returns>
    private static string SanitizeGroupName(string groupName) => string.IsNullOrWhiteSpace(groupName) ? "Default" : groupName.Replace(" ", "");

    /// <summary>
    /// Returns the version-folder name used to locate compatible AssetBundles for
    /// the current Unity version. This maps the running Unity version to a manually
    /// maintained compatibility set (for example "6000.0.x" or "6000.1.x").
    /// </summary>
    /// <returns>
    /// A string representing the compatible AssetBundle version folder.
    /// </returns>
    /// <remarks>
    /// The logic is:
    /// 1. Parse the Unity major version (for example "6000" from "6000.1.2f1").
    /// 2. If the major version is less than 6000, return the older default.
    /// 3. If the Unity version matches an explicit known entry (such as "6000.0"
    ///    or "6000.1"), return that version.
    /// 4. Otherwise assume the Unity version is newer than the known table and
    ///    return the newer default.
    ///
    /// Update the explicit mappings as needed when Unity changes bundle formats.
    /// 
    /// <para>
    /// This mechanism allows the application to use a small number of controlled,
    /// tested bundle versions while still supporting a range of Unity editors and
    /// runtimes. It also protects against silent asset breakage when Unity changes
    /// asset bundle serialization in a new release.
    /// </para>
    ///
    /// <para>
    /// <b>Important:</b> This compatibility table is intentionally conservative.
    /// When upgrading Unity versions or rebuilding asset bundles, the table should
    /// be reviewed and updated to reflect which Unity versions share binary-format
    /// compatibility for your assets.
    /// </para>
    /// </remarks>
    public static string GetCompatibleUnityVersionName()
    {
        // Default version (fallback if nothing matches)
        string olderDefaultVersion = "6000.0.x";
        string newerDefaultVersion = "6000.1.x";

        string u = Application.unityVersion;

        // Extract the *major version*, 6000.1.2f1, 2023.3.5f1, pull out "6000", "2023", etc.
        string[] parts = u.Split('.');
        if (parts.Length == 0)
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName() - Unity version '{u}' could not be parsed. Using older default: {olderDefaultVersion}");
            return olderDefaultVersion;
        }

        if (!int.TryParse(parts[0], out int major))
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName() - Unity major version '{u}' is not numeric. Using older default: {olderDefaultVersion}");
            return olderDefaultVersion;
        }

        // Handle Unity versions < 6
        if (major < 6000)
            return olderDefaultVersion;  // Anything older than Unity 6 - use older default version

        // Handle explicit mappings for supported Unity versions
        if (u.StartsWith("6000.0")) return "6000.0.x";
        if (u.StartsWith("6000.1")) return "6000.1.x";

        Debug.LogWarning($"GetAssetBundleUnityVersionName(): Unity version '{u}' not in compatibility table. Assuming newer version. Using: {newerDefaultVersion}");

        return newerDefaultVersion;
    }

    public static string GetPlatformName()
    {
        string platform = "";
        if (RideUtils.IsAndroid()) platform = "Android";
        else if (RideUtils.IsIOS()) platform = "iOS";
        else if (RideUtils.IsWebGL()) platform = "WebGL";
        else if (RideUtils.IsLinux()) platform = "StandaloneLinux64";
        else if (RideUtils.IsOSX()) platform = "StandaloneOSX";  // OSX and Windows currently placed last in the list to prevent early match when in editor
        else if (RideUtils.IsWindows()) platform = "StandaloneWindows64";
        else Debug.LogWarning("GetAssetBundlePlatformName() - platform not found.");
        return platform;
    }

    /// <summary>
    /// Returns a short name for the currently active render pipeline (BuiltIn, URP, HDRP, etc.).
    /// </summary>
    /// <returns>The name of the active render pipeline.</returns>
    public static string GetRenderPipelineName()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null)
            return "BuiltIn";

        var type = rp.GetType();
        var ns = type.Namespace ?? string.Empty;
        var name = type.Name ?? string.Empty;

        // Heuristics to avoid hard references to URP/HDRP assemblies
        if (ns.Contains("Universal") || name.Contains("Universal"))
            return "URP";

        if (ns.Contains("HighDefinition") || name.Contains("HDRenderPipeline") || name.Contains("HDRP"))
            return "HDRP";

        // Fallback: use the type name so you at least know what custom RP was used.
        return name;
    }
}
}
