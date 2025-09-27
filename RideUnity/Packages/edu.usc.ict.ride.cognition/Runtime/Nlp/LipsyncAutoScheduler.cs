using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Helper class that creates a lipsync timing schedule based on word character length vs utterance character length
    /// </summary>
    public static class LipsyncAutoScheduler
    {
        static readonly float speakTimeMod = 0.95f;

        /// <summary>
        /// Creates a lipsync  schedule manually, so no viseme data but we get word timing data that is used by NVBG
        /// (Word character length/Text character length) * speak time --> Determines word spoken timing
        /// </summary>
        /// <param name="text">Raw input text</param>
        /// <param name="speakTime">Length of spoken speech we must map to</param>
        /// <returns></returns>/
        public static string CreateSchedule(string text, float speakTime)
        {
            speakTime *= speakTimeMod;
            string[] words = text.Split(new char [] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            int sentenceCharacterCount = text.Replace(" ", "").Length;

            StringBuilder output = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;

            using (XmlWriter xml = XmlWriter.Create(output, settings))
            {
                //xml.WriteStartDocument();
                xml.WriteProcessingInstruction("xml", @"version=""1.0"" encoding=""UTF-8""");
                xml.WriteStartElement("speak");
                xml.WriteStartElement("soundFile");
                xml.WriteAttributeString("name", " ");
                xml.WriteEndElement();

                float currentTime = 0;
                int currentMark = 0;
                int currentWord = 0;
                foreach (string word in words)
                {
                    xml.WriteStartElement("mark");
                    xml.WriteAttributeString("name", "T" + currentMark.ToString());
                    xml.WriteAttributeString("time", currentTime.ToString());
                    xml.WriteEndElement();
                    currentMark++;

                    xml.WriteStartElement("word");
                    xml.WriteAttributeString("start", currentTime.ToString());

                    currentTime += (words[currentWord].Length / (float)sentenceCharacterCount * speakTime);

                    xml.WriteAttributeString("end", currentTime.ToString());

                    xml.WriteStartElement("mark");
                    xml.WriteAttributeString("name", "T" + currentMark.ToString());
                    xml.WriteAttributeString("time", currentTime.ToString());
                    xml.WriteEndElement();

                    currentMark++;
                    currentWord++;
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            return output.ToString();
        }

        /// <summary>
        //Rescales lipsync timing schedule based on the new time
        /// </summary>
        /// <param name="lipsyncSchedule">Target lipsync schedule</param>
        /// <param name="newTime">Time to rescale it to</param>
        /// <returns></returns>/
        public static string RescaleLipsyncTime(string lipsyncSchedule, float newTime)
        {
            XmlDocument document = new XmlDocument();
            document.Load(new System.IO.StringReader(lipsyncSchedule));
            XmlElement root = document.DocumentElement;

            XmlNodeList wordNodes = root.SelectNodes("//word");
            XmlNodeList markNodes = root.SelectNodes("//mark");
            XmlNodeList visemeNodes = root.SelectNodes("//viseme");

            float currentTime = newTime;

            if (!float.TryParse(wordNodes[wordNodes.Count - 1].Attributes["end"].Value, out currentTime))
            {
                return lipsyncSchedule;
            }

            float timeMod = newTime / currentTime;

            foreach (XmlNode word in wordNodes)
            {
                word.Attributes["start"].Value = (float.Parse(word.Attributes["start"].Value) * timeMod).ToString();
                word.Attributes["end"].Value = (float.Parse(word.Attributes["end"].Value) * timeMod).ToString();
            }

            foreach (XmlNode mark in markNodes)
            {
                mark.Attributes["time"].Value = (float.Parse(mark.Attributes["time"].Value) * timeMod).ToString();
            }

            foreach (XmlNode visme in visemeNodes)
            {
                visme.Attributes["start"].Value = (float.Parse(visme.Attributes["start"].Value) * timeMod).ToString();
            }

            lipsyncSchedule = document.InnerXml;
            return lipsyncSchedule;
        }
    }
}
