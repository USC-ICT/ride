namespace Ride
{
    /// <summary>
    /// File sink that writes application events as <see href="https://jsonlines.org">JSON Lines</see>
    /// (one JSON object per line), suitable for ingestion by log aggregators and analytics pipelines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only entries where <see cref="LogEntry.isEvent"/> is <c>true</c> are written.
    /// Non-event entries are silently dropped regardless of the kind filter settings —
    /// free-form message strings do not produce valid JSONL and belong in a
    /// <see cref="LoggerUnityFile"/> or <see cref="LoggerUnityFileCsv"/> sink instead.
    /// </para>
    /// <para>
    /// Each line has the shape:
    /// <code>
    /// {"timestamp":"...","kind":"Application","type":"Information","source":"...","eventName":"...","eventData":"..."}
    /// </code>
    /// <c>eventData</c> is serialized via <c>ToString()</c>. Structured serialization
    /// (e.g. Newtonsoft JSON) can be added in a future phase when the event schema is stable.
    /// </para>
    /// </remarks>
    public class LoggerUnityFileJsonl : LoggerUnityFile
    {
        protected override string DefaultExtension => ".jsonl";
        protected override bool ShouldLog(LogEntry entry) => base.ShouldLog(entry) && entry.isEvent;

        protected override string FormatEntry(LogEntry entry)
        {
            string data = entry.eventData != null ? JsonEscape(entry.eventData.ToString()) : string.Empty;

            return "{"
                + $"\"timestamp\":\"{entry.timestampUtc:O}\","
                + $"\"sessionId\":\"{JsonEscape(m_sessionId)}\","
                + $"\"kind\":\"{entry.kind}\","
                + $"\"type\":\"{entry.type}\","
                + $"\"source\":\"{JsonEscape(entry.source)}\","
                + $"\"eventName\":\"{JsonEscape(entry.eventName)}\","
                + $"\"eventData\":\"{data}\""
                + "}";
        }

        static readonly char[] SpecialChars = { '\\', '"', '\n', '\r', '\t' };

        /// <summary>
        /// Escapes a string for embedding in a JSON string literal.
        /// Returns the original string unchanged when no special characters are present,
        /// avoiding allocation in the common case. When special characters are found,
        /// applies five substitutions required by the JSON spec: backslash, quote,
        /// newline, carriage return, and tab.
        /// </summary>
        static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.IndexOfAny(SpecialChars) < 0) return s;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n",  "\\n")
                    .Replace("\r",  "\\r")
                    .Replace("\t",  "\\t");
        }
    }
}
