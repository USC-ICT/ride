using UnityEngine;
using UnityEngine.Playables;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Agent/Shoot Agent")]
    public class Clip_AgentShootAgent : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_AgentShootAgent : RideTimelineBehaviour
        {
            public string m_attackerName;
            public string m_attackeeName;
            public bool m_attackUntilDead;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                RideID attackerID = m_manager.GetEntityID(m_attackerName);
                RideID attackeeID = m_manager.GetEntityID(m_attackeeName);

                RideVector3 attackeePosition = Globals.api.agentSystem.GetAgentPosition(attackeeID);

                Globals.api.agentSystem.SetAgentLookAt(attackerID, attackeePosition);
                Globals.api.attackSystem.AttackAgent(attackerID, attackeeID);

                if (m_attackUntilDead == false)
                {
                    m_isFinished = true;
                    yield break;
                }
                
                while(Globals.api.agentSystem.IsAgentDead(attackeeID) == false)
                {
                    Globals.api.attackSystem.AttackAgent(attackerID, attackeeID);
                    yield return new WaitForEndOfFrame();
                }
                
                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "AgentShootAgent"; }
        public Behaviour_AgentShootAgent m_behaviour = new Behaviour_AgentShootAgent();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_AgentShootAgent>.Create(graph, m_behaviour);
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