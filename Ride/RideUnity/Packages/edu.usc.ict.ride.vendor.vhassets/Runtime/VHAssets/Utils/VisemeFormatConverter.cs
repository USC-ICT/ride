using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
public static class VisemeFormatConverter
{
    #region Functions
    public static string ConvertTtsToFaceFx(TtsReader.TtsData ttsData)
    {
        string xmlText;
        StringWriter strWriter = new StringWriter();
        XmlWriter xmlWriter = null;
        try
        {
            xmlWriter = new XmlTextWriter(strWriter);
            xmlWriter.WriteStartElement("bml");
            xmlWriter.WriteStartElement("speech");
            xmlWriter.WriteAttributeString("id", "sp1"); // TODO: check this

            xmlWriter.WriteStartElement("text");
            WriteWordAndSyncPoint(xmlWriter, ttsData);

            // end text
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("description");
            xmlWriter.WriteAttributeString("level", "1");
            xmlWriter.WriteAttributeString("type", "audio/x-wav");

            xmlWriter.WriteStartElement("file");
            xmlWriter.WriteAttributeString("ref", "tts_to_facefx");
            // end file
            xmlWriter.WriteEndElement();

            // end description
            xmlWriter.WriteEndElement();

            //end speech
            xmlWriter.WriteEndElement();

            // first loop through to combine all the visemes used for each word
            Dictionary<string, List<TtsReader.VisemeData>> visemeMapper = new Dictionary<string, List<TtsReader.VisemeData>>();
            foreach (TtsReader.WordTiming wordTiming in ttsData.m_WordTimings)
            {
                foreach (TtsReader.VisemeData visemeData in wordTiming.m_VisemesUsed)
                {
                    if (!visemeMapper.ContainsKey(visemeData.type))
                    {
                        visemeMapper.Add(visemeData.type, new List<TtsReader.VisemeData>());
                    }

                    visemeMapper[visemeData.type].Add(visemeData);
                }
            }


            // now go through the dictionary of visemes and write out curves
            xmlWriter.WriteStartElement("curves");
            foreach (KeyValuePair<string, List<TtsReader.VisemeData>> kvp in visemeMapper)
            {
                xmlWriter.WriteStartElement("curve");
                xmlWriter.WriteAttributeString("name", kvp.Key);
                xmlWriter.WriteAttributeString("num_keys", kvp.Value.Count.ToString());
                xmlWriter.WriteAttributeString("owner", "analysis");

                // sort the visemes by time in asc order
                kvp.Value.Sort((a,b) => a.start < b.start ? -1 : 1);

                StringBuilder builder = new StringBuilder();
                foreach (TtsReader.VisemeData visemeData in kvp.Value)
                {
                    builder.Append(string.Format("{0} {1} {2} {3} ", visemeData.start, visemeData.articulation, 0, 0));

                }
                builder = builder.Remove(builder.Length - 1, 1); // remove last space
                xmlWriter.WriteString(builder.ToString());

                // end curve
                xmlWriter.WriteEndElement();
            }

            // end curves
            xmlWriter.WriteEndElement();

            // end bml
            xmlWriter.WriteEndElement();
        }
        catch (System.Exception e)
        {
            Debug.LogError("ConvertTtsToFaceFx failed. Reason: " + e.Message);
        }
        finally
        {
            xmlWriter.Flush();
            xmlText = strWriter.ToString();
#if !UNITY_WSA
            xmlWriter?.Close();
#endif
        }

        return xmlText;
    }

    static void WriteWordAndSyncPoint(XmlWriter xmlWriter, TtsReader.TtsData ttsData)
    {
        int markIndex = 0;

        foreach (TtsReader.WordTiming wordTiming in ttsData.m_WordTimings)
        {
            bool hasWordText = !string.IsNullOrWhiteSpace(wordTiming.word);
            if (!hasWordText)
                continue;

            if (markIndex < ttsData.m_Marks.Count)
                WriteSync(xmlWriter, ttsData.m_Marks[markIndex++]);

            xmlWriter.WriteStartElement("word");
            xmlWriter.WriteAttributeString("start", wordTiming.start.ToString());
            xmlWriter.WriteAttributeString("end", wordTiming.end.ToString());
            xmlWriter.WriteString(wordTiming.word);
            xmlWriter.WriteEndElement();

            if (markIndex < ttsData.m_Marks.Count)
                WriteSync(xmlWriter, ttsData.m_Marks[markIndex++]);
        }

        while (markIndex < ttsData.m_Marks.Count)
            WriteSync(xmlWriter, ttsData.m_Marks[markIndex++]);
    }

    static void WriteSync(XmlWriter xmlWriter, TtsReader.MarkData markData)
    {
        xmlWriter.WriteStartElement("sync");
        xmlWriter.WriteAttributeString("id", markData.name);
        xmlWriter.WriteAttributeString("time", markData.time.ToString());
        xmlWriter.WriteEndElement();
    }
    #endregion
}
}
