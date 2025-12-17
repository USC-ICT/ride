using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Ride
{
/// <summary>
/// Builds a complete BML (<act>…</act>) XML document from:
/// 1) the speaker's name, 2) the utterance, and 3) the list of parsed behaviors.
/// The document includes speech marks, intermediate events, gaze, a final event,
/// and behavior elements (<animation>, <head>, <face>) aligned to timing markers.
/// </summary>

public static class BmlBuilder

/// <summary>
/// Creates a full BML <act> document for the given character and utterance.
/// </summary>
/// <param name="characterName">The character/agent id to place in BML (e.g., “ChrKevin”).</param>
/// <param name="utterance">Plain text to be spoken; will be tokenized and marked (T0, T1, …).</param>
/// <param name="behaviors">
/// The behaviors to realize (gesture/head/facial). Each behavior has a phrase and marker word;
/// these are mapped to a speech timing marker via <see cref="TimingMarkerLocator"/>.
/// </param>
/// <returns>
/// An <see cref="XDocument"/> containing the full BML <act> tree, including a proper XML declaration.
/// </returns>
/// <remarks>
/// Pipeline:
/// 1) Parse utterance into (mark, word) pairs.
/// 2) Build <speech> with <mark> boundaries around each word.
/// 3) Add partial “progress” <event>s and a final completion <event>.
/// 4) Add a default <gaze>.
/// 5) For each behavior, compute its timing marker and append the proper BML element.
/// </remarks>

{
    public static XDocument CreateBmlDocument(string characterName, string utterance, List<Behavior> behaviors)
    {
        XNamespace sbm = "http://ict.usc.edu"; //used for farther down in the code

        var parsedText = UtteranceParser.SimpleParser(utterance);
        string speechId = "sp1";   

        // creates the act element
        XElement act = new XElement("act");


        // Creates the participant element. Then adds the id and role attributes to it.
        XElement participant = new XElement("participant",
            new XAttribute("id", characterName),
            new XAttribute("role", "actor"));
        act.Add(participant);


        XElement bml = new XElement("bml");
        act.Add(bml);

        // Add <speech>
        XElement speechElem = XmlSpeechBuilder.CreateSpeechXml(speechId, parsedText);
        Debug.Log("speech element is done:\n" + speechElem.ToString());
        bml.Add(speechElem);

        // Add example <event> elements
        for (int i = 1; i < parsedText.Count * 2; i += 2)
        {
            string spokenSoFar = string.Join(" ", parsedText.GetRange(0, i / 2 + 1).ConvertAll(p => p.Word));
            XElement evt = new XElement("event",
                new XAttribute("message", $"vrAgentSpeech partial 1488584035542-92-1 T{i} {spokenSoFar}"),
                new XAttribute("stroke", $"{speechId}:T{i}")
            );
            bml.Add(evt);
        }

        // Add gaze
        XElement gaze = new XElement("gaze",
            new XAttribute("participant", characterName),
            new XAttribute("target", "all"),
            new XAttribute("direction", "POLAR 0"),
            new XAttribute("angle", "0"),
            new XAttribute("start", $"{speechId}:T0"),
            new XAttribute("joint-range", "HEAD EYES"),
            new XAttribute(XNamespace.Xmlns + "sbm", sbm)
        );
        bml.Add(gaze);

        // Final <event>
        string fullUtterance = string.Join(" ", parsedText.ConvertAll(p => p.Word));
        XElement finalEvent = new XElement("event",
            new XAttribute("message", $"vrSpoke {characterName} all 1488584035542-92-1 {fullUtterance}"),
            new XAttribute("stroke", $"{speechId}:relax"),
            new XAttribute(XNamespace.Xmlns + "sbm", sbm)
        );
        bml.Add(finalEvent);

        // Add behavior animations
        foreach (var behavior in behaviors)
        {
            int markerIndex = TimingMarkerLocator.Locate(utterance, behavior.Phrase, behavior.Marker) ?? 0; //if the marker returns null, it assigns the marker 0 to it
            string marker = $"{speechId}:T{markerIndex}";

            //if (ValidBehaviorTypes.ValidGestureTypes.TryGetValue(behavior.Kind, out string gestureName))
            if (
                behavior.Kind == BehaviorKind.Gesture &&
                behavior.Gesture.HasValue &&
                ValidBehaviorTypes.ValidGestureTypes.TryGetValue(
                    behavior.Gesture.Value.ToString(), out string gestureName)
            )

            {
                bml.Add(new XElement("animation",
                    new XAttribute("name", gestureName),
                    new XAttribute("stroke", marker)
                ));
            }
            //else if (ValidBehaviorTypes.ValidHeadTypes.Contains(behavior.Kind))
            else if (
                behavior.Kind == BehaviorKind.Head &&
                behavior.Head.HasValue &&
                ValidBehaviorTypes.ValidHeadTypes.Contains(
                    behavior.Head.Value.ToString())
            )
            {
                bml.Add(new XElement("head",
                    new XAttribute("type", behavior.Head.Value.ToString().ToUpper()),
                    new XAttribute("amount", "0.1"),
                    new XAttribute("repeats", "0.5"),
                    new XAttribute("relax", marker)
                ));
            }
            // else if (ValidBehaviorTypes.ValidFacsTypes.Contains(behavior.Kind))
            else if (
                behavior.Kind == BehaviorKind.Facial &&
                behavior.Facial.HasValue &&
                ValidBehaviorTypes.ValidFacsTypes.Contains(
                    behavior.Facial.Value.ToString())
            )
            {
                bml.Add(new XElement("face",
                    new XAttribute("type", "facs"),
                    new XAttribute("au", behavior.Facial.Value.ToString().Replace("au", "")),
                    new XAttribute("side", "BOTH"),
                    new XAttribute("relax", marker)
                ));
            }
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), act);
    }
}
}
