using UnityEngine;

namespace Ride.Terrain.Navigation
{
    /// <summary>
    /// Unity-backed concrete implementation of <see cref="INavigation"/> used by Ride navigation systems.
    /// </summary>
    /// <remarks>
    /// <see cref="NavigationSystemUnity"/> creates this component at runtime to act as the Unity object that owns
    /// generated or loaded navigation data, such as <c>NavMeshSurface</c> components and temporary obstacle state.
    /// It is intentionally lightweight: it mainly provides a concrete <see cref="MonoBehaviour"/> host so Ride can
    /// manage navigation assets through the <see cref="INavigation"/> abstraction while still using Unity coroutines,
    /// component lookups, and scene objects under the hood.
    /// </remarks>
    public class NavigationUnity : MonoBehaviour, INavigation
    {
        /// <summary>Gets or sets the Ride identifier associated with this navigation instance.</summary>
        public RideID id { get; set; }
    }
}
