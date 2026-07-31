using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Bridges Unity application log events into the Ride system layer and dispatches them to
    /// registered <see cref="LogCallback"/> listeners.
    /// This is intentionally a thin adapter over <see cref="Application.logMessageReceived"/>.
    /// </summary>
    public class UnityLogBridgeSystem : RideSystemMonoBehaviour
    {
        /// <summary>Represents the Ride-defined categories of application log messages forwarded by this system.</summary>
        public enum LogType
        {
            Error,
            Assert,
            Warning,
            Log,
            Exception
        }

        /// <summary>
        /// Represents a callback that is invoked when the application emits a log message.
        /// </summary>
        /// <param name="condition">The main log message or exception text provided by Unity.</param>
        /// <param name="stackTrace">The stack trace associated with the log entry, if available.</param>
        /// <param name="type">The Ride log type describing the category of the message.</param>
        public delegate void LogCallback(string condition, string stackTrace, LogType type);


        event LogCallback OnLogMessageReceived;
        bool m_isSubscribed;


        void OnEnable() => SubscribeToApplicationLogMessage();

        void OnDisable() => UnsubscribeToApplicationLogMessage();

        /// <summary>
        /// Registers a callback to receive application log messages forwarded through this Ride system.
        /// </summary>
        /// <param name="callback">The callback to invoke when a log message is received.</param>
        public void AddCallback(LogCallback callback)
        {
            if (callback == null)
                return;

            OnLogMessageReceived -= callback;
            OnLogMessageReceived += callback;
        }

        /// <summary>
        /// Removes a previously registered callback from the application log notification list.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void RemoveCallback(LogCallback callback) => OnLogMessageReceived -= callback;

        /// <summary>
        /// Unsubscribes from Unity log callbacks.
        /// </summary>
        public override void SystemShutdown()
        {
            UnsubscribeToApplicationLogMessage();

            base.SystemShutdown();
        }

        /// <summary>
        /// Subscribes this bridge to <see cref="Application.logMessageReceived"/> so Unity log messages
        /// can be forwarded to registered Ride callbacks while the component is enabled.
        /// </summary>
        /// <remarks>
        /// Subscription is guarded by <c>m_isSubscribed</c> to avoid duplicate handler registration
        /// across repeated Unity enable or initialization cycles.
        /// </remarks>
        private void SubscribeToApplicationLogMessage()
        {
            if (!m_isSubscribed)
            {
                Application.logMessageReceived += HandleUnityLogMessageReceived;
                m_isSubscribed = true;
            }
        }

        /// <summary>
        /// Unsubscribes this bridge from <see cref="Application.logMessageReceived"/> so Unity log messages
        /// are no longer forwarded while the component is disabled or shutting down.
        /// </summary>
        /// <remarks>
        /// Unsubscription is guarded by <c>m_isSubscribed</c> so repeated disable or shutdown paths remain safe.
        /// </remarks>
        private void UnsubscribeToApplicationLogMessage()
        {
            if (m_isSubscribed)
            {
                Application.logMessageReceived -= HandleUnityLogMessageReceived;
                m_isSubscribed = false;
            }
        }

        /// <summary>
        /// Receives Unity log messages, converts the Unity log type into the Ride-defined log type,
        /// and forwards the message to all registered Ride callbacks.
        /// </summary>
        /// <param name="condition">The main log message or exception text provided by Unity.</param>
        /// <param name="stackTrace">The stack trace associated with the log entry, if available.</param>
        /// <param name="type">The Unity log type describing the category of the message.</param>
        private void HandleUnityLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
        {
            // Convert Unity's log categories into the Ride-facing enum exposed by the interface.
            var logType = type switch
            {
                UnityEngine.LogType.Error => LogType.Error,
                UnityEngine.LogType.Assert => LogType.Assert,
                UnityEngine.LogType.Warning => LogType.Warning,
                UnityEngine.LogType.Log => LogType.Log,
                UnityEngine.LogType.Exception => LogType.Exception,
                _ => GetUnknownLogType(type)
            };

            OnLogMessageReceived?.Invoke(condition, stackTrace, logType);
        }

        private static LogType GetUnknownLogType(UnityEngine.LogType type) => LogType.Log;
    }
}
