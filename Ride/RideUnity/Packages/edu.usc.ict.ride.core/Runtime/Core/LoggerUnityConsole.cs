using System;
using System.Collections.Generic;
using System.Linq;
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
    public class LoggerUnityConsole : LoggerUnity
    {
        // Thread-local scope stack to support nested scopes per thread
        [ThreadStatic]
        private static Stack<string> _scopeStack;
        private static Stack<string> ScopeStack => _scopeStack ??= new Stack<string>();


        /// <inheritdoc/>
        [HideInCallstack]
        public override void Log(LogEntry entry)
        {
            if (!ShouldLog(entry))
                return;

            string formatted = FormatEntry(entry);

            switch (entry.type)
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

            return $"[{string.Join(" > ", ScopeStack.Reverse())}] ";
        }

        protected override bool ShouldLog(LogEntry entry)
        {
            if (!IsEnabled(entry.type))
                return false;

            if (entry.source == LogSources.UnityLogBridgeSystem && !ReceiveEngineLogMessages)
                return false;

            return base.ShouldLog(entry);
        }

        protected override string FormatEntry(LogEntry entry)
        {
            string scope = FormatScopePrefix();
            if (entry.isEvent)
                return entry.eventData != null
                    ? $"{scope}{entry.eventName} {entry.eventData}"
                    : $"{scope}{entry.eventName}";

            return $"{scope}{entry.message}";
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
