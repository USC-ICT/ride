using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Loads an IPA dictionary from a tab-separated text asset.
    /// Expected format per line:
    /// word<TAB>/ipa/
    ///
    /// Notes:
    /// - Keys are normalized to lower-invariant.
    /// - Pronunciations may be returned with or without surrounding slashes depending on file.
    /// - Some dictionaries include multiple pronunciations; this loader can keep the first or all.
    /// </summary>
    public sealed class IpaDictionary : RideSystemMonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Tab-separated IPA dictionary file (word<TAB>/ipa/).")]
        [SerializeField] private TextAsset m_ipaTsv;

        [Header("Options")]
        [Tooltip("If true, loads automatically in SystemAwake(). Otherwise call Load() manually.")]
        [SerializeField] private bool m_loadOnAwake = true;

        [Tooltip("If true, keeps only the first pronunciation per word.")]
        [SerializeField] private bool m_keepFirstPronunciationOnly = true;

        [Tooltip("If true, strips leading/trailing / from IPA values (e.g., /ab/ -> ab).")]
        [SerializeField] private bool m_stripSlashes = true;

        [Tooltip("If true, allows lookups to strip common punctuation from the word token.")]
        [SerializeField] private bool m_stripEdgePunctuation = true;

        private Dictionary<string, string> m_firstByWord;
        private Dictionary<string, List<string>> m_allByWord;

        public bool IsLoaded { get; private set; }

        public int EntryCount
        {
            get
            {
                if (!IsLoaded)
                    return 0;

                if (m_keepFirstPronunciationOnly)
                    return m_firstByWord != null ? m_firstByWord.Count : 0;

                return m_allByWord != null ? m_allByWord.Count : 0;
            }
        }


        public override void SystemAwake()
        {
            base.SystemAwake();

            if (m_loadOnAwake)
                Load();
        }

        /// <summary>
        /// Loads (or reloads) the dictionary from the configured TextAsset.
        /// Safe to call multiple times.
        /// </summary>
        public void Load()
        {
            ClearInternal();

            if (m_ipaTsv == null || string.IsNullOrEmpty(m_ipaTsv.text))
                return;

            if (m_keepFirstPronunciationOnly)
                m_firstByWord = new Dictionary<string, string>(StringComparer.Ordinal);
            else
                m_allByWord = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            ParseTsv(m_ipaTsv.text);

            IsLoaded = true;
        }

        /// <summary>
        /// Attempts to find an IPA pronunciation for the given word.
        /// Returns true on success.
        /// </summary>
        public bool TryGetIpa(string word, out string ipa)
        {
            ipa = null;

            if (!IsLoaded || string.IsNullOrEmpty(word))
                return false;

            string key = NormalizeKey(word, m_stripEdgePunctuation);
            if (string.IsNullOrEmpty(key))
                return false;

            if (m_keepFirstPronunciationOnly)
                return m_firstByWord != null && m_firstByWord.TryGetValue(key, out ipa);

            if (m_allByWord != null && m_allByWord.TryGetValue(key, out List<string> list) && list != null && list.Count > 0)
            {
                ipa = list[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all pronunciations for a word if the loader is configured to keep them.
        /// Returns an empty list if none exist or if the loader is in first-only mode.
        /// The returned list reference should be treated as read-only.
        /// </summary>
        public IReadOnlyList<string> GetAllIpa(string word)
        {
            if (!IsLoaded || m_keepFirstPronunciationOnly || string.IsNullOrEmpty(word))
                return Array.Empty<string>();

            string key = NormalizeKey(word, m_stripEdgePunctuation);
            if (string.IsNullOrEmpty(key))
                return Array.Empty<string>();

            if (m_allByWord != null && m_allByWord.TryGetValue(key, out List<string> list) && list != null)
                return list;

            return Array.Empty<string>();
        }

        public IEnumerable<KeyValuePair<string, string>> EnumerateEntries()
        {
            if (!IsLoaded)
                yield break;

            if (m_keepFirstPronunciationOnly)
            {
                if (m_firstByWord == null)
                    yield break;

                foreach (var kvp in m_firstByWord)
                    yield return kvp;

                yield break;
            }

            if (m_allByWord == null)
                yield break;

            foreach (var kvp in m_allByWord)
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                foreach (var ipa in kvp.Value)
                {
                    if (string.IsNullOrEmpty(ipa))
                        continue;

                    yield return new(kvp.Key, ipa);
                }
            }
        }

        private void ClearInternal()
        {
            m_firstByWord?.Clear();
            m_allByWord?.Clear();
            m_firstByWord = null;
            m_allByWord = null;
            IsLoaded = false;
        }

        private void ParseTsv(string text)
        {
            // Split by lines without allocating too aggressively.
            int len = text.Length;
            int lineStart = 0;

            while (lineStart < len)
            {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0)
                    lineEnd = len;

                int lineLength = lineEnd - lineStart;
                if (lineLength > 0)
                {
                    // Trim trailing \r.
                    int trimmedEnd = lineEnd;
                    if (text[trimmedEnd - 1] == '\r')
                        trimmedEnd--;

                    ParseLine(text, lineStart, trimmedEnd);
                }

                lineStart = lineEnd + 1;
            }
        }

        /// <summary>
        /// Parses a single dictionary line and stores the normalized word and pronunciation.
        /// </summary>
        /// <param name="text">
        /// Full source text that contains the line being parsed.
        /// </param>
        /// <param name="start">
        /// Zero-based inclusive start index of the line within <paramref name="text" />.
        /// </param>
        /// <param name="endExclusive">
        /// Zero-based exclusive end index of the line within <paramref name="text" />.
        /// </param>
        /// <remarks>
        /// Expected input format is:
        /// <code>
        /// word[TAB]ipa
        /// </code>
        /// Some IPA dictionary sources place alternate pronunciations on the same line,
        /// separated by commas, for example:
        /// <code>
        /// abdominal    /æbˈdɑmənəɫ/, /əbˈdɑmənəɫ/
        /// </code>
        /// This loader currently keeps only the first pronunciation from a comma-separated
        /// same-line pronunciation list. This is intentional so the stored value remains a
        /// single normalized IPA string and does not include punctuation from alternate
        /// pronunciations.
        /// </remarks>
        private void ParseLine(string text, int start, int endExclusive)
        {
            // Ignore comments or empty.
            if (start >= endExclusive)
                return;

            // Find tab.
            int tabIndex = text.IndexOf('\t', start);
            if (tabIndex < 0 || tabIndex >= endExclusive)
                return;

            // Extract word and ipa.
            string rawWord = text.Substring(start, tabIndex - start);
            if (string.IsNullOrEmpty(rawWord))
                return;

            int ipaStart = tabIndex + 1;
            if (ipaStart >= endExclusive)
                return;

            string rawIpa = text.Substring(ipaStart, endExclusive - ipaStart);
            if (string.IsNullOrEmpty(rawIpa))
                return;

            string key = NormalizeKey(rawWord, stripEdgePunctuation: false);
            if (string.IsNullOrEmpty(key))
                return;

            // Some IPA dictionary files store alternate pronunciations on the same line,
            // separated by commas, e.g. /foo/, /bar/.
            // For now we intentionally keep only the first pronunciation from the line.
            int alternateSeparatorIndex = rawIpa.IndexOf(',');
            if (alternateSeparatorIndex >= 0)
                rawIpa = rawIpa.Substring(0, alternateSeparatorIndex);

            string ipa = NormalizeIpa(rawIpa);
            if (string.IsNullOrEmpty(ipa))
                return;

            if (m_keepFirstPronunciationOnly)
            {
                // Keep first seen.
                if (!m_firstByWord.ContainsKey(key))
                    m_firstByWord.Add(key, ipa);
                return;
            }

            if (!m_allByWord.TryGetValue(key, out List<string> list) || list == null)
            {
                list = new List<string>(1);
                m_allByWord[key] = list;
            }

            // Some dictionaries may repeat entries; avoid duplicates.
            if (!list.Contains(ipa))
                list.Add(ipa);
        }

        private string NormalizeIpa(string rawIpa)
        {
            string ipa = rawIpa.Trim();

            if (m_stripSlashes)
            {
                if (ipa.Length >= 2 && ipa[0] == '/' && ipa[ipa.Length - 1] == '/')
                    ipa = ipa.Substring(1, ipa.Length - 2);
            }

            return ipa;
        }

        private static string NormalizeKey(string raw, bool stripEdgePunctuation)
        {
            // Lower invariant. Using ToLowerInvariant to avoid culture issues.
            string s = raw.Trim();
            if (s.Length == 0)
                return string.Empty;

            if (stripEdgePunctuation)
            {
                s = StripPunctuationEdges(s);
                if (s.Length == 0)
                    return string.Empty;
            }

            return s.ToLowerInvariant();
        }

        private static string StripPunctuationEdges(string s)
        {
            int start = 0;
            int end = s.Length - 1;

            while (start <= end && IsEdgePunctuation(s[start]))
                start++;

            while (end >= start && IsEdgePunctuation(s[end]))
                end--;

            if (start == 0 && end == s.Length - 1)
                return s;

            if (end < start)
                return string.Empty;

            return s.Substring(start, (end - start) + 1);
        }

        private static bool IsEdgePunctuation(char c)
        {
            // Edge punctuation we commonly see adjacent to word tokens.
            // This is intentionally conservative.
            switch (c)
            {
                case '.':
                case ',':
                case ';':
                case ':':
                case '!':
                case '?':
                case '"':
                case '\'':
                case '’':
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '<':
                case '>':
                case '/':
                case '\\':
                case '-':
                case '_':
                    return true;
                default:
                    return char.IsWhiteSpace(c);
            }
        }
    }
}
