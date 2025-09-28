using UnityEngine;
using Ride.IO;

namespace Ride.Samples
{
    /// <summary>
    /// A sample class for demonstrating virtual human (VH) debug menus and nonverbal behavior generation in RIDE.
    /// </summary>
    public class SamplesVH : RideMonoBehaviour
    {
        [SerializeField] SamplesVHNonverbalBehaviorGeneratorSystem m_nvbg;
        [SerializeField] SamplesVHSIMA m_sima;
        [SerializeField] SamplesBMLParser m_bml;
        private DebugMenu m_debugMenu;
        private AssetLoadingSystemAssetBundles m_assetBundleSystem;


        /// <summary>
        /// Initializes the debug menu and sets up debug menu gui.
        /// </summary>
        protected override void Start()
        {
            //Caching.ClearCache();

            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_assetBundleSystem = Globals.api.GetSystem<AssetLoadingSystemAssetBundles>();

            m_debugMenu.InsertMenu(0, "SamplesVH", OnGUISamplesVH);
            m_debugMenu.InsertMenu(1, "Nonverbal Behavior Generation", m_nvbg.OnGUINonverbalBehaviorGeneration);
            m_debugMenu.InsertMenu(2, "Nonverbal Behavior Generation", m_nvbg.OnGUIHeadControl);
            m_debugMenu.InsertMenu(3, "SIMA", m_sima.OnGUINonverbalBehaviorGeneration);
            m_debugMenu.InsertMenu(4, "BML", m_bml.OnGUIBMLParser);

            m_debugMenu.SetMenu(0);
            m_debugMenu.ShowMenu(true);
            m_debugMenu.SetMenuSize(0, 0, 0.3f, 1f);
            m_debugMenu.SetWideMenuSize(0, 0, 0.4f, 1f);

            StartCoroutine(m_assetBundleSystem.LoadCachedCatalogs());
        }


        /// <summary>
        /// Handle input to quit the application or toggle the debug menu.
        /// </summary>
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


        /// <summary>
        /// Draw VH debug menu tab.
        /// </summary>
        void OnGUISamplesVH()
        {
            m_debugMenu.Label($"RIDE VH Sample");
            m_debugMenu.Space();
            m_debugMenu.Label($"<b>Use the arrows above to scroll through the different tabs</b>");
            m_debugMenu.Label($"<b>Use the '<>' button to change the menu width</b>");
            m_debugMenu.Label($"<b>Use F11 key to toggle this menu</b>");
            m_debugMenu.Space();
            m_debugMenu.Space();

            if (m_debugMenu.Button("Hide Window"))
                m_debugMenu.ToggleMenu();
        }
    }
}
