using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>Selectable Anthropic Claude models for NLP. Model ids live in code (not
    /// RideConfig) - see the dictionary below; mirrors the ChatGPT NLP pattern.</summary>
    public enum AnthropicModel
    {
        Fable5   = 10,
        Opus5    = 20,
        Sonnet5  = 30,
        Haiku45  = 40,
    }

    /// <summary>
    /// Uses Anthropic Claude (https://www.anthropic.com/api) to provide LLM functionalities.
    /// </summary>
    public class NlpSystemAnthropic : NlpSystemUnity
    {
        [SerializeField, Range(0f, 1f)] private float m_temperature = 0.3f;

        // Hard ceiling on a response. It is a cost/runaway guard, NOT a brevity control - the
        // system prompt sets the desired length.
        [SerializeField, Min(1)] private int m_maxTokens = 2000;
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 20;

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
            get => m_maxTokens;
            set => m_maxTokens = value;
        }

        [SerializeField] private AnthropicModel m_model = AnthropicModel.Haiku45;

        private readonly Dictionary<AnthropicModel, string> m_modelDictionary = new()
        {
            { AnthropicModel.Fable5,  "claude-fable-5"   },
            { AnthropicModel.Opus5,   "claude-opus-5"    },
            { AnthropicModel.Sonnet5, "claude-sonnet-5"  },
            { AnthropicModel.Haiku45, "claude-haiku-4-5" },
        };

        /// <summary>The Anthropic model id currently selected, e.g. for UI display.</summary>
        public string ModelId => m_modelDictionary[m_model];

        /// <summary>Selects which model subsequent requests use.</summary>
        public void SetActiveModel(AnthropicModel model) => m_model = model;


        /// <summary>
        /// Requests response based on provided user input for Anthropic. 
        /// </summary>
        /// <param name="request">User input, string question</param>
        /// <param name="onComplete">Delegate to execute on successful request, typically parses JSON response</param>
        protected override async void RequestInternal(NlpRequest request, Action<NlpResponse> onComplete)
        { 
            //Prepare parameters for the request
            var messagesList = GetParsedHistory();
            messagesList.Add(new AnthropicMessage("user", request.content));

            //Serialize data for the question
            string data = RideIO.JsonSerialize(new
            {
                model = ModelId,
                system = m_initialPrompt,
                messages = messagesList.ToArray(),
                max_tokens = m_maxTokens,
                temperature = m_temperature
            },
            RideIO.GetJsonConfigNoNameHandling());

            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            m_uri = configSystem.config.anthropic.endpoint;
            m_authorizationKey = configSystem.config.anthropic.endpointKey;

            if (RideUtils.IsWebGL() && !RideUtils.IsEditor())
                m_uri = ConfigurationSystemUnity.GetAnthropicProxyEndpoint();

            // Call web service
            using var webRequest = new UnityWebRequest(m_uri, "POST");
            byte [] bodyRaw = Encoding.UTF8.GetBytes(data);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (!(RideUtils.IsWebGL() && !RideUtils.IsEditor()))
                webRequest.SetRequestHeader("x-api-key", m_authorizationKey);
            webRequest.SetRequestHeader("anthropic-version", "2023-06-01");

            DateTime startTime = DateTime.Now;
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();
            DateTime endTime = DateTime.Now;

            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            // Every exit path must invoke onComplete: callers block their conversation flow
            // (including the character's thinking behavior) until a response arrives, so a
            // silent return leaves the character waiting forever.
            if (webRequest.result == UnityWebRequest.Result.ConnectionError     ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                string error = string.IsNullOrEmpty(webRequest.error) ? webRequest.result.ToString() : webRequest.error;
                Debug.LogWarning($"AnthropicSystem.cs::Request() - Failed: {webRequest.result} - {error}");
                onComplete?.Invoke(new NlpResponse(
                    $"I'm sorry, something went wrong. I'm getting the error: '{error}'"));
                return;
            }

            // Deserialize reponse
            var result = webRequest.downloadHandler.text;
            var res = RideIO.JsonDeserialize<AnthropicResponse>(result);
            string content = res.content != null && res.content.Length > 0 ? res.content[0].text : null;

            // An empty response is a real outcome, most often when the answer hit max_tokens.
            // Speaking a short line keeps the conversation - and the character's thinking
            // behavior - from stalling on an utterance that never comes.
            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.LogWarning($"AnthropicSystem.cs::Request() - Empty response content " +
                    $"(stop reason '{res.stop_reason}', max tokens {m_maxTokens}).");
                content = "I'm sorry, I did not receive a text response.";
            }

            // Update conversation history
            NlpInteraction interaction = new NlpInteraction();
            interaction.input = request.content;
            interaction.response = content;
            interaction.inputTimestamp = startTime;
            interaction.responseTimestamp = endTime;
            m_interactionHistory.Add(interaction);

            // Invoke callback on complete
            onComplete?.Invoke(new NlpResponse(content));
        }

        /// <summary>
        /// Sets the prompt string to be used for generating responses from the LLM AI model.
        /// This method allows you to define or modify the prompt string that will guide the AI's response generation. 
        /// </summary>
        /// <param name="prompt">The string containing the prompt text that will be used by the AI</param>
        public override void SetSystemPrompt(string prompt)
        {
            m_initialPrompt = prompt;
        }

        public override void ClearHistory()
        {
            m_interactionHistory.Clear();
        }

        /// <summary>
        /// Parses the cached conversation history with the AI into a JSON-serializable class list.
        /// 
        /// This method processes stored conversation data, converting it into a structured list of objects 
        /// that can be serialized into JSON. The resulting list is used as part of the context sent with 
        /// requests to the AI, ensuring that the AI has the necessary historical context to generate 
        /// appropriate responses.
        /// <returns>List of class that can be serialized into JSON.</returns>
        private List<AnthropicMessage> GetParsedHistory()
        {
            List<AnthropicMessage> history = new();

            foreach (var interaction in m_interactionHistory)
            {
                if (interaction.input != null)
                    history.Add(new AnthropicMessage("user", interaction.input));

                if (interaction.response != null) 
                    history.Add(new AnthropicMessage("assistant", interaction.response));
            }

            return history;
        }

        #region AnthropicDataStruct
        private class AnthropicRequest
        {
            public int max_tokens { get; set; }
            public string model { get; set; }
            public AnthropicMessage[] messages { get; set; }    
        }

        private class AnthropicMessage
        {
            public string role { get; set; }
            public string content { get; set; }
            public AnthropicMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        private class AnthropicResponse
        {
            public AnthropicContent[] content { get; set; }    
            public string id { get; set; }
            public string model { get; set; }
            public string role { get; set; }
            public string stop_reason { get; set; } 
            public string stop_sequence { get; set; }   
            public string type { get; set; }    
            public AnthropicUsage usage { get; set; }   
        }

        private class AnthropicContent
        {
            public string text { get; set; }    
            public string type { get; set; }    
        }

        private class AnthropicUsage
        {
            public int input_tokens { get; set; }
            public int output_tokens { get; set; }
        }
        #endregion
    }
}
