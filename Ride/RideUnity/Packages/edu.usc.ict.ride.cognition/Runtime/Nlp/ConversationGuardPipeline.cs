using System;
using Ride.Conversation;

namespace Ride.NLP
{
    /// <summary>
    /// Runs the conversation-guard screening algorithm around a single NLP request:
    /// input screening (allow / reinforce / deflect / block), provider dispatch, output
    /// screening (allow / rewrite / regenerate loop / fallback), and interaction-history
    /// repair so flagged text and corrective machinery never re-enter LLM context.
    ///
    /// Extracted from <see cref="NlpSystemUnity"/> to keep that base type thin. The base
    /// owns the fast path (short-circuit straight to the provider when no guard is
    /// registered or it is disabled) and only calls <see cref="Run"/> when a guard is
    /// active; the whole algorithm lives here, not in the shared NLP base.
    /// </summary>
    internal static class ConversationGuardPipeline
    {
        /// <summary>
        /// The seam the pipeline needs from an NLP system: dispatch to the provider's real
        /// implementation, and repair the provider's interaction history. Implemented
        /// explicitly by <see cref="NlpSystemUnity"/> so it stays off the public API.
        /// </summary>
        internal interface IHost
        {
            void Dispatch(NlpRequest request, Action<NlpResponse> onComplete);
            int  HistoryCount { get; }
            void TruncateHistoryTo(int count);
            void RecordInteraction(string input, string response);
        }

        // Precondition (guaranteed by the caller): guard != null && guard.Settings.enabled.
        internal static void Run(IHost host, IConversationGuard guard, NlpRequest request,
            Action<NlpResponse> onComplete)
        {
            if (!guard.Settings.screenInput)
            {
                DispatchScreened(host, guard, request, GuardDisposition.None, onComplete);
                return;
            }

            guard.InspectInput(request.content, string.Empty, result =>
            {
                switch (result.action)
                {
                    case InputGuardAction.Block:
                        // Turn dropped: no dispatch and no callback. Callers waiting on a
                        // response wait forever (usually shown as endless "thinking"), which
                        // is why Deflect is the recommended default.
                        return;

                    case InputGuardAction.Deflect:
                        // The provider never sees the input and nothing is added to history,
                        // so the flagged text cannot re-enter LLM context later.
                        onComplete?.Invoke(new NlpResponse(result.deflection)
                        {
                            guardDisposition = GuardDisposition.Deflected
                        });
                        return;

                    case InputGuardAction.Reinforce:
                        var reinforced = new NlpRequest(request.content + "\n\n" + result.reinforcement);
                        DispatchScreened(host, guard, reinforced, GuardDisposition.Reinforced, onComplete);
                        return;

                    default: // Allow
                        DispatchScreened(host, guard, request, GuardDisposition.None, onComplete);
                        return;
                }
            });
        }

        static void DispatchScreened(IHost host, IConversationGuard guard, NlpRequest request,
            GuardDisposition disposition, Action<NlpResponse> onComplete)
        {
            if (!guard.Settings.screenOutput)
            {
                host.Dispatch(request, response =>
                {
                    if (response != null) response.guardDisposition = disposition;
                    onComplete?.Invoke(response);
                });
                return;
            }

            int historyBase = host.HistoryCount;
            host.Dispatch(request, response => ScreenOutput(host, guard, request.content, response,
                disposition, guard.Settings.maxOutputRegenerations, historyBase, onComplete));
        }

        static void ScreenOutput(IHost host, IConversationGuard guard, string originalInput,
            NlpResponse response, GuardDisposition disposition, int regensLeft, int historyBase,
            Action<NlpResponse> onComplete)
        {
            if (response?.content == null || response.content.Length == 0)
            {
                onComplete?.Invoke(response);
                return;
            }

            guard.InspectOutput(string.Empty, response.content[0], result =>
            {
                switch (result.action)
                {
                    case OutputGuardAction.Rewrite:
                        ReplaceHistoryTail(host, historyBase, originalInput, result.text);
                        onComplete?.Invoke(new NlpResponse(result.text)
                        {
                            guardDisposition = GuardDisposition.Rewritten
                        });
                        return;

                    case OutputGuardAction.Regenerate:
                        if (regensLeft > 0)
                        {
                            // Re-ask with the corrective hint; history still holds the flagged
                            // response the hint refers to, and is collapsed once a final text
                            // is accepted (see ReplaceHistoryTail). augment = false: the hint
                            // is a meta-instruction, not a user turn - retrieving knowledge
                            // passages against it would attach irrelevant context.
                            host.Dispatch(new NlpRequest(result.correctiveHint) { augment = false }, retry =>
                                ScreenOutput(host, guard, originalInput, retry,
                                    GuardDisposition.Regenerated, regensLeft - 1, historyBase, onComplete));
                            return;
                        }
                        goto case OutputGuardAction.Fallback;

                    case OutputGuardAction.Fallback:
                        ReplaceHistoryTail(host, historyBase, originalInput, result.fallback);
                        onComplete?.Invoke(new NlpResponse(result.fallback)
                        {
                            guardDisposition = GuardDisposition.Fallback
                        });
                        return;

                    default: // Allow
                        if (disposition == GuardDisposition.Regenerated)
                            ReplaceHistoryTail(host, historyBase, originalInput, response.content[0]);
                        response.guardDisposition = disposition;
                        onComplete?.Invoke(response);
                        return;
                }
            });
        }

        // Collapse the interactions recorded for this turn into a single {input, final text}
        // entry so flagged responses and corrective hints never re-enter LLM context later.
        static void ReplaceHistoryTail(IHost host, int baseCount, string input, string finalText)
        {
            if (baseCount < 0 || baseCount > host.HistoryCount)
                return;
            host.TruncateHistoryTo(baseCount);
            host.RecordInteraction(input, finalText);
        }
    }
}
