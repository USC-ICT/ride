using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        // Connection/deployment config is code-authoritative (NOT [SerializeField]) so changing a default
        // here takes effect immediately, with no prefab/scene edit. For per-deployment overrides, source from RideConfig.
        // Model must match what the vLLM container serves (WebServices/vllm/Dockerfile). Qwen3-4B-Instruct-2507
        // is Apache-2.0 (redistributable); the previous Qwen2.5-3B-Instruct was non-commercial.
        private string m_endpoint = "http://127.0.0.1:8000/v1/chat/completions";
        // Requests use the vLLM "served-model-name" alias, not a specific model id, so the actual model
        // can be swapped via the container's VLLM_MODEL env with no change here (WebServices/vllm/Dockerfile,
        // WebServices/local-ai/.env). vLLM serves one model per process and can't hot-load per request.
        private string m_model = "vhtoolkit-llm";
        private bool m_sendAuthorizationHeader = true;
        private string m_authorizationToken = "local-dev-token";
        private int m_requestTimeoutSeconds = 20; // a cold first call that must load the model may hit this

        [Header("Generation")]
        [SerializeField] private double m_temperature = 0.3;

        /// <inheritdoc/>
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

        [SerializeField] private int m_maxTokens = 2000;
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
            m_initialPrompt = prompt;
            if (m_interactionHistory.Count == 0)
            {
                m_interactionHistory.Add(new NlpInteraction { input = prompt });
                return;
            }

            m_interactionHistory[0] = new NlpInteraction { input = prompt };
        }

        public override void ClearHistory()
        {
            string prompt = string.Empty;
            if (m_interactionHistory.Count > 0)
                prompt = m_interactionHistory[0].input;

            m_interactionHistory.Clear();
            SetSystemPrompt(prompt);
        }

        /// <inheritdoc/>
        protected override void RequestInternal(NlpRequest request, Action<NlpResponse> onComplete)
        {
            StartCoroutine(RequestCoroutine(request, onComplete));
        }

        private IEnumerator RequestCoroutine(NlpRequest request, Action<NlpResponse> onComplete)
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
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            if (m_sendAuthorizationHeader && !string.IsNullOrWhiteSpace(m_authorizationToken))
                webRequest.SetRequestHeader("Authorization", $"Bearer {m_authorizationToken}");

            yield return webRequest.SendWebRequest();

            DateTime endTime = DateTime.Now;
            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            // Every exit path must invoke onComplete: callers block their conversation flow
            // (including the character's thinking behavior) until a response arrives, so a
            // silent return leaves the character waiting forever.
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"NlpSystemVLLM::Request() - Failed: {webRequest.result} - {webRequest.error}");
                onComplete?.Invoke(new NlpResponse(
                    $"I'm sorry, something went wrong. I'm getting the error: '{webRequest.error}'"));
                yield break;
            }

            string response = webRequest.downloadHandler.text;
            VllmAnswer answer = RideIO.JsonDeserialize<VllmAnswer>(response);
            string content = GetAnswerContent(answer);
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("NlpSystemVLLM::Request() - Received an empty response.");
                onComplete?.Invoke(new NlpResponse("I'm sorry, I did not receive a text response."));
                yield break;
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
