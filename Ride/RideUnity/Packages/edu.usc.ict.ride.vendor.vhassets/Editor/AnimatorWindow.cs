using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VHAssets
{
public class AnimatorWindow : EditorWindow
{
    #region Constants
    enum Tabs
    {
        Animations,
        BlendTrees,
        Utilities,
    }

    readonly string[] TabNames = new string[]
    {
        "Animation States",
        "Blend Trees",
        "Utilities",
    };

    const float LabelFieldWidth = 150;
    const string LastControllerPathKey = "LastControllerPath";
    const string NumBodyAnimationFoldersKey = "NumBodyAnimationFolders";
    const string LastBodyAnimationFolderKey = "LastBodyAnimationFolder";
    const string LastFaceAnimationFolderKey = "LastFaceAnimationFolder";
    #endregion

    #region Variables
    string m_ControllerOutputPath = "Assets";
    string m_FaceAnimationFolder = "";
    List<string> m_BodyAnimationFolders = new List<string>();
    //Animator m_MaskSkeleton;
    AvatarMask m_AnimationMask;
    //AnimatorUtils.OptionFlags m_Options = AnimatorUtils.OptionFlags.Force_Animation_Reimport;
    Tabs m_SelectedTab = Tabs.Animations;
    AnimatorController m_SelectedController;
    enum AddAnimOptions { Files, Folders }
    //AddAnimOptions m_SelectedAddAnimOption = AddAnimOptions.Files;
    //string[] m_AddAnimOptionNames = new string[] { "Files", "Folders" };
    int m_SelectedLayer;
    string[] m_LayerNames;
    int m_SelectedStateMachine;
    string[] m_LayerStateMachineNames;
    List<AnimatorStateMachine> m_LayerStateMachines = new List<AnimatorStateMachine>();
    int m_SelectedBlendTree;
    string[] m_LayerBlendTreeNames;
    List<BlendTree> m_LayerBlendTrees = new List<BlendTree>();
    string m_PosturePrefix = "";
    string m_StateNamePrefixRemoval = "";
    //string m_BlendTreeName = "";
    Vector2 m_SelectionScrollList;
    bool m_HasAdditiveRefPose = false;

    bool m_ForceRepaint = false;


    #endregion

    #region Properties
    public string ControllerOutputPath
    {
        get { return m_ControllerOutputPath; }
        set { m_ControllerOutputPath = value; }
    }

    public string FaceAnimationFolder
    {
        get { return m_FaceAnimationFolder; }
        set { m_FaceAnimationFolder = value; }
    }

    public List<string> BodyAnimationFolders
    {
        get { return m_BodyAnimationFolders; }
    }
    #endregion

    #region Functions
    //[MenuItem("VH/Mecanim/Animator Utils")]
    static void Init()
    {
        AnimatorWindow window = (AnimatorWindow)EditorWindow.GetWindow(typeof(AnimatorWindow));
        window.autoRepaintOnSceneChange = true;
        window.wantsMouseMove = true;
        window.titleContent.text = "Animator Utils";
         //window.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
        //    PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 435),
        //    PlayerPrefs.GetFloat(SavedWindowHKey, 309));

        window.Show();

        window.ControllerOutputPath = PlayerPrefs.GetString(LastControllerPathKey, window.ControllerOutputPath);
        int loopCount = PlayerPrefs.GetInt(NumBodyAnimationFoldersKey, 1);
        for (int i = 0; i < loopCount; i++)
        {
            window.BodyAnimationFolders.Add(PlayerPrefs.GetString(LastBodyAnimationFolderKey + i.ToString(), ""));
        }
        window.FaceAnimationFolder = PlayerPrefs.GetString(LastFaceAnimationFolderKey, window.FaceAnimationFolder);
    }

    void OnFocus()
    {
        m_ForceRepaint = false;
    }

    void OnLostFocus()
    {
        m_ForceRepaint = true;
    }

    void Update()
    {
        if (m_ForceRepaint)
        {
            Repaint();
        }
    }

    void OnGUI()
    {
        Tabs prevTab = m_SelectedTab;
        DrawSharedGUI(); // selected tab can change in here
        if (prevTab != m_SelectedTab)
        {
            SwitchedTabs(prevTab, m_SelectedTab);
        }

        switch (m_SelectedTab)
        {
            case Tabs.Animations:
                DrawAnimationStatesGUI();
                break;

            case Tabs.BlendTrees:
                DrawBlendTreeGUI();
                break;

            case Tabs.Utilities:
                DrawUtilitiesGUI();
                break;
        }
    }

    void SwitchedTabs(Tabs prevPanel, Tabs currPanel)
    {
        switch (prevPanel)
        {
            case Tabs.Animations:

                break;

            case Tabs.BlendTrees:

                break;
        }

        switch (currPanel)
        {
            case Tabs.Animations:

                break;

            case Tabs.BlendTrees:

                break;
        }
    }


    void SelectAnimatorController(AnimatorController controller)
    {
        m_SelectedController = controller;
        m_SelectedLayer = m_SelectedStateMachine = m_SelectedBlendTree = 0;

        if (m_SelectedController == null)
        {
            m_LayerNames = null;
        }
        else
        {
            m_LayerNames = new string[m_SelectedController.layers.Length];
            for (int i = 0; i < m_SelectedController.layers.Length; i++)
            {
                m_LayerNames[i] = m_SelectedController.layers[i].name;
            }

            SelectAnimatorLayer(m_SelectedController.layers[m_SelectedLayer]);
        }
    }

    void SelectAnimatorLayer(AnimatorControllerLayer layer)
    {
        m_LayerStateMachines = AnimatorUtils.GetLayerStateMachines(layer);
        if (m_LayerStateMachines.Count == 0)
        {
            m_LayerStateMachineNames = null;
        }
        else
        {
            m_LayerStateMachineNames = new string[m_LayerStateMachines.Count];
            for (int i = 0; i < m_LayerStateMachineNames.Length; i++)
            {
                m_LayerStateMachineNames[i] = m_LayerStateMachines[i].name;
            }
        }

        m_LayerBlendTrees = AnimatorUtils.GetLayerBlendTrees(layer);
        if (m_LayerBlendTrees.Count == 0)
        {
            m_LayerBlendTreeNames = null;
        }
        else
        {
            m_LayerBlendTreeNames = new string[m_LayerBlendTrees.Count];
            for (int i = 0; i < m_LayerBlendTreeNames.Length; i++)
            {
                m_LayerBlendTreeNames[i] = m_LayerBlendTrees[i].name;
            }
        }
    }

    void DrawSharedGUI()
    {
        m_SelectedTab = (Tabs)GUILayout.Toolbar((int)m_SelectedTab, TabNames);
    }

    void DrawAnimatorControllerGUI()
    {
        AnimatorController prev = m_SelectedController;
        m_SelectedController = (AnimatorController)EditorGUILayout.ObjectField("Controller", m_SelectedController, typeof(AnimatorController), false);
        if (prev != m_SelectedController)
        {
            SelectAnimatorController(m_SelectedController);
        }
    }

    void DrawLayerGUI()
    {
        if (m_LayerNames != null && m_SelectedController != null)
        {
            if (m_LayerNames.Length != m_SelectedController.layers.Length)
            {
                SelectAnimatorController(m_SelectedController);
            }

            GUILayout.BeginHorizontal();
            int prevSelectedLayer = m_SelectedLayer;
            m_SelectedLayer = EditorGUILayout.Popup("Layer", m_SelectedLayer, m_LayerNames);
            if (prevSelectedLayer != m_SelectedLayer)
            {
                SelectAnimatorLayer(m_SelectedController.layers[m_SelectedLayer]);
            }
            if (GUILayout.Button("Refresh", GUILayout.Height(14), GUILayout.Width(100)))
            {
                SelectAnimatorController(m_SelectedController);
            }
            GUILayout.EndHorizontal();

            if (m_SelectedTab == Tabs.Animations)
            {
                if (m_LayerStateMachineNames != null)
                {
                    GUILayout.BeginHorizontal();
                    m_SelectedStateMachine = EditorGUILayout.Popup("State Machine", m_SelectedStateMachine, m_LayerStateMachineNames);
                    if (GUILayout.Button("Refresh", GUILayout.Height(14), GUILayout.Width(100)))
                    {
                        SelectAnimatorController(m_SelectedController);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("There are no states on this layer");
                }
            }
            else if (m_SelectedTab == Tabs.BlendTrees)
            {
                if (m_LayerBlendTreeNames != null)
                {
                    GUILayout.BeginHorizontal();
                    m_SelectedBlendTree = EditorGUILayout.Popup("Blend Tree", m_SelectedBlendTree, m_LayerBlendTreeNames);
                    if (GUILayout.Button("Refresh", GUILayout.Height(14), GUILayout.Width(100)))
                    {
                        SelectAnimatorController(m_SelectedController);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("There are no blend trees on this layer");
                }
            }
        }
    }

    void DrawAnimationStatesGUI()
    {
        DrawAnimatorControllerGUI();

        if (ShouldDrawNoAnimatorControllerGUI())
        {
            return;
        }

        DrawLayerGUI();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Posture Prefix", GUILayout.Width(LabelFieldWidth));
        m_PosturePrefix = GUILayout.TextField(m_PosturePrefix);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Remove Substring", GUILayout.Width(LabelFieldWidth));
        m_StateNamePrefixRemoval = GUILayout.TextField(m_StateNamePrefixRemoval);
        GUILayout.EndHorizontal();

        DrawSelectedAnimations();

        if (GUILayout.Button("Add Animations"))
        {
            AddStates();
        }
    }

    void DrawSelectedAnimations()
    {
        UnityEngine.Object[] selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

        GUILayout.Label("Selected Animations", EditorStyles.boldLabel);
        m_SelectionScrollList = EditorGUILayout.BeginScrollView(m_SelectionScrollList, EditorStyles.textArea);
        foreach (UnityEngine.Object selectedObject in selectedObjects)
        {
            if (AssetDatabase.GetAssetPath(selectedObject).LastIndexOf(".fbx") != -1)
            {
                DrawAnimationInfo(selectedObject.name);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawAnimationInfo(string animName)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(animName);
        GUILayout.FlexibleSpace();

        string name = animName;
        if (!string.IsNullOrEmpty(animName) && !string.IsNullOrEmpty(m_StateNamePrefixRemoval))
        {
            name = animName.Replace(m_StateNamePrefixRemoval, "");
        }

        if (m_SelectedTab == Tabs.Animations)
        {
            GUILayout.Label(AnimatorUtils.DoesStateExist(m_SelectedController, name) ? "Added" : "Not Added");
        }
        else if (m_SelectedTab == Tabs.BlendTrees)
        {
            GUILayout.Label(AnimatorUtils.DoesBlendStateExist(m_SelectedController.layers[m_SelectedLayer], name) ? "Added" : "Not Added");
        }

        GUILayout.EndHorizontal();
    }

    void DrawHasRefPoseGUI()
    {
        GUILayout.BeginHorizontal();
        m_HasAdditiveRefPose = GUILayout.Toggle(m_HasAdditiveRefPose, "Has Additive Ref Pose");
        if (GUILayout.Button("Apply", GUILayout.Width(200)))
        {
            SetHasAdditiveReferencePose();
        }
        GUILayout.EndHorizontal();
    }

    void DrawAnimationMaskGUI()
    {
        GUILayout.BeginHorizontal();
        m_AnimationMask = (AvatarMask)EditorGUILayout.ObjectField("Animation Mask", m_AnimationMask, typeof(AvatarMask), false);
        if (GUILayout.Button("Apply Mask", GUILayout.Width(200)))
        {
            ApplyAnimationMasks();
        }
        GUILayout.EndHorizontal();
    }

    void DrawBlendTreeGUI()
    {
        DrawAnimatorControllerGUI();

        if (ShouldDrawNoAnimatorControllerGUI())
        {
            return;
        }

        DrawLayerGUI();

        /*GUILayout.BeginHorizontal();
        GUILayout.Label("Blend Tree Name", GUILayout.Width(LabelFieldWidth));
        m_BlendTreeName = GUILayout.TextField(m_BlendTreeName);
        GUILayout.EndHorizontal();*/

        GUILayout.BeginHorizontal();
        GUILayout.Label("Remove Substring", GUILayout.Width(LabelFieldWidth));
        m_StateNamePrefixRemoval = GUILayout.TextField(m_StateNamePrefixRemoval);
        GUILayout.EndHorizontal();

        DrawSelectedAnimations();

        if (GUILayout.Button("Add Blends"))
        {
            AddBlendStates();
        }
    }

    void DrawUtilitiesGUI()
    {
        DrawAnimatorControllerGUI();
        if (m_SelectedController != null)
        {
            GUILayout.Label("Parameters");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Viseme Parameters"))
            {
                AnimatorUtils.AddVisemeParams(m_SelectedController);
            }

            if (GUILayout.Button("Add AU Parameters"))
            {
                AnimatorUtils.AddAUParams(m_SelectedController);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Select an Animator Controller to add parameters");
        }

        DrawSelectedAnimations();
        DrawAnimationMaskGUI();
        DrawHasRefPoseGUI();
    }

    bool ShouldDrawNoAnimatorControllerGUI()
    {
        if (m_SelectedController == null)
        {
            GUILayout.Label("Please select an Animtor Controller");
        }

        return m_SelectedController == null;
    }

    void AddStates()
    {
        UnityEngine.Object[] selectedObjects = GetDeepAssets();
        AnimatorUtils.AddAnimatorStates(m_LayerStateMachines[m_SelectedStateMachine], selectedObjects, m_PosturePrefix, m_StateNamePrefixRemoval);
    }

    void AddBlendStates()
    {
        if (m_LayerBlendTreeNames == null)
        {
            EditorUtility.DisplayDialog("Error", "You need to create a blend tree on layer " + m_SelectedController.layers[m_SelectedLayer].name
                + " of your animator controller " + m_SelectedController.name, "Ok");
            return;
        }

        UnityEngine.Object[] selectedObjects = GetDeepAssets();
        AnimatorUtils.AddBlendStates(m_SelectedController, m_SelectedLayer, m_LayerBlendTreeNames[m_SelectedBlendTree], m_StateNamePrefixRemoval, selectedObjects);
    }

    void ApplyAnimationMasks()
    {
        UnityEngine.Object[] selectedObjects = GetDeepAssets();
        foreach (UnityEngine.Object obj in selectedObjects)
        {
            AnimatorUtils.ApplyAnimationMask(AssetDatabase.GetAssetPath(obj), m_AnimationMask);
        }
    }

    void SetHasAdditiveReferencePose()
    {
        UnityEngine.Object[] selectedObjects = GetDeepAssets();
        foreach (UnityEngine.Object obj in selectedObjects)
        {
            AnimatorUtils.SetHasAdditiveReferencePose(AssetDatabase.GetAssetPath(obj), m_HasAdditiveRefPose);
        }
    }

    UnityEngine.Object[] GetDeepAssets()
    {
        return Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
    }

    #region Old
    /*
    enum PanelType
    {
        Save_Folder,
        Save_File,
        Open_Folder,
        Open_File
    }

    void InitParameterTab()
    {
        if (m_SelectedController != null)
        {
            m_ParameterData.SetParameterNames(m_SelectedController.parameters);

        }
    }

     // Parameter Window Data
    class ParameterData
    {
        public AnimatorControllerParameter m_Param = new AnimatorControllerParameter();
        public int m_SelectedParameter;
        public string[] m_ParameterNames;

        public void SetParameterNames(AnimatorControllerParameter[] controllerParams)
        {
            m_ParameterNames = new string[controllerParams.Length];
            for (int i = 0; i < controllerParams.Length; i++)
            {
                m_ParameterNames[i] = controllerParams[i].name;
            }
        }

        public void AddParameter(AnimatorControllerParameter controllerParam)
        {
            string[] holder = null;
            if (m_ParameterNames != null)
            {
                holder = new string[m_ParameterNames.Length + 1];
                holder[holder.Length - 1] = controllerParam.name;
                Array.Copy(m_ParameterNames, holder, m_ParameterNames.Length);
                m_ParameterNames = holder;
            }
            else
            {
                m_ParameterNames = new string[1];
                m_ParameterNames[0] = controllerParam.name;
            }
        }
    }
    ParameterData m_ParameterData = new ParameterData();

    string CreateDirectoryWidget(string title, string dir, PanelType type)
    {
        return CreateDirectoryWidget(title, dir, type, "", "");
    }

    string CreateDirectoryWidget(string title, string dir, PanelType type, string defaultName, string fileType)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        dir = GUILayout.TextField(dir);
        if (GUILayout.Button("...", GUILayout.MaxWidth(30)))
        {
            string output = string.Empty;
            switch (type)
            {
            case PanelType.Save_File:
                output = EditorUtility.SaveFilePanel(title, dir, defaultName, fileType);
                break;

            case PanelType.Open_Folder:
                output = EditorUtility.OpenFolderPanel(title, dir, defaultName);
                break;
            }

            if (!string.IsNullOrEmpty(output))
            {
                dir = output;
            }
        }
        GUILayout.EndHorizontal();
        return dir;
    }

    void CreateController()
    {
        if (m_MaskSkeleton == null)
        {
            EditorUtility.DisplayDialog("Error", "You need to set a mask skeleton", "Ok");
            return;
        }
        if (string.IsNullOrEmpty(m_ControllerOutputPath))
        {
            EditorUtility.DisplayDialog("Error", "Select a controller output path", "Ok");
            return;
        }

        PlayerPrefs.SetInt(NumBodyAnimationFoldersKey, m_BodyAnimationFolders.Count);
        for (int i = 0; i < m_BodyAnimationFolders.Count; i++)
        {
            PlayerPrefs.SetString(LastBodyAnimationFolderKey + i.ToString(), m_BodyAnimationFolders[i]);
        }
        PlayerPrefs.SetString(LastControllerPathKey, m_ControllerOutputPath);
        PlayerPrefs.SetString(LastFaceAnimationFolderKey, m_FaceAnimationFolder);
        AnimatorUtils.CreateAnimatorController(m_MaskSkeleton.transform, m_ControllerOutputPath, BodyAnimationFolders, m_FaceAnimationFolder, m_Options);
    }

    void ShowPathModButtons(List<string> container)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Path"))
        {
            AddPathSlot(container);
        }
        if (GUILayout.Button("Remove Path"))
        {
            RemoveLastPathSlot(container);
        }
        GUILayout.EndHorizontal();
    }

    void AddPathSlot(List<string> container)
    {
        container.Add("");
    }

    void RemoveLastPathSlot(List<string> container)
    {
        if (container.Count > 1)
        {
            container.RemoveAt(container.Count - 1);
        }
    }
    */

    /*
    GUILayout.BeginHorizontal(GUILayout.Width(415));
    GUILayout.Label("Add Mode", EditorStyles.boldLabel);
    m_SelectedAddAnimOption = (AddAnimOptions)GUILayout.Toolbar((int)m_SelectedAddAnimOption, m_AddAnimOptionNames, EditorStyles.radioButton);
    GUILayout.EndHorizontal();

    if (m_SelectedAddAnimOption == AddAnimOptions.Files)
    {

    }
    else if (m_SelectedAddAnimOption == AddAnimOptions.Folders)
    {
        for (int i = 0; i < m_BodyAnimationFolders.Count; i++)
        {
            m_BodyAnimationFolders[i] = CreateDirectoryWidget("Body Animation Folder", m_BodyAnimationFolders[i], PanelType.Open_Folder);
        }
        ShowPathModButtons(m_BodyAnimationFolders);

        m_FaceAnimationFolder = CreateDirectoryWidget("Face Animation Folder", m_FaceAnimationFolder, PanelType.Open_Folder);
        m_MaskSkeleton = (Animator)EditorGUILayout.ObjectField("Mask Skeleton", m_MaskSkeleton, typeof(Animator), true);
    }
    */
    #endregion

    #endregion
}
}
