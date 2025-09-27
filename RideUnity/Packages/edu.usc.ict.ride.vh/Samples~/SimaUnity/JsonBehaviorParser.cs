using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Parses nested nonverbal-behavior JSON into a flat list of <see cref="Behavior"/> objects
///. The input JSON is expected to have the shape:
/// {
///   "nonverbal_behavior": {
///     "gestures": [ { "type": "...", "phrase": "...", "stroke": "..." } ],
///     "head_movements": [ { "type": "...", "phrase": "...", "relax": "..." } ],
///     "facial_action_unit": [ { "type": "...", "phrase": "...", "relax": "..." } ]
///   }
/// }
/// </summary>
public static class JsonBehaviorParser
{
    /// <summary>
    /// Converts nested JSON into a flat <see cref="List{T}"/> of <see cref="Behavior"/>.
    /// </summary>
    /// <param name="nestedJson">
    /// A JSON string containing a <c>nonverbal_behavior</c> object with
    /// <c>gestures</c>, <c>head_movements</c>, and <c>facial_action_unit</c> arrays. (It can have multiple of each behavior)
    /// </param>
    /// <returns>
    /// A list of parsed <see cref="Behavior"/> entries. If the JSON is empty, malformed,
    /// or the expected nodes are missing, returns an empty list.
    /// </returns>
    /// <remarks>
    /// Parsing rules:
    /// <list type="bullet">
    ///   <item><description>Gesture items use <c>stroke</c> as the timing marker field.</description></item>
    ///   <item><description>Head and facial items use <c>relax</c> as the timing marker field.</description></item>
    ///   <item><description>Enum parsing is case-insensitive (e.g., "nod", "Nod", "NOD" → <see cref="HeadType.Nod"/>).</description></item>
    ///   <item><description>Unknown or unmapped <c>type</c> values are skipped.</description></item>
    /// </list>
    /// </remarks>
    public static List<Behavior> JsonNestedToFlatList(string nestedJson, string utterance)
    {
        var flatBehaviors = new List<Behavior>();
        var root = JObject.Parse(nestedJson)?["nonverbal_behavior"];
        if (root == null)
            return flatBehaviors;
        // Gestures
        var gestures = root["gestures"];
        if (gestures != null)
        {
            foreach (var g in gestures)
            {
                var typeStr = g["type"]?.ToString();



                int markerIndex = TimingMarkerLocator.Locate(utterance, g["phrase"]?.ToString(), g["stroke"]?.ToString()) ?? 0;
                string Tmarker = $"T{markerIndex}";

                if (Enum.TryParse(typeStr, out GestureType gestureType))
                {
                    flatBehaviors.Add(new Behavior
                    {
                        Kind = BehaviorKind.Gesture,
                        Gesture = gestureType,
                        Phrase = g["phrase"]?.ToString(),
                        Marker = g["stroke"]?.ToString(),
                        TimingMarker = Tmarker,
                    });
                }
            }
        }
        // Head movements
        var heads = root["head_movements"];
        if (heads != null)
        {
            foreach (var h in heads)
            {
                int markerIndex = TimingMarkerLocator.Locate(utterance, h["phrase"]?.ToString(), h["stroke"]?.ToString()) ?? 0;
                string Tmarker = $"T{markerIndex}";

                var typeStr = h["type"]?.ToString();
                if (Enum.TryParse(typeStr, out HeadType headType))
                {
                    flatBehaviors.Add(new Behavior
                    {
                        Kind = BehaviorKind.Head,
                        Head = headType,
                        Phrase = h["phrase"]?.ToString(),
                        Marker = h["relax"]?.ToString(),
                        TimingMarker = Tmarker,
                    });
                }
            }
        }
        // Facial AUs
        var facs = root["facial_action_unit"];
        if (facs != null)
        {
            foreach (var f in facs)
            {
                int markerIndex = TimingMarkerLocator.Locate(utterance, f["phrase"]?.ToString(), f["stroke"]?.ToString()) ?? 0;
                string Tmarker = $"T{markerIndex}";

                var typeStr = f["type"]?.ToString();
                if (Enum.TryParse(typeStr, out FacialAU auType))
                {
                    flatBehaviors.Add(new Behavior
                    {
                        Kind = BehaviorKind.Facial,
                        Facial = auType,
                        Phrase = f["phrase"]?.ToString(),
                        Marker = f["relax"]?.ToString(),
                        TimingMarker = Tmarker,
                    });
                }
            }
        }
        return flatBehaviors;
    }
}