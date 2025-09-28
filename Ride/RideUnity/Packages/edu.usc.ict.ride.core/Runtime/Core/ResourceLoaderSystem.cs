using System;
using System.Collections.Generic;
using UnityEngine;
using Ride.Entities;

namespace Ride
{
    /// <summary>
    /// Registers and provides runtime access to scene-based and prefab-based GameObjects, AudioClips, and <see cref="Ride.Entities.IItem"/> instances.
    /// This system is typically populated by a <see cref="Ride.ResourceLoaderList"/> at scene startup,
    /// and supports retrieval and instantiation of registered assets during runtime.
    ///
    /// This system is part of the <c>ride.core</c> implementation and implements the abstract interface <see cref="Ride.IResourceLoaderSystem"/>.
    /// GameObject accessors and instantiation logic are provided here but excluded from the abstract API to preserve decoupling.
    ///
    /// <seealso cref="Ride.ResourceLoaderList"/>
    /// <seealso cref="Ride.IResourceLoaderSystem"/>
    /// </summary>
    public class ResourceLoaderSystem : RideSystemMonoBehaviour, IResourceLoaderSystem
    {
        List<GameObject> m_resources = new();
        List<IItem> m_items = new();
        List<AudioClip> m_audioClips = new();


        /// <summary>
        /// Returns a registered resource GameObject by name.
        /// These are added via <see cref="AddSceneObject(UnityEngine.GameObject)"/>.
        /// </summary>
        /// <param name="objectName">The name of the registered GameObject</param>
        /// <returns>The GameObject if found, otherwise null</returns>
        public GameObject GetResourceObject(string objectName)
        {
            foreach (var resource in m_resources)
            {
                if (resource.name == objectName)
                    return resource;
            }

            Debug.LogError($"ResourceLoaderSystem.GetResourceObject() - Unable to find scene object with name {objectName}");
            return null;
        }

        /// <summary>
        /// Returns all registered resource GameObjects.
        /// </summary>
        /// <returns>Array of all registered GameObjects</returns>
        public GameObject[] GetAllResourceObjects() => m_resources.ToArray();

        /// <summary>
        /// Finds a GameObject in the currently loaded scene by name, including inactive objects.
        /// </summary>
        /// <param name="objectName">Name of the scene object</param>
        /// <returns>GameObject if found, otherwise null</returns>
        public GameObject GetSceneObject(string objectName)
        {
            GameObject sceneObject = FindSceneObject(objectName);
            if (sceneObject != null)
                return sceneObject;

            Debug.LogError($"ResourceLoaderSystem.GetSceneObject() - Unable to find scene object with name {objectName}");
            return null;
        }

        /// <summary>
        /// Returns an <see cref="IItem"/> registered by item name.
        /// Items are discovered via <see cref="AddSceneObject(UnityEngine.GameObject)"/> if the GameObject has an <see cref="IItem"/> component.
        /// </summary>
        /// <param name="itemName">Logical name of the item</param>
        /// <returns>Matching IItem, or null</returns>
        public IItem GetItem(string itemName)
        {
            foreach (var item in m_items)
            {
                if (item.itemName == itemName)
                    return item;
            }

            Debug.LogError($"ResourceLoaderSystem.GetItem() - Unable to find item with name {itemName} - Check ResourceLoader Item List");
            return null;
        }

        /// <summary>
        /// Returns an <see cref="IItem"/> registered by <see cref="ItemType"/>.
        /// Items are discovered via <see cref="AddSceneObject(UnityEngine.GameObject)"/> if the GameObject has an <see cref="IItem"/> component.
        /// </summary>
        /// <param name="itemType">The item type enum</param>
        /// <returns>Matching IItem, or null</returns>
        public IItem GetItem(ItemType itemType)
        {
            foreach (var item in m_items)
            {
                if (item.type == itemType)
                    return item;
            }

            Debug.LogError($"ResourceLoaderSystem.GetItem() - Unable to find item with type {itemType} - Check ResourceLoader Item List");
            return null;
        }

        /// <summary>
        /// Returns all registered <see cref="IItem"/>s discovered via <see cref="AddSceneObject(UnityEngine.GameObject)"/>.
        /// </summary>
        /// <returns>Array of registered IItem instances</returns>
        public IItem[] GetAllItems() => m_items.ToArray();

        /// <summary>
        /// Retrieves an <see cref="AudioClip"/> by name if it was registered via <see cref="AddAudioClip(UnityEngine.AudioClip)"/>.
        /// </summary>
        /// <param name="clip">The name of the AudioClip</param>
        /// <returns>Matching AudioClip or null</returns>
        public AudioClip GetAudioClip(string clip)
        {
            foreach (var c in m_audioClips)
            {
                if (c.name == clip)
                    return c;
            }

            Debug.LogError($"ResourceLoaderSystem.GetAudioClip() - Unable to find item with name {clip} - Check ResourceLoader Item List");
            return null;
        }


        /// <summary>
        /// Instantiates a new instance of a scene GameObject by name at the given position and rotation.
        /// Uses <see cref="GetSceneObject(string)"/> internally.
        /// </summary>
        /// <param name="objectName">The name of the scene object</param>
        /// <param name="position">Position to instantiate at</param>
        /// <param name="rotation">Rotation to instantiate with</param>
        /// <returns>Instantiated GameObject or null</returns>
        public GameObject InstantiateSceneObject(string objectName, RideVector3 position, RideQuaternion rotation) =>
            InstantiateInternal(GetSceneObject(objectName), objectName, position, rotation);

