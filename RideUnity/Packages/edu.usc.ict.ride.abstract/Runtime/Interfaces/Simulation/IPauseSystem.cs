using System;

namespace Ride
{
    public class PauseResponse : SystemResponse
    {
        public bool isPaused;
    }

    /// <summary>
    /// Interface for pausing RIDE simulations.
    /// </summary>
    public interface IPauseSystem : IRideSystem
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="onComplete"></param>
        void PauseSimulation(Action<PauseResponse> onComplete);

        /// <summary>
        ///
        /// </summary>
        /// <param name="selectedGameObject">If using a controller, this UI gameobject should be selected for navigation.</param>
        /// <param name="onComplete"></param>
        void PauseSimulation(RideID selectedGameObject, Action<PauseResponse> onComplete);

        /// <summary>
        ///
        /// </summary>
        /// <param name="onComplete"></param>
        void ResumeSimulation(Action<PauseResponse> onComplete);

        /// <summary>
        ///
        /// </summary>
        /// <param name="selectedGameObject">If using a controller, this UI gameobject should be selected for navigation.</param>
        /// <param name="onComplete"></param>
        void ResumeSimulation(RideID selectedGameObject, Action<PauseResponse> onComplete);
    }
}
