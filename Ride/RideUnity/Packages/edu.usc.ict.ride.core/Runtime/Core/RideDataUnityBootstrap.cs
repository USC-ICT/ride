using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Abstract MonoBehaviour base for authorable Unity data objects.
    /// 
    /// Implement this class to expose structured configuration or
    /// initialization data to other systems in RIDE.
    /// 
    /// Example use cases include:
    /// - Unit boost parameters (see <see cref="Ride.UnitBoostDataMono"/>)
    /// - Hit box metadata (see <see cref="Ride.Combat.RideEntityHitBoxDataMono"/>)
    /// - Item anchoring rules (see <see cref="Ride.Entities.ItemAnchorDataMono"/>)
    /// - Example movement configuration (see <see cref="Ride.Examples.ExampleMovementDataMono"/>)
    /// 
    /// These objects are typically attached to GameObjects in scenes or prefabs.
    /// 
    /// Implementors must override <see cref="GetData"/> to return the associated data object.
    /// </summary>
    public abstract class RideDataUnityBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Returns the runtime data object associated with this MonoBehaviour.
        /// 
        /// This is typically called by systems at initialization to retrieve
        /// configuration data stored in the Unity scene or prefab.
        /// 
        /// Implementors should return a strongly-typed object,
        /// which may be cast by consumers.
        /// </summary>
        /// <returns>Structured data object backing this bootstrap component.</returns>
        public abstract object GetData();
    }
}
