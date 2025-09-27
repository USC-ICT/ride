using UnityEngine;
using Ride;
using Ride.Terrain.Navigation;

namespace Ride.Samples
{
    public class SamplesCoreNavigationSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        NavigationSystemUnity m_navigationSystem;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_navigationSystem = Globals.api.GetSystem<NavigationSystemUnity>();
        }

        public void OnGUINavigation()
        {
            // setup scene, add plane, cube
            // build nav mesh
            // walk to random points on plane
        }
    }
}
