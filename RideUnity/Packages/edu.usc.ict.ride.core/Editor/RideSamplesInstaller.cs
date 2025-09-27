using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RIDE Samples Manager Window.
/// </summary>
/// <remarks>
/// <para>
/// An EditorWindow that discovers Unity packages under <c>Packages/*</c> that contain a
/// <c>Samples~</c> folder, lists each sample (each direct subfolder), and provides
/// one-click Install/Remove actions. Installing a sample performs a normal filesystem
/// copy into the project at:
/// <c>Assets/Samples/&lt;package-name&gt;/&lt;sample-folder&gt;</c>. Removing deletes that copy
/// and cleans up empty parent folders up to <c>Assets/Samples</c>.
/// </para>
/// 
/// <para>
/// This tool intentionally uses a direct directory copy instead of the UPM "samples"
/// manifest mechanism so it works uniformly across Windows/macOS and does not require
/// package.json edits. It preserves folder structure and .meta files by copying all
/// entries within the sample folder, then calls <see cref="UnityEditor.AssetDatabase.Refresh"/>.
/// </para>
///
/// <para>
/// The window offers:
/// </para>
/// <list type="bullet">
///   <item><description>Global actions: Refresh, Install All (across all packages), Remove All (across all packages).</description></item>
///   <item><description>Per-package actions: Install All / Remove All for that package only.</description></item>
///   <item><description>Per-sample actions: Install / Remove, and Reveal in Finder/Explorer.</description></item>
///   <item><description>Optional filter to show only "RIDE-like" packages (names containing ".ride.").</description></item>
/// </list>
///
/// <para><b>Behavior and constraints</b></para>
/// <list type="bullet">
///   <item><description>Package name is read from <c>package.json</c> (<c>"name"</c> field). If missing, folder name is used.</description></item>
///   <item><description>Samples are discovered by enumerating immediate subdirectories of <c>Samples~</c>.</description></item>
///   <item><description>Install overwrites existing files at the destination path if present.</description></item>
///   <item><description>All file operations are synchronous and run on the main thread inside the editor.</description></item>
///   <item><description>After file ops, the window triggers <c>AssetDatabase.Refresh()</c> to import assets.</description></item>
///   <item><description>On Remove, empty parent folders are deleted up to <c>Assets/Samples</c> (but not above).</description></item>
///   <item><description>Progress bars and dialogs are shown via <c>EditorUtility</c> for user feedback.</description></item>
/// </list>
///
/// <para><b>Tested Unity versions</b>: 2021 LTS and newer.</para>
///
/// <para><b>Typical workflow</b>:</para>
/// <list type="number">
///   <item><description>Open the window: MenuPath <c>Ride/Samples/Manager</c>.</description></item>
///   <item><description>Click <c>Refresh</c> to rescan <c>Packages/*/Samples~</c>.</description></item>
///   <item><description>Use Install/Remove per sample or per package, or the global Install/Remove All.</description></item>
/// </list>
///
/// <para><b>Notes for maintainers</b>:</para>
/// <list type="bullet">
///   <item><description>Folder and file names are sanitized for invalid filesystem characters before creating destination paths.</description></item>
///   <item><description>Regex is used to read <c>"name"</c> from <c>package.json</c>; keep it tolerant to whitespace and formatting.</description></item>
///   <item><description>Consider adding guards if you later support very large sample folders (progress updates, cancellation).</description></item>
///   <item><description>This tool does not parse the UPM <c>"samples"</c> array. It purely scans <c>Samples~</c> folders.</description></item>
///   <item><description>No symlinks or junctions are used to avoid platform permission issues.</description></item>
/// </list>
///
/// <para><b>Limitations</b>:</para>
/// <list type="bullet">
///   <item><description>Does not resolve package dependencies; it only copies sample content.</description></item>
///   <item><description>Overwrites without diff/merge; user changes in <c>Assets/Samples</c> may be lost on reinstall.</description></item>
///   <item><description>Assumes standard Unity project structure and write permissions inside the project.</description></item>
/// </list>
///
/// <para><b>Related Unity documentation</b> (reference):</para>
/// <list type="bullet">
///   <item><description>EditorWindow: https://docs.unity3d.com/ScriptReference/EditorWindow.html</description></item>
///   <item><description>MenuItem: https://docs.unity3d.com/ScriptReference/MenuItem.html</description></item>
///   <item><description>EditorGUILayout: https://docs.unity3d.com/ScriptReference/EditorGUILayout.html</description></item>
///   <item><description>EditorGUI.DisabledScope: https://docs.unity3d.com/ScriptReference/EditorGUI.DisabledScope.html</description></item>
///   <item><description>GUI (immediate mode UI): https://docs.unity3d.com/ScriptReference/GUI.html</description></item>
///   <item><description>AssetDatabase.Refresh: https://docs.unity3d.com/ScriptReference/AssetDatabase.Refresh.html</description></item>
///   <item><description>EditorUtility.DisplayProgressBar: https://docs.unity3d.com/ScriptReference/EditorUtility.DisplayProgressBar.html</description></item>
///   <item><description>EditorUtility.RevealInFinder: https://docs.unity3d.com/ScriptReference/EditorUtility.RevealInFinder.html</description></item>
///   <item><description>Custom packages and Samples~ folder (UPM concepts): https://docs.unity3d.com/Manual/CustomPackages.html</description></item>
/// </list>
///
/// <example>
/// <code>
/// // Open from code:
/// // (equivalent to selecting "Ride/Samples/Manager" in the Unity menu)
/// UnityEditor.EditorWindow.GetWindow&lt;RideSamplesWindow&gt;("RIDE Samples").Show();
/// </code>
/// </example>
///
/// <seealso cref="UnityEditor.EditorWindow"/>
/// <seealso cref="UnityEditor.MenuItemAttribute"/>
/// <seealso cref="UnityEditor.AssetDatabase"/>
/// <seealso cref="UnityEditor.EditorGUILayout"/>
/// <seealso cref="UnityEditor.EditorGUI"/>
/// <seealso cref="UnityEditor.EditorUtility"/>
/// <seealso cref="UnityEngine.GUI"/>
/// </remarks>
public class RideSamplesWindow : EditorWindow
{
    private const string PackagesRoot = "Packages";
    private const string SamplesFolderName = "Samples~";
    private const string InstallRoot = "Assets/Samples";

