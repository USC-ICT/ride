using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Ride
{
//public enum CharacterType { GenericMleAdult, GenericFmlAdult} // It can be changed according to the rig of the animations

[Serializable]
public class CharacterSpec
{
    public string name; // the name of the character, eg, "Kevin", "Barbara", etc
    public string type; // the rig for animation names, eg, "GenericMleAdult"
    public string gender; // gender of the character
    public string attitude; // the attitude of the character, eg, "friendly", "hostile", etc
    public string personality; // the personality of the character, eg, "expressive", "extrovert", etc
    public string context; // the context of the speach, eg, "Imagine a job interview scenario. The job interviewer is talking to an interviewee."
    public string description; // any extra descriotion of the required nonverbal behavior, you can specify what behaviors are needed, eg, 
                                // "use more smiles"
}

public static class CharacterSpecLoader
{
    // Reads: Assets/character_spec.json
    public static CharacterSpec Load(
        string fileName = "character_spec.json",
        string relativeSubdir = "Ride.upm/ride.vh/Samples/SimaUnity")
    {
        var dir = Path.Combine(Application.dataPath, relativeSubdir);
        var path = Path.Combine(dir, fileName);
        try
        {
            var json = File.ReadAllText(path);
            var s = JsonConvert.DeserializeObject<CharacterSpec>(json) ?? new CharacterSpec();

            // minimal defaults
            s.name = string.IsNullOrWhiteSpace(s.name) ? "ChrKevin" : s.name.Trim();
            s.type = string.IsNullOrWhiteSpace(s.type) ? "ChrGenericMaleAdult" : s.type.Trim();
            s.gender = string.IsNullOrWhiteSpace(s.gender) ? "Male" : s.gender.Trim();
            s.attitude = string.IsNullOrWhiteSpace(s.attitude) ? "" : s.attitude.Trim();
            s.personality = string.IsNullOrWhiteSpace(s.personality) ? "" : s.personality.Trim();
            s.context = string.IsNullOrWhiteSpace(s.context) ? "A speaker is talking." : s.context.Trim();
            s.description = string.IsNullOrWhiteSpace(s.description) ? "" : s.context.Trim();
            return s;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"CharacterSpec not found or unreadable at {path}. Using defaults. ({ex.Message})");
            return new CharacterSpec
            {
                name = "Default",
                type = "GenericMale",
                gender = "Male",
                attitude = "",
                personality = "",
                context = "A speaker is talking.",
                description = ""
            };
        }
    }
}
}
