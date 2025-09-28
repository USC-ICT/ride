using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Ride.Entities;
using Ride.Movement;

namespace Ride.Timeline
{
    [DisplayName("Group/Spawn")]
    public class Clip_GroupSpawn : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_GroupSpawn : RideTimelineBehaviour
        {
            public EntityHierarchy m_hierarchy;
            public string m_groupName;
            public string m_prefabName = "ChrUsaArmyInfantryAcu01PrefabDataMono";
            public Vector3 m_location;
            public Vector3 m_rotation;
            public Team m_team;

            public override void ProcessBehaviour()
            {
                RideRay ray = new RideRay(m_location + new Vector3(0, 1000f, 0), RideVector3.down);
                if (!RideMath.Raycast(ray, out RideRaycastHit raycast))
                {
                    Debug.LogWarning($"Clip_GroupSpawn::Raycast couldn't get valid spawn position {m_location}");
                    return;
                }
                Vector3 spawnPosition = raycast.point;

                //--Configure agent property
                var groupParam = new UnitCreationParams(m_groupName, m_prefabName)
                {
                    position = spawnPosition,
                    rotation = Quaternion.Euler(m_rotation),
                    team = m_team
                };

                RideID rideID = RideID.Null;
                switch (m_hierarchy)
                {
                    case EntityHierarchy.Fireteam:
                        rideID = Globals.api.GetSystem<IUnitCreatorSystem>().CreateFireTeam(groupParam);
                        //rideID = Globals.api.agentSystem.CreateFireTeam(groupParam);
                        break;
                    case EntityHierarchy.Squad:
                        rideID = Globals.api.GetSystem<IUnitCreatorSystem>().CreateSquad(groupParam);
                        //rideID = Globals.api.agentSystem.CreateSquad(groupParam);
                        break;
                    case EntityHierarchy.Company:
                        rideID = Globals.api.GetSystem<IUnitCreatorSystem>().CreateCompany(groupParam);
                        //rideID = Globals.api.agentSystem.CreateCompany(groupParam);
                        break;
                    case EntityHierarchy.Platoon:
                        rideID = Globals.api.GetSystem<IUnitCreatorSystem>().CreatePlatoon(groupParam);
                        //rideID = Globals.api.agentSystem.CreatePlatoon(groupParam);
                        break;
                }

                //--Register entity information to Timeline Manager
                EntityData entityData = new EntityData(rideID, m_hierarchy, m_team, m_groupName);
                m_manager.m_entityDictionary.Add(m_groupName, entityData);

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "GroupSpawn"; }
        public Behaviour_GroupSpawn m_behaviour = new Behaviour_GroupSpawn();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_GroupSpawn>.Create(graph, m_behaviour);
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
