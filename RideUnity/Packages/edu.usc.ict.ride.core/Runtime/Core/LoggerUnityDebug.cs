using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// A Unity-based logger that routes log messages to the Unity console using
    /// <see cref="Debug.Log"/>, <see cref="Debug.LogWarning"/>, and <see cref="Debug.LogError"/>.
    /// <para>
    /// Supports timestamped messages, scope-based context tagging, and log level filtering.
    /// Implements <see cref="LoggerUnity"/>, which extends <see cref="ILogger"/>.
    /// </para>
    /// <para>
    /// Unity logging methods:
    /// <list type="bullet">
    /// <item><description><a href="https://docs.unity3d.com/ScriptReference/Debug.Log.html">Debug.Log</a></description></item>
    /// <item><description><a href="https://docs.unity3d.com/ScriptReference/Debug.LogWarning.html">Debug.LogWarning</a></description></item>
    /// <item><description><a href="https://docs.unity3d.com/ScriptReference/Debug.LogError.html">Debug.LogError</a></description></item>
    /// <item><description><a href="https://docs.unity3d.com/ScriptReference/Debug.Assert.html">Debug.Assert</a></description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class LoggerUnityDebug : LoggerUnity
    {
        /// <inheritdoc/>
        public override bool ReceiveEngineLogMessages { get; set; }

        /// <summary>Enables or disables event logging via <see cref="LogEvent"/>.</summary>
        public bool logEvents { get; set; }


        // Thread-local scope stack to support nested scopes per thread
        [ThreadStatic]
        private static Stack<string> _scopeStack;
        private static Stack<string> ScopeStack => _scopeStack ??= new Stack<string>();


        /// <inheritdoc/>
        public override void Log(object message) => Log(LogType.Information, $"[{DateTime.Now:MM/dd HH:mm:ss}] {message}");

        /// <inheritdoc/>
        public override void Log(LogType type, object message)
        {
            string formatted = $"{FormatScopePrefix()}{message}";

            switch (type)
            {
                case LogType.Trace:
                case LogType.Debug:
                case LogType.Information:
                    Debug.Log(formatted);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(formatted);
                    break;

                case LogType.Error:
                case LogType.Critical:
                    Debug.LogError(formatted);
                    break;

                case LogType.None:
                    // Intentionally do nothing
                    break;

                default:
                    Debug.Log(formatted);
                    break;
            }
        }

        /// <inheritdoc/>
        public override void LogEvent(string eventType, object eventData) { if (logEvents) Log($"{eventType} {eventData}"); }

        /// <inheritdoc/>
        public override bool IsEnabled(LogType type) => type != LogType.None;

        /// <inheritdoc/>
        public override IDisposable BeginScope<TState>(TState state)
        {
            string scopeName = state?.ToString();
            ScopeStack.Push(scopeName);
            return new ScopePopper();
        }

        /// <summary>
        /// Formats the current scope prefix by joining nested scope names using " > ".
        /// </summary>
        /// <returns>A string prefix like [Outer > Inner], or empty if no scopes are active.</returns>
        private string FormatScopePrefix()
        {
            if (ScopeStack.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(string.Join(" > ", ScopeStack.Reverse()));
            sb.Append("] ");
            return sb.ToString();
        }

        /// <summary>
        /// A disposable scope handle that removes the topmost scope on <see cref="Dispose"/>.
        /// Used by <see cref="BeginScope{TState}"/>.
        /// </summary>
        private class ScopePopper : IDisposable
        {
            /// <summary>Removes the most recent scope from the scope stack.</summary>
            public void Dispose()
            {
                if (ScopeStack.Count > 0)
                    ScopeStack.Pop();
            }
        }
    }
}
