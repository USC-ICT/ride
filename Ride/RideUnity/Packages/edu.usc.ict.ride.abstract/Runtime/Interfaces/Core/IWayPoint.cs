using Ride.Entities;

namespace Ride.Movement
{
    [System.Flags]
    public enum WaypointFlags
    {
        None,
        Blue_Spawner = 1,
        Red_Spawner = 1 << 1,
        Civilian_Spawner = 1 << 2,
        AreaOfInterest = 1 << 3,

    }

    /// <summary>
    /// Represents a location of importance in the world
    /// </summary>
    public interface IWaypoint : IEntity
    {
        /// <summary>
        /// Position in world space
        /// </summary>
        RideVector3 position { get; set; }

        /// <summary>
        /// Rotation in world space
        /// </summary>
        RideQuaternion rotation { get; set; }

        /// <summary>
        /// Additional attributes that customize this waypoint
        /// </summary>
        WaypointFlags flags { get; set; }
        /// <summary>
        /// from the position center of the area of interest to the radius
        /// the radius defines the size of the area of interest
        /// </summary>
        float radius { get; set; }

        void Init(RideID id, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius);

        void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius);
    }
}
