using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Ride
{
public static class XmlSpeechBuilder
{
    /// <summary>
    /// Creates a <speech> XML element with alternating <mark> and word content
    /// Each word gets two marks: a start mark (T0, T2, ...) and an end mark (T1, T3, ...)
    /// </summary>
    public static XElement CreateSpeechXml(string speechId, List<(string Mark, string Word)> parsedText)
    {
        Debug.Log("Number of behaviors: " + parsedText.Count);

        var speechElement = new XElement("speech",
            new XAttribute("id", speechId),
            new XAttribute("ref", "unused"),
            new XAttribute("type", "application/ssml+xml")
        );

        for (int i = 0; i < parsedText.Count; i++)
        {
            Debug.Log("XMLSpeechBuilder: Loop number for creating marked words: " + i);
            Debug.Log("XMLSpeechBuilder: parsedText[i]: " + parsedText[i]);
            var (startMark, word) = parsedText[i];
            Debug.Log("XMLSpeechBuilder: the word and its startmark are read as: " + startMark + ", " + word);

            // Add start <mark>
            var markStart = new XElement("mark", new XAttribute("name", startMark));
            Debug.Log("XMLSpeechBuilder: mark start: " + markStart.ToString());
            speechElement.Add(markStart);

            // Add the word as text node
            markStart.AddAfterSelf(word);

            // Add end <mark> with incremented index
            string endMark = $"T{i * 2 + 1}";
            var markEnd = new XElement("mark", new XAttribute("name", endMark));
            Debug.Log("XMLSpeechBuilder: mark end: " + markEnd.ToString());
            speechElement.Add(markEnd);
        }

        return speechElement;
    }
}
}
