namespace Ride.Animations
{
    /// <summary>Provides animation controllers for RIDE entities, primarily through Simulation's animation system.</summary>
    public interface IAnimationSystem  : IRideSystem
    {
        /// <summary>Gets the animation controller registered for an entity.</summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>The registered controller, or <see langword="null"/> when none is available.</returns>
        IAnimationController GetAnimationController(RideID id);
    }
}
