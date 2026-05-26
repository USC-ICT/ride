using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>
    /// Implementation of vLLM <https://vllm.ai/>, an open source LLM solution that 
    /// can be deployed as a local endpoint. 
    /// </summary>
    public class NlpSystemVLLM : NlpSystemUnity
    {
        [Header("Endpoint")]
        [SerializeField] private string m_endpoint = "http://127.0.0.1:8000/v1/chat/completions";
        [SerializeField] private string m_model = "Qwen/Qwen2.5-3B-Instruct";
        //[SerializeField] private string m_model = "meta-llama/Llama-3.1-8B-Instruct";
        [SerializeField] private bool m_sendAuthorizationHeader = false;
        [SerializeField] private string m_authorizationToken = string.Empty;

        [Header("Generation")]
        [SerializeField] private double m_temperature = 0.3;
        [SerializeField] private int m_maxTokens = 200;
        [SerializeField] private int m_answerSize = 1;


        /// <inheritdoc/>
        public override void SystemInit()
        {
            m_uri = m_endpoint;
            SetSystemPrompt(m_initialPrompt);
            base.SystemInit();
        }

        /// <inheritdoc/>
        public override void SetSystemPrompt(string prompt)
        {
            if (m_interactionHistory.Count == 0)
            {
                m_interactionHistory.Add(new NlpInteraction { input = prompt });
                return;
            }

            m_interactionHistory[0] = new NlpInteraction { input = prompt };
        }

        /// <inheritdoc/>
        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
        {
            var history = GetParsedHistory();
            history.Add(new VllmMessage { role = "user", content = request.content });

            var question = new VllmQuestion
            {
                model = m_model,
                messages = history.ToArray(),
                temperature = m_temperature,
                max_tokens = m_maxTokens,
                n = m_answerSize,
                stream = false
            };
            string questionJson = RideIO.JsonSerializeNoObjRef(question);

            DateTime startTime = DateTime.Now;
            using var webRequest = new UnityWebRequest(m_endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(questionJson);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            if (m_sendAuthorizationHeader && !string.IsNullOrWhiteSpace(m_authorizationToken))
                webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationToken}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            DateTime endTime = DateTime.Now;
            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            if (webRequest.result == UnityWebRequest.Result.ConnectionError     ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"NlpSystemVLLM::Request() - Failed: {webRequest.result} - {webRequest.error}");
                return;
            }

            string response = webRequest.downloadHandler.text;
            VllmAnswer answer = RideIO.JsonDeserialize<VllmAnswer>(response);
            string content = GetAnswerContent(answer);
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("NlpSystemVLLM::Request() - Received an empty response.");
                return;
            }

            m_interactionHistory.Add(new NlpInteraction
            {
                input = request.content,
                response = content,
                inputTimestamp = startTime,
                responseTimestamp = endTime
            });

            onComplete?.Invoke(new NlpResponse(content));
        }

        private string GetAnswerContent(VllmAnswer answer)
        {
            if (answer.choices == null || answer.choices.Length == 0)
                return string.Empty;

            return answer.choices[0].message.content ?? string.Empty;
        }

        private List<VllmMessage> GetParsedHistory()
        {
            List<VllmMessage> history = new();

            for (int i = 0; i < m_interactionHistory.Count; ++i)
            {
                if (i == 0)
                {
                    history.Add(new VllmMessage { role = "system", content = m_interactionHistory[i].input });
                    continue;
                }

                if (m_interactionHistory[i].input != null)
                    history.Add(new VllmMessage { role = "user", content = m_interactionHistory[i].input });

                if (m_interactionHistory[i].response != null)
                    history.Add(new VllmMessage { role = "assistant", content = m_interactionHistory[i].response });
            }

            return history;
        }

        [Serializable]
        private struct VllmQuestion
        {
            public string model;
            public VllmMessage[] messages;
            public double temperature;
            public int max_tokens;
            public int n;
            public bool stream;
        }

        [Serializable]
        private struct VllmMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private struct VllmAnswer
        {
            public VllmChoice[] choices;
        }

        [Serializable]
        private struct VllmChoice
        {
            public VllmMessage message;
        }
    }
}
