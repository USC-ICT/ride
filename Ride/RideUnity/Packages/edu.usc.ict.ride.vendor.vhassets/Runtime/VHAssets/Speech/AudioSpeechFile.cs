using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VHAssets
{
public class AudioSpeechFile : MonoBehaviour
{
    public sealed class CreationOptions
    {
        public bool ParentInHierarchy = true;
        public Transform Parent;
        public string ParentName = DefaultParentName;
        public string ExplicitName;
    }

    #region Variables
    const string DefaultParentName = "AudioSpeech";
    static readonly Dictionary<Scene, Transform> s_DefaultParentsByScene = new Dictionary<Scene, Transform>();

    public TextAsset m_LipSyncInfo;
    public TextAsset m_UtteranceText;
    public TextAsset m_Xml;
    public AudioClip m_AudioClip;
    BMLReader m_BMLReader;
    public BMLReader.UtteranceTiming m_UtteranceTiming = new BMLReader.UtteranceTiming();
    string m_ConvertedXml = "";
    string m_LipSyncInfoText = "";
    #endregion

    #region Properties
    public float Length
    {
        get { return m_UtteranceTiming.m_Timings.Count > 0 ? m_UtteranceTiming.m_Timings[m_UtteranceTiming.m_Timings.Count - 1].time : 0; }
    }

    public float ClipLength
    {
        get { return m_AudioClip != null ? m_AudioClip.length : 0; }
    }

    public string UtteranceText
    {
        get { return m_UtteranceText != null ? m_UtteranceText.text : ""; }
    }

    public string BmlText
    {
        get { return m_LipSyncInfoText; }
        set { m_LipSyncInfoText = value; }
    }

    public BMLReader.UtteranceTiming UtteranceTiming
    {
        get { return m_UtteranceTiming; }
    }

    public string ConvertedXml
    {
        get { return m_ConvertedXml; }
        set { m_ConvertedXml = value; }
    }
    #endregion

    #region Functions
    void Awake()
    {
        m_BMLReader = new BMLReader();
    }

    public void Start()
    {
        if (m_LipSyncInfo != null)
        {
            m_LipSyncInfoText = m_LipSyncInfo.text;
        }

        ReadBmlData();
        if (m_Xml != null)
        {
            m_ConvertedXml = ConvertXmlToSmartbodyReadable(m_Xml.text);
        }

    }

    public string GetUtteranceText()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < m_UtteranceTiming.m_Timings.Count; i++)
        {
            builder.Append(m_UtteranceTiming.m_Timings[i].text + " ");
        }
        return builder.ToString();
    }

    public BMLReader.UtteranceTiming ReadBmlData()
    {
        if (m_BMLReader == null)
        {
            m_BMLReader = new BMLReader();
        }
        //if (m_LipSyncInfo != null)
        if (!string.IsNullOrEmpty(BmlText))
        {
            m_UtteranceTiming = m_BMLReader.ReadBml(BmlText);
        }
        else if (m_LipSyncInfo != null && !string.IsNullOrEmpty(m_LipSyncInfo.text))
        {
            m_UtteranceTiming = m_BMLReader.ReadBml(m_LipSyncInfo.text);
        }
        else
        {
            Debug.LogWarning("There is no lip sync file assigned to utterance " + name);
        }
        return m_UtteranceTiming;
    }


    public static string ConvertXmlToSmartbodyReadable(string xmlContents)
    {
        string bml = xmlContents;
        bml = bml.Replace(@"<?xml version=""1.0""?>", "");
        bml = bml.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>", "");
        bml = bml.Replace(@"\r\n", "");
        bml = bml.Replace(@"\n", "");
        bml = bml.Replace(System.Environment.NewLine, "");
        //Debug.Log(bml);
        return bml;
    }

    public static AudioSpeechFile CreateAudioSpeechFile(string lipSyncInfo, string xml, AudioClip clip, CreationOptions options = null)
    {
        options ??= new CreationOptions();

        string goName = ResolveName(clip, options);
        GameObject go = new GameObject(goName);
        Transform parent = ResolveParent(options);
        if (parent != null)
            go.transform.SetParent(parent, false);

        AudioSpeechFile audio = go.AddComponent<AudioSpeechFile>();
        audio.m_LipSyncInfoText = lipSyncInfo;
        audio.m_AudioClip = clip;
        //audio.ConvertedXml = ConvertXmlToSmartbodyReadable(xml);
        audio.ConvertedXml = xml;
        audio.ReadBmlData();
        return audio;
    }

    static string ResolveName(AudioClip clip, CreationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ExplicitName))
            return options.ExplicitName.Trim();

        if (clip != null && !string.IsNullOrWhiteSpace(clip.name))
            return clip.name.Trim();

        float clipLength = clip != null ? clip.length : 0f;
        int clipLengthSeconds = Mathf.Max(0, Mathf.CeilToInt(clipLength));
        return $"{DateTime.Now:yyyyMMdd-HHmmss}-audio-{clipLengthSeconds}s";
    }

    static Transform ResolveParent(CreationOptions options)
    {
        if (!options.ParentInHierarchy)
            return null;

        if (options.Parent != null)
            return options.Parent;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        if (s_DefaultParentsByScene.TryGetValue(scene, out Transform cachedParent))
        {
            if (cachedParent != null && cachedParent.gameObject.scene == scene)
                return cachedParent;

            s_DefaultParentsByScene.Remove(scene);
        }

        string parentName = string.IsNullOrWhiteSpace(options.ParentName)
            ? DefaultParentName
            : options.ParentName.Trim();

        foreach (var rootGameObject in scene.GetRootGameObjects())
        {
            if (rootGameObject.name != parentName)
                continue;

            s_DefaultParentsByScene[scene] = rootGameObject.transform;
            return rootGameObject.transform;
        }

        GameObject parentObject = new GameObject(parentName);
        SceneManager.MoveGameObjectToScene(parentObject, scene);
        s_DefaultParentsByScene[scene] = parentObject.transform;
        return parentObject.transform;
    }
    #endregion
}
}
