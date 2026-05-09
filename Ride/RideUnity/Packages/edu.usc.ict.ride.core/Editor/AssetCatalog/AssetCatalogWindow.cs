using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ride
{
/// <summary>
/// Unity Editor window for managing Asset Catalog Groups and viewing Build Summary data.
/// Allows drag-and-drop assignment of assets, group prefix path configuration, and build reviewing/triggering.
/// </summary>
public class AssetCatalogWindow : EditorWindow
{
    [Flags]
    public enum AssetCatalogBuildTargets
    {
        // Add more as needed.  See BuildTarget enum for options.
        None                = 0,
        StandaloneOSX       = 1 << 0,
        iOS                 = 1 << 1,
        Android             = 1 << 2,
        StandaloneWindows64 = 1 << 3,
        WebGL               = 1 << 4,
        StandaloneLinux64   = 1 << 5,
    }


    private AssetCatalogProfile m_assetCatalogProfile;

    private const string ASSET_CATALOG_DATA_PATH = "Assets/AssetCatalogData";
    private const string ASSET_CATALOG_PROFILE_NAME = "AssetCatalogProfile.asset";
    private bool m_catalogIsDirty = false;

    private float m_nameColumnWidth = 200f;
    private float m_pathColumnWidth = 300f;
    private Vector2 m_scrollPosAssetList;
    private bool m_isResizingNameColumn = false;
    private bool m_isResizingPathColumn = false;
    private Rect m_cursorRectName;
    private Rect m_cursorRectPath;

    private List<bool> m_groupFoldouts = new();

    private string m_artAssetSvnRevision = "0";
    private AssetCatalogBuildTargets m_selectedBuildTargets;


    /// <summary>
    /// Opens the Asset Catalog Editor window from the Unity Editor menu.
    /// </summary>
    [MenuItem("Ride/Asset Catalogs/Asset Catalog Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<AssetCatalogWindow>("Asset Catalog Editor");
        window.minSize = new Vector2(800, 500);
    }

    private void OnEnable()
    {
        LoadOrCreateCatalogProfile();

        m_artAssetSvnRevision = "0";
        m_selectedBuildTargets = MapBuildTargetToMask(EditorUserBuildSettings.activeBuildTarget);
    }

    private void OnDisable()
    {
        if (m_catalogIsDirty)
        {
            SavePersistentData(true);
            m_catalogIsDirty = false;
        }
    }

    /// <summary>
    /// Helper method to load the Asset Catalog Profile from Assets/AssetCatalogData/AssetCatalogProfile.asset. Creates this file if missing.
    /// </summary>
    private void LoadOrCreateCatalogProfile()
    {
        if (!AssetDatabase.IsValidFolder(ASSET_CATALOG_DATA_PATH))
        {
            m_assetCatalogProfile = null;
            return;
        }

        m_assetCatalogProfile = AssetDatabase.LoadAssetAtPath<AssetCatalogProfile>($"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
    }

    private void CreateCatalogProfile()
    {
        if (!AssetDatabase.IsValidFolder(ASSET_CATALOG_DATA_PATH))
            AssetDatabase.CreateFolder("Assets", "AssetCatalogData");

        m_assetCatalogProfile = CreateInstance<AssetCatalogProfile>();
        AssetDatabase.CreateAsset(m_assetCatalogProfile, $"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SavePersistentData(bool catalogChanged)
    {
        if (catalogChanged && m_assetCatalogProfile != null)
            EditorUtility.SetDirty(m_assetCatalogProfile);

        if (catalogChanged)
            AssetDatabase.SaveAssets();
    }

    private void OnInspectorUpdate()
    {
        if (m_catalogIsDirty)
        {
            SavePersistentData(m_catalogIsDirty);
            m_catalogIsDirty = false;
        }
    }

    /// <summary>
    /// Draws the toolbar and tabbed interface. intializes based on persistent data or asks the user to create
    /// this data using a help box and button to trigger the creation of the needed files to save data.
    /// </summary>
    private void OnGUI()
    {
        DrawCatalogTab();
    }

    /// <summary>
    /// Draws the UI for editing asset groups and assets within each group.
    /// </summary>
    private void DrawCatalogTab()
    {
        if (m_assetCatalogProfile == null)
        {
            EditorGUILayout.HelpBox("No AssetCatalogData found. Create it to persist asset and build info.", MessageType.Warning);
            if (GUILayout.Button("Create AssetCatalogData"))
            {
                CreateCatalogProfile();
                LoadOrCreateCatalogProfile();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Asset Catalog Groups", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ Add New Asset Group", GUILayout.Width(180)))
        {
            int count = m_assetCatalogProfile.groups.Count(g => g.groupName.StartsWith("New Asset Group"));
            string newGroupName = count == 0 ? "New Asset Group" : $"New Asset Group {count + 1}";
            while (m_assetCatalogProfile.groups.Any(g => g.groupName == newGroupName))
            {
                count++;
                newGroupName = $"New Asset Group {count + 1}";
            }
            m_assetCatalogProfile.groups.Add(new AssetCatalogGroup { groupName = newGroupName, includeInBuild = true });
            m_groupFoldouts.Add(true);
            m_catalogIsDirty = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        bool allSelected = m_assetCatalogProfile.groups.All(g => g.includeInBuild);
        string toggleLabel = allSelected ? "Deselect All" : "Select All";
        if (GUILayout.Button(toggleLabel, GUILayout.Width(100)))
        {
            foreach (var group in m_assetCatalogProfile.groups)
                group.includeInBuild = !allSelected;
            m_catalogIsDirty = true;
        }

        if (GUILayout.Button("Expand All", GUILayout.Width(120)))
        {
            for (int i = 0; i < m_groupFoldouts.Count; i++)
                m_groupFoldouts[i] = true;
        }

        if (GUILayout.Button("Collapse All", GUILayout.Width(120)))
        {
            for (int i = 0; i < m_groupFoldouts.Count; i++)
                m_groupFoldouts[i] = false;
        }
        EditorGUILayout.EndHorizontal();

        if (m_assetCatalogProfile.groups.Count != m_groupFoldouts.Count)
        {
            while (m_groupFoldouts.Count < m_assetCatalogProfile.groups.Count)
                m_groupFoldouts.Add(false);
        }

        m_scrollPosAssetList = EditorGUILayout.BeginScrollView(m_scrollPosAssetList);
        for (int i = 0; i < m_assetCatalogProfile.groups.Count; i++)
            DrawAssetGroupUI(i);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                m_artAssetSvnRevision = AssetCatalogEditorUtility.GetProjectSvnLastChangedRevision();

            EditorGUILayout.LabelField("Catalog Version Info", EditorStyles.boldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("rideBundleVersion", GUILayout.Width(120));
            EditorGUILayout.SelectableLabel(AssetCatalogData.RIDE_VERSION, GUILayout.Width(30), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("--", GUILayout.Width(20));
            EditorGUILayout.LabelField("artAssetVersion", GUILayout.Width(110));
            EditorGUILayout.SelectableLabel(m_artAssetSvnRevision, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Query Remote Paths...", GUILayout.Width(160)))
                AssetCatalogRemoteQueryWindow.ShowWindow(m_assetCatalogProfile);
        }

        EditorGUILayout.Space();
        m_selectedBuildTargets = (AssetCatalogBuildTargets)EditorGUILayout.EnumFlagsField("Build Targets", m_selectedBuildTargets);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Catalog and Bundles"))
        {
            if (m_assetCatalogProfile.groups.Count == 0)
            {
                Debug.LogWarning("[AssetCatalogWindow] No asset groups defined.");
            }
            else
            {
                var targets = GetSelectedBuildTargets(m_selectedBuildTargets);
                if (targets.Count == 0)
                {
                    Debug.LogWarning("[AssetCatalogWindow] No build targets selected.");
                }
                else
                {
                    int index = 0;

                    void Build()
                    {
                        if (index >= targets.Count)
                        {
                            EditorApplication.update -= Build;  // finished all targets
                            return;
                        }

                        var target = targets[index++];

                        Debug.Log($"[AssetCatalogWindow] Building for: {target}");

                        AssetCatalogEditorUtility.BuildSelectedAssetGroups(target, verboseLogging: true);

                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();  // Force console update
                    }

                    EditorApplication.update += Build;
                }
            }
        }
    }

    /// <summary>
    /// Helper method to navigate to the local prefix path folder on button press.
    /// </summary>
    /// <param name="path">The path to open the explorer/finder to.</param>
    private void OpenInExplorer(string path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
            EditorUtility.RevealInFinder(path);
#else
            Debug.LogWarning("Opening folder not supported on this platform.");
#endif
        else
            Debug.LogWarning($"Path does not exist: {path}");
    }

    /// <summary>
    /// Draws a single group in the editor UI, including asset assignment and label menus.
    /// </summary>
    /// <param name="index">Index of the group in the profile.</param>
    private void DrawAssetGroupUI(int index)
    {
        var group = m_assetCatalogProfile.groups[index];
        Rect groupStart = GUILayoutUtility.GetRect(0, 0);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        bool oldIncludeInBuild = group.includeInBuild;
        bool newIncludeInBuild = EditorGUILayout.Toggle(oldIncludeInBuild, GUILayout.Width(16));
        if (newIncludeInBuild != oldIncludeInBuild)
        {
            group.includeInBuild = newIncludeInBuild;
            m_catalogIsDirty = true;
            // optional: SavePersistentData(true);  // if you want immediate save
        }
        m_groupFoldouts[index] = EditorGUILayout.Foldout(m_groupFoldouts[index], group.groupName, true);

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            m_assetCatalogProfile.groups.RemoveAt(index);
            m_groupFoldouts.RemoveAt(index);
            m_catalogIsDirty = true;
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();

        if (m_groupFoldouts[index])
        {
            EditorGUI.BeginChangeCheck();
            string oldName = group.groupName;
            string newName = EditorGUILayout.TextField("Group Name", oldName);
            if (newName != oldName)
            {
                bool nameExists = m_assetCatalogProfile.groups
                    .Where((_, i) => i != index)
                    .Any(g => g.groupName == newName);

                if (nameExists)
                    EditorGUILayout.HelpBox("Group name must be unique.", MessageType.Error);
                else
                {
                    group.groupName = newName;
                    m_catalogIsDirty = true;
                }
            }
            if (string.IsNullOrEmpty(group.localPrefixPath))
                group.localPrefixPath = AssetCatalogUtility.GenerateDefaultLocalPath(group.groupName);

            if (string.IsNullOrEmpty(group.remotePrefixPath))
                group.remotePrefixPath = AssetCatalogUtility.GenerateDefaultRemotePath(group.groupName);

            EditorGUILayout.BeginHorizontal();
            group.localPrefixPath = EditorGUILayout.TextField("Local Prefix Path", group.localPrefixPath);
            GUILayout.Label("/" + AssetCatalogEditorUtility.GetBuildPostfixPath(), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            GUIContent resetLocalContent = new GUIContent(
                EditorGUIUtility.IconContent("UndoHistory").image,
                "Reset the Local Prefix Path to the default generated location for this group.");
            if (GUILayout.Button(resetLocalContent, GUILayout.Width(24)))
            {
                group.localPrefixPath = AssetCatalogUtility.GenerateDefaultLocalPath(group.groupName);
                m_catalogIsDirty = true;
            }
            GUIContent folderContent = new GUIContent(
                EditorGUIUtility.IconContent("Folder Icon").image,
                "Open this Local Prefix Path in the system file browser.");
            if (GUILayout.Button(folderContent, GUILayout.Width(24), GUILayout.Height(20)))
                OpenInExplorer(group.localPrefixPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            group.remotePrefixPath = EditorGUILayout.TextField("Remote Prefix Path", group.remotePrefixPath);
            GUILayout.Label("/" + AssetCatalogEditorUtility.GetBuildPostfixPath(), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            GUIContent resetRemoteContent = new GUIContent(
                EditorGUIUtility.IconContent("UndoHistory").image,
                "Reset the Remote Prefix Path to the default generated location for this group.");
            if (GUILayout.Button(resetRemoteContent, GUILayout.Width(24)))
            {
                group.remotePrefixPath = AssetCatalogUtility.GenerateDefaultRemotePath(group.groupName);
                m_catalogIsDirty = true;
            }
            GUILayout.Space(28);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                m_catalogIsDirty = true;
                SavePersistentData(true);
            }
            DrawGroupAssetSection(group);
        }

        EditorGUILayout.EndVertical();

        Rect groupEnd = GUILayoutUtility.GetLastRect();
        Rect dropRect = new Rect(groupStart.x, groupStart.y, position.width - 40, groupEnd.yMax - groupStart.y);
        HandleDragAndDropIntoGroup(group, dropRect);
    }

    /// <summary>
    /// Handles drag-and-drop operations into a specific group box.
    /// </summary>
    /// <param name="group">The group to add assets to.</param>
    /// <param name="dropArea">UI rect representing the drop target area.</param>
    private void HandleDragAndDropIntoGroup(AssetCatalogGroup group, Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
            return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is GameObject go &&
                            PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                        {
                            string draggedName = dragged.name;
                            if (!group.assets.Exists(e => e.asset != null && e.asset.name == draggedName))
                            {
                                group.assets.Add(new LoadableAsset { asset = dragged });
                                m_catalogIsDirty = true;
                            }
                            else
                            {
                                Debug.LogWarning($"Asset with name '{draggedName}' already exists in group '{group.groupName}'");
                            }
                        }
                    }
                    evt.Use();
                }
                Color overlay = new Color(0.3f, 0.6f, 1f, 0.2f);
                EditorGUI.DrawRect(dropArea, overlay);
                break;
        }
    }


    /// <summary>
    /// Draws the asset table column headers with adjustable width.
    /// </summary>
    private void DrawColumnHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(m_nameColumnWidth));
        GUILayout.Label("Path", EditorStyles.boldLabel, GUILayout.Width(m_pathColumnWidth));
        GUILayout.Label("Labels", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        Rect headerRect = GUILayoutUtility.GetLastRect();

        m_cursorRectName = new Rect(headerRect.x + m_nameColumnWidth - 2, headerRect.y, 4, headerRect.height);
        m_cursorRectPath = new Rect(headerRect.x + m_nameColumnWidth + m_pathColumnWidth - 2, headerRect.y, 4, headerRect.height);

        EditorGUIUtility.AddCursorRect(m_cursorRectName, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(m_cursorRectName, new Color(0.3f, 0.3f, 0.3f, 1f));

        EditorGUIUtility.AddCursorRect(m_cursorRectPath, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(m_cursorRectPath, new Color(0.3f, 0.3f, 0.3f, 1f));
    }

    /// <summary>
    /// Handles mouse drag interactions for resizing table columns.
    /// </summary>
    private void HandleColumnResize()
    {
        Event evt = Event.current;

        if (evt.type == EventType.MouseDown)
        {
            if (m_cursorRectName.Contains(evt.mousePosition)) m_isResizingNameColumn = true;
            if (m_cursorRectPath.Contains(evt.mousePosition)) m_isResizingPathColumn = true;
        }
        if (evt.type == EventType.MouseUp)
        {
            m_isResizingNameColumn = false;
            m_isResizingPathColumn = false;
        }
        if (evt.type == EventType.MouseDrag)
        {
            if (m_isResizingNameColumn)
            {
                m_nameColumnWidth = Mathf.Clamp(evt.mousePosition.x, 100f, position.width - m_pathColumnWidth - 100f);
                Repaint();
            }
            if (m_isResizingPathColumn)
            {
                m_pathColumnWidth = Mathf.Clamp(evt.mousePosition.x - m_nameColumnWidth, 100f, position.width - m_nameColumnWidth - 100f);
                Repaint();
            }
        }
    }

    /// <summary>
    /// Draws the section that displays assets within a selected group, with label menus and removal buttons.
    /// </summary>
    /// <param name="group">The asset group being drawn.</param>
    private void DrawGroupAssetSection(AssetCatalogGroup group)
    {
        EditorGUILayout.Space();
        DrawColumnHeader();
        HandleColumnResize();
        EditorGUILayout.BeginVertical();

        int? removeIndex = null;
        foreach (var entry in group.assets)
        {
            EditorGUILayout.BeginHorizontal("box");
            var previousObj = entry.asset;
            var selectedObj = EditorGUILayout.ObjectField(previousObj, typeof(UnityEngine.Object), false, GUILayout.Width(m_nameColumnWidth));
            if (selectedObj != previousObj)
            {
                string selectedName = selectedObj != null ? selectedObj.name : "";
                bool duplicate = group.assets
                    .Any(e => e != entry && e.asset != null && e.asset.name == selectedName);
                if (duplicate)
                    Debug.LogWarning($"Asset '{selectedName}' already exists in this group. Selection rejected.");
                else
                {
                    entry.asset = selectedObj;
                    m_catalogIsDirty = true;
                }
            }

            if (entry.assetPath == null && entry.asset != null)
                entry.assetPath = AssetDatabase.GetAssetPath(entry.asset);

            GUILayout.Label(entry.assetPath, GUILayout.Width(m_pathColumnWidth));

            GUILayout.BeginVertical();
            string labelSummary = entry.labels.Count > 0 ? string.Join(", ", entry.labels) : "Assign Labels";
            if (EditorGUI.DropdownButton(GUILayoutUtility.GetRect(200, 20), new GUIContent(labelSummary), FocusType.Keyboard))
            {
                GenericMenu menu = new();
                foreach (var label in m_assetCatalogProfile.allLabels)
                {
                    bool selected = entry.labels.Contains(label);
                    menu.AddItem(new GUIContent(label), selected, () =>
                    {
                        if (selected)
                            entry.labels.Remove(label);
                        else
                            entry.labels.Add(label);
                        m_catalogIsDirty = true;
                        Repaint();
                    });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Manage Labels..."), false, () =>
                {
                    LabelManagerWindow.ShowWindow(m_assetCatalogProfile);
                });
                menu.ShowAsContext();
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("X", GUILayout.Width(20)))
                removeIndex = group.assets.IndexOf(entry);

            EditorGUILayout.EndHorizontal();
        }

        int phantomRows = Mathf.Max(4 - group.assets.Count, 4);
        float phantomHeight = phantomRows * 22f;
        GUILayout.Space(phantomHeight);
        Rect dropArea = GUILayoutUtility.GetLastRect();
        HandleDragAndDropIntoGroup(group, dropArea);
        EditorGUILayout.EndVertical();

        if (removeIndex.HasValue && removeIndex.Value >= 0)
        {
            group.assets.RemoveAt(removeIndex.Value);
            m_catalogIsDirty = true;
            GUIUtility.ExitGUI();
        }
    }

    private static AssetCatalogBuildTargets MapBuildTargetToMask(BuildTarget target)
    {
        return target switch
        {
            BuildTarget.StandaloneOSX       => AssetCatalogBuildTargets.StandaloneOSX,
            BuildTarget.iOS                 => AssetCatalogBuildTargets.iOS,
            BuildTarget.Android             => AssetCatalogBuildTargets.Android,
            BuildTarget.StandaloneWindows64 => AssetCatalogBuildTargets.StandaloneWindows64,
            BuildTarget.WebGL               => AssetCatalogBuildTargets.WebGL,
            BuildTarget.StandaloneLinux64   => AssetCatalogBuildTargets.StandaloneLinux64,
            _                               => AssetCatalogBuildTargets.None
        };
    }

    public static List<BuildTarget> GetSelectedBuildTargets(AssetCatalogBuildTargets mask)
    {
        List<BuildTarget> outList = new();
        if ((mask & AssetCatalogBuildTargets.StandaloneOSX) != 0) outList.Add(BuildTarget.StandaloneOSX);
        if ((mask & AssetCatalogBuildTargets.iOS) != 0) outList.Add(BuildTarget.iOS);
        if ((mask & AssetCatalogBuildTargets.Android) != 0) outList.Add(BuildTarget.Android);
        if ((mask & AssetCatalogBuildTargets.StandaloneWindows64) != 0) outList.Add(BuildTarget.StandaloneWindows64);
        if ((mask & AssetCatalogBuildTargets.WebGL) != 0) outList.Add(BuildTarget.WebGL);
        if ((mask & AssetCatalogBuildTargets.StandaloneLinux64) != 0) outList.Add(BuildTarget.StandaloneLinux64);
        return outList;
    }
}


/// <summary>
/// Editor popup window for managing global asset labels.
/// Allows adding and removing labels used for filtering assets in the Asset Catalog.
/// </summary>
public class LabelManagerWindow : EditorWindow
{
    private AssetCatalogProfile m_profile;
    private string m_newLabel = "";

    /// <summary>
    /// Opens the label manager window and populates it with the current label list.
    /// </summary>
    /// <param name="labels">Reference to the list of global labels to manage.</param>
    public static void ShowWindow(AssetCatalogProfile profile)
    {
        var window = GetWindow<LabelManagerWindow>("Manage Labels");
        window.m_profile = profile;
        window.minSize = new Vector2(300, 200);
    }

    /// <summary>
    /// Draws the UI for editing, adding, and removing labels.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Global Labels", EditorStyles.boldLabel);

        for (int i = 0; i < m_profile.allLabels.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var newLabel = EditorGUILayout.TextField(m_profile.allLabels[i]);
            if (newLabel != m_profile.allLabels[i])
            {
                m_profile.allLabels[i] = newLabel;
                SaveProfile();
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                m_profile.allLabels.RemoveAt(i);
                i--;

                SaveProfile();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        m_newLabel = EditorGUILayout.TextField(m_newLabel);

        if (GUILayout.Button("+", GUILayout.Width(30)) && !string.IsNullOrWhiteSpace(m_newLabel))
        {
            if (!m_profile.allLabels.Contains(m_newLabel))
            {
                m_profile.allLabels.Add(m_newLabel);
                SaveProfile();
            }

            m_newLabel = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    private void SaveProfile()
    {
        if (m_profile != null)
        {
            EditorUtility.SetDirty(m_profile);
            AssetDatabase.SaveAssets();
        }
    }
}


/// <summary>
/// Dialog window that queries remote catalog.json files via aws cli for sanity checking.
/// </summary>
class AssetCatalogRemoteQueryWindow : EditorWindow
{
    private sealed class Result
    {
        public string groupName;
        public string renderPipeline;
        public string platform;
        public AssetCatalogData assetCatalogData;
        public bool ok;
        public string error;
        public string remoteCatalogPath;
    }

    // see AssetCatalogUtility.GetRenderPipelineName().
    private static readonly string[] renderPipelines = { "BuiltIn", "URP", "HDRP" };

    private AssetCatalogProfile m_profile;
    private Vector2 m_scroll;
    private readonly List<Result> m_results = new();

    public static void ShowWindow(AssetCatalogProfile profile)
    {
        var w = GetWindow<AssetCatalogRemoteQueryWindow>("Query Remote Catalogs");
        w.minSize = new Vector2(900, 500);
        w.m_profile = profile;
    }

    private void OnGUI()
    {
        if (m_profile == null)
        {
            EditorGUILayout.HelpBox("No AssetCatalogProfile provided.", MessageType.Error);
            if (GUILayout.Button("Close")) Close();
            return;
        }

        EditorGUILayout.LabelField("Remote Catalog Sanity Check", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Downloads catalog.json for each group/platform/render-pipeline using the AWS CLI (aws s3 cp). " +
            "This does not use the runtime loading system. It is a sanity check only.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Query", GUILayout.Width(120)))
                RunQuery();

            if (GUILayout.Button("Clear", GUILayout.Width(80)))
                m_results.Clear();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", GUILayout.Width(100)))
                Close();
        }

        EditorGUILayout.Space(8);

        DrawResultsTable();
    }

    private void DrawResultsTable()
    {
        if (m_results.Count == 0)
        {
            EditorGUILayout.HelpBox("No results yet. Click 'Run Query'.", MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Group", EditorStyles.boldLabel, GUILayout.Width(180));
            GUILayout.Label("Pipeline", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Platform", EditorStyles.boldLabel, GUILayout.Width(140));
            GUILayout.Label("rideBundleVersion", EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label("artAssetVersion", EditorStyles.boldLabel, GUILayout.Width(110));
            GUILayout.Label("OK", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.Label("Remote catalog path / Error", EditorStyles.boldLabel);
        }

        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
        foreach (var r in m_results)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUILayout.Label(r.groupName ?? "", GUILayout.Width(180));
                GUILayout.Label(r.renderPipeline ?? "", GUILayout.Width(80));
                GUILayout.Label(r.platform ?? "", GUILayout.Width(140));
                GUILayout.Label(r.assetCatalogData?.rideBundleVersion ?? "", GUILayout.Width(130));
                GUILayout.Label(r.assetCatalogData?.artAssetVersion ?? "0", GUILayout.Width(110));
                GUILayout.Label(r.ok ? "Y" : "N", GUILayout.Width(30));

                string tail = r.ok ? (r.remoteCatalogPath ?? "") : (r.error ?? r.remoteCatalogPath ?? "");
                EditorGUILayout.SelectableLabel(tail, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void RunQuery()
    {
        m_results.Clear();

        string unityFolder = AssetCatalogUtility.GetCompatibleUnityVersionName();
        if (string.IsNullOrEmpty(unityFolder))
            unityFolder = Application.unityVersion;

        // get all targets
        List<BuildTarget> buildTargets = AssetCatalogWindow.GetSelectedBuildTargets((AssetCatalogWindow.AssetCatalogBuildTargets)(-1));

        // Put downloads in a unique temp folder for this run.
        string tempRoot = Path.Combine(Path.GetTempPath(), "ride_asset_catalog_query", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(tempRoot);

        int total = m_profile.groups.Count * renderPipelines.Length * buildTargets.Count;
        int done = 0;

        try
        {
            foreach (var group in m_profile.groups)
            {
                if (group == null)
                    continue;

                string groupRemotePrefix = group.remotePrefixPath;
                foreach (string renderPipeline in renderPipelines)
                {
                    foreach (var buildTarget in buildTargets)
                    {
                        done++;
                        EditorUtility.DisplayProgressBar(
                            "Query Remote Catalogs",
                            $"{group.groupName} / {renderPipeline} / {buildTarget}",
                            total > 0 ? (float)done / total : 1f);

                        string platformFolder = buildTarget.ToString();
                        string postfix = CombineRemotePath(unityFolder, renderPipeline, platformFolder);
                        string remoteFolder = CombineRemotePath(groupRemotePrefix, postfix);
                        string remoteCatalogPath = CombineRemotePath(remoteFolder, "catalog.json");

                        string localDest = Path.Combine(tempRoot, SanitizeFileName(group.groupName), renderPipeline, platformFolder, "catalog.json");

                        var result = new Result
                        {
                            groupName = group.groupName,
                            renderPipeline = renderPipeline,
                            platform = buildTarget.ToString(),
                            ok = false,
                            error = null,
                            remoteCatalogPath = remoteCatalogPath
                        };

                        if (!TryAwsS3CopyToLocal(remoteCatalogPath, localDest, out string err))
                        {
                            result.error = string.IsNullOrEmpty(err) ? "aws s3 cp failed." : err.Trim();
                            m_results.Add(result);
                            continue;
                        }

                        try
                        {
                            string json = File.ReadAllText(localDest);
                            var catalog = JsonUtility.FromJson<AssetCatalogData>(json);

                            result.ok = catalog != null;
                            result.assetCatalogData = catalog;
                        }
                        catch (Exception ex)
                        {
                            result.error = ex.Message;
                            result.ok = false;
                        }

                        m_results.Add(result);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string CombineRemotePath(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
            return string.Empty;

        string s = parts[0] ?? string.Empty;
        for (int i = 1; i < parts.Length; i++)
        {
            string p = parts[i] ?? string.Empty;
            if (string.IsNullOrEmpty(p))
                continue;

            if (s.EndsWith("/"))
                s = s.TrimEnd('/');
            p = p.TrimStart('/');
            s = s + "/" + p;
        }

        return s;
    }

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "unnamed";

        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }


    /// <summary>
    /// Runs an 'aws s3 cp' to download a file from S3 to the local filesystem.
    /// Returns true on success. On failure, returns false and fills error.
    /// </summary>
    public static bool TryAwsS3CopyToLocal(string s3SourcePath, string localDestPath, out string error)
    {
        error = null;

        if (string.IsNullOrEmpty(s3SourcePath) || string.IsNullOrEmpty(localDestPath))
        {
            error = "Invalid source or destination path.";
            return false;
        }

        // Ensure S3 URI starts with s3://
        string normalizedSource = NormalizeToS3Uri(s3SourcePath);

        if (!normalizedSource.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Invalid S3 URI: {s3SourcePath}";
            return false;
        }

        string destDir = Path.GetDirectoryName(localDestPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        string args = $"s3 cp \"{normalizedSource}\" \"{localDestPath}\"";

        if (!AssetCatalogEditorUtility.TryRunProcess("aws", args, workingDirectory: null, out string stdout, out string stderr))
        {
            error = string.IsNullOrEmpty(stderr) ? stdout : stderr;
            return false;
        }

        if (!File.Exists(localDestPath))
        {
            error = "aws s3 cp reported success, but destination file was not found.";
            return false;
        }

        return true;
    }

    private static string NormalizeToS3Uri(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        path = path.Replace("\\", "/").Trim();

        // Already valid
        if (path.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
            return path;

        // Remove https style URLs
        if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Convert https://s3.amazonaws.com/bucket/key
            var uri = new Uri(path);
            if (uri.Host.Contains("amazonaws.com") && uri.AbsolutePath.Length > 1)
            {
                return "s3://" + uri.AbsolutePath.TrimStart('/');
            }
        }

        // Assume bucket/key format
        return "s3://" + path.TrimStart('/');
    }
}
}
