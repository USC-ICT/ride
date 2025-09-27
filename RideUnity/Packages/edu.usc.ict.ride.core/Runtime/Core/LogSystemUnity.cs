using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Unity-based implementation of the <see cref="ILogSystem"/> interface.
    /// Aggregates multiple <see cref="ILogger"/> instances and dispatches all log messages, events, and errors.
    /// Also listens for Unity engine messages via <see cref="Application.logMessageReceived"/> and forwards them.
    /// <para>
    /// Encapsulates ILoggers and writes to them.  This class listens to Unity's Debug.Log callback
    /// and then writes to all its loggers.  Unity's Debug.Log should be used because when clicking it
    /// in the console, you're brought directly to the log line. If Debug.log gets wrapped in a Ride function
    /// the debugging becomes harder because we jump to the wrapper function
    /// </para>
    /// </summary>
    public class LogSystemUnity : RideSystemMonoBehaviour, ILogSystem
    {
        [SerializeField] List<LoggerUnity> m_loggersToLoad = new List<LoggerUnity>();

        List<ILogger> m_loggers = new List<ILogger>();

        public override void SystemAwake()
        {
            base.SystemAwake();

            foreach (var logger in m_loggersToLoad)
                if (logger != null)
                    AddLogger(logger);
        }

        void LogCallback(string logString, string stackTrace, UnityEngine.LogType type)
        {
            foreach (ILogger logger in m_loggers)
            {
                if (logger.ReceiveEngineLogMessages)
                    logger.Log((LogType)type, logString);
            }
        }

        /// <inheritdoc/>
        public void AddLogger(ILogger logger)
        {
            if (!m_loggers.Contains(logger))
                m_loggers.Add(logger);
            else
                Log($"Failed to add logger {logger.Name} because it's already added");
        }

        /// <inheritdoc/>
        public void RemoveLogger(ILogger logger) => m_loggers.Remove(logger);

        /// <inheritdoc/>
        public void Log(object message) => Log(LogType.Information, message);

        /// <inheritdoc/>
        public void Log(LogType type, object message) => m_loggers.ForEach((logger) => logger.Log(type, message));

        /// <inheritdoc/>
        public void LogEvent(string eventName, object eventData) => m_loggers.ForEach((logger) => logger.LogEvent(eventName, eventData));

        public IDisposable BeginScope<T>(T state)
        {
            var scopes = m_loggers
                .Select(logger => logger.BeginScope(state))
                .ToList();

            return new CompositeScope(scopes);
        }

        private class CompositeScope : IDisposable
        {
            private readonly List<IDisposable> m_scopes;
            public CompositeScope(List<IDisposable> scopes) => m_scopes = scopes;
            public void Dispose() => m_scopes.ForEach(scope => scope.Dispose());
        }
    }
}
