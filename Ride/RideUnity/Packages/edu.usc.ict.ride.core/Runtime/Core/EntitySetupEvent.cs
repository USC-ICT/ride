namespace Ride.WorldState
{
    /// <summary>
    /// Raised after an entity's Ride-side component setup has completed and the created
    /// <see cref="RideMonoBehaviour"/> components are available to other systems.
    /// </summary>
    /// <remarks>
    /// Systems use this event as a follow-up to entity creation when they need access to the actual
    /// configured Unity/Ride component instances attached to the entity, rather than just the logical
    /// entity ID or serialized entity-data payload.
    /// </remarks>
    public class EntitySetupEvent : EntityEvent
    {
        /// <summary>The Ride MonoBehaviour components that were created or configured during entity setup.</summary>
        public readonly RideMonoBehaviour[] entityComponents;

        /// <summary>
        /// Creates an entity-setup event for the specified entity and component list.
        /// </summary>
        /// <param name="id">The entity whose setup has completed.</param>
        /// <param name="components">The Ride MonoBehaviour components associated with the entity.</param>
        public EntitySetupEvent(RideID id, RideMonoBehaviour[] components) : base(id)
        {
            entityComponents = components;
        }
    }
}
