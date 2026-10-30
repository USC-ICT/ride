namespace Ride.Entities.Locomotion
{
    /// <summary>Describes the collider and rigid-body characteristics used to configure locomotion.</summary>
    [System.Serializable]
    public struct LocomotionData
    {
        /// <summary>The collider center relative to the locomotion object.</summary>
        public RideVector3 colliderCenter;
        /// <summary>The collider dimensions.</summary>
        public RideVector3 colliderSize;
        /// <summary>The rigid-body mass.</summary>
        public float mass;
        /// <summary>The linear drag.</summary>
        public float drag;
        /// <summary>The angular drag.</summary>
        public float angularDrag;
    }

    /// <summary>Marks a RIDE system responsible for entity locomotion services.</summary>
    /// <remarks>The current contract is intentionally empty; concrete locomotion implementations expose behavior through related movement interfaces.</remarks>
    public interface ILocomotionSystem : IRideSystem
    {
    }
}
