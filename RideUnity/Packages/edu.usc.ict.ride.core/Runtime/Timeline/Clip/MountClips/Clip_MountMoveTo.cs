using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Mount/MoveTo")]
    public class Clip_MountMoveTo : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_MountMoveTo : RideTimelineBehaviour
        {
            public string m_mountName;
            public Vector3 m_destination;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                RideRay ray = new RideRay(m_destination + new Vector3(0, 1000f, 0), RideVector3.down);
                if (!RideMath.Raycast(ray, out RideRaycastHit raycast))
                {
                    Debug.LogWarning($"Clip_MountMoveTo::Couldn't get valid destination position {m_destination}");
                    yield break; ;
                }
                Vector3 destinationPosition = raycast.point;

                RideID mountID = m_manager.GetEntityID(m_mountName);
                Globals.api.movementSystem.MoveToPosition(mountID, destinationPosition);

                if (m_waitUntilFinished)
                {
                    bool destinationReached = false;
                    while (destinationReached == false)
                    {
                        Vector3 mountPosition = Globals.api.agentSystem.GetAgentPosition(mountID);
                        if (Vector3.Distance(mountPosition, destinationPosition) <= 2f)  //-2 is hard-coded value for approximate distance between agent prefab and point on the ground
                        {
                            destinationReached = true;
                        }
                        yield return new WaitForEndOfFrame();
                    }
                }

                m_isFinished = true;
                yield return null;
            }
        }

        [HideInInspector] public override string m_commandType { get => "MountMoveTo"; }
        public Behaviour_MountMoveTo m_behaviour = new Behaviour_MountMoveTo();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_MountMoveTo>.Create(graph, m_behaviour);
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