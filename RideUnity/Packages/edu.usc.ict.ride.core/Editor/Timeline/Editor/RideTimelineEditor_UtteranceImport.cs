using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using Ride.Timeline;
using System;
using System.IO;
using UnityEngine.Playables;

public class RideTimelineEditor_UtteranceImport : EditorWindow
{
    static string m_importFolderPath;
    static string m_exportFolderPath;
    private string m_pathDebugMessage;

    private int m_batchSelected = 0;
    private string[] m_batchOptions = new string[] { "Single", "Batch" };
    public AudioClip m_audio;
    public TextAsset m_utteranceText;
    public TextAsset m_lipSyncInfo;
    public TextAsset m_xml;
    public string m_characterName;

    private string m_generationDebugMessage;

    /// KEY: name of the cutscene, based on name of audio clip. VALUE: Assets required to create cutscenes.
    public Dictionary<string, UtteranceElements> m_cutscenes = new Dictionary<string, UtteranceElements>();
    private List<TimelineAsset> m_createdTimelines = new List<TimelineAsset>();

    [MenuItem("Ride/Timeline/Generate Cutscene Timeline")]
    public static void ShowWindow()
    {
        GetWindow<RideTimelineEditor_UtteranceImport>(true, "Generate Cutscene Timeline", false);
    }

