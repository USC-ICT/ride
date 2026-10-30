using System;

namespace Ride
{
    /// <summary>Returns the simulation's pause state after a pause or resume request.</summary>
    public class PauseResponse : SystemResponse
    {
        /// <summary>Whether the simulation is paused.</summary>
        public bool isPaused;
    }

    /// <summary>
    /// Interface for pausing RIDE simulations.
    /// </summary>
    public interface IPauseSystem : IRideSystem
    {
        /// <summary>Pauses the simulation.</summary>
        /// <param name="onComplete">Callback invoked with the resulting pause state.</param>
        void PauseSimulation(Action<PauseResponse> onComplete);

        /// <summary>Pauses the simulation and selects a UI object for controller navigation.</summary>
        /// <param name="selectedGameObject">If using a controller, this UI gameobject should be selected for navigation.</param>
        /// <param name="onComplete">Callback invoked with the resulting pause state.</param>
        void PauseSimulation(RideID selectedGameObject, Action<PauseResponse> onComplete);

        /// <summary>Resumes the simulation.</summary>
        /// <param name="onComplete">Callback invoked with the resulting pause state.</param>
        void ResumeSimulation(Action<PauseResponse> onComplete);

        /// <summary>Resumes the simulation and selects a UI object for controller navigation.</summary>
        /// <param name="selectedGameObject">If using a controller, this UI gameobject should be selected for navigation.</param>
        /// <param name="onComplete">Callback invoked with the resulting pause state.</param>
        void ResumeSimulation(RideID selectedGameObject, Action<PauseResponse> onComplete);
    }
}
