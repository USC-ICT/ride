using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Package Source Switcher (Editor)
    /// Unity Editor window for switching a Unity project's included Unity packages from registry/git sources
    /// to local <c>file:</c> package references based on a chosen source root and the project's dependency graph.
    /// </summary>
    /// <remarks>
    /// <para><b>Purpose</b></para>
    /// <para>
    /// This tool helps developers work on RIDE Unity packages from a local source checkout without manually editing
    /// <c>Packages/manifest.json</c>. It scans a local Unity package root for <c>package.json</c> files, reads the
    /// current Unity project's direct package dependencies from <c>manifest.json</c>, and then computes which Unity
    /// packages are reachable through dependencies. For every reachable Unity package that also exists locally, the
    /// tool can rewrite the manifest so the package is included via a direct <c>file:</c> reference.
    /// </para>
    ///
    /// <para><b>Menu</b></para>
    /// <para>
    /// Unity: <c>Ride/Package Source Switcher</c>
    /// </para>
    ///
    /// <para><b>Key Features</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Local Unity package discovery:</b> Scans a configured local root and indexes Unity packages by reading their <c>package.json</c> files.</description></item>
    ///   <item><description><b>Dependency closure:</b> Starts from the Unity project's direct dependencies in <c>manifest.json</c> and walks their dependencies to determine which Unity packages are required.</description></item>
    ///   <item><description><b>Local-first graph resolution:</b> Uses local <c>package.json</c> dependency data first and falls back to <c>Packages/packages-lock.json</c> when a Unity package is not locally present.</description></item>
    ///   <item><description><b>Prefix filtering:</b> Limits the preview and apply set to Unity packages whose names begin with a configured prefix such as <c>edu.usc.ict</c>.</description></item>
    ///   <item><description><b>Manifest rewrite:</b> Replaces reachable Unity packages with direct <c>file:</c> entries so Unity resolves them from local source.</description></item>
    ///   <item><description><b>Simple revert:</b> Provides an <c>svn revert</c> operation for <c>Packages/manifest.json</c> and <c>Packages/packages-lock.json</c>.</description></item>
    /// </list>
    ///
    /// <para><b>How It Decides What To Switch</b></para>
    /// <para>
    /// The tool begins with the Unity packages listed directly in <c>manifest.json</c>. It then walks dependencies
    /// transitively. If a Unity package exists in the configured local source root, the dependency information from
    /// that local package's <c>package.json</c> is treated as authoritative. Otherwise, the tool falls back to the
    /// currently resolved dependency data in <c>packages-lock.json</c>. This allows remote Unity packages to lead to
    /// additional locally available Unity packages that should also be rewritten as <c>file:</c> references.
    /// </para>
    ///
    /// <para><b>Persistence</b></para>
    /// <para>
    /// The local Unity package root and package-name prefix filter are stored in <c>EditorPrefs</c> so the window
    /// remembers its last-used settings across editor sessions.
    /// </para>
    ///
    /// <para><b>Output</b></para>
    /// <para>
    /// When applied, the tool rewrites <c>Packages/manifest.json</c> through Newtonsoft JSON objects.
    /// Only the relevant dependency values are updated to <c>file:</c> references for reachable Unity packages found locally.
    /// </para>
    ///
    /// <para><b>Assumptions</b></para>
    /// <list type="bullet">
    ///   <item><description>The current Unity project contains a valid <c>Packages/manifest.json</c>.</description></item>
    ///   <item><description>The local source root contains Unity packages with valid <c>package.json</c> files.</description></item>
    ///   <item><description><c>svn</c> is available on the machine if the revert button is used.</description></item>
    /// </list>
    ///
    /// <para><b>Limitations</b></para>
    /// <list type="bullet">
    ///   <item><description>This version only switches Unity packages to local <c>file:</c> sources; it does not restore registry values by reconstructing prior versions.</description></item>
    ///   <item><description>Dependency traversal is limited to Unity package metadata available from local <c>package.json</c> files and the current <c>packages-lock.json</c>.</description></item>
    ///   <item><description>Only Unity packages matching the configured prefix filter are shown and considered for replacement.</description></item>
    /// </list>
    /// </remarks>
    public class PackageSourceSwitcherWindow : EditorWindow
    {
        private sealed class PackagePlanEntry
        {
            public string Name;
            public bool IsDirectManifestDependency;
            public bool IsReachable;
            public bool ExistsLocally;
            public bool IsCurrentlyLocalFile;
            public string LocalPath;
            public string CurrentManifestValue;
            public string SourceLabel;
            public string ActionLabel;
            public string Notes;
        }

        private sealed class LocalPackageInfo
        {
            public string Name;
            public string DirectoryPath;
            public Dictionary<string, string> Dependencies = new(StringComparer.Ordinal);
        }

        private sealed class LockPackageInfo
        {
            public string Name;
            public Dictionary<string, string> Dependencies = new(StringComparer.Ordinal);
        }


        private const string TitleText = "Package Source Switcher";
        private const string PrefsLocalRoot = "PackageSourceSwitcher.LocalRoot";
        private const string PrefsPrefixFilter = "PackageSourceSwitcher.PrefixFilter";
        private const string DefaultPrefixFilter = "edu.usc.ict";
        private const string PendingPackageRefreshSessionKey = "PackageSourceSwitcher.PendingPackageRefresh";

        private static bool s_packageEventsRegistered;

        private readonly List<PackagePlanEntry> m_planEntries = new();
        private readonly Color m_localSourceTint = new Color(1.00f, 0.58f, 0.58f);
        private Vector2 m_scroll;
        private string m_localRootPath = string.Empty;
        private string m_prefixFilter = DefaultPrefixFilter;
        private string m_statusMessage = "Click Refresh Preview to scan the project and local package root.";
        private MessageType m_statusType = MessageType.Info;


        [MenuItem("Ride/Package Source Switcher")]
        public static void Open()
        {
            var window = GetWindow<PackageSourceSwitcherWindow>(false, TitleText);
            window.minSize = new Vector2(900f, 540f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void InitializePackageEvents()
        {
            RegisterPackageEvents();
        }

        private void OnEnable()
        {
            RegisterPackageEvents();
            m_localRootPath = EditorPrefs.GetString(PrefsLocalRoot, GetDefaultLocalRoot());
            m_prefixFilter = EditorPrefs.GetString(PrefsPrefixFilter, DefaultPrefixFilter);
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();
            DrawStatus();
            EditorGUILayout.Space();
            DrawSummary();
            EditorGUILayout.Space();
            DrawPlanTable();
            EditorGUILayout.Space();
            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("Convert reachable Unity packages found under a local source root into direct 'file:' Unity package entries in this project's manifest.json.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Local Unity Package Root", GUILayout.Width(130f));

                var newPath = EditorGUILayout.TextField(m_localRootPath ?? string.Empty);
                if (!string.Equals(newPath, m_localRootPath, StringComparison.Ordinal))
                {
                    m_localRootPath = newPath;
                    EditorPrefs.SetString(PrefsLocalRoot, m_localRootPath ?? string.Empty);
                }

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    var selectedPath = EditorUtility.OpenFolderPanel("Select local Unity Packages folder", m_localRootPath, string.Empty);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        m_localRootPath = selectedPath;
                        EditorPrefs.SetString(PrefsLocalRoot, m_localRootPath);
                        GUI.FocusControl(null);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Unity Package Prefix", GUILayout.Width(130f));

                var newPrefix = EditorGUILayout.TextField(m_prefixFilter ?? string.Empty);
                if (!string.Equals(newPrefix, m_prefixFilter, StringComparison.Ordinal))
                {
                    m_prefixFilter = newPrefix;
                    EditorPrefs.SetString(PrefsPrefixFilter, m_prefixFilter ?? string.Empty);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(140f)))
                    RefreshPlan();

                if (GUILayout.Button("Apply Local 'file:' Entries", GUILayout.Width(180f)))
                    ApplyPlan();

                if (GUILayout.Button("SVN Revert Manifest + Packages Lock files", GUILayout.Width(280f)))
                    RevertManifestAndPackagesLock();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Packages Folder", GUILayout.Width(150f)))
                    EditorUtility.RevealInFinder(GetProjectPackagesDirectory());
            }
        }

        private void DrawStatus()
        {
            EditorGUILayout.HelpBox(m_statusMessage, m_statusType);
        }

        private void DrawSummary()
        {
            int reachableLocalCount = m_planEntries.Count(e => e.IsReachable && e.ExistsLocally);
            int directManifestCount = m_planEntries.Count(e => e.IsDirectManifestDependency);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Project: {Directory.GetParent(Application.dataPath)?.FullName ?? "(unknown)"}");
                EditorGUILayout.LabelField($"Manifest direct dependencies: {directManifestCount}");
                EditorGUILayout.LabelField($"Reachable Unity packages found in local root: {reachableLocalCount}");
                EditorGUILayout.LabelField($"Local Unity package root: {m_localRootPath}");
                EditorGUILayout.LabelField($"Unity package prefix filter: {m_prefixFilter}");
            }
        }

        private void DrawPlanTable()
        {
            EditorGUILayout.LabelField("Unity Package Preview", EditorStyles.boldLabel);

            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_scroll))
            {
                m_scroll = scrollView.scrollPosition;

                if (m_planEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No preview data yet.");
                    return;
                }

                foreach (var entry in m_planEntries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        Color previousColor = GUI.color;
                        if (entry.IsCurrentlyLocalFile)
                            GUI.color = m_localSourceTint;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel, GUILayout.Width(300f));
                            DrawEntryActionControl(entry);
                            EditorGUILayout.LabelField(entry.SourceLabel, GUILayout.Width(180f));
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(entry.IsDirectManifestDependency ? "Direct" : "Dependency", GUILayout.Width(90f));
                        }

                        GUI.color = previousColor;

                        //if (!string.IsNullOrEmpty(entry.LocalPath))
                        //    EditorGUILayout.SelectableLabel(entry.LocalPath, GUILayout.Height(18f));
                        //if (!string.IsNullOrEmpty(entry.Notes))
                        //    EditorGUILayout.LabelField(entry.Notes, EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.LabelField("Use the row action button to update one Unity package at a time, or Apply Local 'file:' Entries to update every reachable local Unity package at once. Revert shells out to svn revert for manifest.json and packages-lock.json.", EditorStyles.wordWrappedMiniLabel);
        }

        private void RefreshPlan()
        {
            try
            {
                var plan = BuildPlan();
                m_planEntries.Clear();
                m_planEntries.AddRange(plan);

                int localizableCount = plan.Count(p => p.IsReachable && p.ExistsLocally);
                SetStatus($"Preview refreshed. {localizableCount} reachable Unity packages will be written as 'file:' entries.", MessageType.Info);
            }
            catch (Exception ex)
            {
                m_planEntries.Clear();
                UnityEngine.Debug.LogError(ex);
                SetStatus($"Failed to build preview: {ex.Message}", MessageType.Error);
            }
        }

        private void ApplyPlan()
        {
            try
            {
                var plan = BuildPlan();
                int updatedCount = ApplyEntriesToManifest(plan.Where(p => p.IsReachable && p.ExistsLocally));

                RefreshAfterManifestChange();
                SetStatus($"Updated manifest.json. {updatedCount} Unity package entries changed to local 'file:' references.", MessageType.Info);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                SetStatus($"Failed to apply local file entries: {ex.Message}", MessageType.Error);
            }
        }

        private void DrawEntryActionControl(PackagePlanEntry entry)
        {
            bool canApplyEntry = entry.IsReachable && entry.ExistsLocally;
            using (new EditorGUI.DisabledScope(!canApplyEntry))
            {
                if (GUILayout.Button(entry.ActionLabel, GUILayout.Width(120f)))
                    ApplySingleEntry(entry);
            }
        }

        private void ApplySingleEntry(PackagePlanEntry entry)
        {
            try
            {
                bool changed = ApplyEntriesToManifest(new[] { entry }) > 0;
                RefreshAfterManifestChange();

                if (changed)
                    SetStatus($"Updated manifest.json for Unity package {entry.Name}.", MessageType.Info);
                else
                    SetStatus($"Unity package {entry.Name} already points to the selected local 'file:' source.", MessageType.Info);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                SetStatus($"Failed to update Unity package {entry.Name}: {ex.Message}", MessageType.Error);
            }
        }

        private void RevertManifestAndPackagesLock()
        {
            string projectRoot = GetProjectRoot();
            string manifestPath = GetManifestPath();
            string lockPath = GetPackagesLockPath();

            try
            {
                string arguments = $"revert \"{manifestPath}\" \"{lockPath}\"";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "svn",
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start svn process.");

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
                }

                RefreshAfterManifestChange();
                SetStatus("SVN revert completed for manifest.json and packages-lock.json.", MessageType.Info);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                SetStatus($"SVN revert failed: {ex.Message}", MessageType.Error);
            }
        }

        private List<PackagePlanEntry> BuildPlan()
        {
            ValidateInputs();

            var localPackages = ScanLocalPackages(m_localRootPath);
            var manifestDependencies = LoadManifestDependencies();
            var lockDependencies = LoadLockDependencies();
            var filteredLocalPackages = FilterLocalPackages(localPackages, m_prefixFilter);
            var filteredManifestDependencies = FilterStringDictionary(manifestDependencies, m_prefixFilter);
            var filteredLockDependencies = FilterLockDependencies(lockDependencies, m_prefixFilter);

            var reachable = ResolveReachablePackages(filteredManifestDependencies.Keys, filteredLocalPackages, filteredLockDependencies);
            var planEntries = new List<PackagePlanEntry>();
            var allNames = new HashSet<string>(filteredManifestDependencies.Keys, StringComparer.Ordinal);
            allNames.UnionWith(reachable);
            allNames.UnionWith(filteredLocalPackages.Keys);

            foreach (string packageName in allNames)
            {
                bool isDirect = filteredManifestDependencies.ContainsKey(packageName);
                bool isReachable = reachable.Contains(packageName);
                bool existsLocally = filteredLocalPackages.TryGetValue(packageName, out LocalPackageInfo localPackage);
                string currentValue = filteredManifestDependencies.TryGetValue(packageName, out string manifestValue) ? manifestValue : null;

                if (!isReachable)
                    continue;

                var entry = new PackagePlanEntry
                {
                    Name = packageName,
                    IsDirectManifestDependency = isDirect,
                    IsReachable = isReachable,
                    ExistsLocally = existsLocally,
                    IsCurrentlyLocalFile = IsManifestLocalFile(currentValue),
                    LocalPath = existsLocally ? localPackage.DirectoryPath : string.Empty,
                    CurrentManifestValue = currentValue,
                    SourceLabel = BuildSourceLabel(currentValue, existsLocally),
                    ActionLabel = BuildActionLabel(isReachable, existsLocally, currentValue),
                    Notes = BuildNotes(isReachable, existsLocally, filteredLocalPackages, filteredLockDependencies, packageName)
                };

                planEntries.Add(entry);
            }

            return planEntries;
        }

        private static string BuildSourceLabel(string currentValue, bool existsLocally)
        {
            if (string.IsNullOrEmpty(currentValue))
                return existsLocally ? "Current: not in manifest" : "Current: unavailable";

            if (IsManifestLocalFile(currentValue))
                return "Current: local 'file:'";

            if (currentValue.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                currentValue.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                return "Current: git/url";

            return "Current: registry";
        }

        private static string BuildActionLabel(bool isReachable, bool existsLocally, string currentValue)
        {
            if (!isReachable)
                return "Ignore";

            if (!existsLocally)
                return "Keep Remote";

            if (string.IsNullOrEmpty(currentValue))
                return "Add 'file:'";

            if (currentValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                return "Refresh 'file:'";

            return "Replace 'file:'";
        }

        private static string BuildNotes(
            bool isReachable,
            bool existsLocally,
            IReadOnlyDictionary<string, LocalPackageInfo> localPackages,
            IReadOnlyDictionary<string, LockPackageInfo> lockDependencies,
            string packageName)
        {
            if (!isReachable)
                return "Unity package exists in the local source root but is not currently required by this Unity project.";

            if (existsLocally && localPackages.TryGetValue(packageName, out LocalPackageInfo localPackage))
                return $"Dependencies resolved from local package.json ({localPackage.Dependencies.Count} deps).";

            if (lockDependencies.TryGetValue(packageName, out LockPackageInfo lockPackage))
                return $"Reachable through packages-lock.json ({lockPackage.Dependencies.Count} deps), but no local package folder was found.";

            return "Reachable from manifest dependencies, but no dependency metadata was found for deeper traversal.";
        }

        private static HashSet<string> ResolveReachablePackages(
            IEnumerable<string> roots,
            IReadOnlyDictionary<string, LocalPackageInfo> localPackages,
            IReadOnlyDictionary<string, LockPackageInfo> lockDependencies)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>(roots.Where(name => !string.IsNullOrWhiteSpace(name)));

            while (queue.Count > 0)
            {
                string packageName = queue.Dequeue();
                if (!visited.Add(packageName))
                    continue;

                IEnumerable<string> dependencyNames = Enumerable.Empty<string>();
                if (localPackages.TryGetValue(packageName, out LocalPackageInfo localPackage))
                    dependencyNames = localPackage.Dependencies.Keys;
                else if (lockDependencies.TryGetValue(packageName, out LockPackageInfo lockPackage))
                    dependencyNames = lockPackage.Dependencies.Keys;

                foreach (string dependencyName in dependencyNames)
                {
                    if (!visited.Contains(dependencyName))
                        queue.Enqueue(dependencyName);
                }
            }

            return visited;
        }

        private Dictionary<string, string> LoadManifestDependencies()
        {
            var manifest = JObject.Parse(File.ReadAllText(GetManifestPath()));
            if (manifest["dependencies"] is not JObject dependencies)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            return dependencies.Properties()
                .ToDictionary(property => property.Name, property => (string)property.Value, StringComparer.Ordinal);
        }

        private Dictionary<string, LockPackageInfo> LoadLockDependencies()
        {
            string lockPath = GetPackagesLockPath();
            if (!File.Exists(lockPath))
                return new Dictionary<string, LockPackageInfo>(StringComparer.Ordinal);

            var lockRoot = JObject.Parse(File.ReadAllText(lockPath));
            if (lockRoot["dependencies"] is not JObject dependencies)
                return new Dictionary<string, LockPackageInfo>(StringComparer.Ordinal);

            var results = new Dictionary<string, LockPackageInfo>(StringComparer.Ordinal);
            foreach (var property in dependencies.Properties())
            {
                var packageObject = property.Value as JObject;
                var packageDependencies = packageObject?["dependencies"] as JObject;
                results[property.Name] = new LockPackageInfo
                {
                    Name = property.Name,
                    Dependencies = ToStringDictionary(packageDependencies)
                };
            }

            return results;
        }

        private static Dictionary<string, LocalPackageInfo> ScanLocalPackages(string localRootPath)
        {
            var results = new Dictionary<string, LocalPackageInfo>(StringComparer.Ordinal);
            foreach (string packageJsonPath in Directory.GetFiles(localRootPath, "package.json", SearchOption.AllDirectories))
            {
                string directoryPath = Path.GetDirectoryName(packageJsonPath);
                if (string.IsNullOrEmpty(directoryPath))
                    continue;

                var packageRoot = JObject.Parse(File.ReadAllText(packageJsonPath));
                string packageName = (string)packageRoot["name"];
                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                var dependenciesObject = packageRoot["dependencies"] as JObject;
                results[packageName] = new LocalPackageInfo
                {
                    Name = packageName,
                    DirectoryPath = directoryPath,
                    Dependencies = ToStringDictionary(dependenciesObject)
                };
            }

            return results;
        }

        private static Dictionary<string, string> ToStringDictionary(JObject obj)
        {
            if (obj == null)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            return obj.Properties()
                .ToDictionary(property => property.Name, property => (string)property.Value, StringComparer.Ordinal);
        }

        private static Dictionary<string, string> FilterStringDictionary(
            IReadOnlyDictionary<string, string> source,
            string prefixFilter)
        {
            return source
                .Where(kvp => MatchesPrefix(kvp.Key, prefixFilter))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        }

        private static Dictionary<string, LocalPackageInfo> FilterLocalPackages(
            IReadOnlyDictionary<string, LocalPackageInfo> source,
            string prefixFilter)
        {
            return source
                .Where(kvp => MatchesPrefix(kvp.Key, prefixFilter))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new LocalPackageInfo
                    {
                        Name = kvp.Value.Name,
                        DirectoryPath = kvp.Value.DirectoryPath,
                        Dependencies = kvp.Value.Dependencies
                            .Where(dep => MatchesPrefix(dep.Key, prefixFilter))
                            .ToDictionary(dep => dep.Key, dep => dep.Value, StringComparer.Ordinal)
                    },
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, LockPackageInfo> FilterLockDependencies(
            IReadOnlyDictionary<string, LockPackageInfo> source,
            string prefixFilter)
        {
            return source
                .Where(kvp => MatchesPrefix(kvp.Key, prefixFilter))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new LockPackageInfo
                    {
                        Name = kvp.Value.Name,
                        Dependencies = kvp.Value.Dependencies
                            .Where(dep => MatchesPrefix(dep.Key, prefixFilter))
                            .ToDictionary(dep => dep.Key, dep => dep.Value, StringComparer.Ordinal)
                    },
                    StringComparer.Ordinal);
        }

        private static bool MatchesPrefix(string packageName, string prefixFilter)
        {
            if (string.IsNullOrWhiteSpace(prefixFilter))
                return true;

            return packageName.StartsWith(prefixFilter, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsManifestLocalFile(string currentValue)
        {
            return !string.IsNullOrEmpty(currentValue) &&
                   currentValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        }

        private static void RequestPackageResolve()
        {
            try
            {
                SessionState.SetBool(PendingPackageRefreshSessionKey, true);
                Client.Resolve();
            }
            catch (Exception ex)
            {
                SessionState.SetBool(PendingPackageRefreshSessionKey, false);
                UnityEngine.Debug.LogWarning($"Package Manager resolve request failed: {ex.Message}");
            }
        }

        private static void RegisterPackageEvents()
        {
            if (s_packageEventsRegistered)
                return;

            Events.registeredPackages += OnRegisteredPackages;
            s_packageEventsRegistered = true;
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (!SessionState.GetBool(PendingPackageRefreshSessionKey, false))
                return;

            SessionState.SetBool(PendingPackageRefreshSessionKey, false);

            foreach (PackageSourceSwitcherWindow window in Resources.FindObjectsOfTypeAll<PackageSourceSwitcherWindow>())
            {
                if (window == null)
                    continue;

                window.RefreshPlan();
                window.Repaint();
            }
        }

        private void RefreshAfterManifestChange()
        {
            AssetDatabase.Refresh();
            RequestPackageResolve();
            RefreshPlan();
        }

        private static int ApplyEntriesToManifest(IEnumerable<PackagePlanEntry> entries)
        {
            string manifestPath = GetManifestPath();
            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            var dependencies = manifest["dependencies"] is JObject existingDependencies
                ? new JObject(existingDependencies.Properties().Select(property => new JProperty(property.Name, property.Value?.DeepClone())))
                : new JObject();

            int changedCount = 0;
            var appliedEntries = new List<PackagePlanEntry>();

            foreach (PackagePlanEntry entry in entries)
            {
                if (!entry.IsReachable || !entry.ExistsLocally || string.IsNullOrEmpty(entry.LocalPath))
                    continue;

                string fileReference = BuildFileReference(entry.LocalPath);
                JProperty existingProperty = dependencies.Property(entry.Name, StringComparison.Ordinal);
                bool changed = existingProperty == null ||
                               !string.Equals((string)existingProperty.Value, fileReference, StringComparison.Ordinal);

                if (existingProperty != null)
                    existingProperty.Value = fileReference;
                else
                    dependencies.Add(new JProperty(entry.Name, fileReference));

                if (changed)
                    changedCount++;

                appliedEntries.Add(entry);
            }

            manifest["dependencies"] = dependencies;
            WriteJsonFile(manifestPath, manifest);
            VerifyManifestContainsAppliedEntries(manifestPath, appliedEntries);
            return changedCount;
        }

        private static void VerifyManifestContainsAppliedEntries(string manifestPath, IEnumerable<PackagePlanEntry> entries)
        {
            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            var dependencies = manifest["dependencies"] as JObject
                               ?? throw new InvalidOperationException("manifest.json is missing the dependencies object after write.");

            foreach (PackagePlanEntry entry in entries)
            {
                string expectedValue = BuildFileReference(entry.LocalPath);
                string actualValue = (string)dependencies.Property(entry.Name, StringComparison.Ordinal)?.Value;
                if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"manifest.json verification failed for {entry.Name}. Expected '{expectedValue}', found '{actualValue ?? "(missing)"}'.");
                }
            }
        }

        private static string BuildFileReference(string directoryPath) => "file:" + directoryPath.Replace("\\", "/");

        private static void WriteJsonFile(string path, JToken token)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.Indentation = 2;
                jsonWriter.IndentChar = ' ';
                token.WriteTo(jsonWriter);
            }

            string content = stringBuilder
                .ToString()
                .Replace("\r\n", "\n");

            if (!content.EndsWith("\n", StringComparison.Ordinal))
                content += "\n";

            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private void ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(m_localRootPath))
                throw new InvalidOperationException("Local package root is empty.");

            if (!Directory.Exists(m_localRootPath))
                throw new DirectoryNotFoundException($"Local package root does not exist: {m_localRootPath}");

            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Could not find Packages/manifest.json", manifestPath);
        }

        private static string GetProjectRoot() => Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("Could not resolve Unity project root.");
        private static string GetDefaultLocalRoot() => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private static string GetProjectPackagesDirectory() => Path.Combine(GetProjectRoot(), "Packages");
        private static string GetManifestPath() => Path.Combine(GetProjectPackagesDirectory(), "manifest.json");
        private static string GetPackagesLockPath() => Path.Combine(GetProjectPackagesDirectory(), "packages-lock.json");

        private void SetStatus(string message, MessageType type)
        {
            m_statusMessage = message;
            m_statusType = type;
        }
    }
}
