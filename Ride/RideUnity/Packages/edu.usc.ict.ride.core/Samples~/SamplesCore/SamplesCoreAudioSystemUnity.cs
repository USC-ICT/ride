using UnityEngine;
using Ride;
using Ride.Audio;

namespace Ride.Samples
{
    public class SamplesCoreAudioSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        AudioSystemUnity m_audioSystem;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_audioSystem = Globals.api.GetSystem<AudioSystemUnity>();
        }

        public void OnGUIAudioSystem()
        {
            if (m_debugMenu.Button("Play Audio Clip"))
                m_audioSystem.Play("sample-3s");

            // TODO - Ride Refactor - add function to interface
            m_debugMenu.Space();
            m_debugMenu.Label("<b>Available AudioClips:</b>");
            //var resources = m_resourceLoader.GetAllResourceObjects();
            //foreach (var resource in resources)
            //    m_debugMenu.Label(resource.name);
        }
    }
}
