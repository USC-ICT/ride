using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreDebugOnScreenLogVHAssets : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        DebugOnScreenLogVHAssets m_onScreenLog;


        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_onScreenLog = Globals.api.GetSystem<DebugOnScreenLogVHAssets>();
        }

        public void OnGUIDebugOnScreenLog()
        {
            // TODO - Ride Refactor - add function to interface
            var onScreenLog = m_debugMenu.Toggle(m_onScreenLog.m_log.IsShowing, "OnScreenDebugLog");
            if (onScreenLog != m_onScreenLog.m_log.IsShowing)
                m_onScreenLog.m_log.ShowLog(!m_onScreenLog.m_log.IsShowing);

            if (m_debugMenu.Button("Log"))
                Debug.Log("This is a Log Info Message");

            if (m_debugMenu.Button("Log Warning"))
                Debug.LogWarning("This is a Warning Message");

            if (m_debugMenu.Button("Log Error"))
                Debug.LogError("This is a Error Message");
        }
    }
}
