namespace Ride
{
    /// <summary>
    /// Defines how a transform will move along a path
    /// </summary>
    public interface ITweenMovementBehaviour : ITweenBehaviourSystem
    {
        /// <summary>
        /// Get the movement pathes that are available movement tweening
        /// </summary>
        /// <returns></returns>
        RideID[] GetPathes();

        /// <summary>
        /// Move along the given path
        /// </summary>
        /// <param name="path">The path to move along</param>
        /// <param name="transform">The transform that will move</param>
        /// <param name="duration">The duration to reach the end of the spline</param>
        void MoveAlong(RideID path, RideID transform, float duration);

        /// <summary>
        /// Move along the given path
        /// </summary>
        /// <param name="path">The path to move along</param>
        /// <param name="transform">The transform that will move</param>
        /// <param name="duration">The duration to reach the end of the spline</param>
        /// <param name="lookAtTarget">The target the transform will look at when moving alonng the spline</param>
        void MoveAlong(RideID path, RideID transform, float duration, RideID lookAtTarget);
    }
}
