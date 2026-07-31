using System;
using System.Text;
using Ride.Conversation;

namespace Ride.NLP
{
    /// <summary>
    /// Attaches retrieved knowledge (RAG) to an outgoing NLP request and keeps the provider's
    /// interaction history clean afterwards.
    ///
    /// Sits at the dispatch boundary of <see cref="NlpSystemUnity"/> - AFTER conversation-guard
    /// input screening (a deflected/blocked turn is never retrieved for) and BEFORE the
    /// provider call (so the model sees the passages for this turn). Because it lives here,
    /// every INlpSystem.Request caller gets retrieval automatically when an
    /// <see cref="IKnowledgeSystem"/> is registered with items; no app-side integration code.
    ///
    /// History hygiene (the reason this class exists rather than a one-line prepend): providers
    /// record request.content into their interaction history and resend that history every
    /// turn. Retrieved passages therefore must NOT live in the recorded content, or every past
    /// retrieval gets replayed forever (token bloat + stale-passage answers). The pipeline
    /// dispatches an augmented copy of the request, then repairs the recorded history entry
    /// back to the clean user turn once the response arrives - the same repair approach
    /// <see cref="ConversationGuardPipeline"/> uses. Providers stay unchanged.
    /// </summary>
    internal static class KnowledgeAugmentation
    {
        /// <summary>
        /// The seam the pipeline needs from an NLP system: dispatch to the provider's real
        /// implementation, and repair a recorded history input. Implemented explicitly by
        /// <see cref="NlpSystemUnity"/> so it stays off the public API.
        /// </summary>
        internal interface IHost
        {
            void DispatchToProvider(NlpRequest request, Action<NlpResponse> onComplete);
            void RepairHistoryInput(string recordedInput, string cleanInput);

            /// <summary>The most recent user+assistant turn, for retrieval-query context
            /// (empty when the conversation has no completed turns yet).</summary>
            string RecentTurnText { get; }
        }

        // Follow-up utterances are often anaphoric ("like which ones?") and carry none of
        // the topic's terms, so retrieving on the raw utterance alone surfaces unrelated
        // passages. The tail of the previous turn is passed as weighted context (see
        // KnowledgeSettings.contextQueryWeight). Context participates in scoring only;
        // the outgoing request still carries the clean utterance.
        const int RecentTurnQueryChars = 600;

        // At or below this many content tokens, an utterance is an acknowledgment, not a
        // query ("sure", "ok", "thanks") - see the low-signal branch in Run.
        // Conversational filler: words that carry a turn but never identify a topic. They are
        // deliberately NOT scoring stopwords - removing them from the index would change every
        // term weight - and are used only to decide whether an utterance names a subject at
        // all. Without this, "sure, tell me about it" counts three content terms and retrieves
        // on them, and whichever passage happens to use those words becomes the answer.
        //
        // Run through the shared tokenizer so entries land in the same normalized, stemmed form
        // as query tokens; spelling them out literally would silently miss stemmed variants.
        static readonly System.Collections.Generic.HashSet<string> s_fillerWords =
            new System.Collections.Generic.HashSet<string>(KnowledgeIndex.Tokenize(
                "about ahead all also always any anything ask bit both can could did do does doing " +
                "down else even ever every explain first get give go going gonna good great " +
                "guess hear hello help here hey hi how just keep kind know let like little lot " +
                "made make man many maybe mean might more most much need next nice now ok okay " +
                "one only other our out over please pretty really right said say see should show " +
                "sorry sound sounds start stuff sure take talk tell thank thanks then there these " +
                "thing things think those through time told too try use used very want way " +
                "well went were yeah yep yes yet"));

        // Interrogative words that mark a short utterance as a genuine query. Question
        // words are stopwords for scoring purposes, so "who is arno" counts a single
        // content token - but it is a question about that token, not an acknowledgment,
        // and must retrieve on its own text.
        static readonly string[] s_questionWords =
            { "who", "what", "when", "where", "why", "how", "which", "whose", "whom" };

        // A question mark or a leading/embedded interrogative word means "treat as a
        // query" regardless of how few content tokens remain after stopword removal.
        static bool LooksLikeQuestion(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (text.IndexOf('?') >= 0)
                return true;
            var lower = text.ToLowerInvariant();
            foreach (var word in s_questionWords)
            {
                int at = lower.IndexOf(word, StringComparison.Ordinal);
                while (at >= 0)
                {
                    bool startOk = at == 0 || !char.IsLetter(lower[at - 1]);
                    int end = at + word.Length;
                    bool endOk = end >= lower.Length || !char.IsLetter(lower[end]);
                    if (startOk && endOk)
                        return true;
                    at = lower.IndexOf(word, at + 1, StringComparison.Ordinal);
                }
            }
            return false;
        }

