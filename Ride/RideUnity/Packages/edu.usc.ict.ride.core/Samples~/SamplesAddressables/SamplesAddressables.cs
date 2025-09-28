using UnityEngine;
using Ride;
using Ride.IO;

namespace Ride.Samples
{
    public class SamplesAddressables : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();

            m_debugMenu.InsertMenu( 0, "SamplesAddressables", OnGUISamplesAddressables);

            m_debugMenu.SetMenu(0);
            m_debugMenu.ShowMenu(true);
            m_debugMenu.SetMenuSize(0, 0, 0.3f, 1f);
            m_debugMenu.SetWideMenuSize(0, 0, 0.4f, 1f);
        }

        protected override void Update()
        {
            if (Globals.api.inputSystem.GetKeyDown(RideKeyCode.Escape))
            {
                RideUtils.QuitApplication();
            }

            if (Globals.api.inputSystem.GetKeyDown(RideKeyCode.F11))
            {
                m_debugMenu.ToggleMenu();
            }
        }

        void OnGUISamplesAddressables()
        {
            m_debugMenu.Label($"RIDE Core Sample");
            m_debugMenu.Space();
            m_debugMenu.Label($"<b>Use the arrows above to scroll through the different tabs</b>");
            m_debugMenu.Label($"<b>Use the '<>' button to change the menu width</b>");
            m_debugMenu.Label($"<b>Use F11 key to toggle this menu</b>");
            m_debugMenu.Space();
            m_debugMenu.Space();

            if (m_debugMenu.Button("Hide Window"))
                m_debugMenu.ToggleMenu();

            m_debugMenu.Space();
            m_debugMenu.Space();
            m_debugMenu.Label($"// TODO");
            m_debugMenu.Label($"// Instructions on how to build addressables in an empty Unity project");
            m_debugMenu.Label($"// - add prefab to scene");
            m_debugMenu.Label($"// - mark it as addressables");
            m_debugMenu.Label($"// - open Addressables Groups window");
            m_debugMenu.Label($"// - add a label");
            m_debugMenu.Label($"// - choose catalog name / location");
            m_debugMenu.Label($"// - Build Addressables menu option");
            m_debugMenu.Label($"// - at runtime, button to load / Instantiate prefab just created");
        }
    }
}
