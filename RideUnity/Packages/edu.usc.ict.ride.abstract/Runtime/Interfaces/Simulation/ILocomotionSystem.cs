namespace Ride.Entities.Locomotion
{
    [System.Serializable]
    public struct LocomotionData
    {
        public RideVector3 colliderCenter;
        public RideVector3 colliderSize;
        public float mass;
        public float drag;
        public float angularDrag;
    }

    public interface ILocomotionSystem : IRideSystem
    {
    }
}
