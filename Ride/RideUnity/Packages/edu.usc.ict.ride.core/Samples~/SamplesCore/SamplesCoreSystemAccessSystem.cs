using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreSystemAccessSystem : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        SystemAccessSystem m_systemAccessSystem;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_systemAccessSystem = Globals.api.GetSystem<SystemAccessSystem>();
        }

        public void OnGUISystemAccess()
        {
            m_debugMenu.Label($"SystemAccessSystem.GetSystem<SystemAccessSystem>():");
            var testSystemAccess = m_systemAccessSystem.GetSystem<SystemAccessSystem>();
            m_debugMenu.Label(testSystemAccess == null ? "Fail" : "Success");
        }
    }
}
