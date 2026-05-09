using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
public class TtsReader : MonoBehaviour
{
    #region Constants
    public class TtsData
    {
        public List<WordTiming> m_WordTimings = new List<WordTiming>();
        public List<MarkData> m_Marks = new List<MarkData>();
    }

    public class WordTiming
    {
        public string word = "";
        public float start;
        public float end;
        public List<VisemeData> m_VisemesUsed = new List<VisemeData>();
        public List<MarkData> m_Marks = new List<MarkData>();

        public WordTiming(string _word, float _start, float _end)
        {
            word = _word;
            start = _start;
            end = _end;
        }

        public float Duration { get { return end - start; } }
    }

    public class VisemeData
    {
        public float start;
        public float articulation;
        public string type = "";

        public VisemeData(float _start, float _articulation, string _type)
        {
            start = _start;
            articulation = _articulation;
            type = _type;
        }
    }

    public class MarkData
    {
        public string name = "";
        public float time;

        public MarkData(string _name, float _time)
        {
            name = _name;
            time = _time;
        }
    }
    #endregion

    #region Variables
    //List<TtsTiming> m_Timings = new List<TtsTiming>();
    [SerializeField] float m_RampPct = 0.0f;
    [SerializeField] float m_WordTimingFudge = 0.1f;
    #endregion

    #region Functions
    public TtsData ReadTtsXml(string xmlStr, out string audioFilePath)
    {
        //m_Character = character;
        //bool succeeded = true;
        StringReader xml = null;
        XmlTextReader reader = null;
        TtsData ttsData = null;
        audioFilePath = "";

#if !UNITY_WSA
        try
        {
            xml = new StringReader(xmlStr);
            reader = new XmlTextReader(xml);
            ttsData = ParseTts(reader, out audioFilePath);
        }
        catch (Exception e)
        {
            //succeeded = false;
            Debug.LogError(string.Format("Failed when loading. Error: {0} {1}. couldn't load string {2}", e.Message, e.InnerException, xmlStr));
        }
        finally
        {
            xml?.Close();
            reader?.Close();
        }
#else
        Debug.LogErrorFormat("TtsReader.ReadTtsXml() - not implemented on this platform.");
#endif

        return ttsData;
    }

