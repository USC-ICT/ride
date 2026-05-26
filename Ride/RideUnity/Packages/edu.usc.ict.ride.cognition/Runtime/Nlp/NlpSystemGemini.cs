using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>
    /// Uses Google Gemini (https://ai.google.dev) to provide LLM functionalities.
    /// </summary>
    public class NlpSystemGemini : NlpSystemUnity
    {
        /// <summary>
        /// Requests a response from Gemini based on the provided user input.
        /// </summary>
        /// <param name="request">The user input to send to the model.</param>
        /// <param name="onComplete">Callback invoked with the model's response on success.</param>
        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
        {
            var config = Systems.Get<ConfigurationSystemUnity>().config.gemini;
            string url = $"{config.endpoint}/{config.model}:generateContent?key={config.endpointKey}";

            var contents = GetParsedHistory();
            contents.Add(new GeminiContent("user", request.content));

            string data = RideIO.JsonSerialize(new GeminiRequest
            {
                systemInstruction = string.IsNullOrEmpty(m_initialPrompt) ? null : new GeminiSystemInstruction(m_initialPrompt),
                contents = contents.ToArray(),
            },
            RideIO.GetJsonConfigNoNameHandling());

            using var webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

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
                Debug.LogWarning($"NlpSystemGemini::Request() - Failed: {webRequest.result}");
                return;
            }

            var res = RideIO.JsonDeserialize<GeminiResponse>(webRequest.downloadHandler.text);
            string responseText = res.candidates[0].content.parts[0].text;

            NlpInteraction interaction = new NlpInteraction
            {
                input = request.content,
                response = responseText,
                inputTimestamp = startTime,
                responseTimestamp = endTime,
            };
            m_interactionHistory.Add(interaction);

            onComplete?.Invoke(new NlpResponse(responseText));
        }

        /// <summary>
        /// Sets the system prompt used to guide Gemini's responses.
        /// </summary>
        /// <param name="prompt">The system instruction text.</param>
        public override void SetSystemPrompt(string prompt)
        {
            m_initialPrompt = prompt;
        }

        /// <summary>
        /// Converts stored interaction history into Gemini-format content objects.
        /// </summary>
        /// <returns>A list of <see cref="GeminiContent"/> representing prior turns.</returns>
        private List<GeminiContent> GetParsedHistory()
        {
            var history = new List<GeminiContent>();
            foreach (var interaction in m_interactionHistory)
            {
                if (interaction.input != null)
                    history.Add(new GeminiContent("user", interaction.input));
                if (interaction.response != null)
                    history.Add(new GeminiContent("model", interaction.response));
            }
            return history;
        }

        #region GeminiDataStructs
        private class GeminiRequest
        {
            public GeminiSystemInstruction systemInstruction { get; set; }
            public GeminiContent[] contents { get; set; }
        }

        private class GeminiSystemInstruction
        {
            public GeminiPart[] parts { get; set; }
            public GeminiSystemInstruction(string text)
            {
                parts = new[] { new GeminiPart(text) };
            }
        }

        private class GeminiContent
        {
            public string role { get; set; }
            public GeminiPart[] parts { get; set; }
            public GeminiContent(string role, string text)
            {
                this.role = role;
                parts = new[] { new GeminiPart(text) };
            }
        }

        private class GeminiPart
        {
            public string text { get; set; }
            public GeminiPart(string text) { this.text = text; }
        }

        private class GeminiResponse
        {
            public GeminiCandidate[] candidates { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiContent content { get; set; }
            public string finishReason { get; set; }
        }
        #endregion
    }
}