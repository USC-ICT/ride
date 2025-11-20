using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>
    /// Uses Anthropic Claude (https://www.anthropic.com/api) to provide LLM functionalities.
    /// </summary>
    public class NlpSystemAnthropic : NlpSystemUnity
    {
        private string m_model = "claude-haiku-4-5"; //claude-sonnet-4-5


        /// <summary>
        /// Initializes the system by configuring the Anthropic endpoint, setting up authorization, 
        /// defining the initial prompt, and performing base initialization.
        /// 
        /// This method connects with the configuration system to retrieve and apply necessary 
        /// settings such as the AI endpoint URL and the authorization key. It also sets up the 
        /// initial prompt that guides the AI's responses. Finally, it performs any essential 
        /// base initialization required for the system to function correctly.        
        /// </summary>
        public override void SystemInit()
        {
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_uri = configSystem.config.anthropic.endpoint;
            m_authorizationKey = configSystem.config.anthropic.endpointKey;

            base.SystemInit();
        }

        /// <summary>
        /// Requests response based on provided user input for Anthropic. 
        /// </summary>
        /// <param name="request">User input, string question</param>
        /// <param name="onComplete">Delegate to execute on successful request, typically parses JSON response</param>
        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
        { 
            //Prepare parameters for the request
            var messagesList = GetParsedHistory();
            messagesList.Add(new AnthropicMessage("user", request.content));

            //Serialize data for the question
            string data = RideIO.JsonSerialize(new
            {
                model = m_model,
                system = m_initialPrompt,
                messages = messagesList.ToArray(),
                max_tokens = 1024
            }
             , RideIO.GetJsonConfigNoNameHandling());

            //Call web service
            using var webRequest = new UnityWebRequest(m_uri, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", m_authorizationKey);
            webRequest.SetRequestHeader("anthropic-version", "2023-06-01");

            DateTime startTime = DateTime.Now;
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            DateTime endTime = DateTime.Now;
            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            if (webRequest.result == UnityWebRequest.Result.ConnectionError     ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                UnityEngine.Debug.LogWarning($"AnthropicSystem.cs::Request() - Failed: {webRequest.result}");
                return;
            }

            //Deserialize reponse
            var result = webRequest.downloadHandler.text;
            var res = RideIO.JsonDeserialize<AnthropicResponse>(result);

            //Update conversation history
            NlpInteraction interaction = new NlpInteraction();
            interaction.input = request.content;
            interaction.response = res.content[0].text;
            interaction.inputTimestamp = startTime;
            interaction.responseTimestamp = endTime;
            m_interactionHistory.Add(interaction);

            //Invoke callback on complete
            onComplete?.Invoke(new NlpResponse(/*result, */res.content[0].text));
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
                {
                    history.Add(new AnthropicMessage("user", interaction.input));
                }
                if (interaction.response != null) 
                {
                    history.Add(new AnthropicMessage("assistant", interaction.input));
                }
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
