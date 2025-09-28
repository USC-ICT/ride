using System;

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
        /// <inheritdoc/>
        public string Name { get => name; set => name = value; }

        /// <summary>
        /// Gets or sets whether this logger should receive log messages routed from the Unity engine.
        /// When enabled, messages from <see cref="UnityEngine.Application.logMessageReceived"/> will be forwarded.
        /// </summary>
        public abstract bool ReceiveEngineLogMessages { get; set; }

        /// <inheritdoc/>
        public abstract void Log(object message);

        /// <inheritdoc/>
        public abstract void Log(LogType type, object message);

        /// <inheritdoc/>
        public abstract void LogEvent(string eventType, object eventData);

        /// <inheritdoc/>
        public virtual bool IsEnabled(LogType type) => type != LogType.None;

        /// <inheritdoc/>
        public virtual IDisposable BeginScope<TState>(TState state)
        {
            // No scope tracking — return a no-op disposable
            return ILogger.NullScope.Instance;
        }
    }
}
