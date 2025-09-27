using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Provides a high-level abstraction for managing UnityEngine.Object instances (typically GameObjects) 
    /// within the RIDE framework using the <see cref="RideID"/> identity system.
    /// 
    /// This system is responsible for the lifecycle and association of GameObjects with RideIDs,
    /// enabling consistent creation, lookup, and manipulation of runtime and pre-existing scene objects
    /// without leaking Unity-specific IDs across subsystems.
    ///
    /// It supports multiple creation sources (scene, Resources, cloning), lookup by name or object reference,
    /// insertion of preexisting objects, and identity translation between RideID and Unity instance IDs.
    /// 
    /// <para>
    /// Note: This system depends on Unity's GameObject lifecycle and should be considered Unity-specific.
    /// See <see cref="UnityEngine.Object"/> for details about Unity’s native object model.
    /// </para>
    /// 
    /// <para>
    /// internal: Used by systems that require dynamic instantiation, pooling, or linking of simulation
    /// objects to their visual representations in Unity scenes.
    /// </para>
    /// 
    /// <seealso cref="IRideSystem"/>
    /// <seealso cref="RideID"/>
    /// <seealso cref="UnityEngine.Object"/>
    /// </summary>
    public interface IGameObjectSystem : IRideSystem
    {
        #region Creation

        /// <summary>
        /// Creates a new runtime GameObject with the specified name.
        /// </summary>
        /// <param name="name">The name to assign to the new object.</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID Create(string name);

        /// <summary>
        /// Creates a new runtime GameObject at a specified position and rotation.
        /// </summary>
        /// <param name="name">The name to assign to the new object.</param>
        /// <param name="position">The world position to place the object.</param>
        /// <param name="rotation">The world rotation to assign to the object.</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID Create(string name, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Duplicates an existing Ride object.
        /// </summary>
        /// <param name="original">The RideID of the object to duplicate.</param>
        /// <returns>The RideID of the duplicated object, or RideID.Null if creation failed.</returns>
        RideID Create(RideID original);

        /// <summary>
        /// Duplicates an existing Ride object at a specified position and rotation.
        /// </summary>
        /// <param name="original">The RideID of the object to duplicate.</param>
        /// <param name="position">The world position for the new object.</param>
        /// <param name="rotation">The world rotation for the new object.</param>
        /// <returns>The RideID of the duplicated object, or RideID.Null if creation failed.</returns>
        RideID Create(RideID original, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Instantiates a new object from a scene-based prefab.
        /// </summary>
        /// <param name="sceneObjectName">The name of the prefab in the current scene.</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID CreateFromScene(string sceneObjectName);

        /// <summary>
        /// Instantiates a new object from a scene-based prefab at a specified position and rotation.
        /// </summary>
        /// <param name="sceneObjectName">The name of the prefab in the current scene.</param>
        /// <param name="position">The world position for the new object.</param>
        /// <param name="rotation">The world rotation for the new object.</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID CreateFromScene(string sceneObjectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Instantiates a new object from a Resources folder asset.
        /// </summary>
        /// <param name="resourceName">The Resources path of the asset (without extension).</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID CreateFromResource(string resourceName);

        /// <summary>
        /// Instantiates a new object from a Resources folder asset at a specified position and rotation.
        /// </summary>
        /// <param name="resourceName">The Resources path of the asset (without extension).</param>
        /// <param name="position">The world position for the new object.</param>
        /// <param name="rotation">The world rotation for the new object.</param>
        /// <returns>The RideID of the created object, or RideID.Null if creation failed.</returns>
        RideID CreateFromResource(string resourceName, RideVector3 position, RideQuaternion rotation);

        #endregion

        #region Registration / Insertion

        /// <summary>
        /// Adds an existing UnityEngine.Object to the system and assigns it a RideID.
        /// </summary>
        /// <param name="existingEntity">The engine object to register.</param>
        /// <returns>The RideID of the registered object, or RideID.Null if registration failed.</returns>
        RideID AddExistingObject(object existingEntity);

        /// <summary>
        /// Registers an object in the system using its Unity engine instance ID.
        /// Intended for scene objects that existed before runtime.
        /// </summary>
        /// <param name="engineGameObjectInstanceId">The Unity instance ID of the object (not a RideID).</param>
        /// <returns>The RideID associated with this object.</returns>
        RideID InsertObject(int engineGameObjectInstanceId);

        #endregion

        #region Lookup

        /// <summary>
        /// Looks up a RideID by Unity engine instance ID.
        /// </summary>
        /// <param name="engineGameObjectInstanceId">The Unity instance ID (not a RideID).</param>
        /// <returns>The associated RideID, or RideID.Null if not found.</returns>
        /// <remarks>
        /// Use with caution: Unity instance IDs are not stable across sessions and may collide. 
        /// This method is intended for advanced scenarios only.
        /// </remarks>
        RideID GetObject(int engineGameObjectInstanceId);

        /// <summary>
        /// Looks up a RideID using an engine object reference.
        /// </summary>
        /// <param name="engineObject">A reference to a UnityEngine.Object or compatible object.</param>
        /// <returns>The associated RideID, or RideID.Null if not found.</returns>
        RideID GetObject(object engineObject);

        /// <summary>
        /// Attempts to get the RideID for a Unity engine object reference.
        /// </summary>
        /// <param name="engineObject">The engine object to look up.</param>
        /// <param name="id">The associated RideID, if found.</param>
        /// <returns>True if found; false otherwise.</returns>
        bool TryGetObject(object engineObject, out RideID id);

        /// <summary>
        /// Returns all RideIDs currently tracked by the system.
        /// </summary>
        IEnumerable<RideID> GetAll();

        /// <summary>
        /// Searches for a Ride object by name. If multiple exist, returns the first found.
        /// </summary>
        /// <param name="objName">The name of the object to search for.</param>
        /// <returns>The RideID of the first object found, or RideID.Null if none match.</returns>
        RideID Find(string objName);

        /// <summary>
        /// Returns the Unity engine instance ID associated with a RideID.
        /// </summary>
        /// <param name="rideId">The RideID to convert.</param>
        /// <returns>The Unity engine instance ID, or -1 if not found.</returns>
        int GetEngineObjectId(RideID rideId);

        /// <summary>
        /// Determines whether a RideID is currently associated with a valid object.
        /// </summary>
        /// <param name="rideId">The RideID to check.</param>
        /// <returns>True if the object exists; false otherwise.</returns>
        bool Exists(RideID rideId);

        /// <summary>
        /// Gets the current name of a Ride object.
        /// </summary>
        /// <param name="rideId">The RideID of the object.</param>
        /// <returns>The assigned name, or an empty string if not found.</returns>
        string GetName(RideID rideId);

        #endregion

        #region Modification

        /// <summary>
        /// Sets the name of a Ride object.
        /// </summary>
        /// <param name="rideId">The RideID of the object.</param>
        /// <param name="name">The new name to assign.</param>
        void SetName(RideID rideId, string name);

        /// <summary>
        /// Sets a Ride object active or inactive in the scene.
        /// </summary>
        /// <param name="rideId">The RideID of the object.</param>
        /// <param name="active">True to activate, false to deactivate.</param>
        void SetActive(RideID rideId, bool active);

        /// <summary>
        /// Destroys a Ride object, optionally after a delay.
        /// </summary>
        /// <param name="rideId">The RideID of the object to destroy.</param>
        /// <param name="delay">Time (in seconds) to delay before destroying the object.</param>
        void Destroy(RideID rideId, float delay = 0);

        /// <summary>
        /// Resets all RideMonoBehaviour components attached to the specified object.
        /// </summary>
        /// <param name="rideId">The RideID of the object to reset.</param>
        void ResetEntity(RideID rideId);

        #endregion
    }
}