    public void OnGUI()
    {
        var debugTextStyle = new GUIStyle(EditorStyles.label);
        debugTextStyle.normal.textColor = Color.magenta;

        using (new GUILayout.VerticalScope())
        {
            m_importFolderPath = EditorGUILayout.TextField("Import Path", m_importFolderPath);
            m_exportFolderPath = EditorGUILayout.TextField("Export Path", m_exportFolderPath);

            GUILayout.Label(m_pathDebugMessage, debugTextStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }


        using (new GUILayout.VerticalScope())
        {
            m_batchSelected = GUILayout.SelectionGrid(m_batchSelected, m_batchOptions, 1, EditorStyles.radioButton);

            if (m_batchSelected == 0)
            {
                GUILayout.Space(5f);
                //m_avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", m_avatar, typeof(GameObject), true);
                //m_audioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", m_audioSource, typeof(AudioSource), true);
                m_audio = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", m_audio, typeof(AudioClip), true);
                m_utteranceText = (TextAsset)EditorGUILayout.ObjectField("Utterance Text", m_utteranceText, typeof(TextAsset), true);
                m_lipSyncInfo = (TextAsset)EditorGUILayout.ObjectField("Lipsync bml", m_lipSyncInfo, typeof(TextAsset), true);
                m_xml = (TextAsset)EditorGUILayout.ObjectField("xml", m_xml, typeof(TextAsset), true);
                m_characterName = EditorGUILayout.TextField("Character Name", m_characterName);
            }
            else
            {
                m_characterName = EditorGUILayout.TextField("Character Name", m_characterName);
            }
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        GUILayout.Space(10f);

        using (new GUILayout.VerticalScope())
        {
            if (GUILayout.Button("Create Cutscene Timeline in Active Scene"))
            {
                if (ArePathsValid() == false) return;

                PopulateCutsceneList();
                CreateDirectorAssets();
                InstantiateInTheScene();
            }
            GUILayout.Label(m_generationDebugMessage, debugTextStyle);
        }
    }

    private void InstantiateInTheScene()
    {
        foreach (var timeline in m_createdTimelines)
        {
            var directorObject = new GameObject();
            directorObject.name = timeline.name;

            var directorComp = directorObject.AddComponent<PlayableDirector>();
            directorComp.playableAsset = timeline;
            directorComp.playOnAwake = false;
        }

        m_createdTimelines.Clear();
        /*todo 
         get the reference to the scene
        Add GameObject
        Add PlayableDirector to GO
        Add newly added director as a reference
         */
    }

    private void PopulateCutsceneList()
    {
        m_cutscenes.Clear();

        List<AudioClip> audioClips = new List<AudioClip>();
        List<TextAsset> utteranceTexts = new List<TextAsset>();
        List<TextAsset> lipsyncTexts = new List<TextAsset>();
        List<TextAsset> xmlTexts = new List<TextAsset>();

        if (m_batchSelected == 0)
        {
            audioClips.Add(m_audio);
            utteranceTexts.Add(m_utteranceText);
            lipsyncTexts.Add(m_lipSyncInfo);
            xmlTexts.Add(m_xml);
        }
        else
        {
            audioClips = GetAssetsOfType<AudioClip>(".ogg");
            utteranceTexts = GetAssetsOfType<TextAsset>(".txt");
            lipsyncTexts = GetAssetsOfType<TextAsset>(".bml");
            xmlTexts = GetAssetsOfType<TextAsset>(".xml");

        }

        foreach (var audioClip in audioClips)
        {
            UtteranceElements utteranceElement = new UtteranceElements();

            var utterance = GetMatchingTextAsset(audioClip.name, utteranceTexts);
            var lipsync = GetMatchingTextAsset(audioClip.name, lipsyncTexts);
            var xml = GetMatchingTextAsset(audioClip.name, xmlTexts);

            utteranceElement.m_audio = audioClip;
            utteranceElement.m_utteranceText = utterance;
            utteranceElement.m_lipSyncInfo = lipsync;
            utteranceElement.m_xml = xml;

            if (AreElementsValid(utteranceElement) == false)
                continue;

            m_cutscenes.Add(audioClip.name, utteranceElement);
            m_generationDebugMessage = string.Empty;
        }
    }

    private void CreateDirectorAssets()
    {
        //TODO: Make sure there isn't any director asset with same name already in the directory.
        m_createdTimelines.Clear();

        foreach (var cutscene in m_cutscenes)
        {
            TimelineAsset timeline = CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, m_exportFolderPath + $"/{cutscene.Key}.playable");

            RideTimelineTrack track = timeline.CreateTrack<RideTimelineTrack>();
            TimelineClip clip = track.CreateClip<Clip_vhCutscene>();

            Clip_vhCutscene cutsceneScript = clip.asset as Clip_vhCutscene;
            cutsceneScript.m_behaviour.m_audio = cutscene.Value.m_audio;
            cutsceneScript.m_behaviour.m_utteranceText = cutscene.Value.m_utteranceText;
            cutsceneScript.m_behaviour.m_lipSyncInfo = cutscene.Value.m_lipSyncInfo;
            cutsceneScript.m_behaviour.m_xml = cutscene.Value.m_xml;
            cutsceneScript.m_behaviour.m_characterName = m_characterName;
            cutsceneScript.m_createLayer = true;


            m_createdTimelines.Add(timeline);
        }
    }

    private static List<T> GetAssetsOfType<T>(string fileExtension) where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();

        DirectoryInfo directory = new DirectoryInfo(m_importFolderPath);
        FileInfo[] fileInfo = directory.GetFiles(/*"*" + fileExtension*/);  //Commented out param, bml and xml have .txt extension.


        for (int i = 0; i < fileInfo.Length; i++)
        {
            if (fileInfo[i].Name.Contains(fileExtension) == false)
                continue;
            if (fileExtension == ".txt" && (fileInfo[i].Name.Contains("bml") || fileInfo[i].Name.Contains("xml")))
                continue;

            string filePath = fileInfo[i].FullName;
            filePath = filePath.Substring(filePath.IndexOf("Assets"));
            var asset = AssetDatabase.LoadAssetAtPath<T>(filePath);
            if (asset == null)
                continue;

            assets.Add(asset);
        }

        return assets;
    }

    private TextAsset GetMatchingTextAsset(string fileName, List<TextAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.name.Contains(fileName))
                return asset;
        }

        return null;
    }

    private bool ArePathsValid()
    {
        if (Directory.Exists(m_importFolderPath) == false && m_batchSelected > 0)
        {
            m_pathDebugMessage = "Invalid Import Path.";
            return false;
        }
        if (Directory.Exists(m_exportFolderPath) == false)
        {
            m_pathDebugMessage = "Invalid Export Path.";
            return false;
        }
        m_pathDebugMessage = "";
        return true;
    }

    private bool AreElementsValid(UtteranceElements elements)
    {
        if (elements.m_audio == null || elements.m_utteranceText == null || elements.m_lipSyncInfo == null || elements.m_xml == null)
        {
            m_generationDebugMessage = $"Couldn't generate cutscene director for assets {elements.m_audio.name}.";
            return false;
        }

        return true;
    }

    [Serializable]
    public struct UtteranceElements
    {
        public AudioClip m_audio;
        public TextAsset m_utteranceText;
        public TextAsset m_lipSyncInfo;
        public TextAsset m_xml;
    }
}
