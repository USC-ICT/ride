using UnityEngine;
using Ride;
using Ride.Effects;

namespace Ride.Samples
{
    public class SamplesCoreParticleEffectSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        ParticleEffectSystemUnity m_particleEffectSystem;

        RideID m_particleId;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_particleEffectSystem = Globals.api.GetSystem<ParticleEffectSystemUnity>();
        }

        public void OnGUIParticleEffects()
        {
            if (m_debugMenu.Button("Create From Resource"))
                m_particleId = m_particleEffectSystem.CreateFromResource("SampleParticleSystem");

            if (m_debugMenu.Button("Create From Scene Object"))
                m_particleId = m_particleEffectSystem.CreateFromScene("SampleParticleSystem");

            if (m_debugMenu.Button("Destroy"))
            {
                if (m_particleId != RideID.Null)
                {
                    m_particleEffectSystem.Destroy(m_particleId);
                    m_particleId = RideID.Null;
                }
            }

            m_debugMenu.Space();

            if (m_debugMenu.Button("Play"))
                if (m_particleId != RideID.Null)
                    m_particleEffectSystem.Play(m_particleId);

            if (m_debugMenu.Button("Pause"))
                if (m_particleId != RideID.Null)
                    m_particleEffectSystem.Pause(m_particleId);

            if (m_debugMenu.Button("Stop"))
                if (m_particleId != RideID.Null)
                    m_particleEffectSystem.Stop(m_particleId);

            if (m_particleId != RideID.Null)
            {
                m_debugMenu.Space();

                bool isPlaying = m_particleEffectSystem.IsPlaying(m_particleId);
                m_debugMenu.Label($"Is Playing: {(isPlaying ? "Yes" : "No")}");
            }
        }
    }
}
