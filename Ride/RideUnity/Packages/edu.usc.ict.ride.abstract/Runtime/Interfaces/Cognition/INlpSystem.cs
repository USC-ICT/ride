using System;
using Ride.Conversation;

namespace Ride.NLP
{
    /// <summary>
    /// Base class for holing content to be send to NLP service.
    /// </summary>
    public class NlpRequest : ServiceRequest
    {
        public string content;

        // When false, the request bypasses the conversation guard (see IConversationGuard).
        // Set false for internal/meta LLM calls (analysis prompts, summarization, etc.) that
        // are not user conversation turns. Defaults to true: user-facing turns are screened.
        public bool screen = true;

        // Transient reference material attached by the knowledge pipeline for THIS turn only
        // (see IKnowledgeSystem). It is composed into the outgoing payload at dispatch, and
        // the provider's interaction history is repaired to hold the clean user turn - so
        // retrieved context is never replayed into LLM context on later turns and never
        // compounds token cost. Set by the NLP layer, not by callers.
        public string context;

        // When false, the knowledge pipeline skips retrieval for this request. Set false for
        // internal/meta LLM calls (corrective hints, summarization, etc.) that are not user
        // conversation turns. Defaults to true: user-facing turns are augmented when a
        // knowledge system is registered and has items.
        public bool augment = true;

        public NlpRequest(string request)
        {
            this.content = request;
        }
    }

    /// <summary>
    /// Base class for storing NLP service response.
    /// </summary>
    public class NlpResponse : SystemResponse
    {
        public string[] content;

        // What the conversation guard did to this turn (GuardDisposition.None when the guard
        // is absent/disabled or took no action). Callers that keep their own transcript and
        // replay it into LLM context should skip recording Deflected turns, so flagged input
        // never re-enters the context. See IConversationGuard.
        public GuardDisposition guardDisposition = GuardDisposition.None;

        public NlpResponse (string response)
        {
            content = new string[1];
            this.content[0] = response;
        }
        public NlpResponse(string[] response)
        {
            this.content = response;
        }
    }

    /// <summary>
    /// Struct intended storing the history of interaction between a user and a NLP service.
    /// </summary>
    public struct NlpInteraction
    {
        public string input;
        public string response;
        public DateTime inputTimestamp;
        public DateTime responseTimestamp;
    }

    /// <summary>
    /// Base interface for natural language processing system interfaces.
    /// </summary>
    public interface INlpSystem : IRideSystem
    {
        /// <summary>
        /// Requests NLP response based on provided input.
        /// </summary>
        /// <param name="uri">URL to send request to</param>
        /// <param name="content">Content input</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        void Request(NlpRequest request, Action<NlpResponse> onComplete);

        /// <summary>
        /// Set system prompt
        /// </summary>
        /// <param name="prompt"></param>
        void SetSystemPrompt(string prompt);
    }
}
