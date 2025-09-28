using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VHAssets
{
public class BMLConverter : EditorWindow
{
    #region Variables
    #endregion

    #region Functions

    void Setup()
    {
    }

    void OnDestroy()
    {
    }

    public static void Convert(string bmlFile)
    {
        if (!bmlFile.Contains(".bml.txt"))
        {
            Debug.LogError("the bml converter only works on files with extension .bml.txt. Can't convert " + bmlFile);
            return;
        }

        string filenameNoExtensions = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(bmlFile));

        GameObject utteranceGO = new GameObject(filenameNoExtensions);
        AudioSpeechFile utterance = utteranceGO.AddComponent<AudioSpeechFile>();

        SetupPrefab(bmlFile, utterance, Path.GetDirectoryName(bmlFile));

        DestroyImmediate(utteranceGO);
        AssetDatabase.SaveAssets();
    }

    public static void SetupPrefab(string bmlFile, AudioSpeechFile utterance, string path)
    {
        AssetDatabase.Refresh();

        utterance.m_LipSyncInfo = (TextAsset)AssetDatabase.LoadAssetAtPath(bmlFile, typeof(TextAsset));
        if (utterance.m_LipSyncInfo == null)
        {
            Debug.LogError("couldn't find: " + bmlFile);
            return;
        }

        string utteranceTextFilePath = string.Format("{0}/{1}.txt", path, utterance.name);
        utterance.m_UtteranceText = (TextAsset)AssetDatabase.LoadAssetAtPath(utteranceTextFilePath, typeof(TextAsset));
        if (utterance.m_UtteranceText == null)
        {
            Debug.Log("couldn't find (not required): " + utteranceTextFilePath);
        }

        string xmlFile = string.Format("{0}/{1}.xml", path, utterance.name);
        utterance.m_Xml = (TextAsset)AssetDatabase.LoadAssetAtPath(xmlFile, typeof(TextAsset));
        if (utterance.m_UtteranceText == null)
        {
            Debug.Log("couldn't find (not required): " + xmlFile);
        }

        string audioFileName = string.Format("{0}/{1}.wav", Path.GetDirectoryName(bmlFile), utterance.name);
        utterance.m_AudioClip = (AudioClip)AssetDatabase.LoadAssetAtPath(audioFileName, typeof(AudioClip));
        if (utterance.m_AudioClip == null)
        {
            // try .ogg instead
            audioFileName = string.Format("{0}/{1}.ogg", Path.GetDirectoryName(bmlFile), utterance.name);
            utterance.m_AudioClip = (AudioClip)AssetDatabase.LoadAssetAtPath(audioFileName, typeof(AudioClip));

            if (utterance.m_AudioClip == null)
            {
                Debug.LogError(string.Format("Failed to load audio clip {0} for utterance {1}", audioFileName, bmlFile));
                return;
            }
        }

        // this updates the inspector
        EditorUtility.SetDirty(utterance);

        // create the output folder, if it doesn't already exist
        string prefabsFolderPath = string.Format("{0}/Prefabs", path);
        if (!Directory.Exists(prefabsFolderPath))
        {
            AssetDatabase.CreateFolder(path, "Prefabs");
            AssetDatabase.Refresh();
        }

        // create the prefab in the output dir
        string prefabPath = string.Format("{0}/{1}.prefab", prefabsFolderPath, utterance.name);
        GameObject previousMotionPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        if (previousMotionPrefab == null)
        {
            PrefabUtility.SaveAsPrefabAsset(utterance.gameObject, prefabPath);
        }
        else
        {
            //PrefabUtility.SaveAsPrefabAssetAndConnect(utterance.gameObject, prefabPath, InteractionMode.AutomatedAction);
            //PrefabUtility.SaveAsPrefabAsset(utterance.gameObject);

            // Unity hasn't created a replacement for this function yet
            // https://forum.unity.com/threads/how-to-generate-prefabs-w-scripts-w-the-new-system.613747/#post-4113010
            #pragma warning disable 0618
            PrefabUtility.ReplacePrefab(utterance.gameObject, previousMotionPrefab, ReplacePrefabOptions.ReplaceNameBased);
        }
    }
#endregion
}
}
