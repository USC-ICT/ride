using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.IO
{
    /// <summary>
    /// Defines an input controller that can be attached to multiple controllable entities.
    /// The input system uses this contract to maintain controller-to-entity assignments.
    /// </summary>
    public interface IInputControllerNew
    {
        /// <summary>Gets the category of input target driven by this controller.</summary>
        InputControlType controllerType { get; }

        /// <summary>
        /// Adds an entity to the set controlled by this controller.
        /// </summary>
        /// <param name="id">The entity identifier to add.</param>
        void AddControlledActorID(RideID id);

        /// <summary>
        /// Removes an entity from the set controlled by this controller.
        /// </summary>
        /// <param name="id">The entity identifier to remove.</param>
        void RemoveControlledActorID(RideID id);

        /// <summary>
        /// Gets the entities assigned to this controller.
        /// </summary>
        /// <returns>The assigned entity identifiers.</returns>
        RideID[] GetControlledActors();

        /// <summary>Gets the identifier of this controller.</summary>
        RideID controllerId { get; }
    }
}
