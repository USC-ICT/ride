using System;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Global logging entry point for RIDE systems.
    /// <para>
    /// Defaults to a silent fallback implementation unless initialized with a custom <see cref="ILogSystem"/> via <see cref="SetLogSystem"/>.
    /// </para>
    /// <para>
    /// Provides static helpers for basic logging, event tracking, and scoped logging compatible with
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger">Microsoft.Extensions.Logging</see>.
    /// </para>
    /// <para>
    /// Prefer <c>RideLog</c> over <c>Debug.*</c> throughout the Ride codebase. All public methods carry
    /// <c>[HideInCallstack]</c> so double-clicking a log entry in the Unity console jumps directly to the
    /// call site rather than into this class. Entries also reach all registered sinks (file, cloud, etc.)
    /// and carry session ID, kind, and source metadata automatically.
    /// </para>
    /// </summary>
    public static class RideLog
    {
        /// <summary>Gets the active logging implementation.</summary>
        public static ILogSystem LogSystem { get; private set; } = new LogSystemEmpty();

        /// <summary>
        /// Replaces the active log system with a custom implementation.
        /// </summary>
        /// <param name="system">The log system to assign.</param>
        public static void SetLogSystem(ILogSystem system) => LogSystem = system ?? new LogSystemEmpty();

        /// <summary>Writes an existing structured log entry.</summary>
        /// <param name="entry">The entry to write.</param>
        [HideInCallstack] public static void Log(LogEntry entry) => LogSystem.Log(entry);
        /// <summary>Writes an informational developer message.</summary>
        /// <param name="message">The message or value to log.</param>
        [HideInCallstack] public static void Log(object message) => LogDeveloper(LogType.Information, message);
        /// <summary>Writes an informational developer message from a specified source.</summary>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void Log(object message, string source) => LogDeveloper(LogType.Information, message, source);
        /// <summary>Writes a developer message with a specified severity.</summary>
        /// <param name="type">The log severity.</param>
        /// <param name="message">The message or value to log.</param>
        [HideInCallstack] public static void Log(LogType type, object message) => LogDeveloper(type, message);
        /// <summary>Writes a developer message with a specified severity and source.</summary>
        /// <param name="type">The log severity.</param>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void Log(LogType type, object message, string source) => LogDeveloper(type, message, source);
        /// <summary>Writes a warning developer message.</summary>
        /// <param name="message">The message or value to log.</param>
        [HideInCallstack] public static void LogWarning(object message) => LogDeveloper(LogType.Warning, message);
        /// <summary>Writes a warning developer message from a specified source.</summary>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogWarning(object message, string source) => LogDeveloper(LogType.Warning, message, source);
        /// <summary>Writes an error developer message.</summary>
        /// <param name="message">The message or value to log.</param>
        [HideInCallstack] public static void LogError(object message) => LogDeveloper(LogType.Error, message);
        /// <summary>Writes an error developer message from a specified source.</summary>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogError(object message, string source) => LogDeveloper(LogType.Error, message, source);
        /// <summary>Writes a structured application event.</summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="data">The event payload.</param>
        [HideInCallstack] public static void LogEvent(string eventName, object data) => LogApplicationEvent(eventName, data);
        /// <summary>Writes a structured application event from a specified source.</summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="data">The event payload.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogEvent(string eventName, object data, string source) => LogApplicationEvent(eventName, data, source);
        /// <summary>Writes an informational developer log entry.</summary>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogDeveloper(object message, string source = LogSources.RideLog) => LogDeveloper(LogType.Information, message, source);
        /// <summary>Writes a developer log entry.</summary>
        /// <param name="type">The log severity.</param>
        /// <param name="message">The message or value to log.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogDeveloper(LogType type, object message, string source = LogSources.RideLog) => Log(LogEntry.CreateDeveloper(type, message, source));
        /// <summary>Writes a structured application-event log entry.</summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="data">The event payload.</param>
        /// <param name="source">The logical source name.</param>
        [HideInCallstack] public static void LogApplicationEvent(string eventName, object data, string source = LogSources.RideLog) => Log(LogEntry.CreateApplicationEvent(eventName, data, source));

        /// <summary>
        /// Begins a named logging scope that applies to all subsequent logs until disposed.
        /// </summary>
        /// <typeparam name="T">The type of scope identifier.</typeparam>
        /// <param name="scope">The scope description or identifier.</param>
        /// <returns>An IDisposable that ends the scope when disposed.</returns>
        /// <example>
        /// <code>
        /// using (RideLog.BeginScope("Loading Scene: Village"))
        /// {
        ///     RideLog.Log("Initializing terrain");
        ///     RideLog.LogWarning("Terrain shader fallback detected");
        /// }
        /// </code>
        /// </example>
        public static IDisposable BeginScope<T>(T scope) => LogSystem.BeginScope(scope);
    }

    /// <summary>
    /// A no-op implementation of <see cref="ILogSystem"/> and <see cref="IRideSystem"/> that performs no logging or system behavior.
    /// <para>
    /// Used as a fallback when no active log system is assigned. All log methods are silent,
    /// allowing code to safely invoke logging without checking for null references.
    /// </para>
    /// <para>
    /// This class is useful in testing environments, headless contexts, or for temporarily disabling logging output.
    /// </para>
    /// </summary>
    class LogSystemEmpty : ILogSystem
    {
        /// <inheritdoc/>
        public void AddLogger(ILogger logger) { }
        /// <inheritdoc/>
        public void RemoveLogger(ILogger logger) { }
        /// <inheritdoc/>
        public void Log(LogEntry entry) { }
        /// <inheritdoc/>
        public void Log(object message) { }
        /// <inheritdoc/>
        public void Log(LogType type, object message) { }
        /// <inheritdoc/>
        public void LogEvent(string eventName, object eventData) { }
        /// <inheritdoc/>
        public IDisposable BeginScope<T>(T scope) => ILogger.NullScope.Instance;

        // IRideSystem
        /// <inheritdoc/>
        public bool SystemAwakeCalled => false;
        /// <inheritdoc/>
        public bool SystemInitCalled => false;
        /// <inheritdoc/>
        public bool IsActive => false;
        /// <inheritdoc/>
        public RideID id => RideID.Null;
        /// <inheritdoc/>
        public string name => nameof(LogSystemEmpty);
        /// <inheritdoc/>
        public void SystemAwake() { }
        /// <inheritdoc/>
        public void SystemInit() { }
        /// <inheritdoc/>
        public void SystemUpdate(float deltaTime) { }
        /// <inheritdoc/>
        public void SystemLateUpdate(float deltaTime) { }
        /// <inheritdoc/>
        public void SystemFixedUpdate(float fixedDeltaTime) { }
        /// <inheritdoc/>
        public void SystemShutdown() { }
        /// <inheritdoc/>
        public void Activate() { }
        /// <inheritdoc/>
        public void Deactivate() { }
    }
}
