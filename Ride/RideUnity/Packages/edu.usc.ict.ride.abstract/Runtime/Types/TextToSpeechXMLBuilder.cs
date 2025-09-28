using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;


namespace Ride.TextToSpeech
{
    // Had to define a custom version of KeyValuePair so that it would xml serialize.  The built-in one wouldn't.  
    //http://blogs.msdn.com/b/seshadripv/archive/2005/11/02/488273.aspx
    public struct KeyValuePairS<K, V>
    {
        public K Key { get; set; }
        public V Value { get; set; }

        public KeyValuePairS(K key, V value) : this() { Key = key; Value = value; }
    }

    public struct GenerateAudioReplyViseme
    {
        public string type;
        public double start;
        public double articulation;

        public GenerateAudioReplyViseme(string _type, double _start, double _articulation) { type = _type; start = _start; articulation = _articulation; }
    }

    /// <summary>
    /// Data container for mapped speech, sent to XML Builder
    /// </summary>
    public class AudioSpeechMap
    {
        public string soundFile;
        public List<KeyValuePairS<double, double>> WordBreakList;  // start/end
        public List<KeyValuePairS<string, double>> MarkList;  // name/time
        public List<GenerateAudioReplyViseme> VisemeList;  // type/start/articulation
    }

    // used for sorting the events into the correct order to mimick the messages we've always sent
    public enum SortEnum
    {
        // if time is equal, the order is:
        OddMark,
        EndWord,
        EvenMark,
        StartWord,
        Viseme,
    }

    public class SortClass
    {
        public SortEnum sortEnum;
        public object obj;

        public SortClass(SortEnum _sortEnum, object _obj) { sortEnum = _sortEnum; obj = _obj; }
    }

    /// <summary>
    /// Helper class that creates a lipsync XML file 
    /// </summary>
    public static class TextToSpeechXMLBuilder
    {
        public static string BuildSpeechXML(AudioSpeechMap audioSpeechMap)
        {
            var output = new StringBuilder();
            var settings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };

            using (XmlWriter xml = XmlWriter.Create(output, settings))
            {
                //xml.WriteStartDocument();
                xml.WriteProcessingInstruction("xml", @"version=""1.0"" encoding=""UTF-8""");
                xml.WriteStartElement("speak");
                xml.WriteStartElement("soundFile");
                xml.WriteAttributeString("name", audioSpeechMap.soundFile);
                xml.WriteEndElement();  // soundFile


                var combinedList = new List<KeyValuePair<double, SortClass>>();
                foreach (var kv in audioSpeechMap.WordBreakList)
                {
                    combinedList.Add(new KeyValuePair<double, SortClass>(kv.Key, new SortClass(SortEnum.StartWord, kv)));
                    combinedList.Add(new KeyValuePair<double, SortClass>(kv.Value, new SortClass(SortEnum.EndWord, kv)));
                }

                foreach (var kv in audioSpeechMap.MarkList)
                {
                    // special case due to how the xml has been generated all these years
                    // determine whether the marker is odd or even, and sort accordingly
                    string markName = kv.Key;
                    int markNumber = 0;
                    if (markName.StartsWith("T"))
                    {
                        markName = markName.Remove(0, 1); // remove the T
                        markNumber = Convert.ToInt32(markName);
                    }

                    SortEnum sortEnum = markNumber % 2 == 0 ? SortEnum.EvenMark : SortEnum.OddMark;

                    combinedList.Add(new KeyValuePair<double, SortClass>(kv.Value, new SortClass(sortEnum, kv)));
                }

                foreach (var kv in audioSpeechMap.VisemeList)
                    combinedList.Add(new KeyValuePair<double, SortClass>(kv.start, new SortClass(SortEnum.Viseme, kv)));

                combinedList.Sort((x, y) =>
                {
                    int compare = x.Key.CompareTo(y.Key);
                    if (compare == 0)
                        return x.Value.sortEnum.CompareTo(y.Value.sortEnum);
                    else
                        return compare;
                });

                foreach (var kv in combinedList)
                {
                    SortClass value = kv.Value;

                    if (value.sortEnum == SortEnum.EvenMark ||
                        value.sortEnum == SortEnum.OddMark)
                    {
                        var v = (KeyValuePairS<string, double>)value.obj;
                        xml.WriteStartElement("mark");
                        xml.WriteAttributeString("name", v.Key);
                        xml.WriteAttributeString("time", v.Value.ToString());
                        xml.WriteEndElement();  // mark
                    }
                    else if (value.sortEnum == SortEnum.StartWord)
                    {
                        var v = (KeyValuePairS<double, double>)value.obj;
                        xml.WriteStartElement("word");
                        xml.WriteAttributeString("start", v.Key.ToString());
                        xml.WriteAttributeString("end", v.Value.ToString());

                    }
                    else if (value.sortEnum == SortEnum.Viseme)
                    {
                        var v = (GenerateAudioReplyViseme)value.obj;
                        xml.WriteStartElement("viseme");
                        xml.WriteAttributeString("start", v.start.ToString());
                        xml.WriteAttributeString("articulation", v.articulation.ToString());
                        xml.WriteAttributeString("type", v.type);
                        xml.WriteEndElement();  // viseme
                    }
                    else if (value.sortEnum == SortEnum.EndWord)
                    {
                        xml.WriteEndElement();  // word
                    }
                }

                xml.WriteEndElement();  // speak
            }

