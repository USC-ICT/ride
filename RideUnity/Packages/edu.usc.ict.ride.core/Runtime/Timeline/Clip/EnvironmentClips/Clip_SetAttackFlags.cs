using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Ride.Combat;

namespace Ride.Timeline
{
    [DisplayName("Environment/Set Attack Flag")]
    public class Clip_SetAttackFlags : RideTimelineClip
    {
        public enum AttackFlags
        {
            TurnOffAutoAttack,
        }

        [Serializable]
        public class Behaviour_SetAttackFlags : RideTimelineBehaviour
        {
            public AttackFlags m_options;


            public override void ProcessBehaviour()
            {
                switch(m_options)
                {
                    case AttackFlags.TurnOffAutoAttack:
                        {
                            Globals.api.attackSystem.flags &= ~AttackSystemFlags.Acquire_Targets;
                            Globals.api.attackSystem.flags &= ~AttackSystemFlags.Auto_Attack;
                            break;
                        }
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "EnvironmentSetAttackFlag"; }
        public Behaviour_SetAttackFlags m_behaviour = new Behaviour_SetAttackFlags();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_SetAttackFlags>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            string uniqueLabel = GetUniqueLabel(m_commandType, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
