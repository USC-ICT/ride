using System;

namespace Ride
{
    /// <summary>
    /// Represents the severity or verbosity of a log message.
    /// This enum is designed to align with <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel">Microsoft.Extensions.Logging.LogLevel</see>
    /// to support future compatibility with standard .NET logging infrastructure.
    /// </summary>
    public enum LogType
    {
        /// <summary>Logs that contain the most detailed messages. These messages may contain sensitive application data.</summary>
        Trace = 0,

        /// <summary>Logs that are used for interactive investigation during development.</summary>
        Debug = 1,

        /// <summary>Logs that track the general flow of the application.</summary>
        Information = 2,

        /// <summary>Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application to stop.</summary>
        Warning = 3,

        /// <summary>Logs that highlight when the current flow of execution is stopped due to a failure.</summary>
        Error = 4,

        /// <summary>Logs that describe an unrecoverable application or system crash, or a catastrophic failure.</summary>
        Critical = 5,

        /// <summary>Disables logging for this level.</summary>
        None = 6,
    }

    /// <summary>
    /// Represents a log system capable of managing multiple loggers and dispatching structured and leveled log messages.
    /// Used by the <see cref="RideLog"/> static entry point to enable centralized logging.
    /// <para>
    /// This interface is designed to be forward-compatible with
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger">Microsoft.Extensions.Logging.ILogger</see>,
    /// enabling future integration with standard .NET logging providers and structured log sinks.
    /// </para>
    /// </summary>
    public interface ILogSystem : IRideSystem
    {
        /// <summary>
        /// Adds a logger to the active logger list.
        /// Multiple loggers can be active simultaneously.
        /// </summary>
        /// <param name="logger">The logger instance to add.</param>
        void AddLogger(ILogger logger);

        /// <summary>
        /// Removes a logger from the active logger list.
        /// </summary>
        /// <param name="logger">The logger instance to remove.</param>
        void RemoveLogger(ILogger logger);

        /// <summary>
        /// Logs a fully structured entry.
        /// </summary>
        /// <param name="entry">The structured log entry to dispatch.</param>
        void Log(LogEntry entry);

        /// <summary>
        /// Logs a general message at the default <see cref="LogType.Information"/> level.
        /// Accepts any object, which will be logged using its <c>ToString()</c> representation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Log(object message);

        /// <summary>
        /// Logs a message at the specified log level.
        /// Accepts any object, which will be logged using its <c>ToString()</c> representation.
        /// </summary>
        /// <param name="type">The severity or verbosity of the message.</param>
        /// <param name="message">The message to log.</param>
        void Log(LogType type, object message);

        /// <summary>
        /// Logs a named event with associated data.
        /// Events are commonly used for telemetry or tracing.
        /// </summary>
        /// <param name="eventName">The event name or type identifier (e.g., "AgentDied").</param>
        /// <param name="eventData">Structured event data (e.g., a dictionary or serializable object).</param>
        void LogEvent(string eventName, object eventData);

        /// <summary>
        /// Begins a new logging scope. Scopes may be used to group log output by logical context.
        /// </summary>
        /// <typeparam name="TState">The type of scope context.</typeparam>
        /// <param name="state">The context to associate with the scope.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the scope when disposed.</returns>
        IDisposable BeginScope<T>(T state);
    }
}
