using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Collections.Generic;

public static class BehaviorLogger
{
    public static void SaveBehaviorsToFile(List<Behavior> behaviors, string fileName = "Behaviors.json", string customPath = null)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new StringEnumConverter());

            string json = JsonConvert.SerializeObject(behaviors, settings);

            string folderPath = string.IsNullOrEmpty(customPath)
                ? Application.persistentDataPath
                : customPath;

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string path = Path.Combine(folderPath, fileName);
            File.WriteAllText(path, json);

            Debug.Log($"✅ Behaviors saved to: {path}\n⏰ JSON Preview:\n{json}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to save behaviors: {ex.Message}");
        }
    }
}
