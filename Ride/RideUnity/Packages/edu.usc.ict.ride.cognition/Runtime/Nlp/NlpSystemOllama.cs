using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>Two hot-swappable local models served by Ollama. Model ids live in code (not RideConfig);
    /// Ollama keeps both resident and switches per request, so swapping is instant and needs no restart.</summary>
    public enum OllamaModel
    {
        ModelA = 10,
        ModelB = 20,
    }

    /// <summary>
    /// Local LLM via <see href="https://ollama.com">Ollama</see>'s OpenAI-compatible
    /// <c>/v1/chat/completions</c> endpoint (default port 11434). Unlike vLLM, Ollama loads models on
    /// demand by name and can hold several resident at once, so this system exposes two selectable models
    /// that can be hot-swapped at runtime via <see cref="ToggleModel"/> / <see cref="SetActiveModel"/>.
    /// Coroutine-based (no async); connection/model config is code-authoritative (not [SerializeField]). 
    /// </summary>
    public class NlpSystemOllama : NlpSystemUnity
    {
        private string m_endpoint = "http://127.0.0.1:11434/v1/chat/completions";
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

        // The two hot-swappable models. Names must match models the Ollama container has pulled
        // (WebServices/ollama-local/.env: OLLAMA_MODEL_A / OLLAMA_MODEL_B).
        [SerializeField] private OllamaModel m_activeModel = OllamaModel.ModelA;

        private readonly Dictionary<OllamaModel, string> m_modelDictionary = new()
        {
            { OllamaModel.ModelA, "phi4-mini"  },  // MIT license, 3.8B -- primary distributable default
            { OllamaModel.ModelB, "qwen3:1.7b" },  // Apache-2.0, 1.7B -- fast fallback
        };

        /// <summary>The currently selected model enum.</summary>
        public OllamaModel ActiveModel => m_activeModel;

        /// <summary>The Ollama model id currently in use (e.g. "phi4-mini"), for UI display.</summary>
        public string ActiveModelId => m_modelDictionary[m_activeModel];

        /// <summary>Selects which of the two models subsequent requests use. Instant - no restart.</summary>
        public void SetActiveModel(OllamaModel model) => m_activeModel = model;

        /// <summary>Flips between the two configured models (hot-swap).</summary>
        public void ToggleModel() =>
            m_activeModel = m_activeModel == OllamaModel.ModelA ? OllamaModel.ModelB : OllamaModel.ModelA;

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
            history.Add(new OllamaMessage { role = "user", content = request.content });

            var question = new OllamaQuestion
            {
                model = ActiveModelId,
                messages = history.ToArray(),
                temperature = m_temperature,
                max_tokens = m_maxTokens,
                n = m_answerSize,
                stream = false
            };
            string questionJson = RideIO.JsonSerializeNoObjRef(question);

            DateTime startTime = DateTime.Now;
            using var webRequest = new UnityWebRequest(m_endpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(questionJson));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            DateTime endTime = DateTime.Now;
            m_responseTime = (endTime - startTime).TotalMilliseconds + " ms";

            // Every exit path must invoke onComplete: callers block their conversation flow
            // (including the character's thinking behavior) until a response arrives, so a
            // silent return leaves the character waiting forever.
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"NlpSystemOllama::Request() - Failed: {webRequest.result} - {webRequest.error}");
                onComplete?.Invoke(new NlpResponse(
                    $"I'm sorry, something went wrong. I'm getting the error: '{webRequest.error}'"));
                yield break;
            }

            OllamaAnswer answer = RideIO.JsonDeserialize<OllamaAnswer>(webRequest.downloadHandler.text);
            string content = GetAnswerContent(answer);
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("NlpSystemOllama::Request() - Received an empty response.");
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

        private string GetAnswerContent(OllamaAnswer answer)
        {
            if (answer.choices == null || answer.choices.Length == 0)
                return string.Empty;

            return answer.choices[0].message.content ?? string.Empty;
        }

        private List<OllamaMessage> GetParsedHistory()
        {
            List<OllamaMessage> history = new();

            for (int i = 0; i < m_interactionHistory.Count; ++i)
            {
                if (i == 0)
                {
                    history.Add(new OllamaMessage { role = "system", content = m_interactionHistory[i].input });
                    continue;
                }

                if (m_interactionHistory[i].input != null)
                    history.Add(new OllamaMessage { role = "user", content = m_interactionHistory[i].input });

                if (m_interactionHistory[i].response != null)
                    history.Add(new OllamaMessage { role = "assistant", content = m_interactionHistory[i].response });
            }

            return history;
        }

        [Serializable]
        private struct OllamaQuestion
        {
            public string model;
            public OllamaMessage[] messages;
            public double temperature;
            public int max_tokens;
            public int n;
            public bool stream;
        }

        [Serializable]
        private struct OllamaMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private struct OllamaAnswer
        {
            public OllamaChoice[] choices;
        }

        [Serializable]
        private struct OllamaChoice
        {
            public OllamaMessage message;
        }
    }
}