    private Vector2 m_scroll;
    private readonly List<PackageInfo> m_packages = new List<PackageInfo>();
    private bool m_showOnlyRidePackages = true; // filter to com.ride.* or edu.usc.ict.ride.* if you want


    [MenuItem("Ride/Samples/Manager")]
    public static void Open()
    {
        var wnd = GetWindow<RideSamplesWindow>("RIDE Samples");
        wnd.minSize = new Vector2(520, 380);
        wnd.Refresh();
        wnd.Show();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            Refresh();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Install All (All Packages)", GUILayout.Width(220)))
            InstallAllPackages();

        if (GUILayout.Button("Remove All (All Packages)", GUILayout.Width(220)))
            RemoveAllPackages();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        m_showOnlyRidePackages = EditorGUILayout.ToggleLeft("Show only RIDE-like packages (name contains \".ride.\")", m_showOnlyRidePackages);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Install root:", InstallRoot);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Install copies files from Packages/<pkg>/Samples~/<sample> into {InstallRoot} /<pkg-name>/<sample>. Use Remove to delete the installed copy.", MessageType.Info);

        EditorGUILayout.Space();
        using (var sv = new EditorGUILayout.ScrollViewScope(m_scroll))
        {
            m_scroll = sv.scrollPosition;

            if (m_packages.Count == 0)
                EditorGUILayout.HelpBox($"No packages with Samples~ were found under: {PackagesRoot}", MessageType.Warning);

            foreach (var pkg in m_packages)
            {
                if (m_showOnlyRidePackages && !pkg.PackageName.Contains(".ride."))
                    continue;

                DrawPackageBlock(pkg);
                EditorGUILayout.Space(10);
            }
        }
    }

    private void DrawPackageBlock(PackageInfo pkg)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.BeginHorizontal();
            pkg.Foldout = EditorGUILayout.Foldout(pkg.Foldout, pkg.PackageName, true);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(pkg.Samples.Count == 0))
            {
                if (GUILayout.Button("Install All", GUILayout.Width(120)))
                    InstallAllSamples(pkg);

                if (GUILayout.Button("Remove All", GUILayout.Width(120)))
                    RemoveAllSamples(pkg);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Path:", pkg.PackageDirRelative);
            EditorGUILayout.LabelField("Samples~:", pkg.SamplesRootRelative);

            if (!pkg.Foldout) return;

            if (pkg.Samples.Count == 0)
            {
                EditorGUILayout.HelpBox("No sample folders found under Samples~.", MessageType.None);
                return;
            }

            EditorGUILayout.Space(4);
            foreach (var s in pkg.Samples)
                DrawSampleRow(pkg, s);
        }
    }

    private void DrawSampleRow(PackageInfo pkg, SampleInfo s)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(10);
            GUILayout.Label($"- {s.SampleFolderName}", GUILayout.Width(260));

            string installPath = GetInstallPath(pkg.PackageName, s.SampleFolderName);
            bool installed = Directory.Exists(installPath);

            GUILayout.Label(installed ? $"Installed: {MakeProjectRelative(installPath)}" : "Not installed", GUILayout.ExpandWidth(true));

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = installed ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 0.3f);

            if (!installed)
            {
                if (GUILayout.Button("Install", GUILayout.Width(100)))
                    InstallSample(pkg, s);
            }
            else
            {
                if (GUILayout.Button("Remove", GUILayout.Width(100)))
                    RemoveSample(pkg, s);
            }

            GUI.backgroundColor = old;

            if (GUILayout.Button("Reveal", GUILayout.Width(80)))
            {
                string src = s.AbsolutePath;
                if (Directory.Exists(src))
                    EditorUtility.RevealInFinder(src);
                else
                    EditorUtility.DisplayDialog("Not found", $"Sample folder missing:\n{src}", "OK");
            }
        }
    }

    // Actions

    private void InstallSample(PackageInfo pkg, SampleInfo s)
    {
        string src = s.AbsolutePath;
        string dst = GetInstallPath(pkg.PackageName, s.SampleFolderName);

        try
        {
            EditorUtility.DisplayProgressBar("Installing Sample", s.SampleFolderName, 0.5f);
            CopyDirectory(src, dst);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Install failed: {ex.Message}");
            EditorUtility.DisplayDialog("Install failed", ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void RemoveSample(PackageInfo pkg, SampleInfo s)
    {
        string dst = GetInstallPath(pkg.PackageName, s.SampleFolderName);
        if (!Directory.Exists(dst)) return;

        try
        {
            EditorUtility.DisplayProgressBar("Removing Sample", s.SampleFolderName, 0.5f);
            Directory.Delete(dst, true);
            DeleteEmptyParentsUpTo(InstallRoot, Path.GetDirectoryName(dst));
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remove failed: {ex.Message}");
            EditorUtility.DisplayDialog("Remove failed", ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void InstallAllSamples(PackageInfo pkg)
    {
        foreach (var s in pkg.Samples)
            InstallSample(pkg, s);
    }

    private void RemoveAllSamples(PackageInfo pkg)
    {
        foreach (var s in pkg.Samples)
            RemoveSample(pkg, s);
    }

    private void InstallAllPackages()
    {
        foreach (var pkg in m_packages)
            InstallAllSamples(pkg);
    }

    private void RemoveAllPackages()
    {
        foreach (var pkg in m_packages)
            RemoveAllSamples(pkg);
    }

    // Discovery

    private void Refresh()
    {
        m_packages.Clear();

        string projRoot = Directory.GetCurrentDirectory();
        string pkgsRootAbs = Path.Combine(projRoot, PackagesRoot);

        if (!Directory.Exists(pkgsRootAbs))
            return;

        foreach (var dir in Directory.GetDirectories(pkgsRootAbs))
        {
            string samplesRoot = Path.Combine(dir, SamplesFolderName);
            if (!Directory.Exists(samplesRoot))
                continue;

            var pkg = new PackageInfo
            {
                PackageDirAbsolute = dir,
                PackageDirRelative = MakeProjectRelative(dir),
                SamplesRootAbsolute = samplesRoot,
                SamplesRootRelative = MakeProjectRelative(samplesRoot),
                PackageName = ReadPackageName(dir)
            };

            // fall back to folder name if name not found
            if (string.IsNullOrEmpty(pkg.PackageName))
                pkg.PackageName = Path.GetFileName(dir);

            foreach (var sub in Directory.GetDirectories(samplesRoot))
            {
                var s = new SampleInfo
                {
                    SampleFolderName = Path.GetFileName(sub),
                    AbsolutePath = sub
                };
                pkg.Samples.Add(s);
            }

            // Only add packages that have at least one sample
            m_packages.Add(pkg);
        }

        Repaint();
    }

    private static string ReadPackageName(string packageDir)
    {
        try
        {
            string jsonPath = Path.Combine(packageDir, "package.json");
            if (!File.Exists(jsonPath)) return null;

            string text = File.ReadAllText(jsonPath);

            // Very small regex to find: "name": "value"
            var m = Regex.Match(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            if (m.Success) return m.Groups[1].Value;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to read package.json name from: {packageDir} - {ex.Message}");
        }
        return null;
    }

    // Paths and utils

    private static string GetInstallPath(string packageName, string sampleFolder)
    {
        // Assets/Samples/<package-name>/<sample-folder>
        string root = Path.Combine(Directory.GetCurrentDirectory(), InstallRoot);
        string pkg = Path.Combine(root, SanitizeFolderName(packageName));
        return Path.Combine(pkg, SanitizeFolderName(sampleFolder));
    }

    private static string SanitizeFolderName(string name)
    {
        // Remove chars invalid on Windows/macOS
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        return name.Trim();
    }

    private static string MakeProjectRelative(string absolutePath)
    {
        string root = Directory.GetCurrentDirectory();
        absolutePath = absolutePath.Replace('\\', '/');
        root = root.Replace('\\', '/');

        if (absolutePath.StartsWith(root))
            return absolutePath.Substring(root.Length + 1);
        return absolutePath;
    }

    private static void CopyDirectory(string src, string dst)
    {
        if (!Directory.Exists(dst))
            Directory.CreateDirectory(dst);

        foreach (var file in Directory.GetFiles(src))
        {
            var name = Path.GetFileName(file);
            var destFile = Path.Combine(dst, name);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(src))
        {
            var name = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(dst, name));
        }
    }

    private static void DeleteEmptyParentsUpTo(string stopAtProjectRelative, string startAbsolute)
    {
        string projectRoot = Directory.GetCurrentDirectory();
        string stopAbs = Path.Combine(projectRoot, stopAtProjectRelative);

        var current = startAbsolute;
        while (!string.IsNullOrEmpty(current) && current.StartsWith(stopAbs))
        {
            if (Directory.Exists(current) && Directory.GetFileSystemEntries(current).Length == 0)
            {
                Directory.Delete(current, false);
                current = Path.GetDirectoryName(current);
            }
            else
            {
                break;
            }
        }
    }

    // Data

    private class PackageInfo
    {
        public string PackageName;
        public string PackageDirAbsolute;
        public string PackageDirRelative;
        public string SamplesRootAbsolute;
        public string SamplesRootRelative;
        public bool Foldout = true;
        public List<SampleInfo> Samples = new List<SampleInfo>();
    }

    private class SampleInfo
    {
        public string SampleFolderName;
        public string AbsolutePath;
    }
}
