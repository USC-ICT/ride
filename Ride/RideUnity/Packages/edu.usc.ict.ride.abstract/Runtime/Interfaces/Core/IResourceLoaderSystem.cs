using Ride.Entities;

namespace Ride
{
    /// <summary>
    /// Provides an abstract interface for loading and accessing registered <see cref="Ride.Entities.IItem"/> instances
    /// by name or <see cref="Ride.Entities.ItemType"/>. This system allows decoupled item lookup and instantiation
    /// without depending on Unity-specific types like <c>GameObject</c>.
    ///
    /// This interface is implemented by <see cref="Ride.ResourceLoaderSystem"/> in the <c>ride.core</c> package,
    /// which handles actual GameObject management and Unity-specific instantiation logic.
    ///
    /// <seealso cref="Ride.ResourceLoaderSystem"/>
    /// </summary>
    public interface IResourceLoaderSystem : IRideSystem
    {
        /// <summary>
        /// Retrieves a registered <see cref="IItem"/> by its logical name.
        /// The item must have been registered through a backing implementation such as <see cref="ResourceLoaderSystem"/>.
        /// </summary>
        /// <param name="itemName">The unique name of the item to retrieve</param>
        /// <returns>The matching <see cref="IItem"/>, or null if not found</returns>
        IItem GetItem(string itemName);

        /// <summary>
        /// Retrieves a registered <see cref="IItem"/> by its <see cref="ItemType"/> enum value.
        /// </summary>
        /// <param name="itemType">The item type associated with the item</param>
        /// <returns>The matching <see cref="IItem"/>, or null if not found</returns>
        IItem GetItem(ItemType itemType);

        /// <summary>
        /// Returns all currently registered <see cref="IItem"/> instances available to the system.
        /// </summary>
        /// <returns>An array of all <see cref="IItem"/>s in the registry</returns>
        IItem[] GetAllItems();

        /// <summary>
        /// Instantiates a new <see cref="IItem"/> based on the provided <see cref="ItemType"/>.
        /// The implementation determines how the item is created (e.g., via prefab cloning or proxy construction).
        /// </summary>
        /// <param name="type">The type of item to instantiate</param>
        /// <returns>The instantiated <see cref="IItem"/>, or null if the type is unrecognized</returns>
        IItem InstantiateItem(ItemType type);

        /// <summary>
        /// Instantiates a new <see cref="IItem"/> based on the item's registered name.
        /// The implementation determines how the item is created (e.g., via prefab cloning or proxy construction).
        /// </summary>
        /// <param name="itemName">The name of the item to instantiate</param>
        /// <returns>The instantiated <see cref="IItem"/>, or null if the name is unrecognized</returns>
        IItem InstantiateItem(string itemName);


        // GameObject-based versions are implemented only in ride.core systems and not part of the abstract interface.
        //GameObject GetResourceObject(string objectName);
        //GameObject[] GetAllResourceObjects();
        //GameObject GetSceneObject(string objectName);
        //GameObject InstantiateSceneObject(string objectName, RideVector3 position, RideQuaternion rotation);
        //GameObject InstantiateResource(string objectName, RideVector3 position, RideQuaternion rotation);
        //void AddSceneObject(GameObject obj);
    }
}
