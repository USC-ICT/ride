using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Scene-configured hub that receives every <see cref="LogEntry"/> produced by Ride code
    /// and fans it out to all registered <see cref="ILogger"/> sinks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the runtime implementation of <see cref="ILogSystem"/>. It is registered with
    /// <see cref="RideLog"/> at startup so that all static logging calls (<see cref="RideLog.Log(LogEntry)"/>,
    /// <see cref="RideLog.LogWarning"/>, etc.) route through it automatically.
    /// </para>
    /// <para>
    /// Loggers can be wired up in two ways:
    /// <list type="bullet">
    ///   <item><description>At design time — add a <see cref="LoggerUnity"/> component to the inspector list; it is registered during <c>SystemAwake</c></description></item>
    ///   <item><description>At runtime — call <see cref="AddLogger"/> directly (used by systems like <c>InterpretationSystem</c> that register their own <c>Observer</c>)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Each registered logger receives the full <see cref="LogEntry"/> and independently decides what
    /// to do with it based on <see cref="LogEntry.kind"/>, <see cref="LogEntry.source"/>, and
    /// <see cref="LogEntry.type"/>. This system applies no filtering of its own.
    /// </para>
    /// </remarks>
    public class LogSystemUnity : RideSystemMonoBehaviour, ILogSystem
    {
        [SerializeField] List<LoggerUnity> m_loggersToLoad = new();

        List<ILogger> m_loggers = new();


        static string s_sessionId;
        static string s_sessionStartUtc;

        /// <summary>
        /// Unique identifier for this application session. Shared across scene loads;
        /// reset each time Play is pressed in the editor.
        /// </summary>
        public string SessionId => s_sessionId ??= Guid.NewGuid().ToString();

        /// <summary>
        /// UTC timestamp of when this session was first initialized. Paired with
        /// <see cref="SessionId"/> and used by sinks to build stable path hierarchies.
        /// </summary>
        public string SessionStartUtc => s_sessionStartUtc ??= DateTime.UtcNow.ToString("O");

        // Reset once per Play session in the editor (handles Disable Domain Reload);
        // in builds the process exits between runs so no reset is needed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { s_sessionId = null; s_sessionStartUtc = null; }


        public override void SystemAwake()
        {
            base.SystemAwake();

            foreach (var logger in m_loggersToLoad)
                if (logger != null)
                    AddLogger(logger);
        }

        public override void SystemInit()
        {
            base.SystemInit();

            RideLog.LogApplicationEvent("SessionStart", JsonUtility.ToJson(BuildSessionStartData(SessionId)), nameof(LogSystemUnity));
        }

        /// <inheritdoc/>
        public void AddLogger(ILogger logger)
        {
            if (!m_loggers.Contains(logger))
            {
                logger.SetSessionId(SessionId, SessionStartUtc);
                m_loggers.Add(logger);
            }
            else
            {
                Log($"Failed to add logger {logger.Name} because it's already added");
            }
        }

        /// <inheritdoc/>
        public void RemoveLogger(ILogger logger) => m_loggers.Remove(logger);

        /// <inheritdoc/>
        [HideInCallstack]
        public void Log(LogEntry entry)
        {
            // Use foreach, not List.ForEach() - the delegate overload generates a compiler closure that escapes HideInCallstack property.
            foreach (var logger in m_loggers)
                logger.Log(entry);
        }

        /// <inheritdoc/>
        public void Log(object message) => Log(LogEntry.CreateDeveloper(LogType.Information, message));

        /// <inheritdoc/>
        public void Log(LogType type, object message) => Log(LogEntry.CreateDeveloper(type, message));

        /// <inheritdoc/>
        public void LogEvent(string eventName, object eventData) => Log(LogEntry.CreateApplicationEvent(eventName, eventData));

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

        // For a more complete hardware manifest (GPU vendor/driver, shader level,
        // deviceUniqueIdentifier, iOS device fields, etc.) fire a separate
        // "SystemInfoDetailed" event using the same BuildSessionStartData pattern.
        [Serializable]
        public struct SessionStartData
        {
            public string sessionId;
            public string platform;
            public string productName;
            public string version;
            public string unityVersion;
            public string deviceName;
            public string deviceModel;
            public string deviceType;
            public string os;
            public string processorType;
            public int    processorCount;
            public int    systemMemoryMb;
            public string gpuName;
            public int    gpuMemoryMb;
        }

        public static SessionStartData BuildSessionStartData(string sessionId) => new()
        {
            sessionId      = sessionId,
            platform       = Application.platform.ToString(),
            productName    = Application.productName,
            version        = Application.version,
            unityVersion   = Application.unityVersion,
            deviceName     = SystemInfo.deviceName,
            deviceModel    = SystemInfo.deviceModel,
            deviceType     = SystemInfo.deviceType.ToString(),
            os             = SystemInfo.operatingSystem,
            processorType  = SystemInfo.processorType,
            processorCount = SystemInfo.processorCount,
            systemMemoryMb = SystemInfo.systemMemorySize,
            gpuName        = SystemInfo.graphicsDeviceName,
            gpuMemoryMb    = SystemInfo.graphicsMemorySize,
        };
    }
}
