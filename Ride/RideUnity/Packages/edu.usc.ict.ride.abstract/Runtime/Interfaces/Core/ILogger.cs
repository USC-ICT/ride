using System;

namespace Ride
{
    /// <summary>
    /// Defines the contract for a logger used within the RIDE logging system.
    /// 
    /// This logger supports standard logging, severity-based logging, and
    /// custom structured event logging. It may route output to Unity's console,
    /// external log sinks, or both depending on the implementation.
    /// 
    /// This interface closely mirrors <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger">Microsoft.Extensions.Logging.ILogger</see>
    /// and is intended to support future compatibility with the .NET logging infrastructure.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Represents a no-op disposable scope returned by <see cref="BeginScope{TState}"/>
        /// when no actual scope tracking is implemented.
        /// </summary>
        public class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }

        /// <summary>
        /// Gets the name of this logger instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets whether this logger should also receive Unity engine log messages
        /// such as Debug.Log, Debug.LogError, etc.
        /// </summary>
        bool ReceiveEngineLogMessages { get; set; }

        /// <summary>
        /// Called once by <see cref="LogSystemUnity"/> when this logger is registered.
        /// Both values are fixed for the lifetime of the session and reset on each Play in the editor.
        /// </summary>
        void SetSessionId(string sessionId, string sessionStartUtc);

        /// <summary>
        /// Logs a fully structured Ride log entry.
        /// </summary>
        /// <param name="entry">The entry to write.</param>
        void Log(LogEntry entry);

        /// <summary>
        /// Logs a message at the default severity level. Accepts any object,
        /// which will be logged using its <c>ToString()</c> representation.
        /// </summary>
        /// <param name="message">The text message to log.</param>
        void Log(object message);

        /// <summary>
        /// Logs a message at the specified severity level. Accepts any object,
        /// which will be logged using its <c>ToString()</c> representation.
        /// </summary>
        /// <param name="type">The severity or category of the log.</param>
        /// <param name="message">The text message to log.</param>
        void Log(LogType type, object message);

        /// <summary>
        /// Logs a structured event for telemetry, debugging, or analytics.
        /// </summary>
        /// <param name="eventType">A string identifier for the type of event (e.g. "AgentDied", "PathfindingFail").</param>
        /// <param name="eventData">An object representing event data, such as a dictionary or serializable struct.</param>
        void LogEvent(string eventType, object eventData);

        /// <summary>
        /// Checks whether the given log level is currently enabled.
        /// Helps reduce cost of message construction when logging is disabled.
        /// </summary>
        /// <param name="type">The log level to check.</param>
        /// <returns><c>true</c> if the log level is enabled; otherwise, <c>false</c>.</returns>
        bool IsEnabled(LogType type);

        /// <summary>
        /// Begins a new logging scope. Scopes may be used to group log output by logical context.
        /// </summary>
        /// <typeparam name="TState">The type of scope context.</typeparam>
        /// <param name="state">The context to associate with the scope.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the scope when disposed.</returns>
        /// <example>
        /// <code>
        /// using (logger.BeginScope("Loading Scene: Village"))
        /// {
        ///     logger.Log(LogType.Information, "Initializing terrain");
        ///     logger.Log(LogType.Warning, "Terrain shader fallback detected");
        /// }
        /// </code>
        /// </example>
        IDisposable BeginScope<TState>(TState state);
    }
}
