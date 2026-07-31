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
    private readonly Dictionary<string, bool> m_attributeEditorExpanded = new();
    private readonly Dictionary<string, List<AssetCatalogAttribute>> m_attributeDisplayOrder = new();
    private readonly HashSet<AssetCatalogAttribute> m_customAttributeKeys = new();
    private readonly HashSet<AssetCatalogAttribute> m_customAttributeValues = new();
    private readonly Dictionary<string, List<string>> m_suggestedAttributeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        { "category", new List<string> { "character", "vehicle", "building", "prop" } },
        { "age", new List<string> { } },
        { "ageGroup", new List<string> { "child", "young", "middle", "older" } },
        { "gender", new List<string> { "male", "female", "ambiguous" } },
        { "ethnicity", new List<string> { "white", "africanDescent", "asian", "latino", "middleEastern" } },
        { "clothing", new List<string> { "casual", "formal", "uniform", "workwear", "military" } },
        { "role", new List<string> { "civilian", "soldier", "medic", "student", "parent" } },
    };

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
        m_attributeDisplayOrder.Clear();

        if (!AssetDatabase.IsValidFolder(ASSET_CATALOG_DATA_PATH))
        {
            m_assetCatalogProfile = null;
            return;
        }

        m_assetCatalogProfile = AssetDatabase.LoadAssetAtPath<AssetCatalogProfile>($"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
        RebuildAttributeDisplayOrderCache();
    }

    private void CreateCatalogProfile()
    {
        if (!AssetDatabase.IsValidFolder(ASSET_CATALOG_DATA_PATH))
            AssetDatabase.CreateFolder("Assets", "AssetCatalogData");

        m_assetCatalogProfile = CreateInstance<AssetCatalogProfile>();
        AssetDatabase.CreateAsset(m_assetCatalogProfile, $"{ASSET_CATALOG_DATA_PATH}/{ASSET_CATALOG_PROFILE_NAME}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RebuildAttributeDisplayOrderCache();
    }

    private void SavePersistentData(bool catalogChanged)
    {
        if (catalogChanged && m_assetCatalogProfile != null)
            EditorUtility.SetDirty(m_assetCatalogProfile);

        if (catalogChanged)
            AssetDatabase.SaveAssets();
    }

    private void ExportAssetCatalogProfileJson()
    {
        if (m_assetCatalogProfile == null)
            return;

        string profileAssetPath = AssetDatabase.GetAssetPath(m_assetCatalogProfile);
        string defaultDirectory = GetAbsoluteDirectory(profileAssetPath);
        string projectName = string.IsNullOrWhiteSpace(Application.productName) ? Path.GetFileNameWithoutExtension(profileAssetPath) : Application.productName;
        string defaultFileName = $"{projectName}-AssetCatalog.json";
        string outputPath = EditorUtility.SaveFilePanel("Export Asset Catalog JSON", defaultDirectory, defaultFileName, "json");
        if (string.IsNullOrWhiteSpace(outputPath))
            return;

        string json = EditorJsonUtility.ToJson(m_assetCatalogProfile, true).Replace("\n", Environment.NewLine);
        File.WriteAllText(outputPath, json);
        AssetDatabase.Refresh();
        Debug.Log($"Exported asset catalog profile JSON to {outputPath}", m_assetCatalogProfile);
    }

    private static string GetAbsoluteDirectory(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return Directory.GetCurrentDirectory();

        string absolutePath = Path.GetFullPath(assetPath);
        string directory = Path.GetDirectoryName(absolutePath);
        return string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory;
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
            if (GUILayout.Button("Export Catalog JSON...", GUILayout.Width(140)))
                ExportAssetCatalogProfileJson();
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
    /// Draws the section that displays assets within a selected group, including labels and generic key/value attributes.
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
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            var previousObj = entry.asset;
            var selectedObj = EditorGUILayout.ObjectField(previousObj, typeof(UnityEngine.Object), false, GUILayout.Width(m_nameColumnWidth));
            if (selectedObj != previousObj)
            {
                string selectedName = selectedObj != null ? selectedObj.name : string.Empty;
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

            DrawAttributeEditor(group, entry);

            EditorGUILayout.EndVertical();
        }

        int phantomRows = Mathf.Max(2 - group.assets.Count, 1);
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

    private void DrawAttributeEditor(AssetCatalogGroup group, LoadableAsset entry)
    {
        if (entry.attributes == null)
            entry.attributes = new List<AssetCatalogAttribute>();

        string stateKey = GetAttributeStateKey(group, entry);
        if (!m_attributeEditorExpanded.ContainsKey(stateKey))
            m_attributeEditorExpanded[stateKey] = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string newDescription = EditorGUILayout.TextField("Description", entry.description ?? string.Empty);
            if (newDescription != (entry.description ?? string.Empty))
            {
                entry.description = newDescription;
                m_catalogIsDirty = true;
            }

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                Rect foldoutRect = GUILayoutUtility.GetRect(90f, EditorGUIUtility.singleLineHeight, GUILayout.Width(90f));
                m_attributeEditorExpanded[stateKey] = EditorGUI.Foldout(foldoutRect, m_attributeEditorExpanded[stateKey], "Attributes", true);
                EditorGUILayout.SelectableLabel(
                    GetAttributeSummary(entry.attributes),
                    EditorStyles.miniLabel,
                    GUILayout.MinWidth(120f),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            if (!m_attributeEditorExpanded[stateKey])
                return;

            // Reserved for future grouped suggestion sets once we have enough curated presets
            // to justify a dedicated UI. Keeping the block here makes it easy to re-enable later.
            /*
            using (new EditorGUI.DisabledScope(true))
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Suggestion Sets", EditorStyles.miniBoldLabel, GUILayout.Width(88));
                GUILayout.Button("Common", EditorStyles.miniButton, GUILayout.Width(68));
                GUILayout.Button("VH", EditorStyles.miniButton, GUILayout.Width(44));
                GUILayout.Button("Units", EditorStyles.miniButton, GUILayout.Width(52));
                GUILayout.Button("Buildings", EditorStyles.miniButton, GUILayout.Width(72));
                GUILayout.FlexibleSpace();
            }
            */

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Quick Add", EditorStyles.miniBoldLabel, GUILayout.Width(58));
                if (GUILayout.Button("+ Custom", EditorStyles.miniButton, GUILayout.Width(70)))
                    AddAttribute(entry);

                foreach (string suggestedKey in m_suggestedAttributeValues.Keys)
                {
                    string buttonLabel = ObjectNames.NicifyVariableName(suggestedKey);
                    if (GUILayout.Button(buttonLabel, EditorStyles.miniButton))
                        AddAttribute(entry, suggestedKey);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4f);

            if (entry.attributes.Count == 0)
            {
                EditorGUILayout.HelpBox("No attributes yet. Use Quick Add for common fields, or start with a fully custom key/value pair.", MessageType.Info);
            }

            List<AssetCatalogAttribute> displayAttributes = GetDisplayAttributes(stateKey, entry.attributes);
            int? removeIndex = null;
            for (int i = 0; i < displayAttributes.Count; i++)
            {
                var attribute = displayAttributes[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawAttributeKeyField(attribute);
                    GUILayout.Label("=", GUILayout.Width(10));
                    DrawAttributeValueField(attribute);
                    if (GUILayout.Button("X", GUILayout.Width(20)) && entry.attributes != null)
                        removeIndex = entry.attributes.IndexOf(attribute);
                }
            }

            if (removeIndex.HasValue)
            {
                m_customAttributeKeys.Remove(entry.attributes[removeIndex.Value]);
                m_customAttributeValues.Remove(entry.attributes[removeIndex.Value]);
                entry.attributes.RemoveAt(removeIndex.Value);
                m_catalogIsDirty = true;
            }
        }
    }

    private void AddAttribute(LoadableAsset entry, string key = "", string value = "")
    {
        entry.attributes.Add(new AssetCatalogAttribute { key = key, value = value });
        m_catalogIsDirty = true;
    }

    private void DrawAttributeKeyField(AssetCatalogAttribute attribute)
    {
        List<string> suggestedKeys = m_suggestedAttributeValues.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        List<string> keyOptions = new() { "<Custom...>" };
        keyOptions.AddRange(suggestedKeys);
        bool keyIsCustom = m_customAttributeKeys.Contains(attribute) || string.IsNullOrWhiteSpace(attribute.key) || !m_suggestedAttributeValues.ContainsKey(attribute.key);
        int currentIndex = keyIsCustom ? 0 : Mathf.Max(1, keyOptions.IndexOf(attribute.key));
        int selectedIndex = EditorGUILayout.Popup(currentIndex, keyOptions.ToArray(), GUILayout.Width(130));
        if (selectedIndex == 0)
        {
            m_customAttributeKeys.Add(attribute);
            string newKey = EditorGUILayout.TextField(attribute.key, GUILayout.MinWidth(120));
            if (newKey != attribute.key)
            {
                attribute.key = newKey;
                m_catalogIsDirty = true;
            }
        }
        else
        {
            m_customAttributeKeys.Remove(attribute);
            string newKey = keyOptions[selectedIndex];
            if (newKey != attribute.key)
            {
                attribute.key = newKey;
                m_catalogIsDirty = true;
            }
        }
    }

    private void DrawAttributeValueField(AssetCatalogAttribute attribute)
    {
        List<string> suggestions = GetSuggestedAttributeValues(attribute.key);
        List<string> valueOptions = new() { "<Custom...>" };
        valueOptions.AddRange(suggestions);
        bool valueIsCustom = m_customAttributeValues.Contains(attribute) || string.IsNullOrWhiteSpace(attribute.value) || !suggestions.Contains(attribute.value);
        int currentIndex = !valueIsCustom ? Mathf.Max(1, valueOptions.IndexOf(attribute.value)) : 0;
        int selectedIndex = EditorGUILayout.Popup(currentIndex, valueOptions.ToArray(), GUILayout.Width(130));
        if (selectedIndex == 0)
        {
            m_customAttributeValues.Add(attribute);
            string newValue = EditorGUILayout.TextField(attribute.value, GUILayout.MinWidth(140));
            if (newValue != attribute.value)
            {
                attribute.value = newValue;
                m_catalogIsDirty = true;
            }
        }
        else
        {
            m_customAttributeValues.Remove(attribute);
            string newValue = valueOptions[selectedIndex];
            if (newValue != attribute.value)
            {
                attribute.value = newValue;
                m_catalogIsDirty = true;
            }
        }
    }

    private List<string> GetSuggestedAttributeValues(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && m_suggestedAttributeValues.TryGetValue(key, out List<string> values))
            return values;

        return new List<string>();
    }

    /// <summary>
    /// Returns the attribute list to render in the editor by combining the load-time sorted cache
    /// with any live edits made while the window remains open. Cached items keep their original
    /// display order, newly added attributes appear at the bottom, and removed attributes drop out
    /// of the rendered list without reordering the underlying serialized data.
    /// </summary>
    /// <param name="stateKey">Stable cache key for the current asset entry.</param>
    /// <param name="attributes">Live attribute list stored on the asset entry.</param>
    /// <returns>The ordered attribute list that should be drawn for the current GUI frame.</returns>
    private List<AssetCatalogAttribute> GetDisplayAttributes(string stateKey, List<AssetCatalogAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return new List<AssetCatalogAttribute>();

        if (!m_attributeDisplayOrder.TryGetValue(stateKey, out List<AssetCatalogAttribute> cachedOrder))
            cachedOrder = new List<AssetCatalogAttribute>();

        List<AssetCatalogAttribute> displayOrder = new();
        foreach (AssetCatalogAttribute attribute in cachedOrder)
        {
            if (attribute != null && attributes.Contains(attribute))
                displayOrder.Add(attribute);
        }

        foreach (AssetCatalogAttribute attribute in attributes)
        {
            if (attribute != null && !displayOrder.Contains(attribute))
                displayOrder.Add(attribute);
        }

        m_attributeDisplayOrder[stateKey] = displayOrder;
        return displayOrder;
    }

    /// <summary>
    /// Rebuilds the per-entry display-order cache from the current catalog profile by sorting each
    /// entry's existing attributes alphabetically once at load time. This establishes the baseline
    /// visual order used for comparison while leaving the serialized attribute lists unchanged.
    /// </summary>
    private void RebuildAttributeDisplayOrderCache()
    {
        m_attributeDisplayOrder.Clear();

        if (m_assetCatalogProfile == null || m_assetCatalogProfile.groups == null)
            return;

        foreach (AssetCatalogGroup group in m_assetCatalogProfile.groups)
        {
            if (group?.assets == null)
                continue;

            foreach (LoadableAsset entry in group.assets)
            {
                if (entry?.attributes == null || entry.attributes.Count == 0)
                    continue;

                string stateKey = GetAttributeStateKey(group, entry);
                m_attributeDisplayOrder[stateKey] = entry.attributes
                    .Where(attribute => attribute != null)
                    .OrderBy(attribute => attribute.key ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(attribute => attribute.value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
    }

    private string GetAttributeStateKey(AssetCatalogGroup group, LoadableAsset entry)
    {
        int assetIndex = group.assets.IndexOf(entry);
        string assetName = entry.asset != null ? entry.asset.name : "(none)";
        return $"{group.groupName}::{assetIndex}::{assetName}";
    }

    private static string GetAttributeSummary(List<AssetCatalogAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return "No attributes";

        List<string> parts = new();
        foreach (var attribute in attributes)
        {
            string key = string.IsNullOrWhiteSpace(attribute.key) ? "key" : attribute.key;
            string value = string.IsNullOrWhiteSpace(attribute.value) ? "value" : attribute.value;
            parts.Add($"{key}={value}");
        }

        return string.Join(", ", parts);
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
    private enum ResultSortColumn
    {
        None,
        Group,
        Pipeline,
        Platform,
        RideBundleVersion,
        ArtAssetVersion,
        Ok,
        PathOrError
    }

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
    private ResultSortColumn m_sortColumn = ResultSortColumn.None;
    private bool m_sortAscending = true;

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
            "Downloads catalog.json files for each group using the AWS CLI (aws s3 cp --recursive). " +
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
            DrawSortHeader("Group", ResultSortColumn.Group, GUILayout.Width(180));
            DrawSortHeader("Pipeline", ResultSortColumn.Pipeline, GUILayout.Width(80));
            DrawSortHeader("Platform", ResultSortColumn.Platform, GUILayout.Width(140));
            DrawSortHeader("rideBundleVersion", ResultSortColumn.RideBundleVersion, GUILayout.Width(130));
            DrawSortHeader("artAssetVersion", ResultSortColumn.ArtAssetVersion, GUILayout.Width(110));
            DrawSortHeader("OK", ResultSortColumn.Ok, GUILayout.Width(40));
            DrawSortHeader("Remote catalog path / Error", ResultSortColumn.PathOrError);
        }

        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
        foreach (var r in GetSortedResults())
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUILayout.Label(r.groupName ?? "", GUILayout.Width(180));
                GUILayout.Label(r.renderPipeline ?? "", GUILayout.Width(80));
                GUILayout.Label(r.platform ?? "", GUILayout.Width(140));
                GUILayout.Label(r.assetCatalogData?.rideBundleVersion ?? "", GUILayout.Width(130));
                GUILayout.Label(r.assetCatalogData?.artAssetVersion ?? "0", GUILayout.Width(110));
                GUILayout.Label(r.ok ? "Y" : "N", GUILayout.Width(40));

                string tail = r.ok ? (r.remoteCatalogPath ?? "") : (r.error ?? r.remoteCatalogPath ?? "");
                EditorGUILayout.SelectableLabel(tail, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSortHeader(string label, ResultSortColumn column, params GUILayoutOption[] options)
    {
        string text = label;
        if (m_sortColumn == column)
            text += m_sortAscending ? " ^" : " v";

        if (GUILayout.Button(text, EditorStyles.boldLabel, options))
        {
            if (m_sortColumn == column)
                m_sortAscending = !m_sortAscending;
            else
            {
                m_sortColumn = column;
                m_sortAscending = true;
            }
        }
    }

    private IEnumerable<Result> GetSortedResults()
    {
        if (m_sortColumn == ResultSortColumn.None)
            return m_results;

        if (m_sortColumn == ResultSortColumn.Ok)
            return m_sortAscending
                ? m_results.OrderBy(r => r.ok)
                : m_results.OrderByDescending(r => r.ok);

        return m_sortAscending
            ? m_results.OrderBy(GetSortValue, StringComparer.OrdinalIgnoreCase)
            : m_results.OrderByDescending(GetSortValue, StringComparer.OrdinalIgnoreCase);
    }

    private string GetSortValue(Result result)
    {
        return m_sortColumn switch
        {
            ResultSortColumn.Group => result.groupName ?? "",
            ResultSortColumn.Pipeline => result.renderPipeline ?? "",
            ResultSortColumn.Platform => result.platform ?? "",
            ResultSortColumn.RideBundleVersion => result.assetCatalogData?.rideBundleVersion ?? "",
            ResultSortColumn.ArtAssetVersion => result.assetCatalogData?.artAssetVersion ?? "0",
            ResultSortColumn.PathOrError => result.ok ? (result.remoteCatalogPath ?? "") : (result.error ?? result.remoteCatalogPath ?? ""),
            _ => ""
        };
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

        int total = m_profile.groups.Count;
        int done = 0;

        try
        {
            foreach (var group in m_profile.groups)
            {
                if (group == null)
                    continue;

                string groupRemotePrefix = group.remotePrefixPath;
                string groupRemoteRoot = CombineRemotePath(groupRemotePrefix, unityFolder);
                string groupLocalRoot = Path.Combine(tempRoot, SanitizeFileName(group.groupName));

                done++;
                EditorUtility.DisplayProgressBar(
                    "Query Remote Catalogs",
                    group.groupName,
                    total > 0 ? (float)done / total : 1f);

                bool copyOk = TryAwsS3CopyCatalogsToLocal(groupRemoteRoot, groupLocalRoot, out string copyError);

                foreach (string renderPipeline in renderPipelines)
                {
                    foreach (var buildTarget in buildTargets)
                    {
                        string platformFolder = buildTarget.ToString();
                        string postfix = CombineRemotePath(unityFolder, renderPipeline, platformFolder);
                        string remoteFolder = CombineRemotePath(groupRemotePrefix, postfix);
                        string remoteCatalogPath = CombineRemotePath(remoteFolder, "catalog.json");

                        string localDest = Path.Combine(groupLocalRoot, renderPipeline, platformFolder, "catalog.json");

                        var result = new Result
                        {
                            groupName = group.groupName,
                            renderPipeline = renderPipeline,
                            platform = buildTarget.ToString(),
                            ok = false,
                            error = null,
                            remoteCatalogPath = remoteCatalogPath
                        };

                        if (!copyOk)
                        {
                            result.error = string.IsNullOrEmpty(copyError) ? "aws s3 cp failed." : copyError.Trim();
                            m_results.Add(result);
                            continue;
                        }

                        if (!File.Exists(localDest))
                        {
                            result.error = "catalog.json not found after aws s3 cp.";
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
    /// Runs a filtered recursive 'aws s3 cp' to download catalog.json files from S3 to the local filesystem.
    /// Returns true on success. On failure, returns false and fills error.
    /// </summary>
    public static bool TryAwsS3CopyCatalogsToLocal(string s3SourcePath, string localDestPath, out string error)
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

        if (!Directory.Exists(localDestPath))
            Directory.CreateDirectory(localDestPath);

        if (!normalizedSource.EndsWith("/"))
            normalizedSource += "/";

        string args = $"s3 cp \"{normalizedSource}\" \"{localDestPath}\" --recursive --exclude \"*\"";
        foreach (string renderPipeline in renderPipelines)
            args += $" --include \"{renderPipeline}/*/catalog.json\"";

        if (!AssetCatalogEditorUtility.TryRunProcess("aws", args, workingDirectory: null, out string stdout, out string stderr))
        {
            error = string.IsNullOrEmpty(stderr) ? stdout : stderr;
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
