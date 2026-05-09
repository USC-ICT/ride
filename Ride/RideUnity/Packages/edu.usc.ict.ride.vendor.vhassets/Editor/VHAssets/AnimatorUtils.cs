using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VHAssets
{
public class AnimatorUtils
{
    #region Constants
    [Flags]
    public enum OptionFlags
    {
        Force_Animation_Reimport = 1,
    }

    // returns true if the settings were modified
    delegate bool OnModifyImportSettings(ModelImporterClipAnimation modelImpoterClipAnim, object importData);
    #endregion

    #region Variables

    #endregion

    #region Functions

    #region State Functions
    public static void AddAnimatorStates(AnimatorControllerLayer layer, UnityEngine.Object[] motions, string basePostureName, string removeSubstring)
    {
        AddAnimatorStates(layer.stateMachine, motions, basePostureName, removeSubstring);
    }

    public static void AddAnimatorStates(AnimatorStateMachine stateMachine, UnityEngine.Object[] motions, string basePostureName, string removeSubstring)
    {
        // need to handle posture anims first
        foreach (UnityEngine.Object obj in motions)
        {
            string motionPath = AssetDatabase.GetAssetPath(obj);
            Motion motion = LoadMotion(motionPath);
            if (motion != null)
            {
                string tempName = obj.name;
                if (!string.IsNullOrEmpty(removeSubstring))
                {
                    tempName = tempName.Replace(removeSubstring, "");
                }

                if (tempName == basePostureName)
                {
                    AddAnimatorStates(stateMachine, obj, basePostureName, removeSubstring);
                    SetAnimationLooping(motionPath, true);
                }
            }
        }

        foreach (UnityEngine.Object obj in motions)
        {
            AddAnimatorStates(stateMachine, obj, basePostureName, removeSubstring);
        }
    }

    public static void AddAnimatorStates(AnimatorControllerLayer layer, UnityEngine.Object objMotion, string basePostureName, string removeSubstring)
    {
        AddAnimatorStates(layer.stateMachine, objMotion, basePostureName, removeSubstring);
    }

    public static void AddAnimatorStates(AnimatorStateMachine stateMachine, UnityEngine.Object objMotion, string basePostureName, string removeSubstring)
    {
        Motion motion = LoadMotion(AssetDatabase.GetAssetPath(objMotion));
        if (motion != null)
        {
            motion.name = objMotion.name;
            if (!string.IsNullOrEmpty(removeSubstring))
            {
                motion.name = motion.name.Replace(removeSubstring, "");
            }

            AddAnimatorState(stateMachine, motion.name, motion, basePostureName);
        }
    }

    public static AnimatorState AddAnimatorState(AnimatorControllerLayer layer, string stateName, Motion motion)
    {
        return AddAnimatorState(layer.stateMachine, stateName, motion, string.Empty);
    }

    public static AnimatorState AddAnimatorState(AnimatorControllerLayer layer, string stateName, Motion motion, string basePostureStateName)
    {
        return AddAnimatorState(layer.stateMachine, stateName, motion, basePostureStateName);
    }

    public static AnimatorState AddAnimatorState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        return AddAnimatorState(stateMachine, stateName, motion, string.Empty);
    }

    public static AnimatorState AddAnimatorState(AnimatorStateMachine stateMachine, string stateName, string motionPath, string basePostureStateName)
    {
        Motion motion = LoadMotion(motionPath);
        return AddAnimatorState(stateMachine, stateName, motion, basePostureStateName);
    }

    public static AnimatorState AddAnimatorState(AnimatorStateMachine stateMachine, string stateName, Motion motion, string basePostureStateName)
    {
        AnimatorState state = GetState(stateMachine, stateName);
        if (state == null)
        {
            state = stateMachine.AddState(stateName);
        }
        state.motion = motion;

        //bool isPosture = IsPostureAnimation(stateName);
        if (!string.IsNullOrEmpty(basePostureStateName))
        {
            //string postureStateName = GetAnimationPostureRoot(stateName);
            ChildAnimatorState postureState = Array.Find<ChildAnimatorState>(stateMachine.states, s => s.state.name == basePostureStateName);
            if (postureState.state != null && postureState.state != state)
            {
                // make sure this transition doesn't already exist
                AnimatorStateTransition exitTransition = Array.Find<AnimatorStateTransition>(state.transitions, t => t.destinationState == postureState.state);
                if (exitTransition == null)
                {
                    exitTransition = state.AddExitTransition();
                }

                // non-idle anims need a way to transition back to the idle after they finish playing
                exitTransition.hasExitTime = true;
                exitTransition.destinationState = postureState.state;

            }
        }

        return state;
    }

    public static void RemoveAnimatorState(AnimatorControllerLayer layer, string stateName)
    {
        AnimatorState state = GetState(layer.stateMachine, stateName);
        if (state != null)
        {
            layer.stateMachine.RemoveState(state);
        }
    }

    public static bool DoesStateExist(AnimatorControllerLayer layer, string stateName)
    {
        return DoesStateExist(layer.stateMachine, stateName);
    }

    public static bool DoesStateExist(AnimatorController controller, AnimationClip motion)
    {
        return DoesStateExist(controller, motion);
    }

    /// <summary>
    /// Recursively returns all statemachines on a layer
    /// </summary>
    /// <returns>The layer state machines.</returns>
    /// <param name="layer">Layer.</param>
    public static List<AnimatorStateMachine> GetLayerStateMachines(AnimatorControllerLayer layer)
    {
        List<AnimatorStateMachine> subStateMachines = new List<AnimatorStateMachine>();
        Stack<AnimatorStateMachine> stateMachines = new Stack<AnimatorStateMachine>();
        stateMachines.Push(layer.stateMachine);
        subStateMachines.Add(layer.stateMachine);

        while (stateMachines.Count > 0)
        {
            AnimatorStateMachine currMachine = stateMachines.Pop();

            foreach (ChildAnimatorStateMachine childMachine in currMachine.stateMachines)
            {
                stateMachines.Push(childMachine.stateMachine);
                subStateMachines.Add(childMachine.stateMachine);
            }
        }

        return subStateMachines;
    }

    public static List<BlendTree> GetLayerBlendTrees(AnimatorControllerLayer layer)
    {
        List<BlendTree> blendTrees = new List<BlendTree>();
        Stack<AnimatorStateMachine> stateMachines = new Stack<AnimatorStateMachine>();
        stateMachines.Push(layer.stateMachine);

        while (stateMachines.Count > 0)
        {
            AnimatorStateMachine currMachine = stateMachines.Pop();

            foreach (ChildAnimatorStateMachine childMachine in currMachine.stateMachines)
            {
                stateMachines.Push(childMachine.stateMachine);
            }

            foreach (ChildAnimatorState childMotion in currMachine.states)
            {
                BlendTree blendTree = childMotion.state.motion as BlendTree;
                if (blendTree != null)
                {
                    blendTrees.Add(blendTree);
                }
            }
        }

        return blendTrees;
    }

    public static AnimatorState GetState(AnimatorControllerLayer layer, string stateName)
    {
        return GetState(layer.stateMachine, stateName);
    }

    public static AnimatorState GetState(AnimatorStateMachine stateMachine, string stateName)
    {
        Stack<AnimatorStateMachine> stateMachines = new Stack<AnimatorStateMachine>();
        stateMachines.Push(stateMachine);

        while (stateMachines.Count > 0)
        {
            AnimatorStateMachine currMachine = stateMachines.Pop();
            foreach (ChildAnimatorState childState in currMachine.states)
            {
                if (string.Compare(childState.state.name, stateName, true) == 0)
                {
                    return childState.state;
                }
            }

            foreach (ChildAnimatorStateMachine childMachine in currMachine.stateMachines)
            {
                stateMachines.Push(childMachine.stateMachine);
            }
        }

        return null;
    }

    public static bool DoesStateExist(AnimatorStateMachine stateMachine, string stateName)
    {
        return GetState(stateMachine, stateName) != null;
    }

    public static bool DoesStateExist(ChildAnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState childState in stateMachine.stateMachine.states)
        {
            if (string.Compare(childState.state.name, stateName, true) == 0)
            {
                return true;
            }
        }
        return false;
    }

    public static bool DoesStateExist(ChildAnimatorStateMachine stateMachine, AnimationClip motion)
    {
        return DoesStateExist(stateMachine, motion.name);
    }


    public static bool DoesStateExist(ChildMotion[] motions, string stateName)
    {
        foreach (ChildMotion childMotion in motions)
        {
            if (childMotion.motion != null && childMotion.motion.name == stateName)
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region Animator Parameter Functions
    public static AnimatorControllerParameter FindAnimatorParamter(AnimatorController controller, string paramName)
    {
        return Array.Find<AnimatorControllerParameter>(controller.parameters, c => c.name == paramName);
    }

    public static void AddVisemeParams(AnimatorController controller)
    {
        AddVisemeParams(controller, string.Empty, string.Empty);
    }

    public static void AddVisemeParams(AnimatorController controller, string prefix, string suffix)
    {
        if (prefix == null)
        {
            prefix = string.Empty;
        }
        if (suffix == null)
        {
            suffix = string.Empty;
        }

        AnimatorControllerParameterType type = AnimatorControllerParameterType.Float;
        AddAnimatorParamter(controller, prefix + "face_neutral" + suffix, type);
        AddAnimatorParamter(controller, prefix + "FV" + suffix, type);
        AddAnimatorParamter(controller, prefix + "open" + suffix, type);
        AddAnimatorParamter(controller, prefix + "PBM" + suffix, type);
        AddAnimatorParamter(controller, prefix + "ShCh" + suffix, type);
        AddAnimatorParamter(controller, prefix + "tBack" + suffix, type);
        AddAnimatorParamter(controller, prefix + "tRoof" + suffix, type);
        AddAnimatorParamter(controller, prefix + "tTeeth" + suffix, type);
        AddAnimatorParamter(controller, prefix + "W" + suffix, type);
        AddAnimatorParamter(controller, prefix + "wide" + suffix, type);
    }

    public static void AddAUParams(AnimatorController controller)
    {
        AddAUParams(controller, string.Empty, string.Empty);
    }

    public static void AddAUParams(AnimatorController controller, string prefix, string suffix)
    {
        AnimatorControllerParameterType type = AnimatorControllerParameterType.Float;
        AddAnimatorParamter(controller, prefix + "001_inner_brow_raiser_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "001_inner_brow_raiser_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "002_outer_brow_raiser_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "002_outer_brow_raiser_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "004_brow_lowerer_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "004_brow_lowerer_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "005_upper_lid_raiser" + suffix, type);
        AddAnimatorParamter(controller, prefix + "006_cheek_raiser" + suffix, type);
        AddAnimatorParamter(controller, prefix + "007_lid_tightener" + suffix, type);
        AddAnimatorParamter(controller, prefix + "010_upper_lip_raiser" + suffix, type);
        AddAnimatorParamter(controller, prefix + "012_lip_corner_puller_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "012_lip_corner_puller_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "014_smile_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "014_smile_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "025_lips_part" + suffix, type);
        AddAnimatorParamter(controller, prefix + "026_jaw_drop" + suffix, type);
        AddAnimatorParamter(controller, prefix + "045_blink_lf" + suffix, type);
        AddAnimatorParamter(controller, prefix + "045_blink_rt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "112_happy" + suffix, type);
        AddAnimatorParamter(controller, prefix + "124_disgust" + suffix, type);
        AddAnimatorParamter(controller, prefix + "126_fear" + suffix, type);
        AddAnimatorParamter(controller, prefix + "127_surprise" + suffix, type);
        AddAnimatorParamter(controller, prefix + "129_angry" + suffix, type);
        AddAnimatorParamter(controller, prefix + "130_sad" + suffix, type);
        AddAnimatorParamter(controller, prefix + "131_contempt" + suffix, type);
        AddAnimatorParamter(controller, prefix + "132_browraise1" + suffix, type);
        AddAnimatorParamter(controller, prefix + "133_browraise2" + suffix, type);
        AddAnimatorParamter(controller, prefix + "134_hurt_brows" + suffix, type);
        AddAnimatorParamter(controller, prefix + "136_furrow" + suffix, type);
    }

    public static AnimatorControllerParameter AddAnimatorParamter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter param = FindAnimatorParamter(controller, paramName);
        bool newParam = param == null;
        if (param == null)
        {
            param = new AnimatorControllerParameter();
        }

        param.name = paramName;
        param.type = type;

        if (newParam)
        {
            // calling this function before setting the name/type of the param gives unity a lot of errors
            controller.AddParameter(param);
        }

        return param;
    }
    #endregion

    #region Blend Tree Functions
    public static BlendTree GetBlendTree(AnimatorControllerLayer layer, string blendTreeName)
    {
        BlendTree blendTree = null;
        AnimatorState state = GetState(layer, blendTreeName);
        if (state != null)
        {
            blendTree = state.motion as BlendTree;
        }
        return blendTree;
    }

    public static BlendTree CreateBlendTree(AnimatorController controller, int layerIndex, string blendTreeName, BlendTreeType blendTreeType)
    {
        BlendTree blendTree = GetBlendTree(controller.layers[layerIndex], blendTreeName);
        if (blendTree == null)
        {
            controller.CreateBlendTreeInController(blendTreeName, out blendTree, layerIndex);
        }

        blendTree.blendType = blendTreeType;
        return blendTree;
    }

    static string GetNameFromObject(UnityEngine.Object obj, string substringToRemove)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        string name = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrEmpty(substringToRemove))
        {
            name = name.Replace(substringToRemove, "");
        }
        return name;
    }

    public static BlendTree AddBlendStates(AnimatorController controller, int layerIndex, string blendTreeName, string substringToRemove, UnityEngine.Object[] blendMotions)
    {
        BlendTree blendTree = CreateBlendTree(controller, layerIndex, blendTreeName, BlendTreeType.Direct);
        foreach (UnityEngine.Object obj in blendMotions)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            Motion motion = LoadMotion(assetPath);
            if (motion != null)
            {
                string paramName = motion.name = GetNameFromObject(obj, substringToRemove);
                AddAnimatorParamter(controller, paramName, AnimatorControllerParameterType.Float);
                AddBlendState(blendTree, motion, paramName);
            }
        }

        SetNormalizeBlendValues(blendTree, true);
        return blendTree;
    }

    /*public static BlendTree AddBlendStates(AnimatorController controller, int layerIndex, string blendTreeName, string substringToRemove, BlendTreeType blendTreeType, List<string> faceAnimationPaths)
    {
        BlendTree blendTree = CreateBlendTree(controller, layerIndex, blendTreeName, blendTreeType);

        foreach (string motionFilePath in faceAnimationPaths)
        {
            string motionName = Path.GetFileNameWithoutExtension(motionFilePath);
            motionName = motionName.Remove(0, substringToRemove.Length);
            string paramName = motionName;

            AddAnimatorParamter(controller, paramName, AnimatorControllerParameterType.Float);

            // setup face
            AddBlendState(blendTree, motionFilePath, paramName, substringToRemove);
        }

        SetNormalizeBlendValues(blendTree, true);

        return blendTree;
    }*/

    static void SetNormalizeBlendValues(BlendTree blendTree, bool normalize)
    {
        var serializedObject = new UnityEditor.SerializedObject(blendTree);
        var normalizedBlendValues= serializedObject.FindProperty("m_NormalizedBlendValues");
        if (normalizedBlendValues != null)
        {
            normalizedBlendValues.boolValue = normalize;
            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }
    }

    public static void AddBlendState(BlendTree blendTree, string motionPath, string blendParamName, string motionSubstringToRemove)
    {
        string motionName = Path.GetFileNameWithoutExtension(motionPath);
        if (!string.IsNullOrEmpty(motionSubstringToRemove))
        {
            motionName = motionName.Replace(motionSubstringToRemove, "");
        }


        Motion motion = LoadMotion(motionPath);
        motion.name = motionName;

        AddBlendState(blendTree, motion, blendParamName);
    }

    public static void AddBlendState(BlendTree blendTree, Motion motion, string blendParamName)
    {
        if (!DoesStateExist(blendTree.children, motion.name))
        {
            blendTree.AddChild(motion);
        }

        // setup direct blend tree param to the same param that we just created on the controller
        ChildMotion[] childMotions = blendTree.children;
        int index = Array.FindIndex<ChildMotion>(childMotions, cm => cm.motion == motion);
        if (index != -1)
        {
            childMotions[index].directBlendParameter = blendParamName;
            blendTree.children = childMotions;
        }
        else
        {
            Debug.LogError("Can't find motion " + motion.name + " in Face blend Tree");
        }
    }

    public static bool DoesBlendStateExist(AnimatorControllerLayer layer, string blendStateName)
    {
        if (DoesBlendStateExist(layer.stateMachine.states, blendStateName))
        {
            return true;
        }

        return false;
    }

    public static bool DoesBlendStateExist(ChildAnimatorState[] states, string blendStateName)
    {
        foreach (ChildAnimatorState child in states)
        {
            if (DoesBlendStateExist(child.state, blendStateName))
            {
                return true;
            }
        }

        return false;
    }

    public static bool DoesBlendStateExist(AnimatorState state, string blendStateName)
    {
        BlendTree tree = state.motion as BlendTree;
        if (tree == null)
        {
            return false;
        }

        return DoesStateExist(tree.children, blendStateName);
    }
    #endregion

    #region Util Functions
    /// <summary>
    /// Returns a list of file paths of the animation files that are currently selected in the project view
    /// </summary>
    /// <returns>The selected animations.</returns>
    public static List<string> GetSelectedAnimations()
    {
        List<string> animNames = new List<string>();
        UnityEngine.Object[] selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
        foreach (UnityEngine.Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Path.GetExtension(path) == "")
            {
                // they have a folder selected
                animNames.AddRange(GetAnimationsPathsRecursive(path));
            }
            else
            {
                animNames.Add(path);
            }
        }

        return animNames;
    }

    /// <summary>
    /// Returns a list of file paths for all the animation files in the provided folder path, recursively
    /// </summary>
    /// <returns>The animations paths recursive.</returns>
    /// <param name="folder">Folder.</param>
    static List<string> GetAnimationsPathsRecursive(string folder)
    {
        List<string> animNames = new List<string>();
        animNames.AddRange(GetAnimationPathsRecursive(folder, "*.fbx"));
        animNames.AddRange(GetAnimationPathsRecursive(folder, "*.obj"));
        animNames.AddRange(GetAnimationPathsRecursive(folder, "*.dae"));
        return animNames;
    }

    public static string[] GetAnimationPathsRecursive(string folder, string fileExt)
    {
        return Directory.GetFiles(folder, fileExt, SearchOption.AllDirectories);
    }

    /// <summary>
    /// Loads a motion by file name. The file path should be project relative
    /// </summary>
    /// <returns>The motion.</returns>
    /// <param name="filePath">File path.</param>
    public static Motion LoadMotion(string filePath)
    {
        filePath = filePath.Replace(GetRootProjectFolderPath(), "");
        Motion motion = (Motion)AssetDatabase.LoadAssetAtPath(filePath, typeof(Motion));
        if (motion == null)
        {
            Debug.LogWarning(filePath + " doesn't have an animation clip");
        }
        return motion;
    }

    public static Motion LoadMotion(UnityEngine.Object obj)
    {
        return LoadMotion(AssetDatabase.GetAssetPath(obj));
    }

    public static void ApplyAnimationMask(AnimationClip motion, AvatarMask mask)
    {
        ApplyAnimationMask(AssetDatabase.GetAssetPath(motion), mask);
    }

    static void UpdateAnimationsByMask(string mask, SerializedProperty clipAnimations)
    {
        if (clipAnimations.isArray)
        {
            Assembly assembly = Assembly.Load("UnityEditor");
            
            for (int i = 0; i < clipAnimations.arraySize; i++)
            {
                SerializedProperty currentAnimation = clipAnimations.GetArrayElementAtIndex(i);
                Type animationClipInfoProperties = assembly.GetType("UnityEditor.AnimationClipInfoProperties", true);
                object invoker = Activator.CreateInstance(animationClipInfoProperties, new object[] { currentAnimation });
                
                MethodInfo method = animationClipInfoProperties.GetMethod("MaskToClip");
                object[] parametersArray = new object[] { AssetDatabase.LoadAssetAtPath<AvatarMask>(mask) };
                method.Invoke(invoker, parametersArray);
            }
        }
    }

    static bool OnModifyAnimationMask(ModelImporterClipAnimation clipAnimation, object data)
    {
        bool forceReimport = false;
        AvatarMask mask = (AvatarMask)data;

        if (clipAnimation.maskSource != mask)
        {
            clipAnimation.maskType = mask == null ? ClipAnimationMaskType.CreateFromThisModel : ClipAnimationMaskType.CopyFromOther;
            clipAnimation.maskSource = mask;
            forceReimport = true;
            //SerializedObject modelImporterEditor = new SerializedObject(clipAnimation);
            //SerializedProperty clipAnimations = modelImporterEditor.FindProperty("m_ClipAnimations");
            //UpdateAnimationsByMask(mask.name, clipAnimations);
        }
        return forceReimport;
    }

    static bool OnModifyAnimationLoopPose(ModelImporterClipAnimation clipAnimation, object data)
    {
        bool forceReimport = false;
        bool isLooping = (bool)data;
        if (clipAnimation.loopPose != isLooping && clipAnimation.loop != isLooping)
        {
            clipAnimation.loopPose = clipAnimation.loopTime = clipAnimation.loop = isLooping;
            forceReimport = true;
        }
        return forceReimport;
    }

    static bool OnModifyAnimationIsAdditiveReferencePose(ModelImporterClipAnimation clipAnimation, object data)
    {
        bool forceReimport = false;
        bool hasAdditiveRefPose = (bool)data;
        if (clipAnimation.hasAdditiveReferencePose != hasAdditiveRefPose)
        {
            clipAnimation.hasAdditiveReferencePose = hasAdditiveRefPose;
            forceReimport = true;
        }
        return forceReimport;
    }

    static void ReimportAnimation(string motionPath, OnModifyImportSettings cb, object reimportData)
    {
        ModelImporter modelImporter = ModelImporter.GetAtPath(motionPath) as ModelImporter;
        if (modelImporter == null)
        {
            Debug.LogError("didn't load " + motionPath);
            return;
        }

        ReimportAnimation(modelImporter, cb, reimportData);
    }

    static void ReimportAnimation(ModelImporter modelImporter, OnModifyImportSettings cb, object reimportData)
    {
        ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;

        bool needsReimport = false;
        for (int i = 0; i < clipAnimations.Length; i++)
        {
            if (cb(clipAnimations[i], reimportData))
            {
                needsReimport = true;
            }

        }
        modelImporter.clipAnimations = clipAnimations;

        if (needsReimport)
        {
            modelImporter.SaveAndReimport();
        }
    }

    public static void ApplyAnimationMask(string motionPath, AvatarMask mask)
    {
        ReimportAnimation(motionPath, OnModifyAnimationMask, mask);
    }

    public static void SetAnimationLooping(string motionPath, bool isLooping)
    {
        ReimportAnimation(motionPath, OnModifyAnimationLoopPose, isLooping);
    }

    public static void SetHasAdditiveReferencePose(string motionPath, bool hasAdditiveReferencePose)
    {
        ReimportAnimation(motionPath, OnModifyAnimationIsAdditiveReferencePose, hasAdditiveReferencePose);
    }


    static string GetRootProjectFolderPath()
    {
        return Application.dataPath.Replace("/Assets", "") + "/";
    }

    static string GetFullFilePath(string projectRelativePath)
    {
        return Application.dataPath + "/" +  projectRelativePath;
    }

    static string GetAnimationPostureRoot(string name)
    {
        int index = name.IndexOf('_');
        if (index != -1)
        {
            name = name.Remove(index);
        }
        return name;
    }

    static string GetFullPathWithoutExtension(string path)
    {
        return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
    }

    static bool IsOptionOn(OptionFlags flags, OptionFlags test)
    {
        return (flags & test) == test;
    }

    public static bool DoesStateExist(AnimatorController controller, string stateName)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (DoesStateExist(layer, stateName))
            {
                return true;
            }
        }
        return false;
    }

    public static int GetLayerIndex(AnimatorController controller, string layerName)
    {
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == layerName)
            {
                return i;
            }
        }

        Debug.LogError("Animator Controller " + controller.name + " does not have a layer named " + layerName);
        return -1;
    }

    #endregion

    #region Old
    /*
 *
 *
 * static readonly string[] StandardFaceParameterNames = new string[]
    {

    };



    [Flags]
    enum MaskBodyParts
    {
        Base = 1,
        Body = 1 << 1,
        Head = 1 << 2,
        Left_Leg = 1 << 3,
        Right_Leg = 1 << 4,
        Left_Arm = 1 << 5,
        Right_Arm = 1 << 6,
        Left_Hand = 1 << 7,
        Right_Hand = 1 << 8,
        Left_Foot_IK = 1 << 9,
        Right_Foot_IK = 1 << 10,
        Left_Hand_IK = 1 << 11,
        Right_Hand_IK = 1 << 12,
        All = (1 << 13) - 1,
        Left_Upper_Body = Left_Arm | Left_Hand | Left_Hand_IK,
        Right_Upper_Body = Right_Arm | Right_Hand | Right_Hand_IK,
    }

 * const string Base = "_Base";
    const string ArmsHead = "_ArmsHead";
    const string Face = "_Face";
    const string MaskExt = ".mask";
    const string BaseLayer = "Base Layer";
    const string ArmsHeadLayer = "Arms Head Layer";
    const string FaceLayer = "Face Layer";

 * #if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
    public static void CreateAnimatorController(Transform avatar, string ctrlPath, List<string> bodyAnimationPaths, string faceAnimationPath, OptionFlags options)
    {
    }
#else
    public static void CreateAnimatorController(Transform avatar, string ctrlPath, List<string> bodyAnimationPaths, string faceAnimationPath, OptionFlags options)
    {
        List<string> bodyAnims = new List<string>();
        foreach (string bodyAnimPath in bodyAnimationPaths)
        {
            bodyAnims.AddRange(GetAnimationsPathsRecursive(bodyAnimPath));
        }
        CreateAnimatorController(avatar, ctrlPath, bodyAnims, GetAnimationsPathsRecursive(faceAnimationPath), options);
    }

    /// <summary>
    /// Creates the animator controller and add states for each motion found in the provided motion path, recursively.
    /// </summary>
    /// <param name="ctrlPath">Ctrl path.</param>
    /// <param name="motionPath">Motion path.</param>
    public static void CreateAnimatorController(Transform avatar, string ctrlPath, string bodyAnimationPath, string faceAnimationPath, OptionFlags options)
    {
        CreateAnimatorController(avatar, ctrlPath, GetAnimationsPathsRecursive(bodyAnimationPath), GetAnimationsPathsRecursive(faceAnimationPath), options);
    }

    static void CreateAnimatorController(Transform avatar, string ctrlPath, List<string> bodyAnimationPaths, List<string> faceAnimationPaths, OptionFlags options)
    {
        AnimatorController controller = null;

        // strip the path
        string ctrlAssetPath = ctrlPath.Replace(GetRootProjectFolderPath(), "");

        // check if the controller already exists
        if (File.Exists(ctrlPath))
        {
            // already exists. Load it and remove previous data
            controller = (AnimatorController)AssetDatabase.LoadAssetAtPath(ctrlAssetPath, typeof(AnimatorController));
            //RemoveControllerStates(controller);
        }
        else
        {
            // create controller
            controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlAssetPath);
        }

        CreateLayer(controller, BaseLayer);
        CreateLayer(controller, ArmsHeadLayer);
        CreateLayer(controller, FaceLayer);

        // shallow copy
        AnimatorControllerLayer[] layers = controller.layers;

        // create the masks
        string maskBasePath = GetFullPathWithoutExtension(ctrlAssetPath);
        AvatarMask baseMask = CreateAvatarMask(maskBasePath + Base + MaskExt, MaskBodyParts.All, avatar);
        AvatarMask upperBodyMask = CreateAvatarMask(maskBasePath + ArmsHead + MaskExt, MaskBodyParts.Left_Upper_Body | MaskBodyParts.Right_Upper_Body | MaskBodyParts.Body, avatar);
        AvatarMask faceMask = CreateAvatarMask(maskBasePath + Face + MaskExt, MaskBodyParts.Head, avatar);

        // setup layers for the base, the body, and the face for the controller
        AnimatorControllerLayer baseLayer = SetupLayer(FindLayer(layers, BaseLayer), AnimatorLayerBlendingMode.Override, 1.0f, true, null);
        AnimatorControllerLayer upperBodyLayer = SetupLayer(FindLayer(layers, ArmsHeadLayer), AnimatorLayerBlendingMode.Override, 1.0f, false, upperBodyMask);
        AnimatorControllerLayer faceLayer = SetupLayer(FindLayer(layers, FaceLayer), AnimatorLayerBlendingMode.Override, 1.0f, false, faceMask);

        foreach (string motionFilePath in bodyAnimationPaths)
        {
            if (IsPostureAnimation(motionFilePath))
            {
                SetupAnimationClip(motionFilePath, baseLayer.avatarMask, true, IsOptionOn(options, OptionFlags.Force_Animation_Reimport));
            }
            else
            {
                SetupAnimationClip(motionFilePath, baseLayer.avatarMask, false, IsOptionOn(options, OptionFlags.Force_Animation_Reimport));
            }

            // create states based on the body animations that are in the animationFolderPath
            // attach states to the state machine
            AddAnimatorBodyState(baseLayer.stateMachine, Path.GetFileNameWithoutExtension(motionFilePath), LoadMotion(motionFilePath));
            AddAnimatorBodyState(upperBodyLayer.stateMachine, Path.GetFileNameWithoutExtension(motionFilePath), LoadMotion(motionFilePath));
        }

        SetupFaceLayer(controller, faceLayer, baseMask, faceAnimationPaths, options);

        // deep copy back to controller
        controller.layers = layers;

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Finds the layer by name and creates it if it doesn't exist
    /// </summary>
    /// <returns>The layer.</returns>
    /// <param name="controller">Controller.</param>
    /// <param name="layerName">Layer name.</param>
    static AnimatorControllerLayer FindLayer(AnimatorControllerLayer[] layers, string layerName)
    {
        AnimatorControllerLayer layer = Array.Find<AnimatorControllerLayer>(layers, ac => ac.name == layerName);
        if (layer == null)
        {
            // layer doesn't exist, add it
            //Debug.LogError(layerName + " doesn't exsist");
        }
        return layer;
    }

    static AnimatorControllerLayer CreateLayer(AnimatorController controller, string layerName)
    {
        AnimatorControllerLayer layer = FindLayer(controller.layers, layerName);
        if (layer == null)
        {
    controller.AddLayer(layerName);
    }
    else
    {
        RemoveLayerStates(layer);
    }
    return layer;
    }

    static AnimatorControllerLayer SetupLayer(AnimatorControllerLayer layer, AnimatorLayerBlendingMode blendingMode, float weight, bool ikPass, AvatarMask mask)
    {
        layer.blendingMode = blendingMode;
        layer.avatarMask = mask;
        layer.defaultWeight = weight;
        layer.iKPass = ikPass;

        if (layer.stateMachine == null)
        {
            layer.stateMachine = new AnimatorStateMachine();
        }

        return layer;
    }

    static AvatarMask CreateAvatarMask(string assetPath, MaskBodyParts activatedParts, Transform maskTransform)
    {
        AvatarMask mask = null;
        if (File.Exists(assetPath))
        {
            mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(assetPath);
        }
        else
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, assetPath);
        }

        PopulateMaskBodyParts(mask, maskTransform);


        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            MaskBodyParts flag = (MaskBodyParts)(1 << i);
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, (activatedParts & flag) == flag);
        }
        return mask;
    }

    static void PopulateMaskBodyParts(AvatarMask mask, Transform bodyParts)
    {
        mask.transformCount = VHUtils.CountDescendents(bodyParts);

        int index = 0;
        Stack<Transform> transformStack = new Stack<Transform>();
        transformStack.Push(bodyParts);

        while (transformStack.Count > 0)
        {
            Transform trans = transformStack.Pop();
            mask.SetTransformPath(index, AnimationUtility.CalculateTransformPath(trans, bodyParts));
            mask.SetTransformActive(index, true);
            index += 1;

            for (int i = trans.childCount - 1; i >= 0; i--)
            {
                transformStack.Push(trans.GetChild(i));
            }
        }
    }

    static AnimatorState AddAnimatorBodyState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        bool isPosture = IsPostureAnimation(stateName);
        if (!isPosture)
        {
            string postureStateName = GetAnimationPostureRoot(stateName);
            ChildAnimatorState postureState = Array.Find<ChildAnimatorState>(stateMachine.states, s => s.state.name == postureStateName);
            if (postureState.state != null)
            {
                // non-idle anims need a way to transition back to the idle after they finish playing
                AnimatorStateTransition exitTransition = state.AddExitTransition();
                exitTransition.hasExitTime = true;
                exitTransition.destinationState = postureState.state;
            }
        }

        state.motion = motion;
        return state;
    }

    static void SetupFaceLayer(AnimatorController controller, AnimatorControllerLayer faceLayer, AvatarMask animClipMask, List<string> faceAnimationPaths, OptionFlags options)
    {
        BlendTree faceBlendTree = null;
        controller.CreateBlendTreeInController("Face", out faceBlendTree, 2);
        faceBlendTree.name = "Face";
        faceBlendTree.blendType = BlendTreeType.Direct;

        foreach (string motionFilePath in faceAnimationPaths)
        {
            SetupAnimationClip(motionFilePath, animClipMask, false, IsOptionOn(options, OptionFlags.Force_Animation_Reimport));
            string motionName = Path.GetFileNameWithoutExtension(motionFilePath);
            string paramName = GetFaceAnimationName(motionName);
            bool isFaceNeutral = paramName.ToLower().Contains("face_neutral");

            AnimatorControllerParameter param = Array.Find<AnimatorControllerParameter>(controller.parameters, c => c.name == paramName);
            if (param == null)
            {
                // create a float param to control the face
                param = new AnimatorControllerParameter();
                param.name = paramName;
                param.type = AnimatorControllerParameterType.Float;
                param.defaultFloat = isFaceNeutral ? 1 : 0;
                controller.AddParameter(param);
            }

            // setup face
            Motion motion = LoadMotion(motionFilePath);
            motion.name = motionName;
            faceBlendTree.AddChild(motion);

            // setup direct blend tree param to the same param that we just created on the controller
            ChildMotion[] childMotions = faceBlendTree.children;
            int index = Array.FindIndex<ChildMotion>(childMotions, cm => cm.motion == motion);
            if (index != -1)
            {
                childMotions[index].directBlendParameter = paramName;
                faceBlendTree.children = childMotions;
            }
            else
            {
                Debug.LogError("Can't find motion " + motion.name + " in Face blend Tree");
            }
        }

        var serializedObject = new UnityEditor.SerializedObject(faceBlendTree);
        var normalizedBlendValues= serializedObject.FindProperty("m_NormalizedBlendValues");
        if (normalizedBlendValues != null)
        {
            normalizedBlendValues.boolValue = true;
            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfDirtyOrScript();
        }

        AnimatorState faceState = new AnimatorState();

        faceState.name = faceBlendTree.name;
        faceState.motion = faceBlendTree;
    }


    static void RemoveLayerStates(AnimatorControllerLayer layer)
    {
        if (layer.stateMachine != null)
        {
            layer.stateMachine.states = new ChildAnimatorState[1];
        }
    }
    #endif
 */

    #endregion

    #endregion

}
}
