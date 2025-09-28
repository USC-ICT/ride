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
        GPT4 = 0,
        GPT3_5 = 20,
    }

    /// <summary>
    /// Question to be asked to the general OpenAI GPT
    /// </summary>
    [Serializable]
    // Sample JSON:
    // {
    //	    "model": "text-davinci-003",
    //	    "prompt": "Say this is a test",
    //	    "temperature": 0,
    //	    "max_tokens": 7
    // }
    public struct OpenAIQuestion
    {
        public string model;
        public string prompt;
        public double temperature;
        public int max_tokens;
    }

    /// <summary>
    /// Response from OpenAI GPT
    /// </summary>
    [Serializable]
    // Sample JSON:
    //  {
    //	    "id": "cmpl-6PxXPfiCt3zMdWj2zmsDLIgiKyOTP",
    //	    "object": "text_completion",
    //	    "created": 1671645083,
    //	    "model": "text-davinci-003",
    //	    "choices": [{
    //    		"text": "\n\nThis is indeed a test",
    //    	    "index": 0,
    //    	    "logprobs": null,
    //    	    "finish_reason": "length"
    //      }],
    //	    "usage": {
    //    		"prompt_tokens": 5,
    //    	    "completion_tokens": 7,
    //    	    "total_tokens": 12
    //	    }
    // }
    public struct OpenAIAnswer
    {
        public string id;
        public string _object;
        public int created;
        public string model;
        public OpenAIChoice[] choices;
        public OpenAIUsage usage;
    }

    public struct OpenAIChoice
    {
        public string text;
        public int index;
        public string logprobs;
        public string finish_reason;
    }

    public struct OpenAIUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    // Sample JSON:
    // {
    //  "model": "gpt-3.5-turbo",
    //  "messages": [{"role": "user", "content": "Hello!"}]
    // }
    // https://platform.openai.com/docs/api-reference/chat/create 

    /// <summary>
    /// Question to be asked to ChatGPT from OpenAI 
    /// </summary>
    public struct OpenAIChatQuestion
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

    // Example response
    // 
    // {"id":"chatcmpl-6pafTdDGZaQg1mFdkRqb6ZblIkf5O","object":"chat.completion",
    // "created":1677753699,"model":"gpt-3.5-turbo-0301","usage":{"prompt_tokens":19,
    // "completion_tokens":11,"total_tokens":30},"choices":[{"message":{"role":"assistant",
    // "content":"Hello! How can I assist you today?"},"finish_reason":"stop","index":0}]}

    /// <summary>
    /// Response from OpenAI ChatGPT 
    /// </summary>
    public struct OpenAIChatGPTAnswer
    {
        public string id;
        public string _object;
        public int created;
        public OpenAIChatGPTChoice[] choices;
        public OpenAIUsage usage;
    }

    public struct OpenAIChatGPTChoice
    {
        public int index;
        public OpenAIMessage message;
        public string finish_reason;
    }
    #endregion

    /// <summary>
    /// Uses OpenAI ChatGPT (https://openai.com/api) to provide LLM functionalities.
    /// </summary>
    public class NlpSystemChatGPT : NlpSystemUnity
    {
        public double temperature = 0.3;
        public int max_tokens = 200;
       
        protected int m_answerSize = 1;
        
        private string m_model = "gpt-4o";

        private Dictionary<ChatGPTModel, string> m_modelDictionary = new Dictionary<ChatGPTModel, string>()
        { 
            {ChatGPTModel.GPT4, "gpt-4o"},
            {ChatGPTModel.GPT3_5, "gpt-3.5-turbo"},
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
            OpenAIChatQuestion question = new OpenAIChatQuestion
            {
                model = m_model,
                messages = history.ToArray(),
                temperature = temperature,
                max_tokens = max_tokens,
                n = m_answerSize
            };
            string questionJSON = RideIO.JsonSerializeNoObjRef(question);

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

            //Parse response
            OpenAIChatGPTAnswer oaiAnswer = RideIO.JsonDeserialize<OpenAIChatGPTAnswer>(response);
            NlpResponse qnaAnswer = new NlpResponse(/*response, */oaiAnswer.choices[0].message.content);  // Pick first answer for now

            //Update conversation history
            NlpInteraction interaction = new();
            interaction.input = request.content;
            interaction.response = oaiAnswer.choices[0].message.content;
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
                if(i == 0)
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
