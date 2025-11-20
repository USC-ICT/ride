using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Unity Editor window for managing Asset Catalog Groups and viewing Build Summary data.
/// Allows drag-and-drop assignment of assets, group prefix path configuration, and build reviewing/triggering.
/// </summary>
public class AssetCatalogWindow : EditorWindow
{
    private enum Tab { Catalog, BuildSummary }
    private Tab m_currentTab = Tab.Catalog;

    private AssetCatalogProfile m_assetCatalogProfile;
    private BuildSummaryProfile m_buildSummaryProfile;

    private const string ASSET_CATALOG_DATA_PATH = "Assets/AssetCatalogData";
    private const string ASSET_CATALOG_PROFILE_NAME = "AssetCatalogProfile.asset";
    private static string BuildSummaryPath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "BuildSummaryProfile.json");
        }
    }
    private bool m_catalogIsDirty = false;
    private bool m_summaryIsDirty = false;

    private float m_nameColumnWidth = 200f;
    private float m_pathColumnWidth = 300f;
    private Vector2 m_scrollPosAssetList;
    private Vector2 m_scrollPosHistory;
    private Vector2 m_scrollPosJson;
    private bool m_isResizingNameColumn = false;
    private bool m_isResizingPathColumn = false;
    private Rect m_cursorRectName;
    private Rect m_cursorRectPath;

    private int m_selectedBuildIndex = -1;
    private bool m_buildSummaryExpanded = false;
    private List<bool> m_groupFoldouts = new();


    public AssetCatalogProfile AssetCatalogProfile => m_assetCatalogProfile;
    public bool CatalogIsDirty => m_catalogIsDirty;
    public void SetCatalogDirty() => m_catalogIsDirty = true;


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
        LoadOrCreatePersistentData();
    }

    /// <summary>
    /// Loads catalog and build summary profiles, or creates them if missing.
    /// </summary>
    private void LoadOrCreatePersistentData()
    {
        LoadOrCreateCatalogProfile();
        LoadOrCreateBuildSummaryProfile();
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
        m_assetCatalogProfile = AssetDatabase.LoadAssetAtPath<AssetCatalogProfile>(
            $"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
    }

    /// <summary>
    /// Helper method to load the Build Summary Profile from Application.persistentDataPath/BuildSummaryProfile.json. Creates this file if missing.
    /// </summary>
    private void LoadOrCreateBuildSummaryProfile()
    {
        if (File.Exists(BuildSummaryPath))
        {
            m_buildSummaryProfile = CreateInstance<BuildSummaryProfile>();
            string json = File.ReadAllText(BuildSummaryPath);
            JsonUtility.FromJsonOverwrite(json, m_buildSummaryProfile);
            if (m_buildSummaryProfile.builds.Count > 0)
                m_selectedBuildIndex = m_buildSummaryProfile.builds.Count - 1;
        }
        else
            m_buildSummaryProfile = CreateInstance<BuildSummaryProfile>();
    }

    private void CreatePersistentData()
    {
        if (!AssetDatabase.IsValidFolder(ASSET_CATALOG_DATA_PATH))
            AssetDatabase.CreateFolder("Assets", "AssetCatalogData");
        m_assetCatalogProfile = CreateInstance<AssetCatalogProfile>();
        AssetDatabase.CreateAsset(m_assetCatalogProfile, $"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        m_buildSummaryProfile = CreateInstance<BuildSummaryProfile>();
    }

    private void SavePersistentData(bool catalogChanged = false, bool summaryChanged = false)
    {
        if (catalogChanged && m_assetCatalogProfile != null)
            EditorUtility.SetDirty(m_assetCatalogProfile);
        if (catalogChanged)
            AssetDatabase.SaveAssets();
        if (summaryChanged && m_buildSummaryProfile != null)
        {
            string dir = Path.GetDirectoryName(BuildSummaryPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(m_buildSummaryProfile, true);
            File.WriteAllText(BuildSummaryPath, json);
        }
    }

    private void OnInspectorUpdate()
    {
        if (m_catalogIsDirty || m_summaryIsDirty)
        {
            SavePersistentData(m_catalogIsDirty, m_summaryIsDirty);
            m_catalogIsDirty = false;
            m_summaryIsDirty = false;
        }
    }

    /// <summary>
    /// Draws the toolbar and tabbed interface. intializes based on persistent data or asks the user to create
    /// this data using a help box and button to trigger the creation of the needed files to save data.
    /// </summary>
    private void OnGUI()
    {
        if (m_assetCatalogProfile == null || m_buildSummaryProfile == null)
        {
            EditorGUILayout.HelpBox("No AssetCatalogData found. Create it to persist asset and build info.", MessageType.Warning);
            if (GUILayout.Button("Create AssetCatalogData"))
            {
                CreatePersistentData();
                LoadOrCreatePersistentData();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        m_currentTab = (Tab)GUILayout.Toolbar((int)m_currentTab, new[] { "Assets", "Build Summary" }, EditorStyles.toolbarButton);
        EditorGUILayout.EndHorizontal();

        switch (m_currentTab)
        {
            case Tab.Catalog:
                DrawCatalogTab();
                break;
            case Tab.BuildSummary:
                DrawBuildSummaryTab();
                break;
        }
    }

    /// <summary>
    /// Draws the UI for editing asset groups and assets within each group.
    /// </summary>
    private void DrawCatalogTab()
    {
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

        bool allSelected = m_assetCatalogProfile.groups.All(g => g.includeInBuild);
        string toggleLabel = allSelected ? "Deselect All" : "Select All";
        if (GUILayout.Button(toggleLabel, GUILayout.Width(100)))
        {
            foreach (var group in m_assetCatalogProfile.groups)
                group.includeInBuild = !allSelected;
            m_catalogIsDirty = true;
        }

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
        if (GUILayout.Button("Build Catalog and Bundles"))
        {
            if (m_assetCatalogProfile.groups.Count > 0)
            {
                AssetCatalogUtils.BuildSelectedAssetGroups();
                LoadOrCreateBuildSummaryProfile();
                m_currentTab = Tab.BuildSummary;
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
        group.includeInBuild = EditorGUILayout.Toggle(group.includeInBuild, GUILayout.Width(16));
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
                group.localPrefixPath = AssetCatalogUtils.GenerateDefaultLocalPath(group);

            if (string.IsNullOrEmpty(group.remotePrefixPath))
                group.remotePrefixPath = AssetCatalogUtils.GenerateDefaultRemotePath(group);

            EditorGUILayout.BeginHorizontal();
            group.localPrefixPath = EditorGUILayout.TextField("Local Prefix Path", group.localPrefixPath);
            GUILayout.Label("/" + AssetCatalogUtils.GetBuildPostfixPath(), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            if (GUILayout.Button(EditorGUIUtility.IconContent("UndoHistory"), GUILayout.Width(24)))
            {
                group.localPrefixPath = AssetCatalogUtils.GenerateDefaultLocalPath(group);
                m_catalogIsDirty = true;
            }
            if (GUILayout.Button("📁", GUILayout.Width(24)))
                OpenInExplorer(group.localPrefixPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            group.remotePrefixPath = EditorGUILayout.TextField("Remote Prefix Path", group.remotePrefixPath);
            GUILayout.Label("/" + AssetCatalogUtils.GetBuildPostfixPath(), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            if (GUILayout.Button(EditorGUIUtility.IconContent("UndoHistory"), GUILayout.Width(24)))
            {
                group.remotePrefixPath = AssetCatalogUtils.GenerateDefaultRemotePath(group);
                m_catalogIsDirty = true;
            }
            GUILayout.Space(28);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                m_catalogIsDirty = true;
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

                    foreach (Object dragged in DragAndDrop.objectReferences)
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
                                Debug.LogWarning($"Asset with name '{draggedName}' already exists in group '{group.groupName}'");
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
    /// Draws the build summary tab showing build history, JSON snapshots, and per-asset metadata.
    /// </summary>
    private void DrawBuildSummaryTab()
    {
        if (m_buildSummaryProfile == null)
        {
            EditorGUILayout.HelpBox("No build summary data available.", MessageType.Info);
            return;
        }
        var builds = m_buildSummaryProfile.builds;

        if (builds.Count == 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Build History", EditorStyles.boldLabel);
            if (GUILayout.Button("📁", GUILayout.Width(24)))
                OpenInExplorer(Path.GetDirectoryName(BuildSummaryPath));
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(200));

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Build History", EditorStyles.boldLabel);
        if (GUILayout.Button("📁", GUILayout.Width(24)))
            OpenInExplorer(Path.GetDirectoryName(BuildSummaryPath));
        EditorGUILayout.EndHorizontal();
        m_scrollPosHistory = EditorGUILayout.BeginScrollView(m_scrollPosHistory);
        for (int i = builds.Count - 1; i >= 0; i--)
        {
            GUIStyle style = new(GUI.skin.box);
            if (i == m_selectedBuildIndex)
                style.normal.background = Texture2D.whiteTexture;

            if (GUILayout.Button($"Build {i + 1}\n{builds[i].timestamp}", style, GUILayout.ExpandWidth(true), GUILayout.Height(40)))
                m_selectedBuildIndex = i;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical();

        if (m_selectedBuildIndex < 0 || m_selectedBuildIndex >= builds.Count)
            m_selectedBuildIndex = builds.Count - 1;

        var selectedBuild = builds[m_selectedBuildIndex];

        m_buildSummaryExpanded = EditorGUILayout.Foldout(m_buildSummaryExpanded, "Show Catalog JSON");
        if (m_buildSummaryExpanded)
        {
            m_scrollPosJson = EditorGUILayout.BeginScrollView(m_scrollPosJson, GUILayout.Height(250), GUILayout.Width(position.width - 250));
            EditorGUILayout.TextArea(selectedBuild.catalogJsonSnapshot, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(m_nameColumnWidth));
        GUILayout.Label("Bundle Name", EditorStyles.boldLabel, GUILayout.Width(m_pathColumnWidth));
        GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Last Modified", EditorStyles.boldLabel, GUILayout.Width(150));
        GUILayout.Label("Size (bytes)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        foreach (var row in selectedBuild.summaryRows)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(row.assetName, GUILayout.Width(m_nameColumnWidth));
            GUILayout.Label(row.bundleName, GUILayout.Width(m_pathColumnWidth));
            GUILayout.Label(row.status, GUILayout.Width(100));
            GUILayout.Label(row.lastModified, GUILayout.Width(150));
            GUILayout.Label(row.sizeBytes.ToString());
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
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
            Object previousObj = entry.asset;
            Object selectedObj = EditorGUILayout.ObjectField(previousObj, typeof(Object), false, GUILayout.Width(m_nameColumnWidth));
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
                    LabelManagerWindow.ShowWindow(m_assetCatalogProfile.allLabels);
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
}

/// <summary>
/// Editor popup window for managing global asset labels.
/// Allows adding and removing labels used for filtering assets in the Asset Catalog.
/// </summary>
public class LabelManagerWindow : EditorWindow
{
    private List<string> _labels;
    private string _newLabel = "";

    /// <summary>
    /// Opens the label manager window and populates it with the current label list.
    /// </summary>
    /// <param name="labels">Reference to the list of global labels to manage.</param>
    public static void ShowWindow(List<string> labels)
    {
        var window = GetWindow<LabelManagerWindow>("Manage Labels");
        window._labels = labels;
        window.minSize = new Vector2(300, 200);
    }

    /// <summary>
    /// Draws the UI for editing, adding, and removing labels.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Global Labels", EditorStyles.boldLabel);

        for (int i = 0; i < _labels.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _labels[i] = EditorGUILayout.TextField(_labels[i]);

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                _labels.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        _newLabel = EditorGUILayout.TextField(_newLabel);

        if (GUILayout.Button("+", GUILayout.Width(30)) && !string.IsNullOrWhiteSpace(_newLabel))
        {
            if (!_labels.Contains(_newLabel))
                _labels.Add(_newLabel);
            _newLabel = "";
        }
        EditorGUILayout.EndHorizontal();
    }
}
