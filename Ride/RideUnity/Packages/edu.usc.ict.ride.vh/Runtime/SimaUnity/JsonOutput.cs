using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Ride
{
    [Serializable]
    public class JsonOutput
    {
        [JsonProperty("nonverbal_behavior")]
        public NonverbalBehavior NonverbalBehavior { get; set; }

        /// <summary>
        /// Parse JSON, run lightweight schema checks, return model or null (collects warnings).
        /// </summary>
        public static JsonOutput ParseAndValidate(string json, out List<string> warnings)
        {
            warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[JsonOutput] Empty JSON.");
                return null;
            }

            JsonOutput data;
            try
            {
                data = JsonConvert.DeserializeObject<JsonOutput>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonOutput] JSON deserialize error: {ex}");
                return null;
            }

            if (data?.NonverbalBehavior == null)
            {
                Debug.LogError("[JsonOutput] Missing 'nonverbal_behavior' object.");
                return null;
            }

            // Validate blocks
            CheckBlockList(data.NonverbalBehavior.Gestures, "gestures", warnings);
            CheckBlockList(data.NonverbalBehavior.HeadMovements, "head_movements", warnings);
            CheckBlockList(data.NonverbalBehavior.FacialActionUnit, "facial_action_unit", warnings);

            return data;
        }

        private static void CheckBlockList(IEnumerable<ActionBlock> blocks, string label, List<string> warnings)
        {
            if (blocks == null) return;
            int i = 0;
            foreach (var b in blocks)
            {
                i++;
                if (string.IsNullOrWhiteSpace(b.Type))
                    warnings.Add($"{label}[{i}]: 'type' is empty.");
                if (string.IsNullOrWhiteSpace(b.Phrase))
                    warnings.Add($"{label}[{i}]: 'phrase' is empty.");

                ValidateTokenInPhrase(b.Start, b.Phrase, $"{label}[{i}].start", warnings);
                ValidateTokenInPhrase(b.Stroke, b.Phrase, $"{label}[{i}].stroke", warnings);
                ValidateTokenInPhrase(b.Relax, b.Phrase, $"{label}[{i}].relax", warnings);
            }
        }

        private static void ValidateTokenInPhrase(string token, string phrase, string field, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phrase)) return;
            var words = phrase.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (!words.Any(w => string.Equals(
                w.TrimEnd('.', ',', '!', '?', ';', ':'),
                token,
                StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add($"{field}: '{token}' not found in phrase \"{phrase}\".");
            }
        }

        /// <summary>Pretty JSON string of this object.</summary>
        public string ToPrettyJson() =>
            JsonConvert.SerializeObject(this, Formatting.Indented);

        // ------------------------- Saving helpers -------------------------

        /// <summary>
        /// Save this JsonOutput to disk as pretty JSON. Returns full path or null on failure.
        /// </summary>
        public string SaveToFile(string baseFileName = null, string subfolder = "Json_Outputs", string customPath = null)
        {
            try
            {
                string pretty = ToPrettyJson();
                var safeBase = string.IsNullOrWhiteSpace(baseFileName)
                    ? $"json_output_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
                    : MakeFileNameSafe(baseFileName);

                // If customPath is given, use it; otherwise fall back to Unity's persistentDataPath
                var dir = string.IsNullOrWhiteSpace(customPath)
                    ? Path.Combine(Application.persistentDataPath, subfolder)
                    : Path.Combine(customPath, subfolder);

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, $"{safeBase}.json");
                File.WriteAllText(path, pretty, new UTF8Encoding(false));
                Debug.Log($"[JsonOutput] JSON saved to: {path}");
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonOutput] Failed to save JSON: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Convenience: parse, validate, and save raw JSON in one call.
        /// Returns (fullPath, warnings). fullPath is null on failure.
        /// </summary>
        public static (string fullPath, List<string> warnings) SaveFromRaw(
            string rawJson,
            string baseFileName = null,
            string subfolder = "Json_Outputs")
        {
            var model = ParseAndValidate(rawJson, out var warnings);
            if (model == null) return (null, warnings);


            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                baseFileName =
                    model.NonverbalBehavior?.Gestures?.FirstOrDefault()?.Type ??
                    model.NonverbalBehavior?.HeadMovements?.FirstOrDefault()?.Type ??
                    model.NonverbalBehavior?.FacialActionUnit?.FirstOrDefault()?.Type ??
                    "json_output";
            }

            var path = model.SaveToFile(baseFileName, subfolder);
            return (path, warnings);
        }

        private static string MakeFileNameSafe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public static bool LooksLikeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            json = json.Trim();
            return (json.StartsWith("{") && json.EndsWith("}")) || (json.StartsWith("[") && json.EndsWith("]"));
        }
    }

    [Serializable]
    public class NonverbalBehavior
    {
        [JsonProperty("gestures")] public List<ActionBlock> Gestures { get; set; }
        [JsonProperty("head_movements")] public List<ActionBlock> HeadMovements { get; set; }
        [JsonProperty("facial_action_unit")] public List<ActionBlock> FacialActionUnit { get; set; }
    }

    [Serializable]
    public class ActionBlock
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("phrase")] public string Phrase;
        [JsonProperty("start")] public string Start;
        [JsonProperty("stroke")] public string Stroke;
        [JsonProperty("relax")] public string Relax;
    }
}