        /// <summary>
        /// Instantiates a new instance of a registered resource GameObject by name at the given position and rotation.
        /// Uses <see cref="GetResourceObject(string)"/> internally.
        /// </summary>
        /// <param name="objectName">The name of the resource object</param>
        /// <param name="position">Position to instantiate at</param>
        /// <param name="rotation">Rotation to instantiate with</param>
        /// <returns>Instantiated GameObject or null</returns>
        public GameObject InstantiateResource(string objectName, RideVector3 position, RideQuaternion rotation) =>
            InstantiateInternal(GetResourceObject(objectName), objectName, position, rotation);

        /// <summary>
        /// Instantiates a copy of the specified GameObject at the given position and rotation.
        /// Activates the new object before returning it. Logs an error if the source is null.
        ///
        /// Used internally by <see cref="InstantiateSceneObject(string, RideVector3, RideQuaternion)"/>
        /// and <see cref="InstantiateResource(string, RideVector3, RideQuaternion)"/>.
        /// </summary>
        /// <param name="source">The original GameObject to instantiate</param>
        /// <param name="name">Name of the object (used in error/debug messages)</param>
        /// <param name="position">World position to place the instantiated object</param>
        /// <param name="rotation">World rotation to apply to the instantiated object</param>
        /// <returns>The new GameObject instance, or null if the source was null</returns>
        private GameObject InstantiateInternal(GameObject source, string name, RideVector3 position, RideQuaternion rotation)
        {
            if (source == null)
                return null;

            var newObj = Instantiate(source, position, rotation);

#if false
            float numUnits = 50;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, numUnits, UnityEngine.AI.NavMesh.AllAreas))
                Debug.LogWarning($"{objectName} - Closest NavMesh point to {position} is {hit.position}, distance = {Vector3.Distance(position, hit.position)} units.");
            else
                Debug.LogWarning($"{objectName} - No NavMesh point found within {numUnits} units of {position}.");
#endif

            newObj.SetActive(true);
            return newObj;
        }


        /// <summary>
        /// Instantiates an <see cref="IItem"/> by item name. This clones the original registered GameObject and returns its <see cref="IItem"/> component.
        /// </summary>
        /// <param name="itemName">The name of the item</param>
        /// <returns>Instantiated IItem or null</returns>
        public IItem InstantiateItem(string itemName) => InstantiateItemInternal(i => i.itemName == itemName);

        /// <summary>
        /// Instantiates an <see cref="IItem"/> by item type. This clones the original registered GameObject and returns its <see cref="IItem"/> component.
        /// </summary>
        /// <param name="type">The item type</param>
        /// <returns>Instantiated IItem or null</returns>
        public IItem InstantiateItem(ItemType type) => InstantiateItemInternal(i => i.type == type);

        /// <summary>
        /// Searches all registered resource GameObjects for one that contains an <see cref="IItem"/> component
        /// matching the specified predicate. If found, instantiates the GameObject and returns the IItem component.
        ///
        /// Used internally by <see cref="InstantiateItem(string)"/> and <see cref="InstantiateItem(ItemType)"/>.
        /// </summary>
        /// <param name="predicate">Predicate used to select the matching IItem (e.g., by name or type)</param>
        /// <returns>Instantiated IItem component, or null if no match is found</returns>
        private IItem InstantiateItemInternal(Func<IItem, bool> predicate)
        {
            foreach (var resource in m_resources)
            {
                var item = resource.GetComponent<IItem>();
                if (item != null && predicate(item))
                    return Instantiate(resource).GetComponent<IItem>();
            }

            return null;
        }

        /// <summary>
        /// Adds a GameObject to the resource registry. If the object contains an <see cref="IItem"/> component,
        /// it is also registered for item lookup and instantiation.
        /// </summary>
        /// <param name="obj">GameObject to register</param>
        public void AddSceneObject(GameObject obj)
        {
            if (obj == null)
                return;

            m_resources.Add(obj);

            var item = obj.GetComponent<IItem>();
            if (item != null)
                m_items.Add(item);
        }

        /// <summary>
        /// Adds an AudioClip to the registry so it can be retrieved by name via <see cref="GetAudioClip(string)"/>.
        /// </summary>
        /// <param name="clip">AudioClip to register</param>
        public void AddAudioClip(AudioClip clip)
        {
            if (clip == null)
                return;

            m_audioClips.Add(clip);
        }

        /// <summary>
        /// Searches the currently active scene for a GameObject by name.
        /// Includes inactive objects. Returns the first match found among all root GameObjects.
        /// </summary>
        /// <param name="name">Name of the GameObject to find</param>
        /// <returns>The matching GameObject, or null if not found</returns>
        static GameObject FindSceneObject(string name)
        {
            var rootGos = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var parent in rootGos)
            {
                var trs = parent.GetComponentsInChildren<Transform>(true);
                foreach (var t in trs)
                {
                    if (t.name == name)
                        return t.gameObject;
                }
            }

            return null;
        }
    }
}
