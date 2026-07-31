using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>Selectable Gemini models for NLP. Model ids live in code (not RideConfig) - see the
    /// dictionary below; mirrors the ChatGPT NLP pattern.</summary>
    public enum GeminiNlpModel
    {           
        Pro25       = 10,
        Flash35     = 20,
        Flash31Lite = 30,
    }

    /// <summary>
    /// Uses Google Gemini to provide LLM functionality through the Gemini generateContent REST API.
    /// </summary>
    public class NlpSystemGemini : NlpSystemUnity
    {
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 20;
        [SerializeField, Min(1)] private int m_maxOutputTokens = 2000;
        [SerializeField, Range(0f, 2f)] private float m_temperature = 0.3f;

        /// <inheritdoc/>
        public override float Temperature
        {
            get => m_temperature;
            set => m_temperature = value;
        }

        /// <inheritdoc/>
        public override bool SupportsGenerationSettings => true;

        /// <inheritdoc/>
        public override int MaxTokens
        {
            get => m_maxOutputTokens;
            set => m_maxOutputTokens = value;
        }
                
        [SerializeField] private GeminiNlpModel m_model = GeminiNlpModel.Flash35;
                
        private readonly Dictionary<GeminiNlpModel, string> m_modelDictionary = new()
        {
            { GeminiNlpModel.Pro25,         "gemini-2.5-pro"        },
            { GeminiNlpModel.Flash35,       "gemini-3.5-flash"      },
            { GeminiNlpModel.Flash31Lite,   "gemini-3.1-flash-lite" },
        };

        private string ModelId => m_modelDictionary[m_model];

        /// <inheritdoc/>
        public override void SystemInit()
        {
            var config = Systems.Get<ConfigurationSystemUnity>()?.config.gemini ?? RideConfig.GeminiSettings.Default;
            m_uri = config.endpoint;
            m_authorizationKey = config.endpointKey;
            base.SystemInit();
        }

        /// <inheritdoc/>
        protected override async void RequestInternal(NlpRequest request, Action<NlpResponse> onComplete)
        {
            var config = Systems.Get<ConfigurationSystemUnity>()?.config.gemini ?? RideConfig.GeminiSettings.Default;
            string url = BuildGenerateContentUrl(config.endpoint, ModelId);
            string requestJson = BuildRequestJson(request.content);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-goog-api-key", config.endpointKey);

            DateTime startTime = DateTime.Now;
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();
            DateTime endTime = DateTime.Now;

            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                string message = ExtractErrorMessage(webRequest.downloadHandler.text);
                Debug.LogWarning($"NlpSystemGemini::Request() - Failed: {webRequest.result} {message}");
                onComplete?.Invoke(new NlpResponse($"I'm sorry, something went wrong. I'm getting the error: '{message}'"));
                return;
            }

            string responseText = ExtractText(webRequest.downloadHandler.text);
            if (string.IsNullOrWhiteSpace(responseText))
            {
                Debug.LogWarning("NlpSystemGemini::Request() - Gemini response did not contain text.");
                onComplete?.Invoke(new NlpResponse("I'm sorry, I did not receive a text response."));
                return;
            }

            m_interactionHistory.Add(new NlpInteraction
            {
                input = request.content,
                response = responseText,
                inputTimestamp = startTime,
                responseTimestamp = endTime,
            });

            onComplete?.Invoke(new NlpResponse(responseText));
        }

        /// <inheritdoc/>
        public override void SetSystemPrompt(string prompt)
        {
            m_initialPrompt = prompt;
        }

        /// <inheritdoc/>
        public override void ClearHistory()
        {
            m_interactionHistory.Clear();
        }

        private string BuildRequestJson(string userInput)
        {
            JArray contents = GetParsedHistory();
            contents.Add(CreateContent("user", userInput));

            var body = new JObject
            {
                ["contents"] = contents,
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = m_temperature,
                    ["maxOutputTokens"] = m_maxOutputTokens
                }
            };

            if (!string.IsNullOrWhiteSpace(m_initialPrompt))
                body["system_instruction"] = CreateSystemInstruction(m_initialPrompt);

            return body.ToString(Newtonsoft.Json.Formatting.None);
        }

        private JArray GetParsedHistory()
        {
            var history = new JArray();
            foreach (var interaction in m_interactionHistory)
            {
                if (interaction.response == null && interaction.input == m_initialPrompt)
                    continue;

                if (interaction.input != null)
                    history.Add(CreateContent("user", interaction.input));
                if (interaction.response != null)
                    history.Add(CreateContent("model", interaction.response));
            }

            return history;
        }

        private static JObject CreateSystemInstruction(string text)
        {
            return new JObject
            {
                ["parts"] = new JArray(new JObject { ["text"] = text })
            };
        }

        private static JObject CreateContent(string role, string text)
        {
            return new JObject
            {
                ["role"] = role,
                ["parts"] = new JArray(new JObject { ["text"] = text })
            };
        }

        private static string BuildGenerateContentUrl(string endpoint, string model)
        {
            string baseEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? RideConfig.GeminiSettings.Default.endpoint
                : endpoint.TrimEnd('/');
            string resolvedModel = string.IsNullOrWhiteSpace(model) ? "gemini-3.5-flash" : model;
            return $"{baseEndpoint}/{resolvedModel}:generateContent";
        }

        private static string ExtractText(string json)
        {
            try
            {
                JObject response = JObject.Parse(json);
                JArray parts = response["candidates"]?.FirstOrDefault()?["content"]?["parts"] as JArray;
                if (parts == null)
                    return string.Empty;

                return string.Concat(parts
                    .Select(part => part?["text"]?.ToString())
                    .Where(text => !string.IsNullOrEmpty(text)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"NlpSystemGemini::ExtractText() - Failed to parse response: {exception.Message}");
                return string.Empty;
            }
        }

        private static string ExtractErrorMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "empty Gemini error response";

            try
            {
                return JObject.Parse(json)["error"]?["message"]?.ToString() ?? json;
            }
            catch
            {
                return json;
            }
        }
    }
}
