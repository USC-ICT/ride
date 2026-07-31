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
    /// 1. Parse the Unity major/minor version (for example "6000.4" from "6000.4.1f1").
    /// 2. If the major version is less than 6000, return the oldest supported Unity bucket.
    /// 3. If the version falls between supported Unity 6 minors, use the nearest earlier supported bucket.
    /// 4. If the version is newer than the known table, use the newest supported bucket.
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
        // Keep this list sorted oldest to newest so fallback selection stays correct.
        (int major, int minor, string folder)[] supportedVersions =
        {
            (6000, 0, "6000.0.x"),
            (6000, 1, "6000.1.x"),
            (6000, 4, "6000.4.x")
        };

        string u = Application.unityVersion;

        // Extract the major/minor version from values like 6000.4.1f1 or 2023.3.5f1.
        string[] parts = u.Split('.');
        if (parts.Length < 2)
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName() - Unity version '{u}' could not be parsed. Using oldest supported version: {supportedVersions[0].folder}");
            return supportedVersions[0].folder;
        }

        if (!int.TryParse(parts[0], out int major))
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName() - Unity major version '{u}' is not numeric. Using oldest supported version: {supportedVersions[0].folder}");
            return supportedVersions[0].folder;
        }

        if (!int.TryParse(parts[1], out int minor))
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName() - Unity minor version '{u}' is not numeric. Using oldest supported version: {supportedVersions[0].folder}");
            return supportedVersions[0].folder;
        }

        (int major, int minor, string folder) selected = supportedVersions[0];
        bool exactMatch = false;
        foreach (var supported in supportedVersions)
        {
            if (major < supported.major || (major == supported.major && minor < supported.minor))
                break;

            selected = supported;
            if (major == supported.major && minor == supported.minor)
                exactMatch = true;
        }

        if (major > supportedVersions[^1].major || (major == supportedVersions[^1].major && minor > supportedVersions[^1].minor))
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName(): Unity version '{u}' is newer than the compatibility table. Using newest supported version: {selected.folder}");
        }
        else if (!exactMatch)
        {
            Debug.LogWarning($"GetAssetBundleUnityVersionName(): Unity version '{u}' falls between supported compatibility buckets. Using nearest earlier supported version: {selected.folder}");
        }

        return selected.folder;
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
