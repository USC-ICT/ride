using System.Collections.Generic;

namespace Ride
{
    public enum RegionType
    {
        Generic = 0,
        KillZone = 1,
    }

    /// <summary>
    /// Interface for system for defining and monitoring regions of interest
    /// </summary>
    public interface IRegionSystem : IRideSystem
    {
        /// <summary>
        /// Specifies a new polygonal region
        /// </summary>
        /// <param name="region">ordered array of points that specify the verticies of the region</param>
        /// <returns>a generated RideID for the region </returns>
        RideID AddRegion(RideVector2[] region);

        /// <summary>
        /// Specifies a new polygonal region
        /// </summary>
        /// <param name="region">ordered array of points that specify the verticies of the region</param>
        /// <param name="type">type of the region</param>
        /// <returns>a generated RideID for the region </returns>
        RideID AddRegion(RideVector2[] region, RegionType type);

        /// <summary>
        /// Remove a region from those that are defined by this region system
        /// </summary>
        /// <param name="region">The RideID of a region previously added to this system</param>
        void RemoveRegion(RideID region);

        /// <summary>
        /// Get the points that define a region identified by its RideID
        /// </summary>
        /// <param name="id">The RideID of the region</param>
        /// <returns>ordered array of points that specify the verticies of the region</returns>
        RideVector2[] GetRegionPoints(RideID id);

        /// <summary>
        /// Get the RideIDs of all regions in this system
        /// </summary>
        /// <returns>The enumerable RideIDs</returns>
        IEnumerable<RideID> GetRegions();

        /// <summary>
        /// Get the RideIDs of all regions that overlap wtih a given 2-D point
        /// </summary>
        /// <param name="position">a 2-D point in the world</param>
        /// <returns>The emumerable RideIDs</returns>
        IEnumerable<RideID> GetRegionsByPosition(RideVector2 position);

        /// <summary>
        /// Get the RideIDs of all regions that overlap with a given 3-D point
        /// </summary>
        /// <param name="position">a 2-D point in the world</param>
        /// <returns>The emumerable RideIDs</returns>
        IEnumerable<RideID> GetRegionsByPosition(RideVector3 position);

        /// <summary>
        /// Get the RegionType of a region known to the system.
        /// </summary>
        /// <param name="id">The RideID of the region</param>
        /// <returns></returns>
        RegionType GetRegionType(RideID id);

        /// <summary>
        /// Replace the points of an existing region.
        /// </summary>
        /// <param name="regionId">TSS ID of the existing region.</param>
        /// <param name="region">New points of the region.</param>
        /// <param name="type">Type of the region.</param>
        void SetRegion(RideID regionId, RideVector2[] region, RegionType type);

        void SetRegion(RideID regionId, RideVector2[] region);

        /// <summary>
        /// Determine if a 2-D point overlaps with a region
        /// </summary>
        /// <param name="position">a 2-D point in the world</param>
        /// <param name="id">the RideID of the region</param>
        /// <returns>True if the point is in the region</returns>
        bool PositionInRegion(RideVector2 position, RideID id);

        /// <summary>
        /// Determine if a 3-D point overlaps with a region
        /// </summary>
        /// <param name="position">a 3-D point in the world</param>
        /// <param name="id">the RideID of the region</param>
        /// <returns>True if the point is in the region</returns>
        bool PositionInRegion(RideVector3 position, RideID id);

        /// <summary>
        /// Add an agent as one to be monitored by this system.
        /// World events "agentEnterRegion" and "agentExitRegion" will be dispatched accordingly.
        /// RegionChangeEvent data will be included.
        /// </summary>
        /// <param name="id">RideID of the agent</param>
        void MonitorAgent(RideID id);

        /// <summary>
        /// Add a group as one to be monitored by this system.
        /// World events "groupEnterRegion" and "groupExitRegion" will be dispatched accordingly.
        /// RegionChangeEvent data will be included.
        /// </summary>
        /// <param name="id">RideID of the group</param>
        void MonitorGroup(RideID id);

        /// <summary>
        /// Remove an agent from those being monitored by this system.
        /// </summary>
        /// <param name="id">RideID of the agent</param>
        void RemoveAgent(RideID id);

        /// <summary>
        /// Remove a group from those being monitored by this system.
        /// </summary>
        /// <param name="id">RideID of the group</param>
        void RemoveGroup(RideID id);
    }
}
