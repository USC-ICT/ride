using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Provides a suite of utility methods for managing asset catalog groups via script, CLI or interactively in the Unity Editor.
/// Supports batch operations including group creation, asset assignment, label management, and catalog/bundle building.
/// </summary>
public static class AssetCatalogUtils
{
    private const string CATALOG_PROFILE_PATH = "Assets/AssetCatalogData/AssetCatalogProfile.asset";
    private const string BUILD_SUMMARY_FILENAME = "BuildSummaryProfile.json";

    private static AssetCatalogProfile LoadCatalogProfile()
    {
        AssetCatalogProfile profile = null;
        profile = AssetDatabase.LoadAssetAtPath<AssetCatalogProfile>(CATALOG_PROFILE_PATH);
        if (profile == null)
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(LoadCatalogProfile)}]: Asset catalog profile not found at path {CATALOG_PROFILE_PATH}.");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetGroup)}]: Group name is empty or null");
            return;
        }

        string uniqueGroupName = assetGroupName;
        string groupAddedMessage = $"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetGroup)}]: Added new asset group '{uniqueGroupName}'";
        if (profile.groups.Any(group => group.groupName == uniqueGroupName))
        {
            int suffix = 1;
            while (profile.groups.Any(group => group.groupName == $"{assetGroupName}_{suffix}"))
                suffix++; //increment suffix until it assetGroupName_suffix results in a unqiue group name
            uniqueGroupName = $"{assetGroupName}_{suffix}";
            groupAddedMessage = $"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetGroup)}]: Group name '{assetGroupName}' already exists, adding group '{uniqueGroupName}' instead";
        }

        var newGroup = new AssetCatalogGroup
        {
            groupName = uniqueGroupName,
            includeInBuild = true
        };
        newGroup.localPrefixPath = GenerateDefaultLocalPath(newGroup);
        newGroup.remotePrefixPath = GenerateDefaultLocalPath(newGroup);
        profile.groups.Add(newGroup);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI(groupAddedMessage);
    }

    /// <summary>
    /// Generates the default local prefix path for a given asset group.
    /// </summary>
    /// <param name="group">The asset group to generate the path for.</param>
    /// <returns>The default local prefix path.</returns>
    public static string GenerateDefaultLocalPath(AssetCatalogGroup group)
    {
        return Path.Combine(Application.persistentDataPath, "AssetCatalogs", SanitizeGroupName(group.groupName)).Replace("\\", "/");
    }

    /// <summary>
    /// Generates the default local prefix path for a given asset group.
    /// </summary>
    /// <param name="group">The asset group to generate the path for.</param>
    /// <returns>The default local prefix path.</returns>
    public static string GenerateDefaultRemotePath(AssetCatalogGroup group)
    {
        return Path.Combine("ride", "AssetCatalogs", SanitizeGroupName(group.groupName)).Replace("\\", "/");
    }

    /// <summary>
    /// Generates the postfix path based on Unity Version, Render Pipeline and Build Target.
    /// </summary>
    public static string GetBuildPostfixPath()
    {
        return Path.Combine(
            GetSanitizedUnityVersion(),
            GetRenderPipelineName(),
            EditorUserBuildSettings.activeBuildTarget.ToString()
        ).Replace("\\", "/");
    }

    /// <summary>
    /// Sanitizes a group name by replacing spaces and handling empty/null values.
    /// </summary>
    /// <param name="name">The original group name.</param>
    /// <returns>A safe, non-empty, space-free string.</returns>
    private static string SanitizeGroupName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Default" : name.Replace(" ", "");
    }

    /// <summary>
    /// Returns a sanitized Unity version string in the format 'major.minor.x'.
    /// </summary>
    /// <returns>A sanitized Unity version string.</returns>
    private static string GetSanitizedUnityVersion()
    {
        string[] parts = Application.unityVersion.Split('.');
        if (parts.Length >= 2)
            return $"{parts[0]}.{parts[1]}.x";
        return Application.unityVersion; // fallback, just in case
    }

    /// <summary>
    /// Returns a short name for the currently active render pipeline (BuiltIn, URP, HDRP, etc.).
    /// </summary>
    /// <returns>The name of the active render pipeline.</returns>
    private static string GetRenderPipelineName()
    {
        if (GraphicsSettings.currentRenderPipeline == null) return "BuiltIn";
        string type = GraphicsSettings.currentRenderPipeline.GetType().Name;
        if (type.Contains("HD")) return "HDRP";
        if (type.Contains("Universal") || type.Contains("URP")) return "URP";
        return type;
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(SetGroupBuildable)}]: No group named '{groupName}' found");
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
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(groupName))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetGroup)}]: Provided group name is empty or null");
            return;
        }

        int removedCount = profile.groups.RemoveAll(group => group.groupName == groupName);
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetGroup)}]: Removed group '{groupName}'");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetGroup)}]: Group '{groupName}' not found");
    }

    /// <summary>
    /// Adds a single label to the asset catalog profile's global label list if it doesn't already exist.
    /// </summary>
    /// <param name="label">The label to add.</param>
    public static void AddLabel(string label)
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        if (string.IsNullOrWhiteSpace(label))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddLabel)}]: Provided label is empty or null");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveLabel)}]: Provided label is empty or null");
            return;
        }

        if (profile.allLabels.Remove(label))
        {
            foreach (var group in profile.groups)
                foreach (var asset in group.assets)
                    asset.labels.Remove(label);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveLabel)}]: Label '{label}' removed from all asset groups and their respective assets");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveLabel)}]: Label '{label}' not found in label list");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetToGroup)}]: Provided asset is null");

        var group = profile.groups.FirstOrDefault(group => group.groupName == groupName);
        if (group == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetToGroup)}]: Group '{groupName}' not found");
            return;
        }

        if (group.assets.Any(a => a.asset == asset
            || (a.asset != null && a.asset.name == asset.name)))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetToGroup)}]: Asset '{asset.name}' already exists in group '{groupName}' by reference or name");
            return;
        }

        group.assets.Add(new LoadableAsset { asset = asset });
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetToGroup)}]: Added asset '{asset.name}' to group '{groupName}'");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetsFromFolderToGroup)}]: Folder path is empty or null");
            return;
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetsFromFolderToGroup)}]: Folder '{folderPath}' is not valid");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddAssetsFromFolderToGroup)}]: No prefabs found in folder '{folderPath}'");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetFromGroup)}]: Group '{groupName}' not found");
            return;
        }

        int removedCount = group.assets.RemoveAll(a => a.asset != null && a.asset.name == assetName);
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetFromGroup)}]: Removed asset '{assetName}' from group '{groupName}'");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetFromGroup)}]: Asset '{assetName}' not found in group '{groupName}'");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetFromAllGroups)}]: Removed asset '{assetName}' from {totalRemoved} group(s)");
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetFromAllGroups)}]: Asset '{assetName}' not found in any asset group");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetsFromFolderInGroup)}]: Group '{groupName}' not found");
            return;
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetsFromFolderInGroup)}]: Folder path '{folderPath}' is invalid");
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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(RemoveAssetsFromFolderInAllGroups)}]: Folder path '{folderPath}' is invalid");
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
    public static void AddLabelToAssetInGroup(string groupName, string assetName, string label)
    {
        AddLabelsToAssetInGroup(groupName, assetName, new List<string> { label });
    }

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
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddLabelsToAssetInGroup)}]: Group '{groupName}' not found");
            return;
        }

        var asset = group.assets.FirstOrDefault(a => a.asset != null && a.asset.name == assetName);
        if (asset == null)
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddLabelsToAssetInGroup)}]: Asset '{assetName}' not found in group '{groupName}'");
            return;
        }

        AddLabels(labels); //adds labels to global list of labels if not on it

        foreach (var label in labels)
            if (!asset.labels.Contains(label))
                asset.labels.Add(label);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(AddLabelsToAssetInGroup)}]: Added label(s) to asset '{assetName}' in group '{groupName}'");
    }

    /// <summary>
    /// Builds all asset groups that are marked as buildable and generates their catalogs and bundles.
    /// </summary>
    public static void BuildSelectedAssetGroups()
    {
        var profile = LoadCatalogProfile();
        if (profile == null) return;

        string buildSummaryPath = Path.Combine(Application.persistentDataPath, BUILD_SUMMARY_FILENAME);
        BuildSummaryProfile buildSummaryProfile;

        if (File.Exists(buildSummaryPath))
        {
            buildSummaryProfile = ScriptableObject.CreateInstance<BuildSummaryProfile>();
            string json = File.ReadAllText(buildSummaryPath);
            JsonUtility.FromJsonOverwrite(json, buildSummaryProfile);
        }
        else
            buildSummaryProfile = ScriptableObject.CreateInstance<BuildSummaryProfile>();

        bool anyBuilt = false;
        foreach (var group in profile.groups)
        {
            if (!group.includeInBuild)
                continue;

            var buildEntry = new BuildSummaryEntry
            {
                timestamp = DateTime.Now.ToString("g"),
                catalogJsonSnapshot = ""
            };

            buildSummaryProfile.builds.Add(buildEntry);
            BuildAssetBundlesAndCatalog(group, buildEntry);
            anyBuilt = true;
        }

        if (anyBuilt)
        {
            string json = JsonUtility.ToJson(buildSummaryProfile, true);
            File.WriteAllText(buildSummaryPath, json);
        }
        else
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(BuildSelectedAssetGroups)}]: No asset groups marked for build");

    }

    /// <summary>
    /// Builds asset bundles and generates the catalog for a specific group.
    /// </summary>
    /// <param name="group">The asset catalog group to build.</param>
    /// <param name="buildEntry">The build summary entry to populate.</param>
    private static void BuildAssetBundlesAndCatalog(AssetCatalogGroup group, BuildSummaryEntry buildEntry)
    {
        string catalogFolder = Path.Combine(group.localPrefixPath, GetBuildPostfixPath()).Replace("\\", "/");;
        if (!Directory.Exists(catalogFolder))
            Directory.CreateDirectory(catalogFolder);

        AssetCatalogData catalog = new AssetCatalogData
        {
            localPrefixPath = catalogFolder,
            remotePrefixPath = Path.Combine(group.remotePrefixPath, GetBuildPostfixPath()).Replace("\\", "/")
        };

        Dictionary<string, string> previousAssetHashes = new();
        string individualHashesPath = Path.Combine(catalogFolder, "bundle.hashes");
        if (File.Exists(individualHashesPath))
        {
            foreach (var line in File.ReadAllLines(individualHashesPath))
            {
                var parts = line.Split('|');
                if (parts.Length == 2)
                    previousAssetHashes[parts[0]] = parts[1];
            }
        }

        Dictionary<string, string> currentAssetHashes = new();
        List<AssetBundleBuild> builds = new();
        foreach (var entry in group.assets)
        {
            string assetPath = AssetDatabase.GetAssetPath(entry.asset);
            if (string.IsNullOrEmpty(assetPath)) continue;

            string assetName = entry.asset.name;
            string bundleName = assetName.ToLowerInvariant() + "_bundle";
            string bundlePath = Path.Combine(catalogFolder, bundleName);
            string hash = AssetDatabase.GetAssetDependencyHash(assetPath).ToString();
            currentAssetHashes[bundleName] = hash;

            catalog.entries.Add(new AssetCatalogEntry
            {
                assetName = assetName,
                bundleFileName = bundleName,
                labels = new List<string>(entry.labels)
            });

            bool needsBuild = !File.Exists(bundlePath) || !previousAssetHashes.TryGetValue(bundleName, out var prevHash) || prevHash != hash;


            // EDF - set needsBuild to always true.  Otherwise, the BuildPipeline will not run, and we won't have the bundleHash128 info to save to the catalog (see below)
            //       Unity will do its own caching, so calling BuildAssetBundles() is relatively efficient when called multiple times.
            needsBuild = true;


            string status = needsBuild ? (File.Exists(bundlePath) ? "REBUILT" : "BUILT") : "SKIPPED";
            long size = 0;
            string time = File.Exists(bundlePath) ? File.GetLastWriteTime(bundlePath).ToString("g") : DateTime.Now.ToString("g");

            buildEntry.summaryRows.Add(new BuildSummaryRow
            {
                assetName = assetName,
                bundleName = bundleName,
                status = status,
                lastModified = time,
                sizeBytes = size
            });

            LogToEditorAndCLI($"[{status}] {bundleName}");

            if (needsBuild)
            {
                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleName,
                    assetNames = new[] { assetPath }
                });
            }
        }

        if (builds.Count > 0)
        {
            var manifest = BuildPipeline.BuildAssetBundles(catalogFolder, builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

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

        foreach (var row in buildEntry.summaryRows)
        {
            string fullPath = Path.Combine(catalogFolder, row.bundleName);
            if (File.Exists(fullPath))
            {
                FileInfo info = new(fullPath);
                row.sizeBytes = info.Length;
                row.lastModified = info.LastWriteTime.ToString("g");
            }
        }

        using (StreamWriter writer = new(individualHashesPath))
            foreach (var kvp in currentAssetHashes)
                writer.WriteLine($"{kvp.Key}|{kvp.Value}");

        string catalogJson = JsonUtility.ToJson(catalog, true);
        File.WriteAllText(Path.Combine(catalogFolder, "catalog.json"), catalogJson);
        buildEntry.catalogJsonSnapshot = catalogJson;

        string platformManifest = Path.Combine(catalogFolder, EditorUserBuildSettings.activeBuildTarget.ToString());
        if (File.Exists(platformManifest)) File.Delete(platformManifest);
        if (File.Exists(platformManifest + ".manifest")) File.Delete(platformManifest + ".manifest");
    }


    /// <summary>
    /// Parses command-line arguments and executes the appropriate asset catalog command.
    /// This method is intended to be invoked via Unity's `-executeMethod` CLI option.
    /// </summary>
    public static void CLIParser()
    {
        string[] args = Environment.GetCommandLineArgs();
        Dictionary<string, string> argMap = CLIArgsParser(args);

        if (!argMap.TryGetValue("-command", out string command))
        {
            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing required -command argument");
            return;
        }

        //cache args
        argMap.TryGetValue("-groupName", out string groupName);
        argMap.TryGetValue("-assetName", out string assetName);
        argMap.TryGetValue("-label", out string label);
        argMap.TryGetValue("-labels", out string labels);
        argMap.TryGetValue("-folder", out string folder);
        argMap.TryGetValue("-recursive", out string recursive);
        argMap.TryGetValue("-buildable", out string buildableFlag);
        argMap.TryGetValue("-buildTarget", out string buildTarget);
        argMap.TryGetValue("-localPrefixPath", out string localPrefix);
        argMap.TryGetValue("-remotePrefixPath", out string remotePrefix);

        LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Running command: {command}");

        switch (command)
        {
            case "AddAssetGroup":
                if (!string.IsNullOrEmpty(groupName))
                {
                    AddAssetGroup(groupName);
                    var profile = LoadCatalogProfile();
                    AssetCatalogGroup group = null;
                    if (profile != null && profile.groups != null)
                        group = profile.groups.FirstOrDefault(g => g.groupName == groupName || g.groupName.StartsWith(groupName + "_"));

                    if (group != null)
                    {
                        if (!string.IsNullOrEmpty(localPrefix))
                            group.localPrefixPath = localPrefix;
                        if (!string.IsNullOrEmpty(remotePrefix))
                            group.remotePrefixPath = remotePrefix;
                        EditorUtility.SetDirty(profile);
                        AssetDatabase.SaveAssetIfDirty(profile);
                    }
                }
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName for AddAssetGroup");
                break;

            case "RemoveAssetGroup":
                if (!string.IsNullOrEmpty(groupName)) RemoveAssetGroup(groupName);
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName for RemoveAssetGroup");
                break;

            case "AddAssetsFromFolderToGroup":
                if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(folder))
                    AddAssetsFromFolderToGroup(groupName, folder, recursive == "true");
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName or -folder for AddAssetsFromFolderToGroup");
                break;

            case "RemoveAssetsFromFolderInGroup":
                if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(folder))
                    RemoveAssetsFromFolderInGroup(groupName, folder, recursive == "true");
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName or -folder for RemoveAssetsFromFolderInGroup");
                break;

            case "RemoveAssetFromAllGroups":
                if (!string.IsNullOrEmpty(assetName)) RemoveAssetFromAllGroups(assetName);
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -assetName for RemoveAssetFromAllGroups");
                break;

            case "RemoveAssetsFromFolderInAllGroups":
                if (!string.IsNullOrEmpty(folder)) RemoveAssetsFromFolderInAllGroups(folder, recursive == "true");
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -folder for RemoveAssetsFromFolderInAllGroups");
                break;

            case "AddLabel":
                if (!string.IsNullOrEmpty(label)) AddLabel(label);
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -label for AddLabel");
                break;

            case "AddLabels":
                if (!string.IsNullOrEmpty(labels)) AddLabels(CLIListArgParser(labels));
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -labels for AddLabels");
                break;

            case "RemoveLabel":
                if (!string.IsNullOrEmpty(label)) RemoveLabel(label);
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -label for RemoveLabel");
                break;

            case "RemoveLabels":
                if (!string.IsNullOrEmpty(labels)) RemoveLabels(CLIListArgParser(labels));
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -labels for RemoveLabels");
                break;

            case "AddLabelToAssetInGroup":
                if (!string.IsNullOrEmpty(groupName) &&
                    !string.IsNullOrEmpty(assetName) &&
                    !string.IsNullOrEmpty(label))
                    AddLabelToAssetInGroup(groupName, assetName, label);
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName, -assetName, or -label for AddLabelToAsset");
                break;

            case "AddLabelsToAssetInGroup":
                if (!string.IsNullOrEmpty(groupName) &&
                    !string.IsNullOrEmpty(assetName) &&
                    !string.IsNullOrEmpty(labels))
                    AddLabelsToAssetInGroup(groupName, assetName, CLIListArgParser(labels));
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName, -assetName, or -labels for AddLabelsToAsset");
                break;

            case "SetGroupBuildable":
                if (!string.IsNullOrEmpty(groupName) &&
                    !string.IsNullOrEmpty(buildableFlag))
                    SetGroupBuildable(groupName, buildableFlag.ToLower() == "true");
                else LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Missing -groupName or -buildable for SetGroupBuildable");
                break;

            case "BuildSelectedAssetGroups":
                if (!string.IsNullOrEmpty(buildTarget))
                {
                    if (Enum.TryParse(buildTarget, out BuildTarget target))
                    {
                        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
                        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
                        if (!switched)
                        {
                            LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Failed to switch to build target '{buildTarget}'");
                            return;
                        }
                        LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Switched to build target '{buildTarget}'");
                    }
                    else
                    {
                        LogToEditorAndCLI($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Invalid build target '{buildTarget}'");
                        return;
                    }
                }
                BuildSelectedAssetGroups();
                break;

            default:
                Debug.LogError($"[{nameof(AssetCatalogUtils)}.{nameof(CLIParser)}]: Unknown command '{command}'");
                break;
        }
    }

    /// <summary>
    /// Parses the command-line arguments into a dictionary mapping flags (e.g., "-command") to values.
    /// Skips any flag that is not followed by a non-flag value.
    /// </summary>
    /// <param name="args">The array of command-line arguments from <see cref="Environment.GetCommandLineArgs"/>.</param>
    /// <returns>A dictionary mapping argument keys (with dashes) to their corresponding values.</returns>
    private static Dictionary<string, string> CLIArgsParser(string[] args)
    {
        var result = new Dictionary<string, string>();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].StartsWith("-") && !args[i + 1].StartsWith("-"))
                result[args[i]] = args[i + 1];
        return result;
    }

    /// <summary>
    /// Parses a comma-separated string of values into a list of trimmed, non-empty strings.
    /// Used for parsing multi-label values from the CLI (e.g., "-labels label1,label2").
    /// </summary>
    /// <param name="argValue">The comma-separated string to parse.</param>
    /// <returns>A list of trimmed label strings.</returns>
    private static List<string> CLIListArgParser(string argValue)
    {
        return argValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }
}
