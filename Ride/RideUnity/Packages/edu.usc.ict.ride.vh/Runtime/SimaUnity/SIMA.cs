using System;
using UnityEngine;
using Ride.NLP;

namespace Ride
{
/// <summary>
/// 1. Sends an utterance to the OpenAI model (via <see cref="ModelBuilder"/>),
/// 2. Receives behavior JSON, parses it into <see cref="Behavior"/> objects, and generates BML XML.
/// The final XML string is returned via a callback
/// </summary>
/// 
public class SIMA : MonoBehaviour
{
    public string storedUtterance;
    public string result = string.Empty;
    // Referencing the ModelBuilder component directly.
    // The ModelBuilder itself will handle its dependencies
    [SerializeField] private ModelBuilder _modelBuilder;


    Action<string> m_behaviorResult;

    /// <summary>
    /// Entry point to analyze an utterance: sends it to the model and does the JSON response callback.
    /// </summary>
    /// <param name="utterance">The speaker’s utterance to analyze.</param>
    /// <param name="callback">Invoked with the final BML XML string (without XML declaration) once ready.</param>

    public void GetBehavior (string utterance, Action<string> callback)
    {
        storedUtterance = utterance;
        Debug.Log("BehaviorManager: Starting analysis for utterance: " + utterance);
        m_behaviorResult = callback;
        _modelBuilder.AnalyzeUtterance(utterance, OnJsonReceived);

    }


    /// <summary>
    /// Handles the JSON response from the OpenAI model: cleans the payload, parses behaviors,
    /// builds the BML document, and returns the XML to the original caller.
    /// </summary>
    /// <param name="response">The NLP system response containing the model’s JSON output.</param>

    private void OnJsonReceived(NlpResponse response)
    {
        string Generatedjson = response.content[0];
        string json = Generatedjson.Replace("```json", ""); //removes the extra line from above the json file
        json = json.Replace("```", ""); //removes the extra line from below the json file
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("❌ BehaviorManager: Received empty or null JSON from OpenAI.");
            return;
        }

        Debug.Log("🎯 BehaviorManager: Received JSON from OpenAI:\n" + json);

        try
        {
            //parses then saves json file into disk
            var model = JsonOutput.ParseAndValidate(json, out var warnings);
            if (model != null)
            { 
                string fullPath = model.SaveToFile(
                    baseFileName: storedUtterance.Replace(" ", "_"),
                    customPath: ""
                );
                Debug.Log("📁 JSON saved at: " + fullPath);
            }
            if (warnings.Count > 0)
                Debug.LogWarning("[LLM JSON warnings]\n - " + string.Join("\n - ", warnings));
            // end of saving
            var behaviors = JsonBehaviorParser.JsonNestedToFlatList(json, storedUtterance);
            if (behaviors != null && behaviors.Count > 0)
            {
                Debug.Log($"--- Found {behaviors.Count} behaviors: ---"); for (int i = 0; i < behaviors.Count; i++);
            }
            foreach (var behavior in behaviors)
            {
                Debug.Log($"⏰[Behavior] Kind: {behavior.Kind}, Phrase: {behavior.Phrase}, Marker: {behavior.Marker}, Tmarker: {behavior.TimingMarker}" +
                          $"Gesture: {behavior.Gesture}, Head: {behavior.Head}, Facial: {behavior.Facial}");
            }

            BehaviorLogger.SaveBehaviorsToFile(behaviors, customPath: "");


            var doc = BmlBuilder.CreateBmlDocument("ChrKevin", storedUtterance, behaviors);
            string xml = BmlSerializer.SaveToFileAndGetInnerXml(doc, "xml_output.xml");
            string finalXml = "<?xml version='1.0' encoding='utf-8'?>" + xml;
            result = xml;
            Debug.Log("✅ BehaviorManager: BML Output:\n" + xml);

            m_behaviorResult?.Invoke(result);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ BehaviorManager: Error parsing or generating BML: " + ex.Message);
        }
    }
}
}
