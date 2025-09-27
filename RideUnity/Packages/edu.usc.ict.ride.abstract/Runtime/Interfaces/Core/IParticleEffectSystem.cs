namespace Ride.Effects
{
    /// <summary>
    /// System for spawning and controlling particle systems in RIDE
    /// </summary>
    public interface IParticleEffectSystem : IRideSystem
    {
        /// <summary>
        /// Duplicate a particle system from another particle system
        /// </summary>
        /// <param name="pfx">The pfx system to duplicate</param>
        /// <param name="position">The world position</param>
        /// <param name="rotation">The world rotation</param>
        /// <returns></returns>
        RideID Create(RideID pfx, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromScene(string pfxName);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <param name="position">The world position</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromScene(string pfxName, RideVector3 position);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <param name="position">The world position</param>
        /// <param name="rotation">The world rotation</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromScene(string pfxName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromResource(string pfxName);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <param name="position">The world position</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromResource(string pfxName, RideVector3 position);

        /// <summary>
        /// Creates the given particle effect system
        /// </summary>
        /// <param name="pfxName">The name of the prefab, scene object, or resource that you want to create from</param>
        /// <param name="position">The world position</param>
        /// <param name="rotation">The world rotation</param>
        /// <returns>The pfx id</returns>
        RideID CreateFromResource(string pfxName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Play the particle system. If the system was paused, it will pick up where it left off.
        /// If it was stopped, it will play from the start
        /// </summary>
        /// <param name="pfx">The particle system</param>
        void Play(RideID pfx);

        /// <summary>
        /// Pause the particle system
        /// </summary>
        /// <param name="pfx">The particle system</param>
        void Pause(RideID pfx);

        /// <summary>
        /// Stop the particle system from playing
        /// </summary>
        /// <param name="pfx">The particle system</param>
        void Stop(RideID pfx);

        /// <summary>
        /// Destory the particle system
        /// </summary>
        /// <param name="pfx">The particle system</param>
        void Destroy(RideID pfx);

        /// <summary>
        /// Test if the particle system is playing
        /// </summary>
        /// <param name="pfx">The particle system</param>
        /// <returns>True if the particle system is playing, otherwise false</returns>
        bool IsPlaying(RideID pfx);
    }
}
