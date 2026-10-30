using System.Collections;
using System.Collections.Generic;

namespace Ride.Movement
{
    /// <summary>Stores an ordered, mutable sequence of waypoints for movement and formation planning.</summary>
    /// <remarks>
    /// Used by <c>MovementSystemUnity</c>, <c>MovementUtils</c>, and scenario movement procedures when constructing
    /// routes for <see cref="IMover"/> instances.
    /// </remarks>
    public class WaypointPath : IPath
    {
        /// <summary>The ordered waypoint storage backing <see cref="waypoints"/>.</summary>
        List<IWaypoint> m_waypoints = new List<IWaypoint>();

        /// <inheritdoc/>
        public IEnumerable<IWaypoint> waypoints { get { return m_waypoints; } }

        /// <inheritdoc/>
        public int numWaypoints => m_waypoints.Count;

        /// <summary>Initializes an empty waypoint path.</summary>
        public WaypointPath() { }

        /// <summary>Initializes a path from an ordered waypoint array.</summary>
        /// <param name="waypoint">The waypoints to copy into the path.</param>
        public WaypointPath(IWaypoint[] waypoint)
        {
            SetPath(waypoint);
        }

        /// <inheritdoc/>
        public void AddWaypoint(IWaypoint wp)
        {
            m_waypoints.Add(wp);
        }

        /// <inheritdoc/>
        public IWaypoint GetWaypointByIndex(int index)
        {
            return m_waypoints[index];
        }

        /// <inheritdoc/>
        public void RemoveWaypoint(IWaypoint wp)
        {
            m_waypoints.Remove(wp);
        }

        /// <inheritdoc/>
        public void SetPath(IEnumerable<IWaypoint> path)
        {
            m_waypoints.Clear();
            m_waypoints.AddRange(path);
        }

        /// <inheritdoc/>
        public void SetPath(IPath path)
        {
            m_waypoints.Clear();
            m_waypoints.AddRange(path.waypoints);
        }
    }
}
