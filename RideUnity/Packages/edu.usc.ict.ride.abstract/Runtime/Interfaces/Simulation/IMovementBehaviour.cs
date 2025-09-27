namespace Ride.Movement
{
    /// <summary>
    /// The type of IPathNavigator that will be used to move in the world
    /// </summary>
    public enum MovementBehaviour
    {
        Unrestricted,
        WaypointNavigation,
        FollowMover,
        GroupFormationMovement
    }

    /// <summary>
    /// Callback for when a mover reaches a waypoint
    /// </summary>
    /// <param name="mover"></param>
    /// <param name="waypoint"></param>
    public delegate void OnReachedWaypoint(IMover mover, IWaypoint waypoint);

    /// <summary>
    /// Determines when and where a mover will go
    /// </summary>
    public interface IMovementBehaviour : IRideSystem, IIdentity
    {
        /// <summary>
        /// The type defined behaiour used
        /// </summary>
        MovementBehaviour behaviour { get; }

        /// <summary>
        /// Forces the mover to start moving
        /// </summary>
        /// <param name="mover"></param>
        void StartMovement(IMover mover, float desiredSpeed = -1.0f);

        /// <summary>
        /// Stops the mover
        /// </summary>
        /// <param name="mover"></param>
        void StopMovement(IMover mover);

        /// <summary>
        /// Sets the movement strategy that will be used to move to destinations
        /// </summary>
        /// <param name="moveSystem"></param>
        void SetMovementSystem(IMovementSystem moveSystem);
    }
}
