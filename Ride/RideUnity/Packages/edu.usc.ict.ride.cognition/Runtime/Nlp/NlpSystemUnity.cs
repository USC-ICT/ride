using System;
using System.Collections.Generic;
using System.Linq;
using Ride.Conversation;
using UnityEngine;

namespace Ride.NLP
{
    /// <summary>
    /// Provides the shared Unity-facing base implementation for <see cref="INlpSystem"/> components.
    /// Derive from this class when building an NLP provider that participates in the RIDE systems lifecycle,
    /// needs access to shared configuration, or wants to store prompt and interaction history in a consistent way.
    /// Use the <see cref="INlpSystem"/> interface when depending on NLP functionality from calling code, and use
    /// concrete implementations such as <see cref="NlpSystemChatGPT"/>, <see cref="NlpSystemClaude"/>,
    /// <see cref="NlpSystemAWSLex"/>, and <see cref="NlpSystemAskSage"/> when selecting a specific backend.
    /// Timing and other request instrumentation are intentionally left to callers and derived classes rather than
    /// being owned by this shared base type.
    ///
    /// Conversation-guard enforcement: <see cref="Request"/> is a thin template method. It short-circuits
    /// straight to the provider (<see cref="RequestInternal"/>) when no <see cref="IConversationGuard"/> is
    /// registered or it is disabled; otherwise it hands the turn to <see cref="ConversationGuardPipeline"/>,
    /// which owns the screening algorithm. Callers need no guard-specific code. Providers implement
    /// <see cref="RequestInternal"/> instead of overriding Request; internal/meta LLM calls that are not
    /// user conversation turns should set <see cref="NlpRequest.screen"/> = false to bypass screening.
    /// </summary>
    public abstract class NlpSystemUnity : RideSystemMonoBehaviour, INlpSystem, ConversationGuardPipeline.IHost, KnowledgeAugmentation.IHost
    {
        protected string m_uri;
        protected string m_authorizationKey;
        protected string m_responseTime;
        protected string m_initialPrompt = string.Empty;
        protected List<NlpInteraction> m_interactionHistory = new();


        protected RideConfig Config => Systems.Get<ConfigurationSystemUnity>().config;
        public string ResponseTime => m_responseTime;

        /// <summary>
        /// Whether this provider has generation settings to adjust (<see cref="Temperature"/> and
        /// <see cref="MaxTokens"/>). False for scripted/intent-based providers, whose responses are
        /// authored rather than generated; user interfaces can use this to hide controls that would
        /// have no effect.
        /// </summary>
        public virtual bool SupportsGenerationSettings => false;

        /// <summary>
        /// Sampling temperature, where the provider supports one: higher values produce more
        /// varied wording. Exposed here so UI and application code can adjust the setting
        /// without knowing which provider is active. Providers that have no temperature
        /// concept (scripted/intent-based agents) leave this as a no-op, and providers whose
        /// API accepts only its default temperature ignore the assigned value.
        /// </summary>
        public virtual float Temperature { get => 0f; set { } }

        /// <summary>
        /// Upper bound on the tokens a single response may consume, where the provider supports one.
        /// This is a cost and runaway guard rather than a way to ask for shorter answers - the
        /// system prompt controls length, and a limit low enough to truncate a response can leave
        /// it empty. Providers without the concept leave this as a no-op.
        /// </summary>
        public virtual int MaxTokens { get => 0; set { } }


        /// <summary>
        /// Whether requests to this provider go through the conversation guard at all.
        /// True for generative LLM providers. Scripted/intent-based providers (AWS Lex, Rasa,
        /// Azure QnA/LUIS/Text-Analytics, DialogFlow) override this to false: their responses are
        /// authored content, so there is nothing to jailbreak and nothing to screen.
        /// </summary>
        protected virtual bool GuardRequests => true;

        /// <inheritdoc/>
        /// <remarks>
        /// Fast path: with no guard registered (or disabled), calls <see cref="RequestInternal"/>
        /// directly. Otherwise delegates the full screen/dispatch/regenerate/history-repair
        /// algorithm to <see cref="ConversationGuardPipeline"/>. Do not override this; override
        /// <see cref="RequestInternal"/> in providers.
        /// </remarks>
        public void Request(NlpRequest request, Action<NlpResponse> onComplete)
        {
            var guard = (GuardRequests && request != null && request.screen)
                ? Systems.Get<IConversationGuard>() : null;
            if (guard == null || !guard.Settings.enabled)
            {
                DispatchAugmented(request, onComplete);
                return;
            }
            ConversationGuardPipeline.Run(this, guard, request, onComplete);
        }

