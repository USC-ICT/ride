using System;

namespace Ride
{
    /// <summary>
    /// Describes the high-level purpose of a log entry so sinks can filter developer logs,
    /// application events, telemetry, and analytics independently.
    /// </summary>
    public enum LogKind
    {
        Developer,
        Application,
        Telemetry,
        Analytics
    }

    /// <summary>
    /// Common source names used by Ride log producers.
    /// </summary>
    public static class LogSources
    {
        public const string RideLog = nameof(RideLog);
        public const string WorldStateSystem = "WorldStateSystemUnity";
        public const string UnityLogBridgeSystem = "UnityLogBridgeSystem";
    }

    /// <summary>
    /// Represents a single structured log entry routed through the Ride logging system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>LogEntry</c> is the canonical unit of data flowing through the Ride logging pipeline.
    /// Every log call — developer diagnostics, application domain events, telemetry — is eventually
    /// packaged into a <c>LogEntry</c> before being dispatched.
    /// </para>
    ///
    /// <para><b>Creating entries</b></para>
    /// <para>
    /// The preferred way to produce entries is through the static helpers on <see cref="RideLog"/>,
    /// which forward to this struct's factory methods:
    /// <list type="bullet">
    ///   <item><description><see cref="RideLog.LogDeveloper(object, string)"/> — engineer-facing diagnostics</description></item>
    ///   <item><description><see cref="RideLog.LogApplicationEvent(string, object, string)"/> — named domain events (e.g. <c>"scenarioStarted"</c>, <c>"agentDied"</c>)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Direct construction via the factory methods is also acceptable:
    /// </para>
    /// <code>
    /// var entry = LogEntry.CreateDeveloper(LogType.Warning, "Pathfinding fallback triggered", LogSources.RideLog);
    /// var evt   = LogEntry.CreateApplicationEvent("agentDied", new AgentDiedEvent { agent = id }, LogSources.WorldStateSystem);
    /// </code>
    ///
    /// <para><b>Dispatch pipeline</b></para>
    /// <para>
    /// Entries flow through the system as follows:
    /// <list type="number">
    ///   <item><description><see cref="RideLog"/> (static gateway) → <see cref="ILogSystem.Log(LogEntry)"/></description></item>
    ///   <item><description><see cref="LogSystemUnity"/> fans the entry out to every registered <see cref="ILogger"/></description></item>
    ///   <item><description>Each <see cref="ILogger"/> implementation calls <see cref="ILogger.Log(LogEntry)"/> and decides what to do based on <see cref="LogEntry.kind"/>, <see cref="LogEntry.type"/>, and <see cref="LogEntry.source"/></description></item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Known producers</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="RideLog"/> — primary static entry point for all Ride code</description></item>
    ///   <item><description><c>WorldStateSystemUnity</c> — emits application events for every world-state change (e.g. agent lifecycle, scenario events) using <see cref="LogSources.WorldStateSystem"/></description></item>
    ///   <item><description><c>UnityLogBridgeSystem</c> — bridges <c>UnityEngine.Application.logMessageReceived</c> into the pipeline with <see cref="LogSources.UnityLogBridgeSystem"/>; loggers check this source to prevent feedback loops</description></item>
    /// </list>
    ///
    /// <para><b>Known consumers</b></para>
    /// <list type="bullet">
    ///   <item><description><c>LoggerUnityConsole</c> — writes to the Unity console; filters by <see cref="LogKind"/> and suppresses engine-originated entries when <c>ReceiveEngineLogMessages</c> is false</description></item>
    ///   <item><description><c>Observer</c> (ride.simulation) — watches for application events (<see cref="isEvent"/> == true) and converts them into logical predicates for the interpretation system</description></item>
    ///   <item><description><c>PedagogicalLogger</c> — listens for <c>PEDAGOGICAL_EVENT</c> application events and forwards them to an xAPI/LRS sink</description></item>
    ///   <item><description><c>ExampleLoggerImplementation</c> — reference implementation; writes all entries to a flat file</description></item>
    /// </list>
    ///
    /// <para><b>Source field and loop prevention</b></para>
    /// <para>
    /// The <see cref="source"/> field identifies the system that produced the entry. Consumers that also
    /// write to Unity's console (e.g. <c>LoggerUnityConsole</c>) compare <see cref="source"/> against
    /// <see cref="LogSources.UnityLogBridgeSystem"/> to avoid re-logging messages that originated from
    /// the engine itself.
    /// </para>
    ///
    /// <para><b>Event vs message entries</b></para>
    /// <para>
    /// When <see cref="isEvent"/> is <c>true</c>, the entry represents a named domain event and
    /// <see cref="eventName"/> / <see cref="eventData"/> carry the payload; <see cref="message"/> is null.
    /// When <see cref="isEvent"/> is <c>false</c>, <see cref="message"/> carries the payload and
    /// <see cref="eventName"/> / <see cref="eventData"/> are null. Consumers should branch on
    /// <see cref="isEvent"/> rather than null-checking individual fields.
    /// </para>
    /// </remarks>
    public readonly struct LogEntry
    {
        /// <summary>
        /// Creates a message-oriented log entry.
        /// </summary>
        public LogEntry(DateTime timestampUtc, LogKind kind, LogType type, string source, object message)
        {
            this.timestampUtc = timestampUtc;
            this.kind = kind;
            this.type = type;
            this.source = string.IsNullOrWhiteSpace(source) ? LogSources.RideLog : source;
            this.message = message;
            this.eventName = null;
            this.eventData = null;
            this.isEvent = false;
        }

        /// <summary>
        /// Creates an event-oriented log entry.
        /// </summary>
        public LogEntry(DateTime timestampUtc, LogKind kind, LogType type, string source, string eventName, object eventData)
        {
            this.timestampUtc = timestampUtc;
            this.kind = kind;
            this.type = type;
            this.source = string.IsNullOrWhiteSpace(source) ? LogSources.RideLog : source;
            this.message = null;
            this.eventName = eventName;
            this.eventData = eventData;
            this.isEvent = true;
        }

        public DateTime timestampUtc { get; }

        public LogKind kind { get; }

        public LogType type { get; }

        public string source { get; }

        public object message { get; }

        public string eventName { get; }

        public object eventData { get; }

        public bool isEvent { get; }

        /// <summary>Creates a developer-focused message entry.</summary>
        public static LogEntry CreateDeveloper(LogType type, object message, string source = LogSources.RideLog) =>
            new LogEntry(DateTime.UtcNow, LogKind.Developer, type, source, message);

        /// <summary>Creates an application-focused event entry.</summary>
        public static LogEntry CreateApplicationEvent(string eventName, object eventData, string source = LogSources.RideLog, LogType type = LogType.Information) =>
            new LogEntry(DateTime.UtcNow, LogKind.Application, type, source, eventName, eventData);
    }
}
