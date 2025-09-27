using System.Collections.Generic;

namespace Ride.Movement
{
    /// <summary>
    /// The behaviour whenever an IMover reaches a waypoint
    /// </summary>
    public enum PathingBehaviour
    {
        /// <summary>
        /// Stop moving when you reach the end of the path, otherwise continue to the next waypoint
        /// </summary>
        Stop,

        /// <summary>
        /// Return to the first path point when at the end of the path, otherwise continue to the next waypoint
        /// </summary>
        Loop,

        /// <summary>
        /// Reverse direction when at the end of the path, otherwise continue to the next waypoint
        /// </summary>
        PingPong,

        /// <summary>
        /// Choose a random other point on the path whenever you reach a waypoint
        /// </summary>
        Random,
    }

    /// <summary>
    /// Interface for accessing meaningful locations in the world
    /// </summary>
    public interface IPath
    {
        /// <summary>
        /// The points of interest along the path
        /// </summary>
        IEnumerable<IWaypoint> waypoints { get; }

        /// <summary>
        /// Returns the number of waypoints the path consists of
        /// </summary>
        int numWaypoints { get; }

        /// <summary>
        /// Sets the points of interest allong the path
        /// </summary>
        /// <param name="path"></param>
        void SetPath(IEnumerable<IWaypoint> path);

        /// <summary>
        /// Set the path
        /// </summary>
        /// <param name="path"></param>
        void SetPath(IPath path);

        /// <summary>
        /// Adds the waypoint to the end of the path
        /// </summary>
        /// <param name="wp"></param>
        void AddWaypoint(IWaypoint wp);

        /// <summary>
        /// Removes the waypoint from the path
        /// </summary>
        /// <param name="wp"></param>
        void RemoveWaypoint(IWaypoint wp);

        /// <summary>
        /// Returns the waypoint at the given index
        /// </summary>
        /// <param name="index">the index in the waypoint array</param>
        /// <returns>The waypoint</returns>
        IWaypoint GetWaypointByIndex(int index);
    }
}
