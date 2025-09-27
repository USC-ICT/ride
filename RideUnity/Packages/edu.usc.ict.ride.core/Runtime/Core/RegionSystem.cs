using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Default implementation of <see cref="IRegionSystem"/> that tracks agents and groups inside 2D polygonal regions.
    /// These regions are defined by <see cref="UnityRegion"/> components and registered at runtime on startup.
    /// </summary>
    /// <remarks>
    /// This system supports visualization, agent/group monitoring, and dispatches <c>RegionChangeEvent</c>
    /// messages on entry/exit transitions. Region definitions are stored in local-space 2D polygons
    /// and matched using a point-in-polygon test.
    /// </remarks>
    public class RegionSystem : RideSystemMonoBehaviour, IRegionSystem
    {
        public ConvertToRide[] MonitoredAgents; 

        Dictionary<RideID, RideVector2[]> m_regions = new Dictionary<RideID, RideVector2[]>();
        Dictionary<RideID, RegionType> m_regionTypes = new Dictionary<RideID, RegionType>();
        List<RideID> m_monitoredAgents = new List<RideID>();
        List<RideID> m_monitoredGroups = new List<RideID>();
        Dictionary<RideID,List<RideID>> m_occupying_agents = new Dictionary<RideID, List<RideID>>();
        Dictionary<RideID,List<RideID>> m_occupying_groups = new Dictionary<RideID, List<RideID>>();

        protected override void Start()
        {
            base.Start();
            StartCoroutine(Setup());
        }

        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            foreach (RideID region in m_regions.Keys)
            {
                foreach (RideID agent in m_monitoredAgents)
                {
                    if (PositionInRegion(Systems.Agent.GetAgentPosition(agent), region))
                    {
                        // If agent's not already registered, add and dispatch world event
                        if (!m_occupying_agents[region].Contains(agent))
                        {
                            m_occupying_agents[region].Add(agent);
                            var rce = new WorldState.RegionChangeEvent(agent, region);
                            Systems.WorldState.DispatchEvent("agentEnterRegion", rce);
                        }
                    }
                    else
                    {
                        // If agent is registered, remove
                        if (m_occupying_agents[region].Contains(agent))
                        {
                            m_occupying_agents[region].Remove(agent);
                            var rce = new WorldState.RegionChangeEvent(agent, region);
                            Systems.WorldState.DispatchEvent("agentExitRegion", rce);
                        }
                    }
                }

                foreach (RideID group in m_monitoredGroups)
                {
                    if (PositionInRegion(ComputeAverageMemberPosition(group), region))
                    {
                        // If group not already registered, add and dispatch enter event
                        if (!m_occupying_groups[region].Contains(group))
                        {
                            m_occupying_groups[region].Add(group);
                            var ere = new WorldState.RegionChangeEvent(group, region);
                            Systems.WorldState.DispatchEvent("groupEnterRegion", ere);
                        }
                    }
                    else
                    {
                        // If group is regestered, remove
                        if (m_occupying_groups[region].Contains(group))
                        {
                            m_occupying_groups[region].Remove(group);
                            var ere = new WorldState.RegionChangeEvent(group, region);
                            Systems.WorldState.DispatchEvent("groupExitRegion", ere);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a new region in the system using the given polygon shape.
        /// </summary>
        /// <param name="points">Ordered array of 2D points defining the region boundary in local space.</param>
        /// <returns>The generated <see cref="RideID"/> of the new region.</returns>
        public RideID AddRegion(RideVector2[] points) => AddRegion(points, RegionType.Generic);

        public RideID AddRegion(RideVector2[] points, RegionType type)
        {
            RideID id = IdentityFactory.CreateId();
            m_regions.Add(id, points);
            m_regionTypes.Add(id, type);
            m_occupying_agents.Add(id, new List<RideID>());
            m_occupying_groups.Add(id, new List<RideID>());
            return id;
        }

        public void RemoveRegion(RideID id)
        {
            m_regions.Remove(id);
            m_occupying_agents.Remove(id);
            m_occupying_groups.Remove(id);
        }

        public RideVector2[] GetRegionPoints(RideID id)
        {
            if (m_regions.TryGetValue(id, out var region))
                return region;

            RideLog.Log($"Failed to find region with id {id}");
            return Array.Empty<RideVector2>();
        }

        public IEnumerable<RideID> GetRegions() => m_regions.Keys;

        public IEnumerable<RideID> GetRegionsByPosition(RideVector2 position) => GetRegionsByPositionInternal(position.x, position.y);

        public IEnumerable<RideID> GetRegionsByPosition(RideVector3 position) => GetRegionsByPositionInternal(position.x, position.z);

        public bool PositionInRegion(RideVector3 point, RideID id) => RideMath.PointInPolygon(point.x, point.z, GetRegionPoints(id));

        public bool PositionInRegion(RideVector2 point, RideID id) => RideMath.PointInPolygon(point.x, point.y, GetRegionPoints(id));

        public void MonitorAgent(RideID agent) => m_monitoredAgents.Add(agent);

        public void MonitorGroup(RideID group) => m_monitoredGroups.Add(group);

        public void RemoveAgent(RideID agent) => m_monitoredAgents.Remove(agent); // Also remove from m_occupyingAgents values?

        public void RemoveGroup(RideID group) => m_monitoredGroups.Remove(group); // Also remove from m_occupyingGroups values?

        public void RemoveAllAgents()
        {
            m_monitoredAgents.Clear();
            m_monitoredGroups.Clear();
        }

        private RideVector3 ComputeAverageMemberPosition(RideID groupID)
        {
            RideVector3 result = RideVector3.zero;
            var membersEnumerable = Systems.Group.GetMembers(groupID, true, true);
            var members = membersEnumerable as IList<RideID> ?? membersEnumerable.ToList();
            foreach (var member in members)
                result += Systems.Agent.GetAgentPosition(member);

            if (members.Count == 0)
                return RideVector3.zero;

            return result / members.Count;
        }

        private List<RideID> GetRegionsByPositionInternal(float x, float y)
        {
            var result = new List<RideID>();
            foreach (var kvp in m_regions)
            {
                if (RideMath.PointInPolygon(x,y,kvp.Value))
                    result.Add(kvp.Key);
            }

            return result;
        }

        public RegionType GetRegionType(RideID id)
        {
            if (m_regionTypes.TryGetValue(id, out var type))
                return type;

            RideLog.Log($"Failed to find region type with id {id}");
            return RegionType.Generic;
        }

        public void SetRegion(RideID regionId, RideVector2[] region) => SetRegion(regionId, region, GetRegionType(regionId));

        public void SetRegion(RideID regionId, RideVector2[] region, RegionType type)
        {
            m_regions[regionId] = region;
            m_regionTypes[regionId] = type;
            m_occupying_agents[regionId] = new List<RideID>();
            m_occupying_groups[regionId] = new List<RideID>();
        }

        bool SystemReady()
        {
            return Systems.Access != null && Systems.Get<IRegionSystem>() != null;
        }

        bool EntitiesReady()
        {
            //if (MonitoredGroups == null) return false;
            //foreach (Entity.UnityGroup ug in MonitoredGroups) if (ug.rideID == RideID.Null) return false;

            if (MonitoredAgents == null)
                return false;

            foreach (var ctr in MonitoredAgents)
                if (!ctr.Converted)
                    return false;

            return true;
        }

        bool TerrainReady()
        {
#if false
            Terrain.TerrainLoader terrainLoader = GameObject.FindFirstObjectByType<Terrain.TerrainLoader>();
            if (terrainLoader ==  null) return true; // No terrain being loaded in this scene
            if (terrainLoader.IsTerrainLoaded) return true; // Terrain finished loading
            return false;
#else
            Debug.LogError($"RegionSystem.TerrainReady() - TODO - Ride Refactor");
            return false;
#endif
        }

        /// <summary>
        /// Initializes the region system by discovering all <see cref="UnityRegion"/> objects,
        /// registering them with runtime RideIDs, optionally drawing them, and attaching agent monitors.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        IEnumerator Setup()
        {
            // Wait until the system is ready
            yield return new WaitUntil(() => SystemReady());

            // Add the regions to the system
            var regions = FindObjectsByType<UnityRegion>(FindObjectsSortMode.None);
            foreach (UnityRegion ur in regions)
                ur.rideID = AddRegion(ur.m_points);

            // Wait until the entities are all ready
            yield return new WaitUntil(() => EntitiesReady());

            // Start tracking the entities in and out of the regions
            // foreach (Entity.UnityGroup ug in MonitoredGroups) this.MonitorGroup(ug.rideID);
            foreach (var ctr in MonitoredAgents)
                MonitorAgent(ctr.id);

            // Wait until the terrain is loaded
            yield return new WaitUntil(() => TerrainReady());

            // Visualize regions (should be done after terrain loaded)
            foreach (var ur in regions)
            {
                if (ur.m_visualize)
                {
                    var regionVizualizer = new UnityRegionVisualizer();
                    regionVizualizer.DrawRegion(ur.rideID, id, ur.m_color);
                }
            }
        }
    }
}
