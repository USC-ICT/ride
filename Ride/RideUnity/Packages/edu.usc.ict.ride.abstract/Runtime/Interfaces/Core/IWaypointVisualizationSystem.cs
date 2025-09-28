namespace Ride
{
    public interface IWaypointVisualizationSystem : IRideSystem
    {
        /// <summary>
        /// Adds or extends the waypoint visualizer by adding a new waypoint to the agent
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <param name="position">The position of the new waypoint</param>
        bool AddWaypoint(RideID agentId, RideVector3 position);

        /// <summary>
        /// Turns on/off the waypoint visualizer's rendering of lines that intersect enemy Lines of Sight
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <param name="toggle">On/Off parameter</param>
        void SetRenderLOSLines(RideID agentId, bool toggle);

        /// <summary>
        /// Refreshes waypoint line to reflect latest changes in the scene.
        /// </summary>
        /// <param name="agentId">The agent</param>
        void RefreshLines(RideID agentId);

        /// <summary>
        /// Deletes waypoints of the agent.
        /// </summary>
        /// <param name="agentId"></param>
        void RemoveWaypoints(RideID agentId);
    }
}