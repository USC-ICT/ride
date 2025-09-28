using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace Ride.Timeline
{
    [DisplayName("Agent/Move To")]
    public class Clip_AgentMoveTo : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_AgentMoveTo : RideTimelineBehaviour
        {
            public string m_agentName;
            public Vector3 m_destinationPosition;
            public ExposedReference<GameObject> m_destinationObject;
            [HideInInspector] public GameObject m_internalDestinationObject = null;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                RideID agentID = m_manager.GetEntityID(m_agentName);

                Vector3 destination = (m_internalDestinationObject != null) ? GetDestinationPosition(m_internalDestinationObject.transform.position)
                                                                            : GetDestinationPosition(m_destinationPosition);

                Globals.api.movementSystem.MoveToPosition(agentID, destination);

                if(m_waitUntilFinished)
                {
                    bool destinationReached = false;
                    while (destinationReached == false)
                    {
                        Vector3 agentPostion = Globals.api.agentSystem.GetAgentPosition(agentID);
                        if (Vector3.Distance(agentPostion, destination) <= 2f)  //-2 is hard-coded value for approximate distance between agent prefab and point on the ground
                        {
                            destinationReached = true;
                        }
                        yield return new WaitForEndOfFrame();
                    }
                }

                m_isFinished = true;
                yield return null;
            }

            private Vector3 GetDestinationPosition(Vector3 position)
            {
                RideRay ray = new RideRay(position + new Vector3(0, 100f, 0), RideVector3.down);
                if (RideMath.Raycast(ray, out RideRaycastHit rayResult) == false) 
                    { Debug.LogWarning($"Clip_AgentMoveTo.cs: Invalid destination {position}"); return default; }
                
                return rayResult.point;
            }
        }

        [HideInInspector] public override string m_commandType { get => "AgentMoveTo"; }
        public Behaviour_AgentMoveTo m_behaviour = new Behaviour_AgentMoveTo();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_AgentMoveTo>.Create(graph, m_behaviour);
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
