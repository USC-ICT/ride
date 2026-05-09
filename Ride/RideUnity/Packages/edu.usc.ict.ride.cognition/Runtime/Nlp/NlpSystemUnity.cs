using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.NLP
{
    /// <summary>
    /// Provides the shared Unity-facing base implementation for <see cref="INlpSystem"/> components.
    /// Derive from this class when building an NLP provider that participates in the RIDE systems lifecycle,
    /// needs access to shared configuration, or wants to store prompt and interaction history in a consistent way.
    /// Use the <see cref="INlpSystem"/> interface when depending on NLP functionality from calling code, and use
    /// concrete implementations such as <see cref="NlpSystemChatGPT"/>, <see cref="NlpSystemAnthropic"/>,
    /// <see cref="NlpSystemAWSLex"/>, and <see cref="NlpSystemAskSage"/> when selecting a specific backend.
    /// Timing and other request instrumentation are intentionally left to callers and derived classes rather than
    /// being owned by this shared base type.
    /// </summary>
    public abstract class NlpSystemUnity : RideSystemMonoBehaviour, INlpSystem
    {
        protected string m_uri;
        protected string m_authorizationKey;
        protected string m_responseTime;
        protected string m_initialPrompt = string.Empty;
        protected List<NlpInteraction> m_interactionHistory = new();


        protected RideConfig Config => Systems.Get<ConfigurationSystemUnity>().config;
        public string ResponseTime => m_responseTime;


        /// <inheritdoc/>
        public virtual void Request(NlpRequest request, Action<NlpResponse> onComplete) { }

        /// <inheritdoc/>
        public virtual void SetSystemPrompt(string prompt) { }


        //TODO: Add Quit/Wait for request
    }
}
