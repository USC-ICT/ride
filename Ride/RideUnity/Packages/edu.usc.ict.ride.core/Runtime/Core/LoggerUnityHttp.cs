using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride
{
    /// <summary>
    /// HTTP log sink that POSTs batched <see cref="LogEntry"/> objects to a configurable
    /// endpoint as a JSON array. Endpoint and Cognito Identity Pool ID are read from
    /// <see cref="RideConfig.RestServerApiSettings"/> (<c>logsProxyEndpoint</c> and
    /// <c>logsCognitoIdentityPoolId</c>) via <see cref="ConfigurationSystemUnity"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Entries are buffered and flushed on a timer, or immediately when
    /// <c>m_flushIntervalSeconds</c> is zero. HTTP requests are fire-and-forget:
    /// failed requests are silently dropped to avoid feedback loops with
    /// <see cref="LogSources.UnityLogBridgeSystem"/>.
    /// </para>
    /// <para>
    /// The final flush in <c>OnDestroy</c> starts a coroutine that may not complete
    /// if Unity is shutting down — last-batch delivery is best-effort.
    /// </para>
    /// </remarks>
    public class LoggerUnityHttp : LoggerUnity
    {
        [Header("HTTP")]
        [SerializeField, Tooltip("Seconds between HTTP flushes. 0 = flush immediately on every log entry.")]
        float m_flushIntervalSeconds = 5f;


        readonly List<string> m_buffer = new();
        float m_flushTimer;
        bool m_fatalError;


        protected override void Update()
        {
            base.Update();

            if (m_flushIntervalSeconds <= 0f || m_buffer.Count == 0)
                return;

            m_flushTimer += Time.deltaTime;
            if (m_flushTimer >= m_flushIntervalSeconds)
                Flush();
        }

        void OnDestroy() => Flush();

        public override void Log(LogEntry entry)
        {
            if (m_fatalError || !ShouldLog(entry))
                return;

            m_buffer.Add(FormatEntry(entry));

            if (m_flushIntervalSeconds <= 0f)
                Flush();
        }

        protected override string FormatEntry(LogEntry entry)
        {
            string data = entry.eventData != null ? JsonEscape(entry.eventData.ToString()) : string.Empty;

            return "{"
                + $"\"timestamp\":\"{entry.timestampUtc:O}\","
                + $"\"kind\":\"{entry.kind}\","
                + $"\"type\":\"{entry.type}\","
                + $"\"source\":\"{JsonEscape(entry.source)}\","
                + $"\"isEvent\":{(entry.isEvent ? "true" : "false")},"
                + $"\"eventName\":\"{JsonEscape(entry.eventName)}\","
                + $"\"message\":\"{JsonEscape(entry.message?.ToString())}\","
                + $"\"eventData\":\"{data}\""
                + "}";
        }

        void Flush()
        {
            if (m_fatalError || m_buffer.Count == 0)
                return;

            m_flushTimer = 0f;

            string payload = BuildPayload(m_buffer);
            m_buffer.Clear();

            if (gameObject.activeInHierarchy)
                StartCoroutine(Post(payload));
        }

        string BuildPayload(List<string> entries)
        {
            var sb = new StringBuilder(entries.Count * 64);
            sb.Append("{\"sessionId\":\"");
            sb.Append(m_sessionId);
            sb.Append("\",\"sessionStartUtc\":\"");
            sb.Append(m_sessionStartUtc);
            sb.Append("\",\"platform\":\"");
            sb.Append(m_platform);
            sb.Append("\",\"productName\":\"");
            sb.Append(JsonEscape(m_productName));
            sb.Append("\",\"events\":[");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(entries[i]);
            }
            sb.Append("]}");
            return sb.ToString();
        }

        IEnumerator Post(string json)
        {
            string endpoint = ConfigurationSystemUnity.GetLogsProxyEndpoint();
            if (string.IsNullOrEmpty(endpoint))
                yield break;

            string poolId = ConfigurationSystemUnity.GetLogsCognitoIdentityPoolId();

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using var req = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(poolId))
                req.SetRequestHeader("Authorization", $"Bearer {poolId}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                long code = req.responseCode;
                if (code == 400 || code == 401 || code == 403 || code == 404)
                {
                    m_fatalError = true;
                    m_buffer.Clear();
                    Debug.LogError($"[LoggerUnityHttp] Fatal HTTP {code} from {endpoint} — logging disabled. Check logsProxyEndpoint / logsCognitoIdentityPoolId in RideConfig.");
                }
            }
            // ConnectionError and 5xx are silently dropped — transient, keep trying.
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
