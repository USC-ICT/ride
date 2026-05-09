using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VHAssets
{
/// <summary>
/// Custom import class.  These functions get called on asset import or reimport
/// </summary>
public class VHAssetPostProcessor : AssetPostprocessor
{
    static List<string> m_matDirs = new List<string>();
    static List<string> m_filesToDelete = new List<string>();

    struct SyncPointImportDebugData
    {
        public int AuthoredFrame;
        public float LocalFrame;
        public float ComputedTime;
        public float FirstFrame;
        public float LastFrame;
    }

    // Sync-point user properties are authored in source-take frame numbers. This importer callback
    // does not expose a reliable imported sample rate, so use the authored animation frame rate here.
    // Default to 30 fps because that is the current convention for our authored body clips.
    // If that convention changes in the future, update this value or move the conversion to a later
    // import phase that has access to exact clip timing data.
    const float AuthoredAnimationFrameRate = 30f;

    bool m_customMaterialGeneration = false;


    void OnPreprocessModel()
    {
    }

    void OnPreprocessAnimation()
    {
        var modelImporter = assetImporter as ModelImporter;
        if (modelImporter.clipAnimations.Length == 0)
        {
            modelImporter.clipAnimations = modelImporter.defaultClipAnimations;

            Debug.Log($"VHAssetPostProcessor.OnPreprocessAnimation() - {modelImporter.assetPath} - clipAnimations.Length==0, assigning it to defaultClipAnimations", assetImporter);
        }
    }

    /// <summary>
    /// Reads Maya User Properties. This function is called after OnAssignMaterialModel and before OnPostprocessModel
    /// The fbx gameobject hierachy has not been created and connected at this point
    /// </summary>
    /// <param name="go"></param>
    /// <param name="propNames"></param>
    /// <param name="values"></param>
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] values)
    {
        for (int i = 0; i < propNames.Length; i++)
        {
            var propName = propNames[i];
            if ("CustomMaterialGeneration" == propName)
            {
                var value = values[i];
                m_customMaterialGeneration = (bool)value;
                break;
            }
        }

        ParseSyncPoints(go, propNames, values);
    }

    /// <summary>
    /// called after OnPostprocessGameObjectWithUserProperties.  At this point, the entire fbx gameobject hierachy has been
    /// created.  The root object of the hierachy (fbx name) is passed into this function
    /// </summary>
    /// <param name="go"></param>
    void OnPostprocessModel(GameObject go)
    {
        GenerateCustomMaterials(go);
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
        AssetDatabase.Refresh();

        DeleteMaterials();

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Reads Maya User Properties. This function is called after OnAssignMaterialModel and before OnPostprocessModel
    /// The fbx gameobject hierachy has not been created and connected at this point
    /// </summary>
    /// <param name="go"></param>
    /// <param name="propNames"></param>
    /// <param name="values"></param>
    void ParseSyncPoints(GameObject go, string[] propNames, object[] values)
    {
        int i = 0;
        try
        {
            var modelImporter = assetImporter as ModelImporter;
            var eventsAdded = new Dictionary<string, SyncPointImportDebugData>();
            int syncPointPropertyCount = 0;
            int zeroValuedSyncPointCount = 0;

            // Go through the properties one by one
            for (i = 0; i < propNames.Length; i++)
            {
                if ("readyTime" == propNames[i]
                    || "strokeStartTime" == propNames[i]
                    || "emphasisTime" == propNames[i]
                    || "strokeTime" == propNames[i]
                    || "relaxTime" == propNames[i])
                {
                    if (modelImporter != null && modelImporter.clipAnimations.Length != 0)
                    {
                        if (modelImporter.animationType == ModelImporterAnimationType.Legacy)
                        {
                            Debug.LogWarning($"VHAssetPostProcessor.ParseSyncPoints() - Animation '{Path.GetFileNameWithoutExtension(modelImporter.assetPath)}' has sync points but is set as a legacy animation so sync point events can't be added. Change it to humanoid or generic", assetImporter);
                            break;
                        }

                        string syncPointName = propNames[i];
                        object rawValue = values[i];
                        int syncPointFrame = (int)rawValue;

                        syncPointPropertyCount++;
                        if (syncPointFrame == 0)
                            zeroValuedSyncPointCount++;

                        ModelImporterClipAnimation[] anims = modelImporter.clipAnimations;

                        // check to see if the sync point event already exists
                        ModelImporterClipAnimation modelClip = anims[0];

                        int existingEventIndex = Array.FindIndex(modelClip.events, e => e.functionName == syncPointName);
                        bool foundEvent = existingEventIndex >= 0;
                        AnimationEvent[] events = null;
                        AnimationEvent syncPointEvent = foundEvent ? modelClip.events[existingEventIndex] : null;
                        if (!foundEvent)
                        {
                            // this event doesn't exist, add a new one
                            // create a deep copy of the events 
                            int size = modelClip.events.Length;
                            events = new AnimationEvent[size + 1];
                            for (int id = 0; id < size; id++)
                            {
                                events[id] = modelClip.events[id];
                            }

                            syncPointEvent = new AnimationEvent();
                            foundEvent = false;
                        }
                        else
                        {
                            events = modelClip.events;
                        }

                        // FBX sync-point metadata is authored as frame numbers on the source take. Example:
                        // if the imported clip is frames 2..57 and the authored sync point is frame 15, the
                        // local frame offset is 13 frames from the imported clip start. Convert that local
                        // frame offset into clip-local seconds using the authored animation frame rate
                        // (currently 30 fps for these assets), so the event fires at 13 / 30 = 0.433s.
                        float localFrame = Mathf.Max(0f, syncPointFrame - modelClip.firstFrame);
                        syncPointEvent.time = localFrame / AuthoredAnimationFrameRate;

                        //Debug.Log(
                        //    $"VHAssetPostProcessor.ParseSyncPoints() - Normalized sync event " +
                        //    $"clip='{modelClip.name}' name='{syncPointName}' frame={syncPointFrame} localFrame={localFrame:F3} " +
                        //    $"timeSeconds={syncPointEvent.time:F6} sampleRate={sampleRate:F3} firstFrame={modelClip.firstFrame} lastFrame={modelClip.lastFrame}",
                        //    assetImporter);

                        // Mirror the key sync metadata into the event parameters so the Unity inspector shows
                        // something useful when the event is selected. Runtime sync lookup still uses the event's
                        // function name and time; these extra parameter values are informational only.
                        syncPointEvent.floatParameter = syncPointEvent.time;
                        syncPointEvent.intParameter = syncPointFrame;
                        syncPointEvent.stringParameter = $"frame={syncPointFrame}, localFrame={localFrame:0.###}, time={syncPointEvent.time:0.###}s";
                        syncPointEvent.functionName = propNames[i];
                        syncPointEvent.messageOptions = SendMessageOptions.DontRequireReceiver;
                        
                        if (!foundEvent)
                        {
                            events[events.Length - 1] = syncPointEvent;
                        }
                        else
                        {
                            // Replace the existing slot explicitly so reimports do not rely on mutating a found
                            // reference in place. This keeps sync-point event updates deterministic.
                            events[existingEventIndex] = syncPointEvent;
                        }

                        // deep copy back
                        modelClip.events = events;
                        modelImporter.clipAnimations = anims;

                        eventsAdded.Add(propNames[i], new SyncPointImportDebugData
                        {
                            AuthoredFrame = syncPointFrame,
                            LocalFrame = localFrame,
                            ComputedTime = syncPointEvent.time,
                            FirstFrame = modelClip.firstFrame,
                            LastFrame = modelClip.lastFrame,
                        });
                    }
                }
            }

            if (eventsAdded.Count > 0)
            {
                var firstSyncPoint = eventsAdded.Values.First();
                string added = string.Join("\n", eventsAdded.Select(kvp =>
                    $"Event: {kvp.Key} - Frame: {kvp.Value.AuthoredFrame} - LocalFrame: {kvp.Value.LocalFrame:0.###} - Time: {kvp.Value.ComputedTime:0.###}s"));
                Debug.Log(
                    $"VHAssetPostProcessor.ParseSyncPoints() - Animation '{modelImporter.assetPath}' sync points added " +
                    $"(firstFrame={firstSyncPoint.FirstFrame:0.###}, lastFrame={firstSyncPoint.LastFrame:0.###}, authoredFps={AuthoredAnimationFrameRate:0.###}):\n{added}",
                    assetImporter);
            }

            if (syncPointPropertyCount > 0 && zeroValuedSyncPointCount == syncPointPropertyCount)
                Debug.LogWarning($"VHAssetPostProcessor.ParseSyncPoints() - Animation '{modelImporter.assetPath}' imported {syncPointPropertyCount} sync-point properties and all resolved to frame 0. This usually indicates missing or invalid source metadata.", assetImporter);
        }
        catch (Exception e)
        {
            Debug.LogError($"OnPostprocessGameObjectWithUserProperties caught an error on: {go.name} - propName: {propNames[i]} - Exception: {e.Message}");
        }
    }

    void GenerateCustomMaterials(GameObject go)
    {
        // they want to do a custom material search
        if (m_customMaterialGeneration)
        {
            m_customMaterialGeneration = false;

            string startingPath = Path.GetDirectoryName(assetPath);

            // get the root object and then get all of its children
            Renderer[] allRenderersInFBX = go.GetComponentsInChildren<Renderer>();
            foreach (var renderer in allRenderersInFBX)
            {
                if (renderer.sharedMaterials == null)
                    continue;

                var newSharedMaterials = new List<Material>();
                foreach (var sharedMaterial in renderer.sharedMaterials)
                {
                    string unityGeneratedMaterialPath = AssetDatabase.GetAssetPath(sharedMaterial);
                    string customMaterialName = sharedMaterial.name.Replace(go.name + "-", "");  // strip out the fbx name and the - from the default unity material name
                    string unityMaterialPathWithFilename = Application.dataPath.Replace("Assets", "") + unityGeneratedMaterialPath;  // the path to the unity generated material
                    string unityMaterialName = Path.GetFileNameWithoutExtension(unityMaterialPathWithFilename);  // the name of the unity generated material
                    string materialFile = FindMaterial(startingPath, customMaterialName + ".mat", assetImporter);  // look upwards in folders called "Materials" to see if this already exists
                    if (materialFile != null)
                    {
                        // The material already exists, use it
                        materialFile = materialFile.Replace('\\', '/');  // All asset names & paths in Unity use forward slashes, paths using backslashes will not work.
                        materialFile = VHFile.RemovePathUpTo("Assets/", materialFile);  // remove everything before Unity's "Assets" folder, otherwise the load fails
                        newSharedMaterials.Add(AssetDatabase.LoadAssetAtPath<Material>(materialFile));
                    }
                    else
                    {
                        // Create the material directory as a sibling to the model file
                        if (!Directory.Exists(startingPath + "/Materials"))
                            Directory.CreateDirectory(startingPath + "/Materials");

                        string creationPath = startingPath + "/Materials/" + customMaterialName + ".mat";
                        sharedMaterial.name = customMaterialName;

                        // create the material with the fbx name removed as a prefix
                        AssetDatabase.CreateAsset(new Material(sharedMaterial), creationPath);
                        newSharedMaterials.Add(AssetDatabase.LoadAssetAtPath<Material>(creationPath));
                    }

                    // get rid of the unity generated material
                    if (File.Exists(unityMaterialPathWithFilename) && customMaterialName != unityMaterialName)
                    {
                        if (m_filesToDelete.Contains(unityMaterialPathWithFilename) == false)
                            m_filesToDelete.Add(unityMaterialPathWithFilename);
                        if (m_filesToDelete.Contains(unityMaterialPathWithFilename + ".meta") == false)
                            m_filesToDelete.Add(unityMaterialPathWithFilename + ".meta");
                    }

                    // cache the Materials directory if it is exists
                    string unityGeneratedMaterialsDirectory = Path.GetDirectoryName(unityMaterialPathWithFilename);
                    if (Directory.Exists(unityGeneratedMaterialsDirectory))
                    {
                        if (m_matDirs.Contains(unityGeneratedMaterialsDirectory) == false)
                            m_matDirs.Add(unityGeneratedMaterialsDirectory);
                    }
                }

                // give the sharedMaterials array the new material list
                renderer.sharedMaterials = newSharedMaterials.ToArray();
            }
        }
    }

    /// <summary>
    /// Starts in the passed in directory directory and looks for the material named filename in a folder called "Materials".
    /// Recurses upwards if it can't find it.
    /// </summary>
    /// <param name="startPath"></param>
    /// <param name="fileName"></param>
    /// <returns>returns the file path + filename of the requested file, null if it isn't found</returns>
    public static string FindMaterial(string startPath, string fileName, AssetImporter assetImporter)
    {
        const string MaterialFolderName = "/Materials";

        string retVal = null;

        if (string.IsNullOrEmpty(startPath) || string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("Bad parameter(s) passed into FindMaterial");
            return null;
        }

        string fileNameWithPath = string.Empty;
        string searchPath = startPath;
        while (!string.IsNullOrEmpty(searchPath))
        {
            fileNameWithPath = searchPath + MaterialFolderName + "/" + fileName;
            if (Directory.Exists(searchPath + MaterialFolderName) && File.Exists(fileNameWithPath))
            {
                // the material exists, get out
                retVal = fileNameWithPath;

                Debug.Log($"VHAssetPostProcessor.FindMaterial() - {fileName} found in: {searchPath + MaterialFolderName}", assetImporter);
                break;
            }

            // material still not found, move up one directory
            int lastForwardSlash = searchPath.LastIndexOf("/");
            if (lastForwardSlash > -1)
            {
                searchPath = searchPath.Remove(lastForwardSlash);
                //Debug.Log("searchPath: " + searchPath);
            }
            else
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(retVal))
            Debug.Log($"VHAssetPostProcessor.FindMaterial() - {fileName} not found in: {startPath}", assetImporter);

        return retVal;
    }

    static void DeleteMaterials()
    {
        foreach (string folder in m_matDirs)
        {
            if (!Directory.Exists(folder))
                continue;

            // get the files in the directory to delete
            int fileMatchCounter = 0;
            string [] filesInFolder = Directory.GetFiles(folder);
            foreach (string file in filesInFolder)
            {
                string fileModified = file;
                fileModified = fileModified.Replace('\\', '/');

                //Debug.Log("filesInFolder[j]: " + filesInFolder[j]);

                if (m_filesToDelete.Contains(fileModified))
                {
                    // delete
                    ++fileMatchCounter;
                    m_filesToDelete.Remove(fileModified);
                    int assetsIndex = fileModified.LastIndexOf("Assets/");
                    if (assetsIndex != -1)
                    {
                        string fileToDelete = fileModified.Remove(0, assetsIndex);
                        AssetDatabase.DeleteAsset(fileToDelete);

                        Debug.LogWarning($"VHAssetPostProcessor.OnPostprocessAllAssets() - Deleted: {fileToDelete}");
                    }
                }
            }

            //Debug.Log("fileMatchCounter: " + fileMatchCounter + " filesInFolder.Length: " + filesInFolder.Length);
            if (fileMatchCounter == filesInFolder.Length)
            {
                // the folder is now empty, so delete the folder too
                int assetsIndex = folder.LastIndexOf("Assets/");
                string folderToDelete = folder.Remove(0, assetsIndex);
                AssetDatabase.DeleteAsset(folderToDelete);

                Debug.LogWarning($"VHAssetPostProcessor.OnPostprocessAllAssets() - Deleted: {folderToDelete}");
            }
        }
    }
}
}
