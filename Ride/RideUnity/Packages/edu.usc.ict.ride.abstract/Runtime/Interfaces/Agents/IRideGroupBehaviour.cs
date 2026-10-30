namespace Ride.AI
{
    /// <summary>
    /// Identifies an agent behavior that coordinates an entity relative to a designated leader.
    /// Implementations include leader-following behaviors such as <c>Follow</c> and <c>FallIn</c>;
    /// simulation observers also use this interface to associate behavior events with the leader.
    /// </summary>
    public interface IRideGroupBehaviour
    {
        /// <summary>Gets the identifier of the entity that leads this behavior.</summary>
        RideID leader { get; }
    }
}
