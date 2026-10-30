using System;

namespace Ride.IO
{
    /// <summary>
    /// Defines an event-driven input controller associated with one controlled actor.
    /// Player controllers and camera consumers use this contract for view events and actor setup.
    /// </summary>
    public interface IInputController
    {
        /// <summary>Occurs when the controller requests a change of camera view.</summary>
        event EventHandler onViewChangeCall;

        /// <summary>Occurs when the controller requests a camera zoom change.</summary>
        event EventHandler onViewZoomChangeCall;

        /// <summary>
        /// Gets the actor controlled by this input controller.
        /// </summary>
        /// <returns>The controlled actor identifier, or <see cref="RideID.Null"/> when none is assigned.</returns>
        RideID GetControlledActor();

        /// <summary>
        /// Associates the controller with an actor and applies implementation-specific configuration data.
        /// </summary>
        /// <param name="id">The actor identifier.</param>
        /// <param name="data">The controller-specific configuration object.</param>
        void SetupData(RideID id, object data);
    }
}
