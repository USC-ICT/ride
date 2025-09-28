using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Mount/Spawn")]
    public class Clip_MountSpawn : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_MountSpawn : RideTimelineBehaviour
        {
            public MountTypes m_types;
            public string m_mountName;
            public Vector3 m_location;
            public Vector3 m_rotation;

            public override void ProcessBehaviour()
            {
                RideRay ray = new RideRay(m_location + new Vector3(0, 1000f, 0), RideVector3.down);
                if (!RideMath.Raycast(ray, out RideRaycastHit raycast))
                {
                    Debug.LogWarning($"Clip_MountSpawn::Raycast couldn't get valid spawn position {m_location}");
                    return;
                }
                Vector3 spawnPosition = raycast.point;

                string prefabName = string.Empty;
                switch (m_types)
                {
                    case MountTypes.M4A1Sherman:
                        prefabName = "M1A2AbramsDataMono";
                        break;
                    case MountTypes.M777:
                        prefabName = "M777";
                        break;
                    case MountTypes.Humvee:
                        prefabName = "TempHumvee";
                        break;
                }

                //IGameObjectSystem objectSystem = Globals.api.GetSystem<IGameObjectSystem>();
                RideID rideID = Globals.api.gameObjectSystem.CreateFromResource(prefabName, spawnPosition, RideQuaternion.Euler(m_rotation));

                //--Register entity information to Timeline Manager
                EntityData entityData = new EntityData(rideID, EntityHierarchy.Mount, Team.Blue, m_mountName);
                m_manager.m_entityDictionary.Add(m_mountName, entityData);

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "MountSpawn"; }
        public Behaviour_MountSpawn m_behaviour = new Behaviour_MountSpawn();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_MountSpawn>.Create(graph, m_behaviour);
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