        // All dispatch funnels through here so knowledge retrieval (RAG) covers both the
        // fast path and the guard pipeline's dispatch, in D3 order: input screening first
        // (a deflected turn is never retrieved for), retrieval at dispatch, output screening
        // on the response. See KnowledgeAugmentation.
        void DispatchAugmented(NlpRequest request, Action<NlpResponse> onComplete)
        {
            if (KnowledgeAugmentRequests)
                KnowledgeAugmentation.Run(this, request, onComplete);
            else
                RequestInternal(request, onComplete);
        }

        /// <summary>
        /// Whether requests to this provider get knowledge (RAG) augmentation. Follows
        /// <see cref="GuardRequests"/> by default: generative LLM providers benefit from
        /// retrieved context; scripted/intent-based providers (AWS Lex, Rasa, ...) match
        /// utterances against authored intents, and prepended passages would break matching.
        /// </summary>
        protected virtual bool KnowledgeAugmentRequests => GuardRequests;

        /// <summary>
        /// Provider-specific request implementation. Called (directly or via the guard pipeline)
        /// by <see cref="Request"/>; when the guard is active its response is screened before the
        /// caller's callback runs.
        /// </summary>
        protected virtual void RequestInternal(NlpRequest request, Action<NlpResponse> onComplete) { }

        // ---- ConversationGuardPipeline.IHost (explicit; the seam used only by the guard pipeline) ----
        // Explicit implementation keeps these off the public API - callers see only Request().

        void ConversationGuardPipeline.IHost.Dispatch(NlpRequest request, Action<NlpResponse> onComplete)
            => DispatchAugmented(request, onComplete);

        int ConversationGuardPipeline.IHost.HistoryCount => m_interactionHistory.Count;

        void ConversationGuardPipeline.IHost.TruncateHistoryTo(int count)
        {
            if (count >= 0 && m_interactionHistory.Count > count)
                m_interactionHistory.RemoveRange(count, m_interactionHistory.Count - count);
        }

        void ConversationGuardPipeline.IHost.RecordInteraction(string input, string response)
            => m_interactionHistory.Add(new NlpInteraction
            {
                input             = input,
                response          = response,
                inputTimestamp    = DateTime.Now,
                responseTimestamp = DateTime.Now
            });

        // ---- KnowledgeAugmentation.IHost (explicit; the seam used only by the knowledge pipeline) ----

        void KnowledgeAugmentation.IHost.DispatchToProvider(NlpRequest request, Action<NlpResponse> onComplete)
            => RequestInternal(request, onComplete);

        string KnowledgeAugmentation.IHost.RecentTurnText
        {
            get
            {
                // Newest complete turn, skipping index 0 (providers like ChatGPT keep the
                // system prompt there). Used only as retrieval-query context.
                //
                // The response is preferred over the input because a follow-up refers to what
                // was just said, and because applications legitimately send instruction-shaped
                // inputs (for example "introduce yourself using your profile") whose wording
                // describes the task rather than the topic. Treating those as topic context
                // sends retrieval after the wrong subject entirely. The input is used only when
                // there is no response to draw on.
                for (int i = m_interactionHistory.Count - 1; i >= 1; i--)
                {
                    var turn = m_interactionHistory[i];
                    var response = (turn.response ?? string.Empty).Trim();
                    if (response.Length > 0)
                        return response;

                    var input = (turn.input ?? string.Empty).Trim();
                    if (input.Length > 0)
                        return input;
                }
                return string.Empty;
            }
        }

        void KnowledgeAugmentation.IHost.RepairHistoryInput(string recordedInput, string cleanInput)
        {
            // Newest first: the entry to repair is the one this turn just recorded. Providers
            // that record nothing (or record differently) simply never match - a safe no-op.
            for (int i = m_interactionHistory.Count - 1; i >= 0; i--)
            {
                if (m_interactionHistory[i].input != recordedInput)
                    continue;
                var repaired = m_interactionHistory[i];
                repaired.input = cleanInput;
                m_interactionHistory[i] = repaired;
                return;
            }
        }

        /// <inheritdoc/>
        public virtual void SetSystemPrompt(string prompt) { }

        public virtual void ClearHistory() { }

        public virtual List<NlpInteraction> GetHistory()
            => m_interactionHistory.Select(interaction => interaction).ToList();

        public virtual void SetHistory(List<NlpInteraction> history)
        {
            m_interactionHistory = history != null
                ? history.Select(interaction => interaction).ToList()
                : new List<NlpInteraction>();
        }


        //TODO: Add Quit/Wait for request
    }
}
