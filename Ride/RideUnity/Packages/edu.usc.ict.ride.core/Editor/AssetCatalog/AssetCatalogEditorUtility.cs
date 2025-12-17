using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ride
{
/// <summary>
/// Provides a suite of utility methods for managing asset catalog groups via script, CLI or interactively in the Unity Editor.
/// Supports batch operations including group creation, asset assignment, label management, and catalog/bundle building.
/// </summary>
public static class AssetCatalogEditorUtility
{
    private const string CATALOG_PROFILE_PATH = "Assets/AssetCatalogData/AssetCatalogProfile.asset";

    private static AssetCatalogProfile LoadCatalogProfile()
    {
        AssetCatalogProfile profile = null;
        profile = AssetDatabase.LoadAssetAtPath<AssetCatalogProfile>(CATALOG_PROFILE_PATH);
        if (profile == null)
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(LoadCatalogProfile)}]: Asset catalog profile not found at path {CATALOG_PROFILE_PATH}.");
        return profile;
    }

    /// <summary>
    /// Helper method to log to both the Unity Editor's console and to the command line console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private static void LogToEditorAndCLI(string message)
    {
        Debug.Log(message);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Adds a new asset group to the asset catalog profile. Ensures uniqueness if the name already exists.
    /// </summary>
    /// <param name="assetGroupName">Desired name for the group (must not be empty).</param>
    public static void AddAssetGroup(string assetGroupName)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(assetGroupName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetGroup)}]: Group name is empty or null");
            return;
        }

        string uniqueGroupName = assetGroupName;
        string groupAddedMessage = $"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetGroup)}]: Added new asset group '{uniqueGroupName}'";
        if (profile.groups.Any(group => group.groupName == uniqueGroupName))
        {
            int suffix = 1;
            while (profile.groups.Any(group => group.groupName == $"{assetGroupName}_{suffix}"))
                suffix++; //increment suffix until it assetGroupName_suffix results in a unqiue group name
            uniqueGroupName = $"{assetGroupName}_{suffix}";
            groupAddedMessage = $"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetGroup)}]: Group name '{assetGroupName}' already exists, adding group '{uniqueGroupName}' instead";
        }

        var newGroup = new AssetCatalogGroup
        {
            groupName = uniqueGroupName,
            localPrefixPath = AssetCatalogUtility.GenerateDefaultLocalPath(uniqueGroupName),
            remotePrefixPath = AssetCatalogUtility.GenerateDefaultLocalPath(uniqueGroupName),
            includeInBuild = true
        };
        profile.groups.Add(newGroup);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI(groupAddedMessage);
    }

    /// <summary>
    /// Generates the postfix path based on Unity Version, Render Pipeline and Build Target.
    /// </summary>
    public static string GetBuildPostfixPath() => GetBuildPostfixPath(EditorUserBuildSettings.activeBuildTarget);

    public static string GetBuildPostfixPath(BuildTarget buildTarget) =>
        Path.Combine(AssetCatalogUtility.GetCompatibleUnityVersionName(), AssetCatalogUtility.GetRenderPipelineName(), GetPlatformId(buildTarget)).Replace("\\", "/");

    private static string GetPlatformId(BuildTarget buildTarget)
    {
        // use the BuildTarget name as a stable identifier.
        // Examples: "StandaloneWindows64", "Android", "WebGL"
        return buildTarget.ToString();
    }

    /// <summary>
    /// Sets whether a specified asset group should be included in asset catalog builds.
    /// </summary>
    /// <param name="groupName">The name of the asset group.</param>
    /// <param name="isBuildable">True to include the group in builds; false to exclude it.</param>
    public static void SetGroupBuildable(string groupName, bool isBuildable)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        AssetCatalogGroup group = profile.groups.FirstOrDefault(group => group.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(SetGroupBuildable)}]: No group named '{groupName}' found");
            return;
        }

        group.includeInBuild = isBuildable;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
    }

    /// <summary>
    /// Removes an asset group from the asset catalog profile.
    /// </summary>
    /// <param name="groupName">The name of the group to remove (must not be empty).</param>
    public static void RemoveAssetGroup(string groupName)
    {
        var profile = LoadCatalogProfile();
        if (profile == null)
            return;

        if (string.IsNullOrWhiteSpace(groupName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetGroup)}]: Provided group name is empty or null");
            return;
        }

        int removedCount = profile.groups.RemoveAll(group => group.groupName == groupName);
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetGroup)}]: Removed group '{groupName}'");
        }
        else
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetGroup)}]: Group '{groupName}' not found");
        }
    }

    /// <summary>
    /// Adds a single label to the asset catalog profile's global label list if it doesn't already exist.
    /// </summary>
    /// <param name="label">The label to add.</param>
    public static void AddLabel(string label)
    {
        var profile = LoadCatalogProfile();
        if (profile == null)
            return;

        if (string.IsNullOrWhiteSpace(label))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabel)}]: Provided label is empty or null");
            return;
        }

        if (!profile.allLabels.Contains(label))
        {
            profile.allLabels.Add(label);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }
    }

    /// <summary>
    /// Adds multiple labels to the asset catalog profile's global label list, skipping duplicates.
    /// </summary>
    /// <param name="labels">A list of labels to add.</param>
    public static void AddLabels(List<string> labels)
    {
        foreach (string label in labels)
            AddLabel(label);
    }

    /// <summary>
    /// Removes a label from the asset catalog profile's global label list and from all assets that use it.
    /// </summary>
    /// <param name="label">The label to remove.</param>
    public static void RemoveLabel(string label)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(label))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveLabel)}]: Provided label is empty or null");
            return;
        }

        if (profile.allLabels.Remove(label))
        {
            foreach (var group in profile.groups)
                foreach (var asset in group.assets)
                    asset.labels.Remove(label);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveLabel)}]: Label '{label}' removed from all asset groups and their respective assets");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveLabel)}]: Label '{label}' not found in label list");
    }

    /// <summary>
    /// Removes multiple labels from the asset catalog profile's global label list and from all assets that use them.
    /// </summary>
    /// <param name="labels">The list of labels to remove.</param>
    public static void RemoveLabels(List<string> labels)
    {
        foreach (string label in labels)
            RemoveLabel(label);
    }

    /// <summary>
    /// Adds a single prefab asset to a specified asset group, if not already present by reference or name.
    /// </summary>
    /// <param name="groupName">Name of the target asset group.</param>
    /// <param name="asset">The asset to add (must be a valid prefab).</param>
    public static void AddAssetToGroup(string groupName, UnityEngine.Object asset)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (asset == null)
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetToGroup)}]: Provided asset is null");

        var group = profile.groups.FirstOrDefault(group => group.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetToGroup)}]: Group '{groupName}' not found");
            return;
        }

        if (group.assets.Any(a => a.asset == asset
            || (a.asset != null && a.asset.name == asset.name)))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetToGroup)}]: Asset '{asset.name}' already exists in group '{groupName}' by reference or name");
            return;
        }

        group.assets.Add(new LoadableAsset { asset = asset });
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetToGroup)}]: Added asset '{asset.name}' to group '{groupName}'");
    }

    /// <summary>
    /// Adds all prefab assets from a folder to a specified asset group. Supports optional recursive search.
    /// </summary>
    /// <param name="groupName">Name of the target asset group.</param>
    /// <param name="folderPath">Relative path (within Assets/) to the folder.</param>
    /// <param name="recursive">Whether to search subdirectories recursively. Defaults to true.</param>
    public static void AddAssetsFromFolderToGroup(string groupName, string folderPath, bool recursive = true)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetsFromFolderToGroup)}]: Folder path is empty or null");
            return;
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetsFromFolderToGroup)}]: Folder '{folderPath}' is not valid");
            return;
        }

        string[] guids;
        if (recursive)
        {
            var allSubfolders = new List<string> { folderPath };
            allSubfolders.AddRange(AssetDatabase.GetSubFolders(folderPath));
            guids = AssetDatabase.FindAssets("t:Prefab", allSubfolders.ToArray());
        }
        else
            //search only this folder
            guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        if (guids.Length == 0)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetsFromFolderToGroup)}]: No prefabs found in folder '{folderPath}'");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
                AddAssetToGroup(groupName, asset);
        }
    }

    /// <summary>
    /// Removes an asset by name from a specific asset group.
    /// </summary>
    /// <param name="groupName">The name of the group to remove from.</param>
    /// <param name="assetName">The name of the asset to remove.</param>
    public static void RemoveAssetFromGroup(string groupName, string assetName)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        var group = profile.groups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromGroup)}]: Group '{groupName}' not found");
            return;
        }

        int removedCount = group.assets.RemoveAll(a => a.asset != null && a.asset.name == assetName);
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromGroup)}]: Removed asset '{assetName}' from group '{groupName}'");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromGroup)}]: Asset '{assetName}' not found in group '{groupName}'");
    }

    /// <summary>
    /// Removes an asset by name from all groups in the catalog.
    /// </summary>
    /// <param name="assetName">The name of the asset to remove.</param>
    public static void RemoveAssetFromAllGroups(string assetName)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        int totalRemoved = 0;
        foreach (var group in profile.groups)
            totalRemoved += group.assets.RemoveAll(a => a.asset != null && a.asset.name == assetName);
        if (totalRemoved > 0)
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromAllGroups)}]: Removed asset '{assetName}' from {totalRemoved} group(s)");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromAllGroups)}]: Asset '{assetName}' not found in any asset group");
    }

    /// <summary>
    /// Removes all prefabs from the given folder (by name) from a specific group.
    /// </summary>
    /// <param name="groupName">Target group name.</param>
    /// <param name="folderPath">Path to the folder (starting with 'Assets/').</param>
    /// <param name="recursive">Whether to search subfolders (default true).</param>
    public static void RemoveAssetsFromFolderInGroup(string groupName, string folderPath, bool recursive = true)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        var group = profile.groups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetsFromFolderInGroup)}]: Group '{groupName}' not found");
            return;
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetsFromFolderInGroup)}]: Folder path '{folderPath}' is invalid");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        HashSet<string> prefabNames = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !string.IsNullOrEmpty(path) && path.StartsWith(folderPath))
            .Where(path => recursive || Path.GetDirectoryName(path).Replace("\\", "/") == folderPath)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet();

        foreach (string name in prefabNames)
            RemoveAssetFromGroup(groupName, name);
    }

    /// <summary>
    /// Removes all prefabs from the given folder (by name) from all groups.
    /// </summary>
    /// <param name="folderPath">Path to the folder (starting with 'Assets/').</param>
    /// <param name="recursive">Whether to search subfolders (default true).</param>
    public static void RemoveAssetsFromFolderInAllGroups(string folderPath, bool recursive = true)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetsFromFolderInAllGroups)}]: Folder path '{folderPath}' is invalid");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        HashSet<string> prefabNames = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !string.IsNullOrEmpty(path) && path.StartsWith(folderPath))
            .Where(path => recursive || Path.GetDirectoryName(path).Replace("\\", "/") == folderPath)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet();

        foreach (string name in prefabNames)
            RemoveAssetFromAllGroups(name);
    }

    /// <summary>
    /// Adds a single label to a specific asset in a specific group.
    /// </summary>
    /// <param name="groupName">The group containing the asset.</param>
    /// <param name="assetName">The name of the asset.</param>
    /// <param name="label">The label to assign.</param>
    public static void AddLabelToAssetInGroup(string groupName, string assetName, string label) =>
        AddLabelsToAssetInGroup(groupName, assetName, new List<string> { label });

    /// <summary>
    /// Adds multiple labels to a specific asset in a specific group. New labels are added to the global label list if needed.
    /// </summary>
    /// <param name="groupName">The name of the group containing the asset.</param>
    /// <param name="assetName">The name of the asset.</param>
    /// <param name="labels">The list of labels to assign.</param>
    public static void AddLabelsToAssetInGroup(string groupName, string assetName, List<string> labels)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        var group = profile.groups.FirstOrDefault(g => g.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabelsToAssetInGroup)}]: Group '{groupName}' not found");
            return;
        }

        var asset = group.assets.FirstOrDefault(a => a.asset != null && a.asset.name == assetName);
        if (asset == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabelsToAssetInGroup)}]: Asset '{assetName}' not found in group '{groupName}'");
            return;
        }

        AddLabels(labels); //adds labels to global list of labels if not on it

        foreach (var label in labels)
            if (!asset.labels.Contains(label))
                asset.labels.Add(label);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabelsToAssetInGroup)}]: Added label(s) to asset '{assetName}' in group '{groupName}'");
    }

    /// <summary>
    /// Builds all asset groups that are marked as buildable and generates their catalogs and bundles.
    /// </summary>
    public static void BuildSelectedAssetGroups(bool verboseLogging = false) => BuildSelectedAssetGroups(EditorUserBuildSettings.activeBuildTarget, verboseLogging);

    public static void BuildSelectedAssetGroups(BuildTarget buildTarget, bool verboseLogging = false)
    {
        var profile = LoadCatalogProfile();
        if (profile == null)
            return;

        if (profile.groups.Count == 0)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(BuildSelectedAssetGroups)}]: No asset groups marked for build");
            return;
        }

        foreach (var group in profile.groups)
        {
            if (!group.includeInBuild)
                continue;

            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(BuildSelectedAssetGroups)}] Building group '{group.groupName}' for {buildTarget}...");

            BuildAssetBundlesAndCatalog(group, buildTarget, verboseLogging);
        }
    }

    /// <summary>
    /// Builds asset bundles and generates the catalog for a specific group.
    /// </summary>
    /// <param name="group">The asset catalog group to build.</param>
    /// <param name="buildEntry">The build summary entry to populate.</param>
    private static void BuildAssetBundlesAndCatalog(AssetCatalogGroup group, BuildTarget buildTarget, bool verboseLogging = false)
    {
        string postfix = GetBuildPostfixPath(buildTarget);
        string catalogFolder = Path.Combine(group.localPrefixPath, postfix).Replace("\\", "/");;
        if (!Directory.Exists(catalogFolder))
            Directory.CreateDirectory(catalogFolder);

        var catalog = new AssetCatalogData
        {
            rideBundleVersion = AssetCatalogData.RIDE_VERSION,
            unityVersion = Application.unityVersion,
            platform = GetPlatformId(buildTarget),
            renderPipeline = AssetCatalogUtility.GetRenderPipelineName(),
            renderPipelineVersion = GetRenderPipelineVersion(),
            localPrefixPath = catalogFolder,
            remotePrefixPath = Path.Combine(group.remotePrefixPath, postfix).Replace("\\", "/")
        };

        List<AssetBundleBuild> builds = new();
        foreach (var entry in group.assets)
        {
            string assetPath = AssetDatabase.GetAssetPath(entry.asset);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            string assetName = entry.asset.name;
            string bundleName = assetName.ToLowerInvariant() + "_bundle";
            string bundlePath = Path.Combine(catalogFolder, bundleName);

            catalog.entries.Add(new AssetCatalogEntry
            {
                assetName = assetName,
                bundleFileName = bundleName,
                labels = entry.labels.ToList() // make a copy of the labels
            });

            string status = File.Exists(bundlePath) ? "REBUILT" : "BUILT";

            LogToEditorAndCLI($"[{status}] {bundleName}");

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = new[] { assetPath }
            });
        }

        if (builds.Count > 0)
        {
            var manifest = BuildPipeline.BuildAssetBundles(catalogFolder, builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, buildTarget);

            foreach (var assetBundle in manifest.GetAllAssetBundles())
            {
                var hash = manifest.GetAssetBundleHash(assetBundle);
                foreach (var entry in catalog.entries)
                {
                    if (entry.bundleFileName == assetBundle)
                    {
                        entry.bundleHash128 = hash.ToString();
                        break;
                    }
                }
            }
        }

        string catalogJson = JsonUtility.ToJson(catalog, true);
        File.WriteAllText(Path.Combine(catalogFolder, "catalog.json"), catalogJson);

        // Cleanup Unity's platform stub manifest
        string platformManifest = Path.Combine(catalogFolder, buildTarget.ToString());
        if (File.Exists(platformManifest))
            File.Delete(platformManifest);
        if (File.Exists(platformManifest + ".manifest"))
            File.Delete(platformManifest + ".manifest");


        // Logging
        if (verboseLogging)
        {
            foreach (var entry in catalog.entries)
            {
                string bundleFilePath = Path.Combine(catalogFolder, entry.bundleFileName);
                long sizeBytes = File.Exists(bundleFilePath) ? new FileInfo(bundleFilePath).Length : 0;
                float sizeMB = sizeBytes / (1024f * 1024f);
                string labels = (entry.labels != null && entry.labels.Count > 0) ? string.Join(", ", entry.labels) : "None";
                string hash = string.IsNullOrEmpty(entry.bundleHash128) ? "none" : entry.bundleHash128;

                LogToEditorAndCLI(
                    $"[BUNDLE] {entry.bundleFileName} | Asset: '{entry.assetName}' | " +
                    $"Size: {sizeMB:F2} MB | Labels: [{labels}] | Hash: {hash}");
            }
        }

        int bundleCount = 0;
        long totalBytes = 0;

        foreach (var entry in catalog.entries)
        {
            string bundleFilePath = Path.Combine(catalogFolder, entry.bundleFileName);
            if (!File.Exists(bundleFilePath))
                continue;

            var info = new FileInfo(bundleFilePath);
            bundleCount++;
            totalBytes += info.Length;
        }

        float totalMB = totalBytes / (1024f * 1024f);

        LogToEditorAndCLI(
            $"[AssetCatalogUtils.Build] Finished Group='{group.groupName}' Target='{buildTarget}' " +
            $"Bundles={bundleCount} TotalMB={totalMB:F2} Local='{Path.Combine(catalogFolder, "catalog.json")}' " +
            $"Remote='{catalog.remotePrefixPath}'");
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// CLI entrypoint: adds a new asset group.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.AddAssetGroup_CLI -groupName MyGroup [-localPrefixPath path] [-remotePrefixPath path] [-buildable true|false]
    /// </summary>
    public static void AddAssetGroup_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        if (string.IsNullOrWhiteSpace(groupName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetGroup_CLI)}]: Missing -groupName");
            return;
        }

        // Create the group (ensures uniqueness)
        AddAssetGroup(groupName);

        // Optionally override prefix paths and buildable flag
        string localPrefix = GetCommandLineArg("-localPrefixPath");
        string remotePrefix = GetCommandLineArg("-remotePrefixPath");
        bool hasBuildableFlag = GetCommandLineArg("-buildable") != null;
        bool isBuildable = GetCommandLineBool("-buildable", true);

        var profile = LoadCatalogProfile();
        if (profile == null || profile.groups == null)
            return;

        // We do this in case AddAssetGroup made the name unique by appending suffixes.
        AssetCatalogGroup group = profile.groups.FirstOrDefault(g => g.groupName == groupName || g.groupName.StartsWith(groupName + "_", StringComparison.Ordinal));

        if (group == null)
            return;

        if (!string.IsNullOrEmpty(localPrefix))
            group.localPrefixPath = localPrefix;

        if (!string.IsNullOrEmpty(remotePrefix))
            group.remotePrefixPath = remotePrefix;

        if (hasBuildableFlag)
            group.includeInBuild = isBuildable;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        LogToEditorAndCLI(
            $"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetGroup_CLI)}]: Added/updated group '{group.groupName}'");
    }

    /// <summary>
    /// CLI entrypoint: removes an asset group by name.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveAssetGroup_CLI -groupName MyGroup
    /// </summary>
    public static void RemoveAssetGroup_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        if (string.IsNullOrWhiteSpace(groupName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetGroup_CLI)}]: Missing -groupName");
            return;
        }

        RemoveAssetGroup(groupName);
    }

    /// <summary>
    /// CLI entrypoint: removes an asset from a group by name.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveAssetFromGroup_CLI -groupName MyGroup -assetName MyPrefab
    /// </summary>
    public static void RemoveAssetFromGroup_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        string assetName = GetCommandLineArg("-assetName");

        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(assetName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromGroup_CLI)}]: Missing -groupName or -assetName");
            return;
        }

        RemoveAssetFromGroup(groupName, assetName);
    }

    /// <summary>
    /// CLI entrypoint: adds all prefabs from a folder to a specific group.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.AddAssetsFromFolderToGroup_CLI -groupName MyGroup -folder Assets/Path/To/Folder [-recursive true|false]
    /// </summary>
    public static void AddAssetsFromFolderToGroup_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        string folder = GetCommandLineArg("-folder");
        bool recursive = GetCommandLineBool("-recursive", true);

        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(folder))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddAssetsFromFolderToGroup_CLI)}]: Missing -groupName or -folder");
            return;
        }

        AddAssetsFromFolderToGroup(groupName, folder, recursive);
    }

    /// <summary>
    /// CLI entrypoint: removes all prefabs (by name) from a folder from a specific group.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveAssetsFromFolderInGroup_CLI -groupName MyGroup -folder Assets/Path/To/Folder [-recursive true|false]
    /// </summary>
    public static void RemoveAssetsFromFolderInGroup_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        string folder = GetCommandLineArg("-folder");
        bool recursive = GetCommandLineBool("-recursive", true);

        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(folder))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetsFromFolderInGroup_CLI)}]: Missing -groupName or -folder");
            return;
        }

        RemoveAssetsFromFolderInGroup(groupName, folder, recursive);
    }

    /// <summary>
    /// CLI entrypoint: removes an asset (by name) from all groups.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveAssetFromAllGroups_CLI -assetName MyPrefab
    /// </summary>
    public static void RemoveAssetFromAllGroups_CLI()
    {
        string assetName = GetCommandLineArg("-assetName");
        if (string.IsNullOrWhiteSpace(assetName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetFromAllGroups_CLI)}]: Missing -assetName");
            return;
        }

        RemoveAssetFromAllGroups(assetName);
    }

    /// <summary>
    /// CLI entrypoint: removes all prefabs (by name) from a folder from all groups.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveAssetsFromFolderInAllGroups_CLI -folder Assets/Path/To/Folder [-recursive true|false]
    /// </summary>
    public static void RemoveAssetsFromFolderInAllGroups_CLI()
    {
        string folder = GetCommandLineArg("-folder");
        bool recursive = GetCommandLineBool("-recursive", true);

        if (string.IsNullOrWhiteSpace(folder))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveAssetsFromFolderInAllGroups_CLI)}]: Missing -folder");
            return;
        }

        RemoveAssetsFromFolderInAllGroups(folder, recursive);
    }

    /// <summary>
    /// CLI entrypoint: adds a single global label.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.AddLabel_CLI -label MyLabel
    /// </summary>
    public static void AddLabel_CLI()
    {
        string label = GetCommandLineArg("-label");
        if (string.IsNullOrWhiteSpace(label))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabel_CLI)}]: Missing -label");
            return;
        }

        AddLabel(label);
    }

    /// <summary>
    /// CLI entrypoint: adds multiple global labels (comma-separated).
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.AddLabels_CLI -labels Label1,Label2,Label3
    /// </summary>
    public static void AddLabels_CLI()
    {
        string labels = GetCommandLineArg("-labels");
        if (string.IsNullOrWhiteSpace(labels))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(AddLabels_CLI)}]: Missing -labels");
            return;
        }

        AddLabels(CLIListArgParser(labels));
    }

    /// <summary>
    /// CLI entrypoint: removes a single global label.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveLabel_CLI -label MyLabel
    /// </summary>
    public static void RemoveLabel_CLI()
    {
        string label = GetCommandLineArg("-label");
        if (string.IsNullOrWhiteSpace(label))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveLabel_CLI)}]: Missing -label");
            return;
        }

        RemoveLabel(label);
    }

    /// <summary>
    /// CLI entrypoint: removes multiple global labels (comma-separated).
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.RemoveLabels_CLI -labels Label1,Label2
    /// </summary>
    public static void RemoveLabels_CLI()
    {
        string labels = GetCommandLineArg("-labels");
        if (string.IsNullOrWhiteSpace(labels))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(RemoveLabels_CLI)}]: Missing -labels");
            return;
        }

        RemoveLabels(CLIListArgParser(labels));
    }

    /// <summary>
    /// CLI entrypoint: sets whether a group is included in builds.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.SetGroupBuildable_CLI -groupName MyGroup -buildable true|false
    /// </summary>
    public static void SetGroupBuildable_CLI()
    {
        string groupName = GetCommandLineArg("-groupName");
        if (string.IsNullOrWhiteSpace(groupName))
        {
            LogToEditorAndCLI(
                $"[{nameof(AssetCatalogEditorUtility)}.{nameof(SetGroupBuildable_CLI)}]: Missing -groupName");
            return;
        }

        string buildableValue = GetCommandLineArg("-buildable");
        if (string.IsNullOrWhiteSpace(buildableValue))
        {
            LogToEditorAndCLI(
                $"[{nameof(AssetCatalogEditorUtility)}.{nameof(SetGroupBuildable_CLI)}]: Missing -buildable");
            return;
        }

        bool isBuildable = GetCommandLineBool("-buildable", true);
        SetGroupBuildable(groupName, isBuildable);
    }

    /// <summary>
    /// CLI entrypoint: builds all groups marked as buildable.
    /// Usage:
    ///   -executeMethod AssetCatalogUtils.BuildSelectedAssetGroups_CLI
    ///   -executeMethod AssetCatalogUtils.BuildSelectedAssetGroups_CLI -buildTarget StandaloneWindows64 [-verbose 1]
    /// </summary>
    public static void BuildSelectedAssetGroups_CLI()
    {
        string buildTargetName = GetCommandLineArg("-buildTarget");
        bool verboseLogging = GetCommandLineBool("-verbose", false);
        if (!string.IsNullOrEmpty(buildTargetName))
        {
            if (Enum.TryParse(buildTargetName, out BuildTarget target))
            {
                BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
                if (!switched)
                {
                    LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(BuildSelectedAssetGroups_CLI)}]: Failed to switch to build target '{buildTargetName}'");
                    return;
                }

                LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(BuildSelectedAssetGroups_CLI)}]: Switched to build target '{buildTargetName}'");
            }
            else
            {
                LogToEditorAndCLI($"[{nameof(AssetCatalogEditorUtility)}.{nameof(BuildSelectedAssetGroups_CLI)}]: Invalid build target '{buildTargetName}'");
                return;
            }
        }

        BuildSelectedAssetGroups(verboseLogging);
    }

    /// <summary>
    /// Parses a comma-separated string of values into a list of trimmed, non-empty strings.
    /// Used for parsing multi-label values from the CLI (e.g., "-labels label1,label2").
    /// </summary>
    /// <param name="argValue">The comma-separated string to parse.</param>
    /// <returns>A list of trimmed label strings.</returns>
    private static List<string> CLIListArgParser(string argValue) => argValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

    /// <summary>
    /// Returns the value that follows a given flag in the Unity command line,
    /// or null if the flag is not present or has no value.
    /// Example: ... -groupName MyGroup ... -> GetCommandLineArg("-groupName") == "MyGroup"
    /// </summary>
    private static string GetCommandLineArg(string flagName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flagName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    /// <summary>
    /// Returns a boolean parsed from a command line flag value ("true", "1", "yes")
    /// or the provided default if the flag is not present.
    /// </summary>
    private static bool GetCommandLineBool(string flagName, bool defaultValue = false)
    {
        string value = GetCommandLineArg(flagName);
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetRenderPipelineVersion()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null)
            return string.Empty;

        var ns = rp.GetType().Namespace ?? string.Empty;
        string packageName = null;

        if (ns.Contains("HighDefinition"))
            packageName = "com.unity.render-pipelines.high-definition";
        else if (ns.Contains("Universal"))
            packageName = "com.unity.render-pipelines.universal";

        if (!string.IsNullOrEmpty(packageName))
        {
            // Try to get the package info by path; this avoids scanning all packages.
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + packageName);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.version))
                return packageInfo.version;
        }

        return string.Empty;
    }
}
}
