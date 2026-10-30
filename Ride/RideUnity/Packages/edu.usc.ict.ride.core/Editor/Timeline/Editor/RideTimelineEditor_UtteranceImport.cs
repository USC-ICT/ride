using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using Ride.Timeline;
using System;
using System.IO;
using UnityEngine.Playables;

/// <summary>Generates virtual-human cutscene Timeline assets from utterance audio and metadata files.</summary>
/// <remarks>
/// Opened from <c>Ride/Timeline/Generate Utterance Timeline</c>, this window supports a manually
/// selected utterance or a batch folder. It matches text, BML lip-sync, and XML behavior assets to
/// audio clips by name, creates <see cref="TimelineAsset"/> files, and adds a
/// <see cref="PlayableDirector"/> GameObject for each generated Timeline to the active scene.
/// See <a href="https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorWindow.html">Unity EditorWindow documentation</a>.
/// </remarks>
public class RideTimelineEditor_UtteranceImport : EditorWindow
{
    /// <summary>Source folder used for batch asset discovery.</summary>
    static string m_importFolderPath;

    /// <summary>Destination folder for generated Timeline assets.</summary>
    static string m_exportFolderPath;

    /// <summary>Validation message for the configured import and export paths.</summary>
    private string m_pathDebugMessage;

    /// <summary>Index of the selected single or batch generation mode.</summary>
    private int m_batchSelected = 0;

    /// <summary>Labels displayed by the generation-mode selection control.</summary>
    private string[] m_batchOptions = new string[] { "Single", "Batch" };

    /// <summary>Audio clip used in single-utterance mode.</summary>
    public AudioClip m_audio;

    /// <summary>Utterance text asset used in single-utterance mode.</summary>
    public TextAsset m_utteranceText;

    /// <summary>BML lip-sync asset used in single-utterance mode.</summary>
    public TextAsset m_lipSyncInfo;

    /// <summary>Behavior XML asset used in single-utterance mode.</summary>
    public TextAsset m_xml;

    /// <summary>Virtual-human character name assigned to generated cutscene clips.</summary>
    public string m_characterName;

    /// <summary>Status message produced by cutscene discovery or generation.</summary>
    private string m_generationDebugMessage;

    /// <summary>Required cutscene assets keyed by the matching audio clip name.</summary>
    private Dictionary<string, UtteranceElements> m_cutscenes = new Dictionary<string, UtteranceElements>();  /// KEY: name of the cutscene, based on name of audio clip. VALUE: Assets required to create cutscenes.

    /// <summary>Timeline assets awaiting instantiation as Playable Directors in the active scene.</summary>
    private List<TimelineAsset> m_createdTimelines = new List<TimelineAsset>();

    /// <summary>Opens the utterance Timeline generation window.</summary>
    [MenuItem("Ride/Timeline/Generate Utterance Timeline")]
    public static void ShowWindow()
    {
        GetWindow<RideTimelineEditor_UtteranceImport>(true, "Generate Utterance Timeline", false);
    }

    /// <summary>Draws the source, destination, generation-mode, asset, and status controls.</summary>
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

    /// <summary>Adds a non-autoplaying Playable Director for each generated Timeline to the active scene.</summary>
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

    /// <summary>Builds the set of valid cutscene inputs for the selected single or batch mode.</summary>
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

    /// <summary>Creates a Timeline asset and virtual-human cutscene clip for every valid utterance.</summary>
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

    /// <summary>Loads assets whose filenames contain the requested extension marker from the batch import folder.</summary>
    /// <typeparam name="T">Unity asset type to load.</typeparam>
    /// <param name="fileExtension">
    /// Filename marker used to distinguish audio, utterance, BML, and XML inputs. BML and XML
    /// assets may have a physical <c>.txt</c> extension, so matching uses the complete filename.
    /// </param>
    /// <returns>Assets of type <typeparamref name="T"/> that match the marker.</returns>
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

    /// <summary>Finds the first text asset whose name contains the audio clip name.</summary>
    /// <param name="fileName">Audio clip name used as the matching key.</param>
    /// <param name="assets">Candidate text assets.</param>
    /// <returns>The first matching text asset, or <see langword="null"/> when none matches.</returns>
    private TextAsset GetMatchingTextAsset(string fileName, List<TextAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.name.Contains(fileName))
                return asset;
        }

        return null;
    }

    /// <summary>Validates the export folder and, in batch mode, the import folder.</summary>
    /// <returns><see langword="true"/> when the required directories exist; otherwise <see langword="false"/>.</returns>
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

    /// <summary>Checks whether all four assets required to generate an utterance cutscene are present.</summary>
    /// <param name="elements">Candidate utterance assets.</param>
    /// <returns><see langword="true"/> when audio, utterance text, BML, and behavior XML are assigned.</returns>
    private bool AreElementsValid(UtteranceElements elements)
    {
        if (elements.m_audio == null || elements.m_utteranceText == null || elements.m_lipSyncInfo == null || elements.m_xml == null)
        {
            m_generationDebugMessage = $"Couldn't generate cutscene director for assets {elements.m_audio.name}.";
            return false;
        }

        return true;
    }

    /// <summary>Groups the audio and metadata assets required to generate one utterance cutscene.</summary>
    [Serializable]
    public struct UtteranceElements
    {
        /// <summary>Spoken audio for the utterance.</summary>
        public AudioClip m_audio;

        /// <summary>Text content of the utterance.</summary>
        public TextAsset m_utteranceText;

        /// <summary>BML timing and lip-sync data.</summary>
        public TextAsset m_lipSyncInfo;

        /// <summary>Behavior XML containing animation and nonverbal behavior directives.</summary>
        public TextAsset m_xml;
    }
}
