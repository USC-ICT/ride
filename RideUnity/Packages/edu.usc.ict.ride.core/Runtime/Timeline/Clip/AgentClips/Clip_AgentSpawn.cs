using UnityEngine;
using UnityEngine.Playables;
using System;
using System.ComponentModel;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace Ride.Timeline
{
    [DisplayName("Agent/Spawn")]
    public class Clip_AgentSpawn : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_AgentSpawn : RideTimelineBehaviour
        {
            public Vector3 m_location;
            public Vector3 m_rotation;
            public string m_agentName;
            public float m_skillLevel;
            public Team m_team;
            public bool m_waitUntiFinished;
            public override bool waitUntilFinished { get => m_waitUntiFinished; }

            public override void ProcessBehaviour()
            {
                RideRay ray = new RideRay(m_location + new Vector3(0, 1000f, 0), RideVector3.down);
                if (!RideMath.Raycast(ray, out RideRaycastHit raycast))
                {
                    Debug.LogWarning($"Clip_AgentSpawn.cs: Raycast couldn't get valid spawn position {m_location}");
                    return;
                }

                //--Create agent from prefab and register to RIDE
                //IGameObjectSystem objectSystem = Globals.api.GetSystem<IGameObjectSystem>();
                string prefabName = (m_team == Team.Blue) ? "ChrUsaArmyInfantryAcu01PrefabDataMono" : "ChrIrqInsurgentMleAdultAvg01PrefabDataMono";

                //--Configure agent property
                RideID rideID = Globals.api.gameObjectSystem.CreateFromResource(prefabName, raycast.point, RideQuaternion.Euler(m_rotation));
                Globals.api.agentSystem.SetAgentName(rideID, m_agentName);
                Globals.api.agentSystem.SetAgentSkill(rideID, m_skillLevel);


                //--Register entity information to Timeline Manager
                EntityData entityData = new EntityData(rideID, EntityHierarchy.Agent, m_team, m_agentName);
                m_manager.m_entityDictionary.Add(m_agentName, entityData);
            }
        }

        [HideInInspector] public override string m_commandType { get => "AgentSpawn"; }
        public Behaviour_AgentSpawn m_behaviour = new Behaviour_AgentSpawn();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_AgentSpawn>.Create(graph, m_behaviour);
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
