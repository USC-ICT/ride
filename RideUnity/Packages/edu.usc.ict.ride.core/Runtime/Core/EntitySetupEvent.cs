namespace Ride.WorldState
{
    public class EntitySetupEvent : EntityEvent
    {
        public readonly RideMonoBehaviour[] entityComponents;

        public EntitySetupEvent(RideID id, RideMonoBehaviour[] components) : base(id)
        {
            entityComponents = components;
        }
    }
}
