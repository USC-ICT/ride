using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Ride.NLP; // Ensure this namespace is correctly referenced

/// <summary>
/// Unity bridge that prepares a system prompt for the OpenAI chat model and
/// sends user utterances for nonverbal behavior analysis. The raw JSON returned
/// by the model can then be parsed into <see cref="Behavior"/> objects and
/// passed to the BML creator pipeline.
/// </summary>
public class ModelBuilder : MonoBehaviour 
{
    /// <summary>
    /// Reference to the ChatGPT wrapper.
    /// </summary>
    [SerializeField] private NlpSystemChatGPT _chatSystem;

    /// <summary>
    /// Initializes the OpenAI client (endpoint + key) and sets the system prompt.
    /// </summary>
    void Awake()
    {
        if (_chatSystem == null)
        {
            Debug.LogError("ModelBuilder: _chatSystem is not assigned! Please assign the NlpSystemChatGPT component in the Inspector.");
            return;
        }

        // Initialize API keys and endpoint within the NlpSystemChatGPT
        _chatSystem.SystemInit(); 
        
        // Set the static system prompt from this class
        //_chatSystem.SetSystemPrompt(BuildSystemPrompt());
        Debug.Log("ModelBuilder Initialized. System Prompt Set.");
    }

    /// <summary>
    /// Builds the static system prompt for the OpenAI API.
    /// This prompt defines the AI's role, the context of the conversation, 
    /// list of accepted nonverbal behaviors,
    /// expected output format, and constraints.
    /// </summary>


    private static readonly Dictionary<string, string> PossAdjByGender =
        new(StringComparer.OrdinalIgnoreCase)
    {
        { "Male", "his" },
        { "Female", "her" },
        { "Nonbinary", "their" }
    };