    TtsData ParseTts(XmlTextReader reader, out string audioFilePath)
    {
        TtsData ttsData = new TtsData();
        List<WordTiming> timings = new List<WordTiming>();
        List<VisemeData> sameTimeVisemes = new List<VisemeData>();
        VisemeData lastViseme = null;
        List<string> visemesUsed = new List<string>();
        WordTiming currentWordTiming = null;
        StringBuilder currentWordText = new StringBuilder();
        audioFilePath = "";


        WordTiming firstTiming = new WordTiming("", 0, 0.01f);
        timings.Add(firstTiming);
        //foreach (string viseme in visemesUsed)
        {
            // all visemes should be weight 0 at time 0
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "open"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "FV"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "tBack"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "tRoof"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "wide"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "W"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "PBM"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "ShCh"));
            firstTiming.m_VisemesUsed.Add(new VisemeData(0, 0, "tTeeth"));
        }

        sameTimeVisemes.AddRange(firstTiming.m_VisemesUsed);

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                if (reader.Name == "soundFile")
                {
                    audioFilePath = reader["name"];
                }
                else if (reader.Name == "word")
                {
                    currentWordTiming = CreateWordTimingData("", reader["start"], reader["end"]);
                    currentWordText.Clear();

                    if (timings.Count > 1)
                    {
                        foreach (VisemeData prevTimeViseme in sameTimeVisemes)
                        {
                            float startTime = Mathf.Max(Mathf.Lerp(prevTimeViseme.start, currentWordTiming.end, m_RampPct), currentWordTiming.end - m_WordTimingFudge);
                            VisemeData backToZeroViseme = new VisemeData(startTime, 0, prevTimeViseme.type);
                            //VisemeData backToZeroViseme = new VisemeData(Mathf.Max(Mathf.Max(currentWordTiming.end- 0.1f, prevTimeViseme.start), currentWordTiming.end * m_RampPct), 0, prevTimeViseme.type);
                            timings[timings.Count - 1].m_VisemesUsed.Add(backToZeroViseme);
                        }
                        sameTimeVisemes.Clear();
                    }

                    // Store the WordTiming immediately so nested viseme parsing can continue to append to it.
                    // The closing </word> handler later fills in the same object's .word text.
                    timings.Add(currentWordTiming);
                }
                else if (reader.Name == "mark")
                {
                    MarkData markData = new MarkData(reader["name"], float.Parse(reader["time"]));
                    ttsData.m_Marks.Add(markData);
                }
                else if (reader.Name == "viseme")
                {
                    VisemeData visemeData = CreateVisemeData(reader["start"], reader["articulation"], reader["type"]);

                    if (visemeData != null)
                    {
                        if (!visemesUsed.Contains(visemeData.type))
                        {
                            visemesUsed.Add(visemeData.type);
                        }

                        if (lastViseme == null || (lastViseme.start == 0 && Mathf.Abs(visemeData.start - lastViseme.start) < Mathf.Epsilon))
                        {
                            // make sure it's not in the list
                            if (sameTimeVisemes.FindIndex(v => v.type == visemeData.type) == -1)
                            {
                                // same time as last viseme parsed
                                sameTimeVisemes.Add(visemeData);
                            }
                        }
                        else
                        {
                            // this is a different time, the previous visemes should drop their weight to 0 at this new time
                            foreach (VisemeData prevTimeViseme in sameTimeVisemes)
                            {
                                float startTime = Mathf.Max(Mathf.Lerp(prevTimeViseme.start, visemeData.start, m_RampPct), visemeData.start - m_WordTimingFudge);
                                VisemeData backToZeroViseme = new VisemeData(startTime, 0, prevTimeViseme.type);
                                //VisemeData backToZeroViseme = new VisemeData(Mathf.Max(Mathf.Max(visemeData.start - 0.1f, prevTimeViseme.start), visemeData.start * m_RampPct ), 0, prevTimeViseme.type);
                                timings[timings.Count - 1].m_VisemesUsed.Add(backToZeroViseme);
                            }

                            sameTimeVisemes.Clear();
                            sameTimeVisemes.Add(visemeData);
                        }

                        if (timings.Count > 0)
                        {
                            float startTime = Mathf.Max(visemeData.start * m_RampPct, visemeData.start - m_WordTimingFudge);
                            timings[timings.Count - 1].m_VisemesUsed.Add(new VisemeData(startTime, 0, visemeData.type));
                            timings[timings.Count - 1].m_VisemesUsed.Add(visemeData);
                        }

                        lastViseme = visemeData;
                    }
                }
                break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                if (currentWordTiming != null)
                    currentWordText.Append(reader.Value);
                break;
                case XmlNodeType.EndElement:
                if (reader.Name == "word" && currentWordTiming != null)
                {
                    // currentWordTiming is the same WordTiming instance already stored in timings above,
                    // so assigning .word here updates the persisted entry before we clear the local handle.
                    currentWordTiming.word = currentWordText.ToString().Trim();
                    currentWordTiming = null;
                    currentWordText.Clear();
                }
                break;
            }
        }

        /*if (timings.Count > 0)
        {
            foreach (string viseme in visemesUsed)
            {
                // all visemes should be weight 0 at time 0
                timings[0].m_VisemesUsed.Add(new VisemeData(0, 0, viseme));
            }
        }*/

        ttsData.m_WordTimings = timings;

        PostProcessVisemes(ttsData);

        return ttsData;
    }

    void PostProcessVisemes(TtsData ttsData)
    {
        // if the next word timing's viseme set has the same viseme, then don't drop the viseme to 0
        for (int i = 0; i < ttsData.m_WordTimings.Count - 1; i++)
        {
            List<VisemeData> currVisemes = ttsData.m_WordTimings[i].m_VisemesUsed;
            List<VisemeData> nextVisemes = ttsData.m_WordTimings[i + 1].m_VisemesUsed;

            HashSet<string> alreadyChecked = new HashSet<string>();
            for (int j = 0; j < currVisemes.Count; j++)
            {
                VisemeData currViseme = currVisemes[j];
                if (alreadyChecked.Contains(currViseme.type))
                {
                    continue;
                }

                if (nextVisemes.Find(v => v.type == currViseme.type) != null)
                {
                    // the next set of visemes contains the same one that we have in our current set, so don't reset it to 0
                    int index = currVisemes.FindLastIndex(v => v.type == currViseme.type);
                    currVisemes.RemoveAt(index);
                    j--;
                }

                alreadyChecked.Add(currViseme.type);
            }
            
            
            //nextVisemes.Find(v => v.type == );
        }
    }

    WordTiming CreateWordTimingData(string word, string start, string end)
    {
        if (!float.TryParse(start, out float startTime))
        {
            Debug.LogError("Failed to parse start time");
            return null;
        }

        if (!float.TryParse(end, out float endTime))
        {
            Debug.LogError("Failed to parse endTime");
            return null;
        }

        return new WordTiming(word?.Trim() ?? "", startTime, endTime);
    }

    VisemeData CreateVisemeData(string start, string articulation, string type)
    {
        if (!float.TryParse(start, out float startTime))
        {
            Debug.LogError("Failed to parse start time");
            return null;
        }

        if (!float.TryParse(articulation, out float articulationAmount))
        {
            Debug.LogError("Failed to parse articulation");
            return null;
        }

        return new VisemeData(startTime, articulationAmount, type);
    }

    /// <summary>
    /// The key of the dictionary will be T0, T1, Tx .....
    /// </summary>
    /// <returns>The marked words.</returns>
    /// <param name="wordTimings">Word timings.</param>
    public Dictionary<string, WordTiming> GetMarkedWords(List<WordTiming> wordTimings)
    {
        Dictionary<string, WordTiming> markedWords = new Dictionary<string, WordTiming>();
        for (int i = 0; i < wordTimings.Count; i++)
        {
            markedWords.Add("T" + i.ToString(), wordTimings[i]);
        }
        return markedWords;
    }
    #endregion
}
}
