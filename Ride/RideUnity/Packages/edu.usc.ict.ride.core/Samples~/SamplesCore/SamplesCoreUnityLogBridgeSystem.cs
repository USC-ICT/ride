using System;
using System.Collections;
using UnityEngine;

namespace Ride.Samples
{
    public class SamplesCoreApplicationLogMessageSystem : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        ApplicationLogMessageSystem m_applicationLogMessage;
        string m_receivedMessage;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_applicationLogMessage = Globals.api.GetSystem<ApplicationLogMessageSystem>();

            m_applicationLogMessage.AddCallback((condition, stackTrace, type) => StartCoroutine(OnLogMessageReceivedCoroutine(condition, stackTrace, type)));
        }

        public void OnGUIApplicationLogMessage()
        {
            if (m_debugMenu.Button("Log"))
                Debug.Log("This is a Log Info Message");

            if (m_debugMenu.Button("Log Warning"))
                Debug.LogWarning("This is a Warning Message");

            if (m_debugMenu.Button("Log Error"))
                Debug.LogError("This is a Error Message");

            m_debugMenu.Label("<b>Message Received:</b>");
            m_debugMenu.Label(m_receivedMessage);
        }

        IEnumerator OnLogMessageReceivedCoroutine(string message, string stackTrace, IApplicationLogMessageSystem.LogType type)
        {
            string prefix = "";
            string postfix = "";
            switch (type)
            {
                case IApplicationLogMessageSystem.LogType.Error:     prefix = "<color=red>"; postfix = "</color>"; break;
                case IApplicationLogMessageSystem.LogType.Assert:    prefix = "<color=red>"; postfix = "</color>"; break;
                case IApplicationLogMessageSystem.LogType.Warning:   prefix = "<color=yellow>"; postfix = "</color>"; break;
                case IApplicationLogMessageSystem.LogType.Log:       prefix = ""; postfix = ""; break;
                case IApplicationLogMessageSystem.LogType.Exception: prefix = "<color=red>"; postfix = "</color>"; break;
            }

            m_receivedMessage = $"{prefix}{message}{postfix}";

            yield return new WaitForSeconds(2);

            m_receivedMessage = "";
        }
    }
}
