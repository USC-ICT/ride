using System.Collections;
using System.Collections.Generic;

namespace Ride.Movement
{
    public class WaypointPath : IPath
    {
        List<IWaypoint> m_waypoints = new List<IWaypoint>();

        public IEnumerable<IWaypoint> waypoints { get { return m_waypoints; } }

        public int numWaypoints => m_waypoints.Count;

        public WaypointPath() { }

        public WaypointPath(IWaypoint[] waypoint)
        {
            SetPath(waypoint);
        }

        public void AddWaypoint(IWaypoint wp)
        {
            m_waypoints.Add(wp);
        }

        public IWaypoint GetWaypointByIndex(int index)
        {
            return m_waypoints[index];
        }

        public void RemoveWaypoint(IWaypoint wp)
        {
            m_waypoints.Remove(wp);
        }

        public void SetPath(IEnumerable<IWaypoint> path)
        {
            m_waypoints.Clear();
            m_waypoints.AddRange(path);
        }

        public void SetPath(IPath path)
        {
            m_waypoints.Clear();
            m_waypoints.AddRange(path.waypoints);
        }
    }
}
