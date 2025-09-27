namespace Ride
{
    /// <summary>
    /// System for playing, pausing, speeding up, and slowing down the time of the simulation
    /// </summary>
    public interface ITimeSimulationSystem : IRideSystem
    {
        /// <summary>
        /// Returns true if the simulation is paused
        /// </summary>
        bool isPaused { get; }

        /// <summary>
        /// The intended simulation speed.  If paused, it will be applied when unpaused
        /// </summary>
        float speed { get; }

        /// <summary>
        /// Resumes simulation at the previously set speed.
        /// </summary>
        void Play();

        /// <summary>
        /// Sets the simulation to paused state
        /// </summary>
        void Pause();

        /// <summary>
        /// Sets the intended simulation speed.
        /// This will take effect immediately if not paused,
        /// or will be used the next time the simulation is resumed.
        /// </summary>
        /// <param name="speed">The new simulation speed (positive value only). 1 is 'normal speed'.</param>
        void SetSpeed(float speed);
    }
}
