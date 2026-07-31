using UnityEngine;
using Ride;
using Ride.IO;

namespace Ride.Samples
{
    public class SamplesCore : RideMonoBehaviour
    {
        public SamplesCoreSystemAccessSystem m_systemAccessSystem;
        public SamplesCoreGameObjectSystemUnity m_gameObjectSystem;
        public SamplesCoreConfigurationSystemUnity m_configurationSystem;
        public SamplesCoreResourceLoaderSystem m_resourceLoader;
        public SamplesCoreLogSystemUnity m_logSystem;
        public SamplesCoreUnityLogBridgeSystem m_unityLogBridgeSystem;
        public SamplesCoreFramesPerSecondCounter m_framesPerSecondCounter;
        public SamplesCoreDebugOnScreenLogVHAssets m_debugOnScreenLog;
        public SamplesCoreAudioSystemUnity m_audioSystem;
        public SamplesCoreShaderSystemUnity m_shaderSystem;
        public SamplesCoreParticleEffectSystemUnity m_particleEffectSystem;
        public SamplesCoreNavigationSystemUnity m_navigationSystem;
        public SamplesCoreWebRequesterSystemUnity m_webRequester;
        public SamplesCoreAWSFileStorageS3System m_awsFileStorageS3System;
        public SamplesCoreAddressableSystem m_rideAddressableSystem;
        public SamplesCoreRideAssetBundle m_rideAssetBundleSystem;

        DebugMenu m_debugMenu;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();

            m_debugMenu.InsertMenu( 0, "SamplesCore", OnGUISamplesCore);
            m_debugMenu.InsertMenu( 1, "SystemAccess", m_systemAccessSystem.OnGUISystemAccess);
            m_debugMenu.InsertMenu( 2, "GameObject", m_gameObjectSystem.OnGUIGameObject);
            m_debugMenu.InsertMenu( 3, "Configuration", m_configurationSystem.OnGUIConfiguration);
            m_debugMenu.InsertMenu( 4, "ResourceLoader", m_resourceLoader.OnGUIResourceLoader);
            m_debugMenu.InsertMenu( 5, "Log", m_logSystem.OnGUILogSystem);
            m_debugMenu.InsertMenu( 6, "UnityLogBridge", m_unityLogBridgeSystem.OnGUIUnityLogBridge);
            m_debugMenu.InsertMenu( 7, "FramePerSecond", m_framesPerSecondCounter.OnGUIFramesPerSecond);
            m_debugMenu.InsertMenu( 8, "DebugOnScreenLog", m_debugOnScreenLog.OnGUIDebugOnScreenLog);
            m_debugMenu.InsertMenu( 9, "AudioSystem", m_audioSystem.OnGUIAudioSystem);
            m_debugMenu.InsertMenu(10, "ShaderSystem", m_shaderSystem.OnGUIShaderSystem);
            m_debugMenu.InsertMenu(11, "ParticleEffects", m_particleEffectSystem.OnGUIParticleEffects);
            m_debugMenu.InsertMenu(12, "Navigation", m_navigationSystem.OnGUINavigation);
            m_debugMenu.InsertMenu(13, "WebRequester", m_webRequester.OnGUIWebRequester);
            m_debugMenu.InsertMenu(14, "AWSFileStorageS3", m_awsFileStorageS3System.OnGUIAWSFileStorageS3);            
            m_debugMenu.InsertMenu(15, "RideAssetBundle", m_rideAssetBundleSystem.OnGUIRideAssetBundle);

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

        void OnGUISamplesCore()
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
        }
    }
}
