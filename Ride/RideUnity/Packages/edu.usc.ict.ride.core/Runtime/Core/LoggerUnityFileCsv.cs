namespace Ride
{
    /// <summary>
    /// File sink that writes log entries as CSV, one row per entry.
    /// </summary>
    /// <remarks>
    /// Accepts all <see cref="LogKind"/> values — developer messages and application events
    /// are both represented as rows. Use the inherited filter fields to restrict by kind.
    /// <para>
    /// Columns: <c>timestamp, kind, type, source, isEvent, eventName, message, eventData</c>
    /// </para>
    /// <para>
    /// Fields containing commas, double-quotes, or newlines are quoted per
    /// <see href="https://www.rfc-editor.org/rfc/rfc4180">RFC 4180</see>.
    /// </para>
    /// </remarks>
    public class LoggerUnityFileCsv : LoggerUnityFile
    {
        protected override string DefaultExtension => ".csv";

        protected override string FileHeader =>
            "timestamp,sessionId,kind,type,source,isEvent,eventName,message,eventData";

        protected override string FormatEntry(LogEntry entry) =>
            string.Join(",",
                Escape(entry.timestampUtc.ToString("O")),
                Escape(m_sessionId),
                Escape(entry.kind.ToString()),
                Escape(entry.type.ToString()),
                Escape(entry.source),
                entry.isEvent ? "True" : "False",
                Escape(entry.eventName),
                Escape(entry.message?.ToString()),
                Escape(entry.eventData?.ToString()));

        static readonly char[] SpecialChars = { ',', '"', '\n', '\r' };

        /// <summary>
        /// Escapes a string for a CSV field per RFC 4180.
        /// Returns the original string unchanged when no special characters are present.
        /// When commas, quotes, or newlines are found, wraps the entire field in double-quotes —
        /// which makes commas and newlines syntactically harmless inside the field.
        /// The only character requiring actual substitution is <c>"</c>, which becomes <c>""</c>.
        /// </summary>
        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.IndexOfAny(SpecialChars) < 0) return s;
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }
    }
}
