using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Ride.NLP
{
    /// <summary>
    /// Uses Ask Sage (https://www.asksage.ai/) to provide LLM NLP funcationalities. 
    /// </summary>
    public class NlpSystemAskSage : NlpSystemUnity
    {
        protected int   m_answerSize = 1;
        public double   m_temperature = 0.3;
        public int      m_max_tokens = 200;

        [HideInInspector]
        public string m_askSageSystemPrompt;
        [HideInInspector]
        public List<AskSage.AskSageAnswer> messages = new();

        /// <summary>
        /// Initializes the Ask Sage system with the endpoint URI, the corresponding authorization key, and token.
        /// </summary>
        public override void SystemInit()
        {
            // Fetch the endpoint and authorization key for Ask Sage from configuration system
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_uri = configSystem.config.askSage.endpoint;
            m_authorizationKey = configSystem.config.askSage.authorizationToken;

            // Initialize the prompt for the Ask Sage system from the scriptable object if it's not null
            base.SystemInit();
        }

        public override void SetSystemPrompt(string prompt)
        {
            if (m_interactionHistory.Count == 0) { m_interactionHistory.Add(new NlpInteraction { input = prompt }); return; }
            else { m_interactionHistory[0] = new NlpInteraction { input = prompt }; }
        }

        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
        {
            Stopwatch stopwatch = new Stopwatch();

            m_authorizationKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "x-access-tokens", $"{m_authorizationKey}" }
            };

            AskSage.AskSageQuestion quest = new()
            {
                message = request.content,
                system_prompt = m_askSageSystemPrompt,
                model = "GPT Auto",
                persona = "Ask Sage",
                dataset = "Sage",
                temperature = m_temperature
            };

            string questionJSON = RideIO.JsonSerializeNoObjRef<AskSage.AskSageQuestion>(quest);

            // Call web service
            stopwatch.Start();
            DateTime startTime = DateTime.Now;

            // TODO: move to configuration
            m_uri = "https://api.asksage.ai/server/query";

            string response = await RideIO.Post(m_uri, questionJSON, headers, "application/json");
            stopwatch.Stop();
            DateTime endTime = DateTime.Now;
            m_responseTime = stopwatch.ElapsedMilliseconds.ToString() + " ms";

            AskSage.AskSageAnswer answer = RideIO.JsonDeserializeIgnoreNullAndMissing<AskSage.AskSageAnswer>(response);
            NlpResponse qnaAnswer = new NlpResponse(/*response, */answer.message);  // Pick first answer for now

            //Update conversation history
            NlpInteraction interaction = new();
            interaction.input = request.content;
            interaction.response = answer.message;
            interaction.inputTimestamp = startTime;
            interaction.responseTimestamp = endTime;
            m_interactionHistory.Add(interaction);

            onComplete?.Invoke(qnaAnswer);
        }
    }
}
