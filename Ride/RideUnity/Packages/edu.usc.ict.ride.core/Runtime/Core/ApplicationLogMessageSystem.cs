using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;

namespace Ride
{
    /// <summary>
    /// Bridges Unity application log events into the Ride system layer and dispatches them to
    /// registered <see cref="IApplicationLogMessageSystem.LogCallback"/> listeners.
    /// </summary>
    public class ApplicationLogMessageSystem : RideSystemMonoBehaviour, IApplicationLogMessageSystem
    {
        [Tooltip("Routes Unity application log messages into this Ride system so registered callbacks can receive them.")]
        [SerializeField] LogCallbackHandler m_logCallbackHandler;

        List<IApplicationLogMessageSystem.LogCallback> m_callbacks = new();


        /// <summary>
        /// Initializes the system and subscribes to the Unity log callback handler so Ride listeners
        /// can receive application log messages.
        /// </summary>
        public override void SystemAwake()
        {
            base.SystemAwake();

            m_logCallbackHandler.AddCallback(LogCallback);
        }

        /// <summary>
        /// Registers a callback to receive application log messages forwarded through this Ride system.
        /// </summary>
        /// <param name="callback">The callback to invoke when a log message is received.</param>
        public void AddCallback(IApplicationLogMessageSystem.LogCallback callback) => m_callbacks.Add(callback);

        /// <summary>
        /// Removes a previously registered callback from the application log notification list.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void RemoveCallback(IApplicationLogMessageSystem.LogCallback callback) => m_callbacks.Remove(callback);


        /// <summary>
        /// Receives Unity log messages, converts the Unity log type into the Ride-defined log type,
        /// and forwards the message to all registered Ride callbacks.
        /// </summary>
        /// <param name="condition">The main log message or exception text provided by Unity.</param>
        /// <param name="stackTrace">The stack trace associated with the log entry, if available.</param>
        /// <param name="type">The Unity log type describing the category of the message.</param>
        private void LogCallback(string condition, string stackTrace, UnityEngine.LogType type)
        {
            // Convert Unity's log categories into the Ride-facing enum exposed by the interface.
            var logType = type switch
            {
                UnityEngine.LogType.Error => IApplicationLogMessageSystem.LogType.Error,
                UnityEngine.LogType.Assert => IApplicationLogMessageSystem.LogType.Assert,
                UnityEngine.LogType.Warning => IApplicationLogMessageSystem.LogType.Warning,
                UnityEngine.LogType.Log => IApplicationLogMessageSystem.LogType.Log,
                UnityEngine.LogType.Exception => IApplicationLogMessageSystem.LogType.Exception,
                _ => GetUnknownLogType(type)
            };

            foreach (var callback in m_callbacks)
                callback(condition, stackTrace, logType);
        }

        private static IApplicationLogMessageSystem.LogType GetUnknownLogType(UnityEngine.LogType type)
        {
            Debug.LogError($"ApplicationLogMessageSystem.LogCallback() - unknown LogType: {type}");
            return default;
        }
    }
}
