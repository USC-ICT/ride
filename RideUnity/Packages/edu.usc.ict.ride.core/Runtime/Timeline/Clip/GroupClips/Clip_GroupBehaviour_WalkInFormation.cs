using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Ride.Movement;

namespace Ride.Timeline
{
    [DisplayName("Group/Walk in Formation")]
    public class Clip_GroupBehaviour_WalkInFormation : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_WalkInFormation : RideTimelineBehaviour
        {
            public string m_groupName;
            public Vector3 m_destination;
            public FormationProcedureType m_formationType;

            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                RideRay ray = new RideRay(m_destination + new Vector3(0, 1000f, 0), RideVector3.down);
                if (!RideMath.Raycast(ray, out RideRaycastHit raycast))
                {
                    Debug.LogWarning($"Clip_AgentSpawn.cs: Raycast couldn't get valid spawn position {m_destination}");
                    yield break;
                }

                RideID groupID = m_manager.GetEntityID(m_groupName);
                Globals.api.movementSystem.MoveToPositionInFormation(groupID, raycast.point, m_formationType);

                if(m_waitUntilFinished == false) { m_isFinished = true; yield break; }

                bool destinationReached = false;
                while (destinationReached == false)
                {
                    RideID leaderID = Globals.api.groupSystem.GetGroupLeader(groupID);
                    Vector3 leaderPosition = Globals.api.agentSystem.GetAgentPosition(leaderID);
                    if (Vector3.Distance(leaderPosition, raycast.point) <= 3f)  //--3 is hard-coded value for approximate distance between agent prefab and point on the ground
                    {
                        destinationReached = true;
                    }
                    yield return new WaitForEndOfFrame();
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "GroupMoveInFormation"; }
        public Behaviour_WalkInFormation m_behaviour = new Behaviour_WalkInFormation();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_WalkInFormation>.Create(graph, m_behaviour);
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
