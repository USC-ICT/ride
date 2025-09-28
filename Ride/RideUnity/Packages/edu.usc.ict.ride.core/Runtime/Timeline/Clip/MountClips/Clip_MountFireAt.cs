using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Mount/Fire at")]
    public class Clip_MountFireAt : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_MountFireAt : RideTimelineBehaviour
        {
            public ExposedReference<GameObject> m_targetObject;
            [HideInInspector] public GameObject m_internalTargetObject;
            public string m_mountName;

            public override void ProcessBehaviour()
            {
                RideID mountID = m_manager.GetEntityID(m_mountName);
                RideVector3 targetPosition = m_internalTargetObject.transform.position;

                Globals.api.attackSystem.StopAimingAtTarget(mountID);
                Globals.api.attackSystem.FireProjectile(mountID, targetPosition, Globals.api.agentSystem.GetPrimaryWeapon(mountID));
            }
        }

        [HideInInspector] public override string m_commandType { get => "MountFireAt"; }
        public Behaviour_MountFireAt m_behaviour = new Behaviour_MountFireAt();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalTargetObject = m_behaviour.m_targetObject.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_MountFireAt>.Create(graph, m_behaviour);
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