            return output.ToString();
        }

        #region  FaceFX Map
        const string facefxMapping = @"<mapping>
      <entry viseme='0' target='open' amount='0.000000' />

      <entry viseme='1' target='open' amount='0.500000' />
      <entry viseme='1' target='wide' amount='0.600000' />
      <entry viseme='1' target='tBack' amount='0.400000' />

      <entry viseme='2' target='open' amount='0.550000' />

      <entry viseme='3' target='open' amount='0.400000' />
      <entry viseme='3' target='W' amount='0.550000' />

      <entry viseme='4' target='open' amount='0.500000' />
      <entry viseme='4' target='wide' amount='0.600000' />
      <entry viseme='4' target='tBack' amount='0.400000' />

      <entry viseme='5' target='open' amount='0.400000' />
      <entry viseme='5' target='ShCh' amount='0.500000' />
      <entry viseme='5' target='tRoof' amount='0.500000' />

      <entry viseme='6' target='W' amount='0.500000' />
      <entry viseme='6' target='ShCh' amount='0.300000' />
      <entry viseme='6' target='tRoof' amount='0.400000' />

      <entry viseme='7' target='open' amount='0.400000' />
      <entry viseme='7' target='W' amount='0.8500000' />

      <entry viseme='8' target='open' amount='0.400000' />
      <entry viseme='8' target='W' amount='0.550000' />

      <entry viseme='9' target='open' amount='0.500000' />
      <entry viseme='9' target='wide' amount='0.600000' />
      <entry viseme='9' target='tBack' amount='0.400000' />

      <entry viseme='10' target='open' amount='0.400000' />
      <entry viseme='10' target='W' amount='0.550000' />

      <entry viseme='11' target='open' amount='0.500000' />
      <entry viseme='11' target='wide' amount='0.600000' />
      <entry viseme='11' target='tBack' amount='0.400000' />

      <entry viseme='12' target='open' amount='0.200000' />

      <entry viseme='13' target='open' amount='0.100000' />
      <entry viseme='13' target='W' amount='0.70000' />

      <entry viseme='14' target='open' amount='0.400000' />
      <entry viseme='14' target='tRoof' amount='0.80000' />

      <entry viseme='15' target='open' amount='0.1500000' />
      <entry viseme='15' target='wide' amount='0.500000' />
      <entry viseme='15' target='tRoof' amount='0.400000' />

      <entry viseme='16' target='ShCh' amount='0.850000' />
      <entry viseme='16' target='tRoof' amount='0.400000' />

      <entry viseme='17' target='open' amount='0.450000' />
      <entry viseme='17' target='tTeeth' amount='0.900000' />

      <entry viseme='18' target='FV' amount='0.750000' />

      <entry viseme='19' target='open' amount='0.400000' />
      <entry viseme='19' target='tRoof' amount='0.800000' />

      <entry viseme='20' target='open' amount='0.2500000' />
      <entry viseme='20' target='tBack' amount='0.800000' />
      <entry viseme='19' target='tRoof' amount='0.800000' />

      <entry viseme='21' target='PBM' amount='0.900000' />
   </mapping>";


        #endregion
    }
}
