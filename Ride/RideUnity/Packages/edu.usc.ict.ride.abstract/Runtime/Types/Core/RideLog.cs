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
        public static ILogSystem LogSystem { get; private set; } = new LogSystemEmpty();

        /// <summary>
        /// Replaces the active log system with a custom implementation.
        /// </summary>
        /// <param name="system">The log system to assign.</param>
        public static void SetLogSystem(ILogSystem system) => LogSystem = system ?? new LogSystemEmpty();

        [HideInCallstack] public static void Log(LogEntry entry) => LogSystem.Log(entry);
        [HideInCallstack] public static void Log(object message) => LogDeveloper(LogType.Information, message);
        [HideInCallstack] public static void Log(object message, string source) => LogDeveloper(LogType.Information, message, source);
        [HideInCallstack] public static void Log(LogType type, object message) => LogDeveloper(type, message);
        [HideInCallstack] public static void Log(LogType type, object message, string source) => LogDeveloper(type, message, source);
        [HideInCallstack] public static void LogWarning(object message) => LogDeveloper(LogType.Warning, message);
        [HideInCallstack] public static void LogWarning(object message, string source) => LogDeveloper(LogType.Warning, message, source);
        [HideInCallstack] public static void LogError(object message) => LogDeveloper(LogType.Error, message);
        [HideInCallstack] public static void LogError(object message, string source) => LogDeveloper(LogType.Error, message, source);
        [HideInCallstack] public static void LogEvent(string eventName, object data) => LogApplicationEvent(eventName, data);
        [HideInCallstack] public static void LogEvent(string eventName, object data, string source) => LogApplicationEvent(eventName, data, source);
        [HideInCallstack] public static void LogDeveloper(object message, string source = LogSources.RideLog) => LogDeveloper(LogType.Information, message, source);
        [HideInCallstack] public static void LogDeveloper(LogType type, object message, string source = LogSources.RideLog) => Log(LogEntry.CreateDeveloper(type, message, source));
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
        public void AddLogger(ILogger logger) { }
        public void RemoveLogger(ILogger logger) { }
        public void Log(LogEntry entry) { }
        public void Log(object message) { }
        public void Log(LogType type, object message) { }
        public void LogEvent(string eventName, object eventData) { }
        public IDisposable BeginScope<T>(T scope) => ILogger.NullScope.Instance;

        // IRideSystem
        public bool SystemAwakeCalled => false;
        public bool SystemInitCalled => false;
        public bool IsActive => false;
        public RideID id => RideID.Null;
        public string name => nameof(LogSystemEmpty);
        public void SystemAwake() { }
        public void SystemInit() { }
        public void SystemUpdate(float deltaTime) { }
        public void SystemLateUpdate(float deltaTime) { }
        public void SystemFixedUpdate(float fixedDeltaTime) { }
        public void SystemShutdown() { }
        public void Activate() { }
        public void Deactivate() { }
    }
}
