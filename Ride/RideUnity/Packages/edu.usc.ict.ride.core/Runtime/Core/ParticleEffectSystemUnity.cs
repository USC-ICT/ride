using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Effects
{
    /// <summary>
    ///
    /// </summary>
    /// <inheritdoc cref="IParticleEffectSystem"/>
    /// <remarks>This System uses Unity's ParticleSystem https://docs.unity3d.com/ScriptReference/ParticleSystem.html</remarks>
    public class ParticleEffectSystemUnity : RideSystemMonoBehaviour, IParticleEffectSystem
    {
        /// <summary>
        /// Maps ride ids to unity particle effect systems
        /// </summary>
        Dictionary<RideID, ParticleSystem> m_pfx = new Dictionary<RideID, ParticleSystem>();

        public RideID Create(RideID pfx, RideVector3 position, RideQuaternion rotation)
        {
            RideID newPfx = Globals.api.gameObjectSystem.Create(pfx, position, rotation);
            AddParticleSystem(newPfx);
            return newPfx;
        }

        public RideID CreateFromScene(string pfxName) => CreateFromScene(pfxName, RideVector3.zero);

        public RideID CreateFromScene(string pfxName, RideVector3 position) => CreateFromScene(pfxName, position, RideQuaternion.identity);

        public RideID CreateFromScene(string pfxName, RideVector3 position, RideQuaternion rotation)
        {
            RideID pfx = Globals.api.gameObjectSystem.CreateFromScene(pfxName, position, rotation);
            AddParticleSystem(pfx);
            return pfx;
        }

        public RideID CreateFromResource(string pfxName) => CreateFromResource(pfxName, RideVector3.zero);

        public RideID CreateFromResource(string pfxName, RideVector3 position) => CreateFromResource(pfxName, position, RideQuaternion.identity);

        public RideID CreateFromResource(string pfxName, RideVector3 position, RideQuaternion rotation)
        {
            RideID pfx = Globals.api.gameObjectSystem.CreateFromResource(pfxName, position, rotation);
            AddParticleSystem(pfx);
            return pfx;
        }

        void AddParticleSystem(RideID pfx)
        {
            if (pfx != RideID.Null)
            {
                ParticleSystem system = Globals.api.componentSystem.GetComponent<ParticleSystem>(pfx);
                if (system != null)
                {
                    m_pfx.Add(pfx, system);
                }
                else
                {
                    RideLog.LogError($"{pfx} was created but the objet doesn't have a particle system on it");
                }
            }
        }

        ParticleSystem GetSystem(RideID pfx)
        {
            if (m_pfx.ContainsKey(pfx))
            {
                return m_pfx[pfx];
            }
            else
            {
                RideLog.LogError($"UnityParticleEffectSystem doesn't have pfx {pfx}");
                return null;
            }
        }

        public void Destroy(RideID pfx)
        {
            ParticleSystem system = GetSystem(pfx);
            if (system != null)
            {
                Globals.api.gameObjectSystem.Destroy(pfx);
                m_pfx.Remove(pfx);
            }
        }

        public void Pause(RideID pfx)
        {
            var system = GetSystem(pfx);
            if (system != null)
                system.Pause();
        }

        public void Play(RideID pfx)
        {
            var system = GetSystem(pfx);
            if (system != null)
                system.Play();
        }

        public void Stop(RideID pfx)
        {
            var system = GetSystem(pfx);
            if (system != null)
                system.Stop();
        }

        public bool IsPlaying(RideID pfx)
        {
            return GetSystem(pfx).isPlaying;
        }
    }
}
