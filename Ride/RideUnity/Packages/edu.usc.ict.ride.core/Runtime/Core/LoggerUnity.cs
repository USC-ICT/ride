using System;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Abstract base class for Unity-integrated loggers within the RIDE framework.
    /// Extends <see cref="RideMonoBehaviour"/> and implements <see cref="ILogger"/>.
    /// 
    /// <para>
    /// Provides abstract logging methods for derived classes to implement platform-specific
    /// message routing (e.g., to Unity console, files, remote collectors).
    /// Includes support for structured events, scoped logging, and level filtering.
    /// </para>
    /// </summary>
    public abstract class LoggerUnity : RideMonoBehaviour, ILogger
    {
        [Header("Filters")]
        [SerializeField, Tooltip("Log entries tagged as Developer (engineer diagnostics, warnings, errors).")]
        bool m_logDeveloperMessages = true;
        [SerializeField, Tooltip("Log entries tagged as Application (named domain events from simulation systems).")]
        bool m_logApplicationMessages = true;
        [SerializeField, Tooltip("Log entries tagged as Telemetry (frame time, memory, queue depth, etc.).")]
        bool m_logTelemetryMessages;
        [SerializeField, Tooltip("Log entries tagged as Analytics (engagement, funnels, product metrics).")]
        bool m_logAnalyticsMessages;
        [SerializeField, Tooltip("When enabled, Unity engine messages (Debug.Log, Debug.LogWarning, Debug.LogError) are forwarded to this logger via UnityLogBridgeSystem.")]
        bool m_receiveEngineLogMessages;


        protected string m_sessionId = string.Empty;
        protected string m_sessionStartUtc = string.Empty;
        protected string m_platform = string.Empty;
        protected string m_productName = string.Empty;


        /// <inheritdoc/>
        public string Name { get => name; set => name = value; }

        /// <summary>
        /// Gets or sets whether this logger should receive log messages routed from the Unity engine.
        /// When enabled, messages from <see cref="UnityEngine.Application.logMessageReceived"/> will be forwarded.
        /// </summary>
        public virtual bool ReceiveEngineLogMessages { get => m_receiveEngineLogMessages; set => m_receiveEngineLogMessages = value; }

        /// <inheritdoc/>
        public virtual void SetSessionId(string sessionId, string sessionStartUtc)
        {
            m_sessionId = sessionId;
            m_sessionStartUtc = sessionStartUtc;
            m_platform = PlatformShortName();
            m_productName = Application.productName;
        }

        protected static string PlatformShortName()
        {
            if (RideUtils.IsAndroid()) return "android";
            if (RideUtils.IsIOS())     return "ios";
            if (RideUtils.IsWebGL())   return "webgl";
            if (RideUtils.IsLinux())   return "linux";
            if (RideUtils.IsOSX())     return "osx";
            if (RideUtils.IsWindows()) return "win";
            return Application.platform.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Returns true if <paramref name="entry"/> should be processed by this logger based on its <see cref="LogKind"/>.
        /// Override to add sink-specific guards (e.g. source filtering).
        /// </summary>
        protected virtual bool ShouldLog(LogEntry entry)
        {
            return entry.kind switch
            {
                LogKind.Developer   => m_logDeveloperMessages,
                LogKind.Application => m_logApplicationMessages,
                LogKind.Telemetry   => m_logTelemetryMessages,
                LogKind.Analytics   => m_logAnalyticsMessages,
                _                   => true
            };
        }

        /// <summary>
        /// Formats a <see cref="LogEntry"/> into a human-readable string.
        /// Produces a <c>[date time] [kind] [source]</c> prefix followed by the message or event body.
        /// Override in subclasses to alter format (e.g. add scope tags, change timestamp precision).
        /// </summary>
        protected virtual string FormatEntry(LogEntry entry)
        {
            string prefix = $"[{entry.timestampUtc.ToLocalTime():MM/dd HH:mm:ss}] [{entry.kind}] [{entry.source}] ";
            if (entry.isEvent)
                return entry.eventData != null
                    ? $"{prefix}{entry.eventName} {entry.eventData}"
                    : $"{prefix}{entry.eventName}";
            return $"{prefix}{entry.message}";
        }

        /// <inheritdoc/>
        public abstract void Log(LogEntry entry);

        /// <inheritdoc/>
        public virtual void Log(object message) => Log(LogEntry.CreateDeveloper(LogType.Information, message));

        /// <inheritdoc/>
        public virtual void Log(LogType type, object message) => Log(LogEntry.CreateDeveloper(type, message));

        /// <inheritdoc/>
        public virtual void LogEvent(string eventType, object eventData) => Log(LogEntry.CreateApplicationEvent(eventType, eventData));

        /// <inheritdoc/>
        public virtual bool IsEnabled(LogType type) => type != LogType.None;

        /// <inheritdoc/>
        public virtual IDisposable BeginScope<TState>(TState state)
        {
            // No scope tracking - return a no-op disposable
            return ILogger.NullScope.Instance;
        }
    }
}