        // True when an utterance contains at least one term that could identify a topic, i.e.
        // a scoring term that is not conversational filler. One such term is enough - "who is
        // arno" names a subject with a single term.
        static bool NamesASubject(string text)
        {
            foreach (var term in KnowledgeIndex.Tokenize(text))
                if (!s_fillerWords.Contains(term))
                    return true;
            return false;
        }

        internal static void Run(IHost host, NlpRequest request, Action<NlpResponse> onComplete)
        {
            var knowledge = (request != null && request.augment)
                ? Systems.Get<IKnowledgeSystem>() : null;
            if (knowledge == null || !knowledge.Settings.enabled || knowledge.ItemCount == 0
                || string.IsNullOrWhiteSpace(request.content))
            {
                host.DispatchToProvider(request, onComplete);
                return;
            }

            var recent = host.RecentTurnText;
            if (!string.IsNullOrWhiteSpace(recent) && recent.Length > RecentTurnQueryChars)
                recent = recent.Substring(recent.Length - RecentTurnQueryChars);

            var settings = knowledge.Settings;

            // Acknowledgment turns ("sure", "ok", "yes please") carry no query signal -
            // retrieving on them surfaces whatever chunk happens to contain the filler
            // word. The previous turn is the topic such a turn refers to, so retrieve on
            // that instead; with no previous turn either, skip augmentation and let the
            // conversation stand on its own.
            if (!NamesASubject(request.content) && !LooksLikeQuestion(request.content))
            {
                if (string.IsNullOrWhiteSpace(recent))
                {
                    if (settings.logRetrievals)
                        UnityEngine.Debug.Log("[Knowledge] low-signal turn with no context - augmentation skipped");
                    host.DispatchToProvider(request, onComplete);
                    return;
                }
                knowledge.Retrieve(recent, null, settings.topK,
                    passages => Augment(host, knowledge, request, "prev-turn-query", recent, passages, onComplete));
                return;
            }

            knowledge.Retrieve(request.content, recent, settings.topK,
                passages => Augment(host, knowledge, request, "normal", recent, passages, onComplete));
        }

        // Shared continuation: attach the retrieved passages (if any) and dispatch.
        static void Augment(IHost host, IKnowledgeSystem knowledge, NlpRequest request, string mode,
            string recent, System.Collections.Generic.IReadOnlyList<ScoredPassage> passages,
            Action<NlpResponse> onComplete)
        {
            var settings = knowledge.Settings;
            if (settings.logRetrievals)
                LogRetrieval(request.content, mode, recent, passages);

            if (passages == null || passages.Count == 0)
            {
                host.DispatchToProvider(request, onComplete);
                return;
            }

            var context = BuildContextBlock(settings, passages);
            var augmented = new NlpRequest(context + "\n\n" + request.content)
            {
                screen  = request.screen,
                augment = false,        // already augmented; also guards against re-entry
                context = context
            };

            host.DispatchToProvider(augmented, response =>
            {
                // The provider recorded the augmented content as this turn's input (if it
                // records at all); collapse it back to the clean user turn so retrieved
                // context never re-enters LLM context on later turns.
                host.RepairHistoryInput(augmented.content, request.content);
                onComplete?.Invoke(response);
            });
        }

        static void LogRetrieval(string utterance, string mode, string context,
            System.Collections.Generic.IReadOnlyList<ScoredPassage> passages)
        {
            var sb = new StringBuilder("[Knowledge] q=\"");
            sb.Append(utterance.Length > 60 ? utterance.Substring(0, 60) + "..." : utterance);
            sb.Append("\" mode=").Append(mode);
            sb.Append(" ctx=").Append(string.IsNullOrWhiteSpace(context) ? "none" : context.Length + "ch");
            sb.Append(" -> ");
            if (passages == null || passages.Count == 0)
                sb.Append("NO MATCHES");
            else
                for (int i = 0; i < passages.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append(passages[i].title).Append(" (").Append(passages[i].score.ToString("F1")).Append(")");
                }
            UnityEngine.Debug.Log(sb.ToString());
        }

        // Passages joined best-first under settings.maxContextChars; the first passage is
        // truncated rather than dropped when it alone exceeds the cap.
        static string BuildContextBlock(KnowledgeSettings settings, System.Collections.Generic.IReadOnlyList<ScoredPassage> passages)
        {
            var sb = new StringBuilder(settings.contextPreamble);
            int budget = settings.maxContextChars > 0 ? settings.maxContextChars : int.MaxValue;

            for (int i = 0; i < passages.Count; i++)
            {
                var entry = "\n\n[" + (string.IsNullOrEmpty(passages[i].title) ? "untitled" : passages[i].title) + "]\n"
                          + passages[i].text;
                if (entry.Length > budget)
                {
                    if (i == 0)
                        sb.Append(entry, 0, budget);
                    break;
                }
                sb.Append(entry);
                budget -= entry.Length;
            }
            return sb.ToString();
        }
    }
}
