using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using Ride.Entities;

namespace Ride.Timeline
{
    [DisplayName("Agent/Set Posture")]
    public class Clip_AgentSetPosture : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_AgetnSetPosture: RideTimelineBehaviour
        {
            public string m_agentName;
            public AgentPosture m_posture;

            public override void ProcessBehaviour()
            {
                RideID agentID = m_manager.GetEntityID(m_agentName);

                Globals.api.agentSystem.SetAgentPosture(agentID, m_posture);
                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "AgentSetPosture"; }
        public Behaviour_AgetnSetPosture m_behaviour = new Behaviour_AgetnSetPosture();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_AgetnSetPosture>.Create(graph, m_behaviour);
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