using System.Text;
using UnityEngine;

namespace VHAssets
{
public class AudioSpeechFile : MonoBehaviour
{
    #region Variables
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

    public static AudioSpeechFile CreateAudioSpeechFile(string lipSyncInfo, string xml, AudioClip clip)
    {
        GameObject go = new GameObject(clip.name);
        if (string.IsNullOrEmpty(go.name.Trim()))
        {
            go.name = "UnnamedAudioClip";
        }
        AudioSpeechFile audio = go.AddComponent<AudioSpeechFile>();
        audio.m_LipSyncInfoText = lipSyncInfo;
        audio.m_AudioClip = clip;
        //audio.ConvertedXml = ConvertXmlToSmartbodyReadable(xml);
        audio.ConvertedXml = xml;
        audio.ReadBmlData();
        return audio;
    }
    #endregion
}
}
