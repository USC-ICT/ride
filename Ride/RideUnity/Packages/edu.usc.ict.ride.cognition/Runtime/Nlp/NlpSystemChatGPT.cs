using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    #region OpenAIChatGPTDataStructs
        
    public enum ChatGPTModel
    {
        GPT4         = 10,
        GPT5_2       = 20,
        GPT5_4       = 30,
        GPT5_4_mini  = 31,
        GPT5_5       = 40,
        GPT5_6_sol   = 50,
        GPT5_6_terra = 51,
        GPT5_6_luna  = 52
    }

    /// <summary>
    /// How much internal reasoning a reasoning-capable model spends before answering. "None" makes
    /// the model behave as a non-reasoning model, which is the lowest-latency setting and the right
    /// choice for spoken conversation; higher settings trade response time for deliberation on
    /// harder tasks. Ignored by models that do not reason.
    /// </summary>
    public enum ChatGPTReasoningEffort
    {
        None   = 10,
        Low    = 20,
        Medium = 30,
        High   = 40
    }

    /// <summary>
    /// Question to be asked to ChatGPT; GPT 4 and below
    /// https://platform.openai.com/docs/api-reference/chat/create 
    /// </summary>
    public struct OpenAIQuestion
    {
        public string model;
        public OpenAIMessage[] messages;
        public double temperature;
        public int max_tokens;
        public int n;       // How many chat completion choices to generate for each input message.
        public bool stream; // Whether to send partial message deltas
    }

    public struct OpenAIMessage
    {
        public string role; // system, user, or assistant
        public string content;
    }

    /// <summary>
    /// Question to be asked to ChatGPT; GPT 5 and above    
    /// </summary>
    public struct OpenAIQuestionGPT5
    {
        public string model;
        public OpenAIMessage[] messages;
        public double temperature;
        public int max_completion_tokens;
        public int n;       // How many chat completion choices to generate for each input message.
        public bool stream; // Whether to send partial message deltas
    }

    /// <summary>
    /// Question to be asked to a reasoning-capable GPT 5 model. Separate from
    /// <see cref="OpenAIQuestionGPT5"/> because models without reasoning reject the parameter.
    /// </summary>
    public struct OpenAIQuestionGPT5Reasoning
    {
        public string model;
        public OpenAIMessage[] messages;
        public double temperature;
        public int max_completion_tokens;
        public int n;
        public bool stream;
        public string reasoning_effort;
    }

    /// <summary>
    /// Response from OpenAI ChatGPT
    /// </summary>
    public struct OpenAIAnswer
    {
        public string id;
        public string _object;
        public int created;
        public OpenAIChoice[] choices;
        public OpenAIUsage usage;
    }

    public struct OpenAIChoice
    {
        public int index;
        public OpenAIMessage message;
        public string finish_reason;
    }

    public struct OpenAIUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    public struct OpenAIErrorMessage
    {
        public OpenAIError error;
    }

    public struct OpenAIError
    {
        public string message;
        public string type;
        public string param;
        public string code;
    }

    #endregion

    /// <summary>
    /// Uses OpenAI ChatGPT (https://openai.com/api) to provide NLP functionalities.
    /// </summary>
    public class NlpSystemChatGPT : NlpSystemUnity
    {
        public double m_temperature = 0.3;

        /// <inheritdoc/>
        /// <remarks>Ignored for models listed in the default-temperature-only set below.</remarks>
        public override float Temperature
        {
            get => (float)m_temperature;
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

        // Hard ceiling on a response, sent as max_completion_tokens for GPT5+ models. It is a
        // cost/runaway guard, NOT a brevity control - the system prompt sets the desired length.
        // Keep it generous: on reasoning models this budget also covers the model's internal
        // reasoning, so a small value can consume the whole allowance and return empty content.
        // Comfortably above the longest response the speech pipeline will speak, so the limit
        // never shapes an answer, but not so high that tokens are generated only to be discarded.
        public int m_maxTokens = 2000;
        protected int m_answerSize = 1;
        [SerializeField, Min(1)] private int m_requestTimeoutSeconds = 20;

        // Reasoning models spend time and tokens deliberating before they answer, which a spoken
        // conversation cannot afford, so the default turns that off. Only sent to models that
        // reason; the others reject the parameter outright.
        [SerializeField] private ChatGPTReasoningEffort m_reasoningEffort = ChatGPTReasoningEffort.None;

        private static readonly Dictionary<ChatGPTReasoningEffort, string> s_reasoningEffortDictionary = new()
        {
            { ChatGPTReasoningEffort.None,   "none"   },
            { ChatGPTReasoningEffort.Low,    "low"    },
            { ChatGPTReasoningEffort.Medium, "medium" },
            { ChatGPTReasoningEffort.High,   "high"   },
        };

        // Models that accept a reasoning_effort setting. Chat/"instant" models such as
        // gpt-5.2-chat-latest do not reason and reject the parameter.
        private static readonly HashSet<ChatGPTModel> s_reasoningModels = new()
        {
            ChatGPTModel.GPT5_4,
            ChatGPTModel.GPT5_4_mini,
            ChatGPTModel.GPT5_5,
            ChatGPTModel.GPT5_6_sol,
            ChatGPTModel.GPT5_6_terra,
            ChatGPTModel.GPT5_6_luna,
        };

        [SerializeField] private ChatGPTModel m_model = ChatGPTModel.GPT5_6_luna;

        private Dictionary<ChatGPTModel, string> m_modelDictionary = new Dictionary<ChatGPTModel, string>()
        {
            {ChatGPTModel.GPT4,         "gpt-4o"},
            {ChatGPTModel.GPT5_2,       "gpt-5.2-chat-latest"},
            {ChatGPTModel.GPT5_4,       "gpt-5.4" },
            {ChatGPTModel.GPT5_4_mini,  "gpt-5.4-mini" },
            {ChatGPTModel.GPT5_5,       "gpt-5.5" },
            {ChatGPTModel.GPT5_6_sol,   "gpt-5.6-sol" },
            {ChatGPTModel.GPT5_6_terra, "gpt-5.6-terra" },
            {ChatGPTModel.GPT5_6_luna,  "gpt-5.6-luna" }
        };

        // Models that reject any temperature other than the service default: sending a
        // custom value fails the request outright ("Only the default (1) value is
        // supported"), so these fall back to 1 and m_temperature is ignored for them.
        private static readonly HashSet<ChatGPTModel> s_defaultTemperatureOnlyModels = new()
        {
            ChatGPTModel.GPT5_2,
            ChatGPTModel.GPT5_6_sol,
            ChatGPTModel.GPT5_6_terra,
            ChatGPTModel.GPT5_6_luna,
        };

        /// <inheritdoc/>
        public override void SystemInit()
        {
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_uri = configSystem.config.openAIChatGPT.endpoint;
            m_authorizationKey = configSystem.config.openAIChatGPT.endpointKey;

            SetSystemPrompt(m_initialPrompt);
            base.SystemInit();
        }

        /// <inheritdoc/>
        public override void SetSystemPrompt(string prompt)
        {
            m_initialPrompt = prompt;
            if (m_interactionHistory.Count == 0) { m_interactionHistory.Add(new NlpInteraction { input = prompt }); return; }
            else { m_interactionHistory[0] = new NlpInteraction { input = prompt }; }
        }

        public override void ClearHistory()
        {
            string prompt = "";
            if (m_interactionHistory.Count > 0) prompt = m_interactionHistory[0].input;
            m_interactionHistory.Clear();
            SetSystemPrompt(prompt);
        }

        /// <inheritdoc/>
        protected override async void RequestInternal(NlpRequest request, Action<NlpResponse> onComplete)
        {
            //Prepare parameters for the request
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {m_authorizationKey}" }
            };

            var history = GetParsedHistory();
            history.Add(new OpenAIMessage { role = "user", content = request.content });

            //Serialize question to AI
            string questionJSON = "";

            switch (m_model)
            {
                case ChatGPTModel.GPT4:
                    OpenAIQuestion question4 = new OpenAIQuestion
                    {
                        model = m_modelDictionary[m_model],
                        messages = history.ToArray(),
                        temperature = m_temperature,
                        max_tokens = m_maxTokens,
                        n = m_answerSize
                    };
                    questionJSON = RideIO.JsonSerializeNoObjRef(question4);
                    break;
                default: // GPT5_2 and newer: the GPT5+ request format
                    double temperature = s_defaultTemperatureOnlyModels.Contains(m_model) ? 1 : m_temperature;
                    if (s_reasoningModels.Contains(m_model))
                    {
                        OpenAIQuestionGPT5Reasoning question5Reasoning = new OpenAIQuestionGPT5Reasoning
                        {
                            model = m_modelDictionary[m_model],
                            messages = history.ToArray(),
                            temperature = temperature,
                            max_completion_tokens = m_maxTokens,
                            n = m_answerSize,
                            reasoning_effort = s_reasoningEffortDictionary[m_reasoningEffort]
                        };
                        questionJSON = RideIO.JsonSerializeNoObjRef(question5Reasoning);
                        break;
                    }

                    OpenAIQuestionGPT5 question5 = new OpenAIQuestionGPT5
                    {
                        model = m_modelDictionary[m_model],
                        messages = history.ToArray(),
                        temperature = temperature,
                        max_completion_tokens = m_maxTokens,
                        n = m_answerSize
                    };
                    questionJSON = RideIO.JsonSerializeNoObjRef(question5);
                    break;
            }

            if (RideUtils.IsWebGL() && !RideUtils.IsEditor())
                m_uri = ConfigurationSystemUnity.GetOpenAIProxyEndpoint();

            //Call web service
            DateTime startTime = DateTime.Now;
            using var webRequest = new UnityWebRequest(m_uri, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(questionJSON);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (!(RideUtils.IsWebGL() && !RideUtils.IsEditor()))
                webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationKey}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            DateTime endTime = DateTime.Now;
            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            var response = webRequest.downloadHandler.text;

            //Debug.Log($"Web response: {response}");

            //Parse response
            NlpResponse qnaAnswer;
            if (webRequest.result == UnityWebRequest.Result.ProtocolError ||
                webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError)
            {
                // A request that times out or cannot reach the service arrives here with no
                // error body to parse, so fall back to the transport error text. The body is
                // only parsed when there is one: deserializing an empty string throws, because
                // the error types are structs and so cannot be given a null result.
                OpenAIErrorMessage openAIErrorMessage = default;
                if (!string.IsNullOrWhiteSpace(response))
                {
                    try
                    {
                        openAIErrorMessage = RideIO.JsonDeserialize<OpenAIErrorMessage>(response);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"NlpSystemChatGPT::Request() - Could not parse the error " +
                            $"response body: {exception.Message}");
                    }
                }

                string message = openAIErrorMessage.error.message;
                if (string.IsNullOrEmpty(message))
                    message = string.IsNullOrEmpty(webRequest.error) ? webRequest.result.ToString() : webRequest.error;

                string errorType = openAIErrorMessage.error.type;
                if (string.IsNullOrEmpty(errorType))
                    errorType = webRequest.result.ToString();

                qnaAnswer = new NlpResponse($"I'm sorry, something went wrong. I'm getting the error: " +
                    $"'{message}', of type '{errorType}'");
                Debug.LogError($"Error: {message}, type: {errorType}");
            }
            else
            {
                OpenAIAnswer oaiAnswer = RideIO.JsonDeserialize<OpenAIAnswer>(response);
                string content = oaiAnswer.choices != null && oaiAnswer.choices.Length > 0
                    ? oaiAnswer.choices[0].message.content : null;   // Pick first answer for now

                // An empty content string is a real outcome, most often when the response hit
                // max_completion_tokens (on reasoning models the internal reasoning can consume
                // the whole budget). Speaking a short line keeps the conversation - and the
                // character's thinking behavior - from stalling on an utterance that never comes.
                if (string.IsNullOrWhiteSpace(content))
                {
                    string finishReason = oaiAnswer.choices != null && oaiAnswer.choices.Length > 0
                        ? oaiAnswer.choices[0].finish_reason : "unknown";
                    Debug.LogWarning($"NlpSystemChatGPT::Request() - Empty response content " +
                        $"(finish_reason '{finishReason}', max tokens {m_maxTokens}).");
                    content = "I'm sorry, I did not receive a text response.";
                }
                qnaAnswer = new NlpResponse(content);
            }

            //Update conversation history
            NlpInteraction interaction = new();
            interaction.input = request.content;
            interaction.response = qnaAnswer.content[0];
            interaction.inputTimestamp = startTime;
            interaction.responseTimestamp = endTime;
            m_interactionHistory.Add(interaction);

            //Invoke callback on complete
            onComplete?.Invoke(qnaAnswer);
        }

        /// <summary>
        /// Parses the cached conversation history with the AI into a JSON-serializable class list.
        /// This method processes stored conversation data, converting it into a structured list of objects 
        /// that can be serialized into JSON. The resulting list is used as part of the context sent with 
        /// requests to the AI, ensuring that the AI has the necessary historical context to generate 
        /// appropriate responses.
        /// <returns>List of class that can be serialized into JSON.</returns>
        private List<OpenAIMessage> GetParsedHistory()
        {
            List<OpenAIMessage> history = new();

            for (int i = 0; i < m_interactionHistory.Count; ++i)
            {
                if (i == 0)
                {
                    history.Add(new OpenAIMessage { role = "system", content = m_interactionHistory[i].input });
                    continue;
                }
                if (m_interactionHistory[i].input != null)
                {
                    history.Add(new OpenAIMessage { role = "user", content = m_interactionHistory[i].input });
                }
                if (m_interactionHistory[i].response != null)
                {
                    history.Add(new OpenAIMessage { role = "assistant", content = m_interactionHistory[i].response });
                }
            }

            return history;
        }
    }
}
