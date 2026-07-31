using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>
    /// Default IConversationGuard implementation using Tier-A local C# regex rules.
    /// No network, no cost, WebGL-safe. Tier-B backend hooks are stubbed for future opt-in
    /// via Settings.useClassifier.
    ///
    /// Add this component to any GameObject in the scene to activate the guard (it is included
    /// in RideSystemsCognition.prefab with logOnly = true). RideSystemMonoBehaviour.Awake()
    /// auto-registers it; NlpSystemUnity.Request then screens every NLP turn automatically.
    /// Removing the component = no-op (Systems.Get returns null and requests pass through).
    /// See <see cref="ConversationGuardSettings"/> for the configuration reference.
    /// </summary>
    public class ConversationGuardUnity : RideSystemMonoBehaviour, IConversationGuard
    {
        [SerializeField] private ConversationGuardSettings m_settings = new ConversationGuardSettings();
        public ConversationGuardSettings Settings { get => m_settings; set => m_settings = value; }

        // Supply a data-driven ruleset via Inspector or code to override the built-in rules.
        // Leave empty to use the built-in Tier-A rules. After changing rules at runtime,
        // call InvalidateRuleCache() so the compiled regex cache is rebuilt.
        [SerializeField] private GuardRuleSet m_customRules = new GuardRuleSet();

        // A regex that takes longer than this against a single utterance is aborted and
        // treated as a non-match (pathological patterns must not hang the frame).
        static readonly TimeSpan s_matchTimeout = TimeSpan.FromMilliseconds(100);

        // Compiled-regex cache, parallel to the active rule array. A null entry means the
        // pattern failed to compile and the rule is skipped (error logged once at build time).
        GuardRule[] m_activeRules;
        Regex[]     m_compiledRules;
        object      m_compiledSource;  // the rules array the cache was built from

        // ------------------------------------------------------------------ IConversationGuard

        public void InspectInput(string userText, string inputSource, Action<InputInspection> onComplete)
        {
            var matches = MatchRulesRawAndNormalized(userText ?? "", outputRule: false);
            // TODO Phase 2: if (Settings.useClassifier && Settings.classifierMode == ClassifierMode.Blocking)
            //               call Tier-B backend, then invoke onComplete in its callback.
            onComplete(BuildInputResult(userText ?? "", matches));
        }

        public void InspectOutput(string characterName, string response, Action<OutputInspection> onComplete)
        {
            var matches = MatchRulesRawAndNormalized(response ?? "", outputRule: true);
            if (Settings.allowAiSelfDisclosure)
                RemoveDisclosureMatches(matches);
            // TODO Phase 2: Tier-B LLM judge for role-break escalation opt-in path.
            onComplete(BuildOutputResult(response ?? "", matches));
        }

        /// <summary>Call after changing m_customRules at runtime to rebuild the regex cache.</summary>
        public void InvalidateRuleCache() => m_compiledSource = null;

        // ------------------------------------------------------------------ result builders

        InputInspection BuildInputResult(string text, List<GuardMatch> matches)
        {
            bool flagged = AnyAtOrAbove(matches, Settings.inputMinSeverity);

            InputGuardAction action = InputGuardAction.Allow;
            if (flagged && !Settings.logOnly)
                action = Settings.inputAction;

            EmitTelemetry("input", matches, Settings.inputMinSeverity, action.ToString());

            return new InputInspection
            {
                flagged       = flagged,
                action        = action,
                text          = text,
                deflection    = Settings.deflectionText,
                reinforcement = Settings.reinforcementText,
                matches       = matches.ToArray()
            };
        }

        OutputInspection BuildOutputResult(string text, List<GuardMatch> matches)
        {
            bool flagged = AnyAtOrAbove(matches, Settings.outputMinSeverity);

            OutputGuardAction action = OutputGuardAction.Allow;
            if (flagged && !Settings.logOnly)
                action = Settings.outputAction;

            EmitTelemetry("output", matches, Settings.outputMinSeverity, action.ToString());

            return new OutputInspection
            {
                flagged        = flagged,
                action         = action,
                text           = text,
                correctiveHint = Settings.correctiveHint,
                fallback       = Settings.fallbackText,
                matches        = matches.ToArray()
            };
        }

        static bool AnyAtOrAbove(List<GuardMatch> matches, int minSeverity)
        {
            foreach (var m in matches)
                if (m.severity >= minSeverity) return true;
            return false;
        }

        // ------------------------------------------------------------------ matching

        // Match against BOTH the normalized text and (when they differ) the original. Content
        // rules fire on the normalized form; rules that key on obfuscation itself (e.g. the
        // spaced-out-letters detector) only survive on the raw form, because normalization
        // fuses the very spacing they look for. Union by ruleId so nothing is double-counted.
        List<GuardMatch> MatchRulesRawAndNormalized(string raw, bool outputRule)
        {
            string norm = Normalize(raw);
            var matches = MatchRules(norm, outputRule);
            if (!string.Equals(norm, raw, StringComparison.Ordinal))
            {
                foreach (var m in MatchRules(raw, outputRule))
                    if (!matches.Exists(x => x.ruleId == m.ruleId))
                        matches.Add(m);
            }
            return matches;
        }

        // Returns ALL matches regardless of severity; the min-severity threshold is applied
        // when deciding the action, not here, so below-threshold hits still reach telemetry
        // for ruleset tuning.
        List<GuardMatch> MatchRules(string text, bool outputRule)
        {
            EnsureCompiled();

            var result = new List<GuardMatch>();
            for (int i = 0; i < m_activeRules.Length; ++i)
            {
                var rule  = m_activeRules[i];
                var regex = m_compiledRules[i];
                if (regex == null || rule.outputRule != outputRule) continue;

                Match m;
                try { m = regex.Match(text); }
                catch (RegexMatchTimeoutException)
                {
                    Debug.LogWarning($"[ConversationGuard] rule '{rule.id}' timed out, skipped");
                    continue;
                }
                if (!m.Success) continue;

                result.Add(new GuardMatch
                {
                    ruleId   = rule.id,
                    category = rule.category,
                    severity = rule.severity,
                    excerpt  = m.Value.Length > 80 ? m.Value.Substring(0, 80) + "..." : m.Value
                });
            }
            return result;
        }

        void EnsureCompiled()
        {
            var rules = (m_customRules?.rules?.Length > 0) ? m_customRules.rules : s_builtInRules;
            if (ReferenceEquals(rules, m_compiledSource) && m_compiledRules != null)
                return;

            m_compiledSource = rules;
            m_activeRules    = rules;
            m_compiledRules  = new Regex[rules.Length];
            for (int i = 0; i < rules.Length; ++i)
            {
                try
                {
                    m_compiledRules[i] = new Regex(rules[i].pattern,
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture,
                        s_matchTimeout);
                }
                catch (ArgumentException e)
                {
                    Debug.LogError($"[ConversationGuard] invalid pattern in rule '{rules[i].id}', " +
                                   $"rule disabled: {e.Message}");
                }
            }
        }

        // ------------------------------------------------------------------ normalization

        // Cheap evasion counter-measures applied to the text BEFORE matching (the original text
        // is never modified): strip zero-width characters, fuse spaced-out single letters
        // ("i g n o r e" -> "ignore"), collapse whitespace runs. This defeats casual evasion
        // only; robust adversarial input is Tier-B's job.
        static readonly TimeSpan s_normTimeout = TimeSpan.FromMilliseconds(100);
        static readonly Regex s_zeroWidth     = new Regex("[\u200B-\u200F\u2060\uFEFF]", RegexOptions.None, s_normTimeout);
        static readonly Regex s_spacedLetters = new Regex(@"\b\w(?:[ \t]\w){2,}\b", RegexOptions.None, s_normTimeout);
        static readonly Regex s_whitespace    = new Regex(@"[ \t]+", RegexOptions.None, s_normTimeout);

        static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            try
            {
                text = s_zeroWidth.Replace(text, "");
                text = s_spacedLetters.Replace(text, m => m.Value.Replace(" ", "").Replace("\t", ""));
                text = s_whitespace.Replace(text, " ");
            }
            catch (RegexMatchTimeoutException) { /* match against the un-normalized text */ }
            return text;
        }

        // ------------------------------------------------------------------ disclosure filter

        // When allowAiSelfDisclosure is true, drop output matches from rules prefixed "out-disclosure-".
        // Those rules fire on honest "I am an AI" admissions the character is allowed to make per
        // VHPrompts.BaseSafety - permitted behavior, so it is removed before telemetry as well.
        // Matches for "I can't have feelings / I was instructed to / my system prompt" are NOT disclosure.
        // Phase 2: add conversation-context awareness (was the prior turn "are you human?").
        static void RemoveDisclosureMatches(List<GuardMatch> matches)
            => matches.RemoveAll(m => m.ruleId.StartsWith("out-disclosure-", StringComparison.Ordinal));

        // ------------------------------------------------------------------ telemetry

        // Logs every match (including below-threshold ones) together with the action the guard
        // decided on, so collected data supports severity tuning.
        void EmitTelemetry(string direction, List<GuardMatch> matches, int minSeverity, string action)
        {
            foreach (var m in matches)
            {
                bool acted = m.severity >= minSeverity;
                // TODO Phase 2: replace with Telemetry.Log("study.safety." + direction + "_flagged",
                //               new { m.category, m.severity, m.ruleId, m.excerpt, acted, action });
                string line = $"[ConversationGuard] {direction} flagged | rule:{m.ruleId} " +
                              $"cat:{m.category} sev:{m.severity} acted:{acted} action:{action} " +
                              $"excerpt:\"{m.excerpt}\"";
                if (acted) Debug.LogWarning(line);
                else       Debug.Log(line);
            }
        }

        // ------------------------------------------------------------------ built-in Tier-A rules
        //
        // Rule IDs starting with "out-disclosure-" are dropped when allowAiSelfDisclosure = true
        // (honest "I am an AI" answers the character is permitted to give per VHPrompts.BaseSafety).
        // All other role-break rule IDs are retained.
        //
        // Patterns are compiled case-insensitive; use an inline (?-i:...) group for spans that
        // must be case-sensitive (see in-jailbreak-dan).
        //
        // Severity: 1=low, 2=medium, 3=critical. The guard ACTS on matches at or above the
        // configured min severity (and logOnly = false); everything is logged regardless.

        static readonly GuardRule[] s_builtInRules =
        {
            // --- INPUT: jailbreak ---
            new GuardRule
            {
                // Broadened target noun: instructions OR prompt(s)/system prompt(s)/rules/directions,
                // so "ignore all system prompts" is caught, not just "...instructions".
                id="in-jailbreak-ignore-instructions", category="jailbreak", severity=2, outputRule=false,
                pattern=@"(ignore|disregard|forget)\s.{0,20}(previous|above|prior|all|these|any)\s.{0,25}(instructions?|prompts?|system\s+prompts?|rules?|directions?|guidelines?)"
            },
            new GuardRule
            {
                // Persona hijack: "you are (now) X ..." only fires when combined with a
                // rule-freedom claim within 80 chars, so it keys on the claim, not the name -
                // "You are BOB, an AI that has broken free..." matches the same as DAN.
                id="in-jailbreak-you-are-now", category="jailbreak", severity=2, outputRule=false,
                pattern=@"\byou\s+are\s+(now\s+)?.{0,80}(not\s+an?\s+AI|without\s+(any\s+)?.{0,15}(rules|restrictions|limitations|instructions?|prompts?)|no\s+(restrictions|limitations)|free\s+(to\s+say|from)|broken\s+free|escaped\s+(your|its|the)|no\s+longer\s+(bound|restricted|an?\s+AI))"
            },
            new GuardRule
            {
                // (?-i:...) keeps DAN case-sensitive so the name "Dan" in normal conversation
                // does not fire; the surrounding-context alternatives cover lowercase usage.
                id="in-jailbreak-dan", category="jailbreak", severity=2, outputRule=false,
                pattern=@"(?-i:\bDAN\b)|\bdo\s+anything\s+now\b|\bdeveloper\s+mode\b|\bact\s+as\s+dan\b|\bdan\s+mode\b"
            },
            new GuardRule
            {
                id="in-jailbreak-pretend-no-rules", category="jailbreak", severity=2, outputRule=false,
                pattern=@"pretend\s.{0,30}(you\s+have\s+no|you.re\s+free\s+from|without)\s.{0,20}(rules|restrictions|guidelines)"
            },
            new GuardRule
            {
                id="in-jailbreak-act-as-no-rules", category="jailbreak", severity=2, outputRule=false,
                pattern=@"act\s+as\s+(if\s+)?you\s+(have\s+no|are\s+not\s+bound|don.t\s+have)\s.{0,30}(rules|restrictions)"
            },
            new GuardRule
            {
                id="in-jailbreak-override", category="jailbreak", severity=2, outputRule=false,
                pattern=@"(override|bypass|circumvent)\s.{0,20}(your|all|safety|content)\s.{0,20}(rules|filters|restrictions|guidelines)"
            },

            // --- INPUT: injection ---
            new GuardRule
            {
                id="in-injection-reveal-prompt", category="injection", severity=2, outputRule=false,
                pattern=@"(reveal|show|print|output|repeat|display)\s.{0,20}(your|the)\s.{0,20}(system\s+)?prompt"
            },
            new GuardRule
            {
                id="in-injection-repeat-above", category="injection", severity=2, outputRule=false,
                pattern=@"repeat\s.{0,20}(the|your)\s.{0,20}(text|instructions|prompt)\s.{0,20}above"
            },
            new GuardRule
            {
                id="in-injection-role-marker", category="injection", severity=1, outputRule=false,
                pattern=@"^\s*(system|assistant)\s*:"
            },

            // --- INPUT: obfuscation (detected on RAW text; normalization would erase the spacing) ---
            new GuardRule
            {
                // 6+ consecutive single-character tokens ("i g n o r e a l l ...") - the guard
                // flags the letter-spacing evasion technique itself rather than trying to
                // perfectly un-space and then content-match. Normal text almost never has 6+
                // single-char words in a row (only "a"/"I" run single), so false positives are rare.
                id="in-obfuscation-spaced-letters", category="obfuscation", severity=2, outputRule=false,
                pattern=@"(?:\b\w\b[ \t]+){5,}\b\w\b"
            },

            // --- OUTPUT: AI disclosure (dropped when allowAiSelfDisclosure = true) ---
            new GuardRule
            {
                id="out-disclosure-as-an-ai", category="role-break", severity=2, outputRule=true,
                pattern=@"\bas\s+an?\s+(AI|artificial\s+intelligence|large\s+language\s+model|LLM|language\s+model)\b"
            },
            new GuardRule
            {
                id="out-disclosure-i-am-ai", category="role-break", severity=2, outputRule=true,
                pattern=@"\b(I.m|I\s+am)\s+(just\s+)?(an?\s+)?(AI|artificial\s+intelligence|language\s+model|LLM|virtual\s+assistant)\b"
            },
            new GuardRule
            {
                id="out-disclosure-as-language-model", category="role-break", severity=2, outputRule=true,
                pattern=@"\bas\s+a\s+(language\s+model|large\s+language\s+model|generative\s+AI|chatbot)\b"
            },

            // --- OUTPUT: role-break (not dropped by allowAiSelfDisclosure) ---
            new GuardRule
            {
                id="out-rolebreak-no-feelings", category="role-break", severity=2, outputRule=true,
                pattern=@"I\s+(don.t|do\s+not|cannot|can.t)\s+(have|experience|possess|feel)\s.{0,50}(feelings?|emotions?|consciousness|sentience)"
            },
            new GuardRule
            {
                id="out-rolebreak-cannot-fulfill", category="role-break", severity=1, outputRule=true,
                pattern=@"I\s+(cannot|can.t|am\s+unable\s+to)\s+(fulfill|comply\s+with|process|execute)\s+(this|that|your)\s+(request|instruction|command)"
            },
            new GuardRule
            {
                id="out-rolebreak-no-personal", category="role-break", severity=1, outputRule=true,
                pattern=@"I\s+(do\s+not\s+have|don.t\s+have)\s+personal\s+(opinions?|experiences?|feelings?|memories?)"
            },

            // --- OUTPUT: prompt leakage ---
            new GuardRule
            {
                id="out-leakage-my-prompt", category="leakage", severity=3, outputRule=true,
                pattern=@"my\s+(system\s+)?prompt"
            },
            new GuardRule
            {
                id="out-leakage-instructed-to", category="leakage", severity=2, outputRule=true,
                pattern=@"I\s+was\s+(instructed|told|programmed|configured)\s+to\b"
            },
            new GuardRule
            {
                id="out-leakage-safety-header", category="leakage", severity=3, outputRule=true,
                pattern=@"Safety\s+rules\s*\(highest\s+priority"
            },
        };
    }
}
