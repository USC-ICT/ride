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
    /// to repository <c>file:</c> package references based on a chosen repository package root and the project's dependency graph.
    /// </summary>
    /// <remarks>
    /// <para><b>Purpose</b></para>
    /// <para>
    /// This tool helps developers work on RIDE Unity packages from a repository checkout without manually editing
    /// <c>Packages/manifest.json</c>. It scans a repository package root for <c>package.json</c> files, reads the
    /// current Unity project's direct package dependencies from <c>manifest.json</c>, and then computes which Unity
    /// packages are reachable through dependencies. For every reachable Unity package that also exists in the repository, the
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
    ///   <item><description><b>Repository Unity package discovery:</b> Scans a configured repository root and indexes Unity packages by reading their <c>package.json</c> files.</description></item>
    ///   <item><description><b>Dependency closure:</b> Starts from the Unity project's direct dependencies in <c>manifest.json</c> and walks their dependencies to determine which Unity packages are required.</description></item>
    ///   <item><description><b>Repository-first graph resolution:</b> Uses repository <c>package.json</c> dependency data first and falls back to <c>Packages/packages-lock.json</c> when a Unity package is not present in the repository root.</description></item>
    ///   <item><description><b>Prefix filtering:</b> Limits the preview and apply set to Unity packages whose names begin with a configured prefix such as <c>edu.usc.ict</c>.</description></item>
    ///   <item><description><b>Manifest rewrite:</b> Replaces reachable Unity packages with direct <c>file:</c> entries so Unity resolves them from the repository package root.</description></item>
    ///   <item><description><b>Simple revert:</b> Provides an <c>svn revert</c> operation for <c>Packages/manifest.json</c> and <c>Packages/packages-lock.json</c>.</description></item>
    /// </list>
    ///
    /// <para><b>How It Decides What To Switch</b></para>
    /// <para>
    /// The tool begins with the Unity packages listed directly in <c>manifest.json</c>. It then walks dependencies
    /// transitively. If a Unity package exists in the configured repository package root, the dependency information from
    /// that repository package's <c>package.json</c> is treated as authoritative. Otherwise, the tool falls back to the
    /// currently resolved dependency data in <c>packages-lock.json</c>. This allows remote Unity packages to lead to
    /// additional repository-available Unity packages that should also be rewritten as <c>file:</c> references.
    /// </para>
    ///
    /// <para><b>Persistence</b></para>
    /// <para>
    /// The repository package root and package-name prefix filter are stored in <c>EditorPrefs</c> so the window
    /// remembers its last-used settings across editor sessions.
    /// </para>
    ///
    /// <para><b>Output</b></para>
    /// <para>
    /// When applied, the tool rewrites <c>Packages/manifest.json</c> through Newtonsoft JSON objects.
    /// Only the relevant dependency values are updated to <c>file:</c> references for reachable Unity packages found in the repository root.
    /// </para>
    ///
    /// <para><b>Assumptions</b></para>
    /// <list type="bullet">
    ///   <item><description>The current Unity project contains a valid <c>Packages/manifest.json</c>.</description></item>
    ///   <item><description>The repository package root contains Unity packages with valid <c>package.json</c> files.</description></item>
    ///   <item><description><c>svn</c> is available on the machine if the revert button is used.</description></item>
    /// </list>
    ///
    /// <para><b>Limitations</b></para>
    /// <list type="bullet">
    ///   <item><description>This version only switches Unity packages to repository <c>file:</c> sources; it does not restore registry values by reconstructing prior versions.</description></item>
    ///   <item><description>Dependency traversal is limited to Unity package metadata available from repository <c>package.json</c> files and the current <c>packages-lock.json</c>.</description></item>
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
            public bool ExistsInRepository;
            public bool IsCurrentlyRepositoryFile;
            public string InstalledVersion;
            public string RepositoryVersion;
            public string RepositoryPath;
            public string CurrentManifestValue;
            public string SourceLabel;
            public string ActionLabel;
            public string Notes;
        }

        private sealed class RepositoryPackageInfo
        {
            public string Name;
            public string Version;
            public string DirectoryPath;
            public Dictionary<string, string> Dependencies = new(StringComparer.Ordinal);
        }

        private sealed class LockPackageInfo
        {
            public string Name;
            public Dictionary<string, string> Dependencies = new(StringComparer.Ordinal);
        }


        private const string TitleText = "Package Source Switcher";
        private const string PrefsRepositoryRoot = "PackageSourceSwitcher.LocalRoot";
        private const string PrefsPrefixFilter = "PackageSourceSwitcher.PrefixFilter";
        private const string DefaultPrefixFilter = "edu.usc.ict";
        private const string PendingPackageRefreshSessionKey = "PackageSourceSwitcher.PendingPackageRefresh";

        private static bool s_packageEventsRegistered;

        private readonly List<PackagePlanEntry> m_planEntries = new();
        private readonly Color m_repositorySourceTint = new Color(1.00f, 0.58f, 0.58f);
        private readonly Color m_versionMismatchTint = new Color(1.00f, 0.93f, 0.72f);
        private Vector2 m_scroll;
        private string m_repositoryRootPath = string.Empty;
        private string m_prefixFilter = DefaultPrefixFilter;
        private string m_statusMessage = "Click Refresh Preview to scan the project and repository package root.";
        private MessageType m_statusType = MessageType.Info;


        [MenuItem("Ride/Package Source Switcher")]
        public static void Open()
        {
            var window = GetWindow<PackageSourceSwitcherWindow>(false, TitleText);
            window.minSize = new Vector2(1000f, 540f);
            Rect position = window.position;
            if (position.width < 1000f || position.height < 540f)
                window.position = new Rect(position.x, position.y, Mathf.Max(position.width, 1000f), Mathf.Max(position.height, 540f));
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
            m_repositoryRootPath = EditorPrefs.GetString(PrefsRepositoryRoot, GetDefaultRepositoryRoot());
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
            EditorGUILayout.LabelField("Convert reachable Unity packages found under a repository package root into direct 'file:' Unity package entries in this project's manifest.json.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Repository Package Root", GUILayout.Width(160f));

                var newPath = EditorGUILayout.TextField(m_repositoryRootPath ?? string.Empty);
                if (!string.Equals(newPath, m_repositoryRootPath, StringComparison.Ordinal))
                {
                    m_repositoryRootPath = newPath;
                    EditorPrefs.SetString(PrefsRepositoryRoot, m_repositoryRootPath ?? string.Empty);
                }

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    var selectedPath = EditorUtility.OpenFolderPanel("Select repository Unity Packages folder", m_repositoryRootPath, string.Empty);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        m_repositoryRootPath = selectedPath;
                        EditorPrefs.SetString(PrefsRepositoryRoot, m_repositoryRootPath);
                        GUI.FocusControl(null);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Unity Package Prefix", GUILayout.Width(160f));

                var newPrefix = EditorGUILayout.TextField(m_prefixFilter ?? string.Empty);
                if (!string.Equals(newPrefix, m_prefixFilter, StringComparison.Ordinal))
                {
                    m_prefixFilter = newPrefix;
                    EditorPrefs.SetString(PrefsPrefixFilter, m_prefixFilter ?? string.Empty);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(100f)))
                    RefreshPlan();

                if (GUILayout.Button("Apply Repository 'file:' Entries", GUILayout.Width(210f)))
                    ApplyPlan();

                if (GUILayout.Button("SVN Revert Manifest + Packages Lock files", GUILayout.Width(280f)))
                    RevertManifestAndPackagesLock();

                if (GUILayout.Button("Open Unity Package Manager", GUILayout.Width(190f)))
                    OpenUnityPackageManagerWindow();

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
            int reachableRepositoryCount = m_planEntries.Count(e => e.IsReachable && e.ExistsInRepository);
            int directManifestCount = m_planEntries.Count(e => e.IsDirectManifestDependency);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Project: {Directory.GetParent(Application.dataPath)?.FullName ?? "(unknown)"}");
                EditorGUILayout.LabelField($"Manifest direct dependencies: {directManifestCount}");
                EditorGUILayout.LabelField($"Reachable Unity packages found in repository root: {reachableRepositoryCount}");
                EditorGUILayout.LabelField($"Repository package root: {m_repositoryRootPath}");
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
                        if (entry.IsCurrentlyRepositoryFile)
                            GUI.color = m_repositorySourceTint;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel, GUILayout.Width(300f));
                            DrawEntryActionControl(entry);
                            EditorGUILayout.LabelField(entry.SourceLabel, GUILayout.Width(180f));
                            DrawVersionSummary(entry);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(entry.IsDirectManifestDependency ? "Direct" : "Dependency", GUILayout.Width(90f));
                        }

                        GUI.color = previousColor;

                        //if (!string.IsNullOrEmpty(entry.RepositoryPath))
                        //    EditorGUILayout.SelectableLabel(entry.RepositoryPath, GUILayout.Height(18f));
                        //if (!string.IsNullOrEmpty(entry.Notes))
                        //    EditorGUILayout.LabelField(entry.Notes, EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.LabelField("Use the row action button to update one Unity package at a time, or Apply Repository 'file:' Entries to update every reachable repository Unity package at once. Revert shells out to svn revert for manifest.json and packages-lock.json.", EditorStyles.wordWrappedMiniLabel);
        }

        private void RefreshPlan()
        {
            try
            {
                var plan = BuildPlan();
                m_planEntries.Clear();
                m_planEntries.AddRange(plan);

                int repositoryPackageCount = plan.Count(p => p.IsReachable && p.ExistsInRepository);
                SetStatus($"Preview refreshed. {repositoryPackageCount} reachable repository Unity packages will be written as 'file:' entries.", MessageType.Info);
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
                int updatedCount = ApplyEntriesToManifest(plan.Where(p => p.IsReachable && p.ExistsInRepository));

                RefreshAfterManifestChange();
                SetStatus($"Updated manifest.json. {updatedCount} Unity package entries changed to repository 'file:' references.", MessageType.Info);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                SetStatus($"Failed to apply repository file entries: {ex.Message}", MessageType.Error);
            }
        }

        private void DrawEntryActionControl(PackagePlanEntry entry)
        {
            bool canApplyEntry = entry.IsReachable && entry.ExistsInRepository;
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
                    SetStatus($"Unity package {entry.Name} already points to the selected repository 'file:' source.", MessageType.Info);
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

        private void OpenUnityPackageManagerWindow()
        {
            string[] menuPaths =
            {
                "Window/Package Management/Package Manager",
                "Window/Package Manager"
            };

            foreach (string menuPath in menuPaths)
            {
                if (EditorApplication.ExecuteMenuItem(menuPath))
                    return;
            }

            SetStatus("Could not open Unity Package Manager. Tried Window/Package Management/Package Manager and Window/Package Manager.", MessageType.Warning);
            UnityEngine.Debug.LogWarning("Could not open Unity Package Manager because no known menu path was available in this Unity version.");
        }

        private void DrawVersionSummary(PackagePlanEntry entry)
        {
            bool versionsDiffer = VersionsDiffer(entry);
            Color previousColor = GUI.color;

            if (versionsDiffer)
                GUI.color = m_versionMismatchTint;

            string resolvedLabel = $"Current: {GetResolvedVersionLabel(entry)}";
            string localLabel = $"Repo: {GetRepositoryVersionLabel(entry)}";
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            if (versionsDiffer)
            {
                resolvedLabel = $"<b>{resolvedLabel}</b>";
                localLabel = $"<b>{localLabel}</b>";
            }

            EditorGUILayout.LabelField(resolvedLabel, labelStyle, GUILayout.Width(150f));
            EditorGUILayout.LabelField(localLabel, labelStyle, GUILayout.Width(150f));
            GUI.color = previousColor;
        }

        private List<PackagePlanEntry> BuildPlan()
        {
            ValidateInputs();

            var repositoryPackages = ScanRepositoryPackages(m_repositoryRootPath);
            var manifestDependencies = LoadManifestDependencies();
            var lockDependencies = LoadLockDependencies();
            var installedVersions = LoadInstalledVersions();
            var filteredRepositoryPackages = FilterRepositoryPackages(repositoryPackages, m_prefixFilter);
            var filteredManifestDependencies = FilterStringDictionary(manifestDependencies, m_prefixFilter);
            var filteredLockDependencies = FilterLockDependencies(lockDependencies, m_prefixFilter);

            var reachable = ResolveReachablePackages(filteredManifestDependencies.Keys, filteredRepositoryPackages, filteredLockDependencies);
            var planEntries = new List<PackagePlanEntry>();
            var allNames = new HashSet<string>(filteredManifestDependencies.Keys, StringComparer.Ordinal);
            allNames.UnionWith(reachable);
            allNames.UnionWith(filteredRepositoryPackages.Keys);

            foreach (string packageName in allNames)
            {
                bool isDirect = filteredManifestDependencies.ContainsKey(packageName);
                bool isReachable = reachable.Contains(packageName);
                bool existsInRepository = filteredRepositoryPackages.TryGetValue(packageName, out RepositoryPackageInfo repositoryPackage);
                string currentValue = filteredManifestDependencies.TryGetValue(packageName, out string manifestValue) ? manifestValue : null;

                if (!isReachable)
                    continue;

                var entry = new PackagePlanEntry
                {
                    Name = packageName,
                    IsDirectManifestDependency = isDirect,
                    IsReachable = isReachable,
                    ExistsInRepository = existsInRepository,
                    IsCurrentlyRepositoryFile = IsManifestRepositoryFile(currentValue),
                    InstalledVersion = installedVersions.TryGetValue(packageName, out string installedVersion) ? installedVersion : string.Empty,
                    RepositoryVersion = existsInRepository ? repositoryPackage.Version : string.Empty,
                    RepositoryPath = existsInRepository ? repositoryPackage.DirectoryPath : string.Empty,
                    CurrentManifestValue = currentValue,
                    SourceLabel = BuildSourceLabel(currentValue, existsInRepository),
                    ActionLabel = BuildActionLabel(isReachable, existsInRepository, currentValue),
                    Notes = BuildNotes(isReachable, existsInRepository, filteredRepositoryPackages, filteredLockDependencies, packageName)
                };

                planEntries.Add(entry);
            }

            return planEntries;
        }

        private static string BuildSourceLabel(string currentValue, bool existsInRepository)
        {
            if (string.IsNullOrEmpty(currentValue))
                return existsInRepository ? "Current: not in manifest" : "Current: unavailable";

            if (IsManifestRepositoryFile(currentValue))
                return "Current: repository 'file:'";

            if (currentValue.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                currentValue.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                return "Current: git/url";

            return "Current: registry";
        }

        private static string BuildActionLabel(bool isReachable, bool existsInRepository, string currentValue)
        {
            if (!isReachable)
                return "Ignore";

            if (!existsInRepository)
                return "Keep Remote";

            if (string.IsNullOrEmpty(currentValue))
                return "Add 'file:'";

            if (currentValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                return "Refresh 'file:'";

            return "Replace 'file:'";
        }

        private static string BuildNotes(
            bool isReachable,
            bool existsInRepository,
            IReadOnlyDictionary<string, RepositoryPackageInfo> repositoryPackages,
            IReadOnlyDictionary<string, LockPackageInfo> lockDependencies,
            string packageName)
        {
            if (!isReachable)
                return "Unity package exists in the repository package root but is not currently required by this Unity project.";

            if (existsInRepository && repositoryPackages.TryGetValue(packageName, out RepositoryPackageInfo repositoryPackage))
                return $"Dependencies resolved from repository package.json ({repositoryPackage.Dependencies.Count} deps).";

            if (lockDependencies.TryGetValue(packageName, out LockPackageInfo lockPackage))
                return $"Reachable through packages-lock.json ({lockPackage.Dependencies.Count} deps), but no repository package folder was found.";

            return "Reachable from manifest dependencies, but no dependency metadata was found for deeper traversal.";
        }

        private static HashSet<string> ResolveReachablePackages(
            IEnumerable<string> roots,
            IReadOnlyDictionary<string, RepositoryPackageInfo> repositoryPackages,
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
                if (repositoryPackages.TryGetValue(packageName, out RepositoryPackageInfo repositoryPackage))
                    dependencyNames = repositoryPackage.Dependencies.Keys;
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

        private static Dictionary<string, RepositoryPackageInfo> ScanRepositoryPackages(string repositoryRootPath)
        {
            var results = new Dictionary<string, RepositoryPackageInfo>(StringComparer.Ordinal);
            foreach (string packageJsonPath in Directory.GetFiles(repositoryRootPath, "package.json", SearchOption.AllDirectories))
            {
                string directoryPath = Path.GetDirectoryName(packageJsonPath);
                if (string.IsNullOrEmpty(directoryPath))
                    continue;

                var packageRoot = JObject.Parse(File.ReadAllText(packageJsonPath));
                string packageName = (string)packageRoot["name"];
                string packageVersion = (string)packageRoot["version"];
                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                var dependenciesObject = packageRoot["dependencies"] as JObject;
                results[packageName] = new RepositoryPackageInfo
                {
                    Name = packageName,
                    Version = packageVersion,
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

        private static Dictionary<string, RepositoryPackageInfo> FilterRepositoryPackages(
            IReadOnlyDictionary<string, RepositoryPackageInfo> source,
            string prefixFilter)
        {
            return source
                .Where(kvp => MatchesPrefix(kvp.Key, prefixFilter))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new RepositoryPackageInfo
                    {
                        Name = kvp.Value.Name,
                        Version = kvp.Value.Version,
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

        private static bool IsManifestRepositoryFile(string currentValue)
        {
            return !string.IsNullOrEmpty(currentValue) &&
                   currentValue.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetResolvedVersionLabel(PackagePlanEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.InstalledVersion))
                return entry.InstalledVersion;

            return entry.IsReachable ? "(not installed)" : "(unknown)";
        }

        private static string GetRepositoryVersionLabel(PackagePlanEntry entry)
        {
            if (!entry.ExistsInRepository)
                return "(no repository package)";

            if (!string.IsNullOrWhiteSpace(entry.RepositoryVersion))
                return entry.RepositoryVersion;

            return "(missing version)";
        }

        private static bool VersionsDiffer(PackagePlanEntry entry)
        {
            if (!entry.ExistsInRepository || string.IsNullOrWhiteSpace(entry.RepositoryVersion) || string.IsNullOrWhiteSpace(entry.InstalledVersion))
                return false;

            return !string.Equals(entry.InstalledVersion, entry.RepositoryVersion, StringComparison.Ordinal);
        }

        private static Dictionary<string, string> LoadInstalledVersions()
        {
            var results = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                if (packages == null)
                    return results;

                foreach (var packageInfo in packages)
                {
                    if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.name))
                        continue;

                    results[packageInfo.name] = packageInfo.version ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to read installed package versions from Package Manager: {ex.Message}");
            }

            return results;
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
                if (!entry.IsReachable || !entry.ExistsInRepository || string.IsNullOrEmpty(entry.RepositoryPath))
                    continue;

                string fileReference = BuildFileReference(entry.RepositoryPath);
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
                string expectedValue = BuildFileReference(entry.RepositoryPath);
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
            if (string.IsNullOrWhiteSpace(m_repositoryRootPath))
                throw new InvalidOperationException("Repository package root is empty.");

            if (!Directory.Exists(m_repositoryRootPath))
                throw new DirectoryNotFoundException($"Repository package root does not exist: {m_repositoryRootPath}");

            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Could not find Packages/manifest.json", manifestPath);
        }

        private static string GetProjectRoot() => Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("Could not resolve Unity project root.");
        private static string GetDefaultRepositoryRoot() => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
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
