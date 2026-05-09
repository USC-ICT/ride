using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// Editor utility for one-time setup of Azure Speech Recognition support in WebGL projects.
    ///
    /// This window copies the required JavaScript files for Azure ASR into the project's
    /// StreamingAssets folder so they can be loaded at runtime by the WebGL .jslib plugin.
    /// It is intended to be run once per project (or again after updating the package).
    ///
    /// Files copied:
    /// - speech-sdk.bundle.js
    /// - AzureAsrBridge.js
    /// - MicLevel.js
    ///
    /// Target location:
    ///   Assets/StreamingAssets/RideCognitionAzureAsr/
    ///
    /// Usage:
    /// - Open via: RIDE > Cognition > ASR > WebGL Setup (StreamingAssets)
    /// - Click "Copy JS files to StreamingAssets"
    /// - Verify files are present, then build a WebGL player
    ///
    /// Design notes:
    /// - JavaScript files live inside the RIDE package and are not automatically included
    ///   in WebGL builds.
    /// - StreamingAssets is the only Unity-supported mechanism for reliably serving
    ///   arbitrary runtime files in WebGL without modifying WebGLTemplates.
    /// - This tool avoids custom build hooks and keeps project setup explicit and visible.
    ///
    /// Safety notes:
    /// - This tool copies only runtime JavaScript files, not .meta files.
    /// - The copied files should generally NOT be checked into version control.
    /// - Inspector Azure keys are for development only; do NOT ship real keys in WebGL.
    ///
    /// Related components:
    /// - AzureAsr.jslib (WebGL plugin loader)
    /// - AzureAsrBridge (Unity-side JS callback bridge)
    /// - SpeechRecognitionSystemAzureWebGL (RIDE ASR system)
    /// </summary>
    public class AzureAsrWebGLSetupWindow : EditorWindow
    {
        private const string TargetRelativeFolder = "Assets/StreamingAssets/RideCognitionAzureAsr";

        private static readonly string[] RequiredFiles =
        {
            // meta files first to prevent any import issues
            "speech-sdk.bundle.js.meta",
            "AzureAsrBridge.js.meta",
            "MicLevel.js.meta",
            "speech-sdk.bundle.js",
            "AzureAsrBridge.js",
            "MicLevel.js",
        };

        private Vector2 m_scroll;
        private string m_sourcePath;
        private string m_status = "";
        private bool m_overwriteExisting = true;


        [MenuItem("Ride/Cognition/ASR/WebGL Setup (StreamingAssets)...")]
        public static void Open()
        {
            var window = GetWindow<AzureAsrWebGLSetupWindow>(true, "RIDE Azure ASR WebGL Setup", true);
            window.minSize = new Vector2(640, 420);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize the source path once when the window opens.
            // We only auto-detect the first required file, and then treat its folder as the source root.
            TryAutoDetectSourcePath();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Azure ASR WebGL One-Time Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox($"This copies required JavaScript files into:\n{TargetRelativeFolder}\n\nSource folder is inferred from the Source File Path below.\nRun once per project (or again after updating the package).", MessageType.Info);
            EditorGUILayout.Space();

            DrawSourcePathUI();
            EditorGUILayout.Space();

            m_overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing files", m_overwriteExisting);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy JS files to StreamingAssets", GUILayout.Height(30)))
                    CopyFilesFromSourceFolder();

                if (GUILayout.Button("Open target folder", GUILayout.Height(30)))
                    OpenTargetFolder();
            }

            EditorGUILayout.Space();

            DrawCurrentStatus();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status / Log", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.Height(160));
                EditorGUILayout.TextArea(m_status, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSourcePathUI()
        {
            EditorGUILayout.LabelField("Source Folder", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_sourcePath = EditorGUILayout.TextField(m_sourcePath);

                if (GUILayout.Button("Auto-detect", GUILayout.Width(110)))
                    TryAutoDetectSourcePath(force: true);

                if (GUILayout.Button("Browse...", GUILayout.Width(90)))
                {
                    string initialDir = string.Empty;

                    if (!string.IsNullOrEmpty(m_sourcePath))
                    {
                        try
                        {
                            string dir = Path.GetDirectoryName(m_sourcePath);
                            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                                initialDir = dir;
                        }
                        catch
                        {
                            // Ignore.
                        }
                    }

                    string chosen = EditorUtility.OpenFolderPanel(
                        "Select source folder containing Azure ASR JS files",
                        initialDir,
                        "js");

                    if (!string.IsNullOrEmpty(chosen))
                        m_sourcePath = chosen;
                }
            }

            if (!string.IsNullOrEmpty(m_sourcePath))
                EditorGUILayout.LabelField("Source folder:", m_sourcePath);
            else
                EditorGUILayout.LabelField("Source folder:", "<invalid path>");
        }

        private void DrawCurrentStatus()
        {
            string targetFullPath = Path.GetFullPath(TargetRelativeFolder);

            EditorGUILayout.LabelField("Target:", TargetRelativeFolder);
            EditorGUILayout.LabelField("Target exists:", Directory.Exists(targetFullPath) ? "Yes" : "No");

            bool allPresent = RequiredFiles.All(f => File.Exists(Path.Combine(targetFullPath, f)));
            EditorGUILayout.LabelField("All files present:", allPresent ? "Yes" : "No");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Required files:", EditorStyles.boldLabel);

            foreach (string file in RequiredFiles)
            {
                string fullPath = Path.Combine(targetFullPath, file);
                string present = File.Exists(fullPath) ? "OK" : "Missing";
                EditorGUILayout.LabelField($"- {file}: {present}");
            }
        }

        private void CopyFilesFromSourceFolder()
        {
            try
            {
                AppendLine("=== Azure ASR WebGL Setup ===");

                if (string.IsNullOrEmpty(m_sourcePath) || !Directory.Exists(m_sourcePath))
                {
                    AppendLine("ERROR: Invalid source folder. Provide a valid Source File Path.");
                    EditorUtility.DisplayDialog("RIDE Azure ASR WebGL Setup", "Invalid source folder.\n\nSet Source File Path to a real file and retry.", "OK");
                    return;
                }

                // Basic sanity check: the first required file should exist in that folder.
                string firstRequired = Path.Combine(m_sourcePath, RequiredFiles[0]);
                if (!File.Exists(firstRequired))
                {
                    AppendLine($"ERROR: Source folder does not contain expected file: {RequiredFiles[0]}");
                    AppendLine($"Source folder: {m_sourcePath}");
                    EditorUtility.DisplayDialog(
                        "RIDE Azure ASR WebGL Setup",
                        $"Source folder does not contain:\n{RequiredFiles[0]}\n\nSource folder:\n{m_sourcePath}",
                        "OK");
                    return;
                }

                EnsureTargetFolderExists();
                string targetFullPath = Path.GetFullPath(TargetRelativeFolder);

                for (int i = 0; i < RequiredFiles.Length; i++)
                {
                    string fileName = RequiredFiles[i];

                    string src = Path.Combine(m_sourcePath, fileName);
                    string dst = Path.Combine(targetFullPath, fileName);

                    if (!File.Exists(src))
                    {
                        AppendLine($"ERROR: Missing source file: {src}");
                        EditorUtility.DisplayDialog("RIDE Azure ASR WebGL Setup", $"Missing source file:\n{src}", "OK");
                        return;
                    }

                    if (File.Exists(dst) && !m_overwriteExisting)
                    {
                        AppendLine($"Skip (exists): {fileName}");
                        continue;
                    }

                    File.Copy(src, dst, true);
                    AppendLine($"Copied: {fileName}");
                }

                AssetDatabase.Refresh();
                AppendLine("Done. AssetDatabase.Refresh() complete.");

                EditorUtility.DisplayDialog(
                    "RIDE Azure ASR WebGL Setup",
                    "Copied required JS files to StreamingAssets.\n\nNext: build WebGL and verify scripts load from StreamingAssets.",
                    "OK");
            }
            catch (Exception ex)
            {
                AppendLine($"ERROR: {ex}");
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("RIDE Azure ASR WebGL Setup - Error", ex.ToString(), "OK");
            }
        }

        private void TryAutoDetectSourcePath(bool force = false)
        {
            if (!force && !string.IsNullOrEmpty(m_sourcePath))
                return;

            try
            {
                m_sourcePath = string.Empty;

                string found = TryFindSourceFolderForPackage("edu.usc.ict.ride.cognition");

                if (string.IsNullOrEmpty(found))
                {
                    AppendLine("Auto-detect failed: could not locate source folder containing required files.");
                    AppendLine("Tried PackageManager first, then scanned common locations (Packages/ and Library/PackageCache/).");
                    return;
                }

                m_sourcePath = found;
                AppendLine("Auto-detect source folder:");
                AppendLine("  " + m_sourcePath);
            }
            catch (Exception ex)
            {
                AppendLine("Auto-detect exception: " + ex);
            }
        }

        private string TryFindSourceFolderForPackage(string packageName)
        {
            // 1) Best: ask PackageManager for the package path (handles transitive deps + PackageCache).
            string pmRoot = TryGetPackageRootFromPackageManager(packageName);
            if (!string.IsNullOrEmpty(pmRoot))
            {
                string folder = FindFolderContainingAllRequiredFiles(pmRoot);
                if (!string.IsNullOrEmpty(folder))
                    return folder;
            }

            // 2) Fallback: scan common UPM locations.
            foreach (string root in GetCommonPackageSearchRoots())
            {
                string folder = FindFolderContainingAllRequiredFiles(root);
                if (!string.IsNullOrEmpty(folder))
                    return folder;
            }

            return null;
        }

        private static string TryGetPackageRootFromPackageManager(string packageName)
        {
            try
            {
                // Unity provides this in-editor; works even if the dependency is transitive.
                var packages = PackageManagerPackageInfo.GetAllRegisteredPackages();
                if (packages == null)
                    return null;

                foreach (var p in packages)
                    if (p != null && string.Equals(p.name, packageName, StringComparison.OrdinalIgnoreCase))
                        return p.resolvedPath;
            }
            catch
            {
                // Older Unity versions or unusual editor contexts: ignore and fallback to scanning.
            }

            return null;
        }

        private static string[] GetCommonPackageSearchRoots()
        {
            // Keep this tight. Scanning the whole project can be slow.
            // These two cover most real-world cases:
            // - Embedded/local packages: Packages/
            // - Cached UPM packages (including transitive deps): Library/PackageCache/
            try
            {
                string packages = Path.GetFullPath("Packages");
                string cache = Path.GetFullPath(Path.Combine("Library", "PackageCache"));

                return new[]
                {
                    packages,
                    cache
                };
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private string FindFolderContainingAllRequiredFiles(string searchRoot)
        {
            if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
                return null;

            try
            {
                // Fast path: look for a folder that contains the first file, then validate all required files.
                string firstFileName = RequiredFiles[0];

                // Note: This can return many results under PackageCache; we validate each folder.
                var matches = Directory.GetFiles(searchRoot, firstFileName, SearchOption.AllDirectories);

                if (matches == null || matches.Length == 0)
                    return null;

                // Prefer the first folder that has the full set.
                // If multiple candidates exist, the earliest match is typically fine.
                foreach (var file in matches)
                {
                    string folder = Path.GetDirectoryName(file);
                    if (string.IsNullOrEmpty(folder))
                        continue;

                    if (FolderContainsAllRequiredFiles(folder))
                        return folder;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool FolderContainsAllRequiredFiles(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;

            foreach (var file in RequiredFiles)
            {
                string path = Path.Combine(folder, file);
                if (!File.Exists(path))
                    return false;
            }

            return true;
        }

        private static void EnsureTargetFolderExists()
        {
            // Make sure Assets/StreamingAssets/... exists as real folders on disk.
            string targetFullPath = Path.GetFullPath(TargetRelativeFolder);

            if (!Directory.Exists(targetFullPath))
                Directory.CreateDirectory(targetFullPath);
        }

        private static void OpenTargetFolder()
        {
            string targetFullPath = Path.GetFullPath(TargetRelativeFolder);
            if (!Directory.Exists(targetFullPath))
                Directory.CreateDirectory(targetFullPath);

            EditorUtility.RevealInFinder(targetFullPath);
        }

        private void AppendLine(string line)
        {
            m_status += $"{line}\n";
            Repaint();
        }
    }
}
