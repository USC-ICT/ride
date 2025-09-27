using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Defines an interface for accessing and manipulating Unity components on Ride entities.
    /// This system allows querying or adding components based on the <see cref="RideID"/> of the associated entity.
    /// 
    /// Implementations of this interface typically map <see cref="RideID"/>s to <see cref="GameObject"/> instances,
    /// enabling reflection-free and type-safe access to Unity <see cref="Component"/> instances.
    /// 
    /// Note: This interface is limited to Unity components (i.e., <see cref="Component"/>-derived types).
    /// </summary>
    public interface IComponentSystem : IRideSystem
    {
        /// <summary>
        /// Retrieves a component of type <typeparamref name="T"/> from the entity associated with the given <paramref name="owner"/> RideID.
        /// </summary>
        /// <typeparam name="T">The type of the Unity <see cref="Component"/> to retrieve.</typeparam>
        /// <param name="owner">The RideID of the entity.</param>
        /// <returns>The component instance if found; otherwise, <c>null</c>.</returns>
        T GetComponent<T>(RideID owner);

        /// <summary>
        /// Retrieves a component of type <typeparamref name="T"/> from the children of the entity associated with the given <paramref name="owner"/> RideID.
        /// </summary>
        /// <typeparam name="T">The type of the Unity <see cref="Component"/> to retrieve.</typeparam>
        /// <param name="owner">The RideID of the entity.</param>
        /// <param name="includeInactive">If <c>true</c>, includes inactive children in the search.</param>
        /// <returns>The component instance if found; otherwise, <c>null</c>.</returns>
        T GetComponentInChildren<T>(RideID owner, bool includeInactive = false);

        /// <summary>
        /// Retrieves all components of type <typeparamref name="T"/> from the children of the entity associated with the given <paramref name="owner"/> RideID.
        /// </summary>
        /// <typeparam name="T">The type of the Unity <see cref="Component"/> to retrieve.</typeparam>
        /// <param name="owner">The RideID of the entity.</param>
        /// <param name="includeInactive">If <c>true</c>, includes inactive children in the search.</param>
        /// <returns>An array of all found components; otherwise, an empty array.</returns>
        T[] GetComponentsInChildren<T>(RideID owner, bool includeInactive = false);

        /// <summary>
        /// Adds a component of type <typeparamref name="T"/> to the entity associated with the given <paramref name="owner"/> RideID,
        /// if it does not already exist.
        /// </summary>
        /// <typeparam name="T">The type of the Unity <see cref="Component"/> to add.</typeparam>
        /// <param name="owner">The RideID of the entity.</param>
        /// <returns>The existing or newly added component instance; or <c>null</c> if the operation failed.</returns>
        T AddComponent<T>(RideID owner) where T : Component;
    }
}
