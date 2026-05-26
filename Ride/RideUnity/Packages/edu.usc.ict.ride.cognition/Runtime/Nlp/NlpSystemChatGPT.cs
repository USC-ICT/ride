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
        GPT4   = 10,
        GPT5_2 = 20
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
        public int m_maxTokens = 200;
        protected int m_answerSize = 1;

        ChatGPTModel m_model = ChatGPTModel.GPT4;

        private Dictionary<ChatGPTModel, string> m_modelDictionary = new Dictionary<ChatGPTModel, string>()
        {
            {ChatGPTModel.GPT4,   "gpt-4o"},
            {ChatGPTModel.GPT5_2, "gpt-5.2-chat-latest"}
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
            if (m_interactionHistory.Count == 0) { m_interactionHistory.Add(new NlpInteraction { input = prompt }); return; }
            else { m_interactionHistory[0] = new NlpInteraction { input = prompt }; }
        }

        public void ClearHistory()
        {
            string prompt = "";
            if (m_interactionHistory.Count > 0) prompt = m_interactionHistory[0].input;
            m_interactionHistory.Clear();
            SetSystemPrompt(prompt);
        }

        /// <inheritdoc/>
        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
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
                case ChatGPTModel.GPT5_2:
                    OpenAIQuestionGPT5 question5 = new OpenAIQuestionGPT5
                    {
                        model = m_modelDictionary[m_model],
                        messages = history.ToArray(),
                        temperature = 1,
                        max_completion_tokens = m_maxTokens,
                        n = m_answerSize
                    };
                    questionJSON = RideIO.JsonSerializeNoObjRef(question5);
                    break;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            m_uri = ConfigurationSystemUnity.GetOpenAIProxyEndpoint();
#endif

            //Call web service
            DateTime startTime = DateTime.Now;
            using var webRequest = new UnityWebRequest(m_uri, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(questionJSON);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationKey}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"ChatGPTSystem.cs::Request() - Failed: {webRequest.result}");
                return;
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
                OpenAIErrorMessage openAIErrorMessage = RideIO.JsonDeserialize<OpenAIErrorMessage>(response);
                qnaAnswer = new NlpResponse($"I'm sorry, something went wrong. I'm getting the error: " +
                    $"'{openAIErrorMessage.error.message}', of type '{openAIErrorMessage.error.type}'");
                Debug.LogError($"Error: {openAIErrorMessage.error.message}, type: {openAIErrorMessage.error.type}");
            }
            else
            {
                OpenAIAnswer oaiAnswer = RideIO.JsonDeserialize<OpenAIAnswer>(response);
                qnaAnswer = new NlpResponse(oaiAnswer.choices[0].message.content);  // Pick first answer for now
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
