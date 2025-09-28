using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreLogSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        LogSystemUnity m_logSystem;


        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_logSystem = Globals.api.GetSystem<LogSystemUnity>();
        }

        public void OnGUILogSystem()
        {
            // TODO - show loggers currently added to the system

            if (m_debugMenu.Button("Log"))
                m_logSystem.Log("This is a Log Info Message");

            if (m_debugMenu.Button("Log Warning"))
                m_logSystem.Log(LogType.Warning, "This is a Warning Message");

            if (m_debugMenu.Button("Log Error"))
                m_logSystem.Log(LogType.Error, "This is a Error Message");
        }
    }
}
