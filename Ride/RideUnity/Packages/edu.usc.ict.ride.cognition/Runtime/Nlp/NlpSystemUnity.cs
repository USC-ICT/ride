using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

namespace Ride.NLP
{
    /// <summary>
    /// Serves as the base class for natural language processing in RIDE.
    /// </summary>
    public abstract class NlpSystemUnity : RideSystemMonoBehaviour, INlpSystem
    {
        protected string m_uri;
        protected string m_authorizationKey;
        protected string m_responseTime;
        protected string m_initialPrompt = string.Empty;
        protected List<NlpInteraction> m_interactionHistory = new();
        [HideInInspector] public Stopwatch stopwatch = new();

        protected RideConfig Config { get => Globals.api.GetSystem<ConfigurationSystemUnity>().config; }
        public string ResponseTime { get => m_responseTime; }

        /// <inheritdoc/>
        public virtual void Request(NlpRequest request, Action<NlpResponse> onComplete) { }
        
        /// <inheritdoc/>
        public virtual void SetSystemPrompt(string prompt) { }

        //ToDo: Add Quit/Wait for request
    }
}