    public static string BuildSystemPrompt(CharacterSpec spec)
    {
        var sb = new StringBuilder();
        // Line 1: context (fallback to default if empty)
        var ctx = (spec?.context ?? "").Trim();
        sb.AppendLine(!string.IsNullOrWhiteSpace(ctx) ? ctx : "A speaker is talking.");

        // Line 2: attitude (fallback to "neutral" if empty)
        var att = (spec?.attitude ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(att))
        sb.AppendLine($"The speaker has a {att} attitude.");

        // Line 3: personality (fallback to "" if empty)
        var prs = (spec?.personality ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(prs))
        sb.AppendLine($"The speaker has a {prs} personality.");

        var gender = PossAdjByGender.TryGetValue((spec?.gender ?? "").Trim(), out var p) ? p : "his";

        sb.AppendLine($"I will provide you with {gender} utterances, and I would want you to identify {gender} whole nonverbal behavior performance for me.");

        // Line 5: description (fallback to "" if empty)
        var dsc = (spec?.description ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(dsc))
        sb.AppendLine(dsc);

        sb.AppendLine("The nonverbal behavior performance includes hand gestures, head movements, and facial action units.");
        sb.AppendLine("For gestures, choose from this list:");
        sb.AppendLine("- Besides: sweep motion to exclude some entity");
        sb.AppendLine("- Approximation");
        sb.AppendLine("- Negation");
        sb.AppendLine("- Offer");
        sb.AppendLine("- Include");
        sb.AppendLine("- Cycle");
        sb.AppendLine("- Container_big");
        sb.AppendLine("- Container_small");
        sb.AppendLine("- However");
        sb.AppendLine("- You: pointing to the addresse of the speech if they are the emphasis of the speech");
        sb.AppendLine("- Me: pointing to self if self is the emphasis of the speech");
        sb.AppendLine("- Beat_high");
        sb.AppendLine("- Beat_mid");
        sb.AppendLine("- Beat_low");
        sb.AppendLine("- Stop");
        sb.AppendLine("- Greeting");
        sb.AppendLine("For head movements, choose from this list:");
        sb.AppendLine("- Nod");
        sb.AppendLine("- Toss");
        sb.AppendLine("- Shake");
        sb.AppendLine("For facial expressions, choose from this list - each item indicates a facial action unit (au):");
        sb.AppendLine("- au1");
        sb.AppendLine("- au2");
        sb.AppendLine("- au4");
        sb.AppendLine("- au5");
        sb.AppendLine("- au6");
        sb.AppendLine("- au7");
        sb.AppendLine("- au045");
        sb.AppendLine("- au129: This is angry expressoin");
        sb.AppendLine("- au131: This is Contempt expression");
        sb.AppendLine("- au124: This is Disgust expression");
        sb.AppendLine("- au126: This is Fear expression");
        sb.AppendLine("- au112: This is Happy expression");
        sb.AppendLine("- au130: This is Sad expression");
        sb.AppendLine("- au127: This is Surprise expression");
        sb.AppendLine("Each nonverbal behavior consists of three points:");
        sb.AppendLine("- Start: the starting point of the behavior");
        sb.AppendLine("- Stroke: the emphasiz or the peak of the behavior");
        sb.AppendLine("- Relax: the point where the behavior returns to neutral");
        sb.AppendLine("ONLY respond in this JSON format. No other explanation or text.");
        sb.AppendLine("Here is an example:");
        sb.AppendLine(@"{
  ""nonverbal_behavior"": {
    ""gestures"": [
      {
        ""type"": ""gesture"",
        ""phrase"": ""the segment of the utterance where the gesture happens"",
        ""start"": ""the word where the gesture starts"",
        ""stroke"": ""the word that is the stroke point of the gesture"",
        ""relax"": ""the word that is the relax point of the gesture""
      }
    ],
    ""head_movements"": [
      {
        ""type"": ""head movement"",
        ""phrase"": ""the segment of the utterance where the head movement happens"",
        ""start"": ""the word where the head movement starts"",
        ""stroke"": ""the word that is the stroke point of the head movement"",
        ""relax"": ""the word that is the relax point of the head movement""
      }
    ],
    ""facial_action_unit"": [
      {
        ""type"": ""facial action unit"",
        ""phrase"": ""the segment of the utterance where the facial action unit happens"",
        ""start"": ""the word where the facial action unit starts"",
        ""stroke"": ""the word that is the stroke point of the facial action unit"",
        ""relax"": ""the word that is the relax point of the facial action unit""
      }
    ]
  }
}");
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Sends a user utterance to the OpenAI API for nonverbal behavior analysis.
    /// The AI's response (in JSON) will be passed to the onComplete callback.
    /// </summary>
    /// <param name="utterance">The spoken utterance from the speaker.</param>
    /// <param name="onComplete">Callback function to receive the raw JSON response from OpenAI.</param>
    public void AnalyzeUtterance(string utterance, Action<NlpResponse> onComplete)
    {
        if (_chatSystem == null)
        {
            Debug.LogError("Cannot analyze utterance: _chatSystem is not assigned.");
            onComplete?.Invoke(null); // Indicate failure
            return;
        }
        var spec = CharacterSpecLoader.Load("character_spec.json");
        var prompt = BuildSystemPrompt(spec);
        Debug.Log($"SYSTEM PROMPT\n{prompt}");
        string fullUtterance = $"{BuildSystemPrompt(spec)}.  The speaker says: \"{utterance}\"";

        //NlpRequest nlpRequest = new NlpRequest ( $"The speaker says: \"{utterance}\"" ); // Frame the utterance as user input
        NlpRequest nlpRequest = new NlpRequest(fullUtterance); // Frame the utterance as user input

        Debug.Log($"Sending utterance to OpenAI: \"{utterance}\"");


        _chatSystem.Request(nlpRequest, (nlpResponse) =>
        {
            // nlpResponse.response holds the content of the AI's message (which is JSON string)
            Debug.Log("Raw JSON response from OpenAI received: " + nlpResponse.content);
            onComplete?.Invoke(nlpResponse);
        });
    }


}