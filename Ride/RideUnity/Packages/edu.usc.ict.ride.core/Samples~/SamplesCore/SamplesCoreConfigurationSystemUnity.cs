using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreConfigurationSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        ConfigurationSystemUnity m_configurationSystem;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_configurationSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
        }

        public void OnGUIConfiguration()
        {
            m_debugMenu.Label($"{m_configurationSystem.path}");

            if (m_configurationSystem.IsCorrectVersion())
            {
                m_debugMenu.Label($"{m_configurationSystem.config.version}");
            }
            else
            {
                m_debugMenu.Label($"<color=red>Config File Incorrect Version!</color>");
                m_debugMenu.Label($"Found: {m_configurationSystem.config.version}");
                m_debugMenu.Label($"Expected: {RideConfig.Default.version}");
            }

            if (RideUtils.IsWindows() || RideUtils.IsOSX())
            {
                if (m_debugMenu.Button("Open Folder Location"))
                {
                    if (RideUtils.IsWindows())
                        System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(m_configurationSystem.path));
                    else if (RideUtils.IsOSX())
                        System.Diagnostics.Process.Start("/usr/bin/open", string.Format(@"""{0}""", System.IO.Path.GetDirectoryName(m_configurationSystem.path)));
                }

                if (m_debugMenu.Button("Edit Config"))
                {
                    if (RideUtils.IsWindows())
                        System.Diagnostics.Process.Start("notepad.exe", m_configurationSystem.path);
                    else if (RideUtils.IsOSX())
                        System.Diagnostics.Process.Start("qlmanage", string.Format(@"-p ""{0}""", m_configurationSystem.path));
                }
            }

            if (m_configurationSystem.IsCorrectVersion())
            {
                m_debugMenu.Space();

                m_debugMenu.Label($"OWT Access Key: {m_configurationSystem.GetTerrainKey()}");
                m_debugMenu.Label($"AWS Region: {m_configurationSystem.GetTerrainKeyRegion()}");
            }
        }
    }
}
