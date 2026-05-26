using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Helper class that creates a lipsync timing schedule based on word character length vs utterance character length
    /// </summary>
    public static class LipsyncAutoScheduler
    {
        static readonly float speakTimeMod = 0.95f;
        const float ProxyPauseUnitEquivalentCharacters = 3.5f;

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
        /// Creates a lightweight speech map with approximate viseme pulses for local TTS backends
        /// that provide audio duration but not provider timing metadata.
        /// </summary>
        /// <param name="text">Raw input text.</param>
        /// <param name="speakTime">Total spoken duration.</param>
        /// <param name="ipaDictionaries">Optional IPA dictionaries used to approximate phoneme timing and visemes.</param>
        /// <returns>A speech map containing marks, word timing, and simple viseme activity.</returns>
        public static AudioSpeechMap CreateProxySpeechMap(string text, float speakTime, IReadOnlyList<IpaDictionary> ipaDictionaries = null)
        {
            string[] rawWords = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (rawWords.Length == 0)
                rawWords = new[] { text };

            string[] words = new string[rawWords.Length];
            float[] wordCharacterWeights = new float[rawWords.Length];
            float[] pauseUnits = new float[rawWords.Length];
            float totalWordCharacterWeight = 0f;
            float totalPauseUnitWeight = 0f;

            for (int i = 0; i < rawWords.Length; i++)
            {
                string cleanedWord = SanitizeWordForTiming(rawWords[i]);
                if (string.IsNullOrEmpty(cleanedWord))
                    cleanedWord = rawWords[i].Trim();
                if (string.IsNullOrEmpty(cleanedWord))
                    cleanedWord = rawWords[i];

                words[i] = cleanedWord;
                wordCharacterWeights[i] = Mathf.Max(cleanedWord.Length, 1);
                pauseUnits[i] = EstimatePauseUnits(rawWords[i], i == rawWords.Length - 1);
                totalWordCharacterWeight += wordCharacterWeights[i];
                totalPauseUnitWeight += pauseUnits[i];
            }

            totalWordCharacterWeight = Mathf.Max(totalWordCharacterWeight, 1f);
            float totalDurationUnits = totalWordCharacterWeight + (totalPauseUnitWeight * ProxyPauseUnitEquivalentCharacters);
            if (totalDurationUnits <= 0f)
                totalDurationUnits = 1f;

            var wordSegments = new List<ElevenLabsTextToSpeech.WordSegment>(words.Length);

            AudioSpeechMap map = new AudioSpeechMap
            {
                soundFile = " ",
                WordTimingList = new List<WordTimingData>(),
                MarkList = new List<KeyValuePairS<string, double>>(),
                VisemeList = new List<GenerateAudioReplyViseme>(),
            };

            float currentTime = 0f;
            int currentMark = 0;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                float wordDuration = speakTime * (wordCharacterWeights[i] / totalDurationUnits);
                float wordStart = currentTime;
                float wordEnd = wordStart + wordDuration;

                map.MarkList.Add(new KeyValuePairS<string, double>("T" + currentMark, wordStart));
                currentMark++;
                map.WordTimingList.Add(new WordTimingData(word, wordStart, wordEnd));
                map.MarkList.Add(new KeyValuePairS<string, double>("T" + currentMark, wordEnd));
                currentMark++;

                wordSegments.Add(new ElevenLabsTextToSpeech.WordSegment
                {
                    Word = word,
                    StartTimeSeconds = wordStart,
                    EndTimeSeconds = wordEnd,
                    StartCharIndex = -1,
                    EndCharIndex = -1
                });

                float pauseDuration = speakTime * ((pauseUnits[i] * ProxyPauseUnitEquivalentCharacters) / totalDurationUnits);
                currentTime = wordEnd + pauseDuration;
            }

            if (map.WordTimingList.Count > 0)
            {
                WordTimingData lastWord = map.WordTimingList[map.WordTimingList.Count - 1];
                float finalWordEnd = Mathf.Max((float)lastWord.End, speakTime);
                if (finalWordEnd > lastWord.End)
                {
                    map.WordTimingList[map.WordTimingList.Count - 1] = new WordTimingData(lastWord.Text, lastWord.Start, finalWordEnd);
                    wordSegments[wordSegments.Count - 1] = new ElevenLabsTextToSpeech.WordSegment
                    {
                        Word = wordSegments[wordSegments.Count - 1].Word,
                        StartTimeSeconds = wordSegments[wordSegments.Count - 1].StartTimeSeconds,
                        EndTimeSeconds = finalWordEnd,
                        StartCharIndex = wordSegments[wordSegments.Count - 1].StartCharIndex,
                        EndCharIndex = wordSegments[wordSegments.Count - 1].EndCharIndex
                    };
                    map.MarkList[map.MarkList.Count - 1] =
                        new KeyValuePairS<string, double>(map.MarkList[map.MarkList.Count - 1].Key, finalWordEnd);
                }
            }

            if (!AppendPhonemeMappedVisemes(map.VisemeList, wordSegments, ipaDictionaries))
            {
                for (int i = 0; i < words.Length; i++)
                {
                    WordTimingData wordTiming = map.WordTimingList[i];
                    AddApproximateVisemes(map.VisemeList, words[i], (float)wordTiming.Start, (float)wordTiming.End);
                }
            }

            return map;
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

        private static bool AppendPhonemeMappedVisemes(
            List<GenerateAudioReplyViseme> visemes,
            IReadOnlyList<ElevenLabsTextToSpeech.WordSegment> wordSegments,
            IReadOnlyList<IpaDictionary> ipaDictionaries)
        {
            if (visemes == null || wordSegments == null || wordSegments.Count == 0)
                return false;

            List<ElevenLabsTextToSpeech.FacefxKeyframe> keyframes =
                ElevenLabsTextToSpeech.BuildApproximateFacefxSchedule(wordSegments, ipaDictionaries);
            if (keyframes == null || keyframes.Count == 0)
                return false;

            for (int i = 0; i < keyframes.Count; i++)
            {
                ElevenLabsTextToSpeech.FacefxKeyframe keyframe = keyframes[i];
                if (keyframe.FacefxVisemes == null || keyframe.Amounts == null)
                    continue;

                int count = Mathf.Min(keyframe.FacefxVisemes.Length, keyframe.Amounts.Length);
                for (int j = 0; j < count; j++)
                {
                    string viseme = keyframe.FacefxVisemes[j];
                    if (string.IsNullOrEmpty(viseme))
                        continue;

                    visemes.Add(new GenerateAudioReplyViseme(
                        viseme,
                        System.Math.Max(0.0, keyframe.TimeSeconds),
                        keyframe.Amounts[j]));
                }
            }

            return visemes.Count > 0;
        }

        private static void AddApproximateVisemes(List<GenerateAudioReplyViseme> visemes, string word, float wordStart, float wordEnd)
        {
            float wordDuration = Mathf.Max(wordEnd - wordStart, 0.05f);
            int pulseCount = Mathf.Max(1, CountVowelGroups(word));
            float segmentDuration = wordDuration / pulseCount;

            visemes.Add(new GenerateAudioReplyViseme("open", wordStart, 0.0f));

            for (int i = 0; i < pulseCount; i++)
            {
                float pulseStart = wordStart + (segmentDuration * i);
                float pulsePeak = Mathf.Min(wordEnd, pulseStart + (segmentDuration * 0.35f));
                float pulseClose = Mathf.Min(wordEnd, pulseStart + (segmentDuration * 0.8f));
                char vowelHint = GetDominantVowelHint(word, i, pulseCount);

                visemes.Add(new GenerateAudioReplyViseme("open", pulsePeak, 0.7f));

                if (vowelHint == 'o' || vowelHint == 'u' || vowelHint == 'w')
                    visemes.Add(new GenerateAudioReplyViseme("W", pulsePeak, 0.35f));
                else
                    visemes.Add(new GenerateAudioReplyViseme("wide", pulsePeak, 0.2f));

                visemes.Add(new GenerateAudioReplyViseme("open", pulseClose, 0.0f));
            }
        }

        private static int CountVowelGroups(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return 1;

            string lower = word.ToLowerInvariant();
            bool inVowelGroup = false;
            int count = 0;

            for (int i = 0; i < lower.Length; i++)
            {
                bool isVowel = IsVowel(lower[i]);
                if (isVowel && !inVowelGroup)
                {
                    count++;
                    inVowelGroup = true;
                }
                else if (!isVowel)
                {
                    inVowelGroup = false;
                }
            }

            return Mathf.Max(1, count);
        }

        private static char GetDominantVowelHint(string word, int pulseIndex, int pulseCount)
        {
            if (string.IsNullOrWhiteSpace(word))
                return 'a';

            string lower = word.ToLowerInvariant();
            int targetIndex = Mathf.Clamp(Mathf.FloorToInt((pulseIndex + 0.5f) / pulseCount * lower.Length), 0, lower.Length - 1);

            for (int radius = 0; radius < lower.Length; radius++)
            {
                int left = targetIndex - radius;
                if (left >= 0 && IsVowel(lower[left]))
                    return lower[left];

                int right = targetIndex + radius;
                if (right < lower.Length && IsVowel(lower[right]))
                    return lower[right];
            }

            return 'a';
        }

        private static bool IsVowel(char character)
            => character == 'a' || character == 'e' || character == 'i' || character == 'o' || character == 'u' || character == 'y' || character == 'w';

        private static string SanitizeWordForTiming(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return string.Empty;

            int start = 0;
            int end = word.Length - 1;

            while (start <= end && !IsWordTimingCharacter(word[start]))
                start++;

            while (end >= start && !IsWordTimingCharacter(word[end]))
                end--;

            if (start > end)
                return string.Empty;

            return word.Substring(start, end - start + 1);
        }

        private static bool IsWordTimingCharacter(char character)
            => char.IsLetterOrDigit(character) || character == '\'' || character == '-';

        private static float EstimatePauseUnits(string rawWord, bool isLastWord)
        {
            if (string.IsNullOrWhiteSpace(rawWord))
                return 0f;

            float pauseUnits = 0f;
            string trimmed = rawWord.Trim();

            if (trimmed.EndsWith("..."))
                pauseUnits = Mathf.Max(pauseUnits, 2.2f);
            else if (trimmed.EndsWith(".") || trimmed.EndsWith("!") || trimmed.EndsWith("?"))
                pauseUnits = Mathf.Max(pauseUnits, 1.8f);
            else if (trimmed.EndsWith(";") || trimmed.EndsWith(":"))
                pauseUnits = Mathf.Max(pauseUnits, 1.1f);
            else if (trimmed.EndsWith(","))
                pauseUnits = Mathf.Max(pauseUnits, 0.8f);

            if (trimmed.EndsWith("—") || trimmed.EndsWith("--") || trimmed.EndsWith("-"))
                pauseUnits = Mathf.Max(pauseUnits, 0.7f);

            if (isLastWord)
                pauseUnits = 0f;

            return pauseUnits;
        }
    }
}
