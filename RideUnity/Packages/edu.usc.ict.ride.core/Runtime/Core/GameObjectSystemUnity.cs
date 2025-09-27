using System;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;
using Ride.WorldState;

namespace Ride
{
    /// <summary>
    /// Concrete Unity implementation of <see cref="IGameObjectSystem"/>, <see cref="ITransformSystem"/>, and <see cref="IComponentSystem"/>.
    /// This system acts as the bridge between abstract RideID-based entity references and actual UnityEngine.GameObject instances.
    /// 
    /// It supports creating, querying, modifying, and destroying GameObjects through stable RideID handles,
    /// enabling Ride subsystems to operate without referencing Unity types directly.
    /// 
    /// This class will be assigned to <c>Systems.GameObject</c> at runtime and is intended for use in scenes where GameObjects
    /// are dynamically instantiated, modified, or destroyed during simulation or runtime.
    /// 
    /// See also:
    /// - <see href="https://docs.unity3d.com/ScriptReference/GameObject.html">Unity GameObject documentation</see>
    /// </summary>
    public class GameObjectSystemUnity : RideSystemMonoBehaviour, IGameObjectSystem, ITransformSystem, IComponentSystem
    {
        private readonly Dictionary<RideID, GameObject> m_gameObjects = new Dictionary<RideID, GameObject>();


        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            var gos = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in gos)
                Insert(IdentityFactory.CreateId(), go);

            foreach (var go in gos)
                if (go.GetComponent<RideDataUnityBootstrap>() != null)
                    if (go.GetComponent<ConvertToRide>() == null)
                        go.AddComponent<ConvertToRide>();
        }


        #region IGameObjectSystem

        /// <inheritdoc/>
        public RideID Create(string name) => Create(name, RideVector3.zero, RideQuaternion.identity);

        /// <inheritdoc/>
        public RideID Create(string name, RideVector3 position, RideQuaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);

            //RideID id = IdentityFactory.CreateId();
            //m_gameObjects.Add(id, go);

            RideID id = AddGameObject(go);
            return id;
        }

        /// <inheritdoc/>
        public RideID Create(RideID original) => Create(original, RideVector3.zero, RideQuaternion.identity);

        /// <inheritdoc/>
        public RideID Create(RideID original, RideVector3 position, RideQuaternion rotation)
        {
            if (!TryGetGameObject(original, out var originalGO))
            {
                RideLog.LogError($"GameObjectSystemUnity.Create() failed: original RideID does not exist: {original}");
                return RideID.Null;
            }

            var go = Instantiate(originalGO, position, rotation);

            //RideID id = IdentityFactory.CreateId();
            //m_gameObjects.Add(id, go);

            return AddGameObject(go);
        }

        /// <inheritdoc/>
        public RideID CreateFromScene(string sceneObjectName) => CreateFromScene(sceneObjectName, RideVector3.zero, RideQuaternion.identity);

        /// <inheritdoc/>
        public RideID CreateFromScene(string sceneObjectName, RideVector3 position, RideQuaternion rotation)
        {
            var loader = Systems.Get<ResourceLoaderSystem>();
            var go = loader.InstantiateSceneObject(sceneObjectName, position, rotation);
            if (go == null)
            {
                Debug.LogError($"GameObjectSystemUnity.CreateFromScene() - Failed to create object because original '{sceneObjectName}' doesn't exist");
                return RideID.Null;
            }

            //RideID id = IdentityFactory.CreateId();
            //m_gameObjects.Add(id, go);

            return AddGameObject(go);
        }

        /// <inheritdoc/>
        public RideID CreateFromResource(string resourceName) => CreateFromResource(name, RideVector3.zero, RideQuaternion.identity);

        /// <inheritdoc/>
        public RideID CreateFromResource(string resourceName, RideVector3 position, RideQuaternion rotation)
        {
            var loader = Systems.Get<ResourceLoaderSystem>();
            var go = loader.InstantiateResource(resourceName, position, rotation);
            if (go == null)
            {
                Debug.LogError($"GameObjectSystemUnity.CreateFromResource() - Failed to create object because original {resourceName} doesn't exist");
                return RideID.Null;
            }

            //RideID id = IdentityFactory.CreateId();
            //m_gameObjects.Add(id, go);

            return AddGameObject(go);
        }

        /// <inheritdoc/>
        public RideID AddExistingObject(object existingEntity)
        {
            RideID id = RideID.Null;
            if (existingEntity is GameObject entityObject)
                id = AddGameObject(entityObject);

            return id;
        }

        /// <summary>
        /// Attempts to insert a Unity GameObject into the system using its instance ID.
        /// If the object is already tracked, returns the existing RideID; otherwise, searches the scene and adds it.
        /// </summary>
        /// <param name="engineGameObjectInstanceId">Unity instance ID from <c>GetInstanceID()</c>.</param>
        /// <returns>The new or existing <see cref="RideID"/> associated with the object, or <see cref="RideID.Null"/> if not found.</returns>
        public RideID InsertObject(int engineGameObjectInstanceId)
        {
            var go = GetObjectInternal(engineGameObjectInstanceId);
            if (go != null)
                return GetObject(engineGameObjectInstanceId);

            // this object isn't yet represented in the m_gameObjects map, add it
            var gos = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in gos)
            {
                if (g.GetInstanceID() == engineGameObjectInstanceId)
                {
                    //RideID id = IdentityFactory.CreateId();
                    //m_gameObjects.Add(id, go);

                    return AddGameObject(g);
                }
            }

            return RideID.Null;
        }

        /// <summary>
        /// Looks up a RideID associated with a Unity GameObject instance ID.
        /// </summary>
        /// <param name="engineGameObjectInstanceId">The Unity instance ID, typically from <c>gameObject.GetInstanceID()</c>.</param>
        /// <returns>The associated <see cref="RideID"/> if found, or <see cref="RideID.Null"/> otherwise.</returns>
        public RideID GetObject(int engineGameObjectInstanceId)
        {
            foreach (var kvp in m_gameObjects)
                if (kvp.Value.GetInstanceID() == engineGameObjectInstanceId)
                    return kvp.Key;

            return RideID.Null;
        }

        /// <inheritdoc/>
        public RideID GetObject(object engineObject)
        {
            if (engineObject is GameObject unityGameObject)
                foreach (var kvp in m_gameObjects)
                    if (kvp.Value == unityGameObject)
                        return kvp.Key;

            return RideID.Null;
        }

        /// <inheritdoc/>
        public bool TryGetObject(object engineObject, out RideID id)
        {
            id = GetObject(engineObject);
            return id != RideID.Null;
        }

        /// <inheritdoc/>
        public IEnumerable<RideID> GetAll() => m_gameObjects.Keys;

        /// <inheritdoc/>
        public RideID Find(string objName)
        {
            foreach (var kvp in m_gameObjects)
                if (kvp.Value.name == objName)
                    return kvp.Key;

            RideLog.LogError($"UnityGameObjectSystem failed to find object with name {objName}");
            return RideID.Null;
        }

        /// <inheritdoc/>
        public int GetEngineObjectId(RideID rideId)
        {
            if (TryGetGameObject(rideId, out var go))
                return go.GetInstanceID();

            return 0;
        }

        /// <inheritdoc/>
        public bool Exists(RideID id) => TryGetGameObject(id, out var _);

        /// <inheritdoc/>
        public string GetName(RideID rideId)
        {
            if (TryGetGameObject(rideId, out var go))
            {
                return go.name;
            }
            else
            {
                //RideLog.LogError($"Failed to GetName of gameobject {go}");
                return "";
            }
        }

        /// <inheritdoc/>
        public void SetName(RideID rideId, string name)
        {
            if (TryGetGameObject(rideId, out var go))
                go.name = name;
            else
                RideLog.LogError($"Failed to SetName of gameobject {go}");
        }

        /// <inheritdoc/>
        public void SetActive(RideID rideId, bool active)
        {
            if (TryGetGameObject(rideId, out var go))
                go.SetActive(active);
            else
                RideLog.LogError($"Failed to SetActive of gameobject {go}");
        }

        /// <inheritdoc/>
        public void Destroy(RideID rideId, float delay = 0)
        {
            if (TryGetGameObject(rideId, out var go))
            {
                Systems.WorldState?.DispatchEvent(WorldEvent.entityDataDestroyed, new EntityEvent(rideId));
                Destroy(go, delay);
                m_gameObjects.Remove(rideId);
            }
        }

        /// <inheritdoc/>
        public void ResetEntity(RideID rideId)
        {
            if (Exists(rideId))
                Systems.WorldState.DispatchEvent(WorldEvent.entityReset, new EntityEvent(rideId));
        }

        #endregion

        #region ITransformSystem

        /// <inheritdoc/>
        public void SetPosition(RideID transform, RideVector3 pos)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.position = pos;
            else
                RideLog.LogError($"Failed to SetPosition of transform {transform}");
        }

        /// <inheritdoc/>
        public RideVector3 GetPosition(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.position;
            }
            else
            {
                RideLog.LogError($"Failed to GetPosition of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public void SetLocalPosition(RideID transform, RideVector3 pos)
        {
            if (TryGetGameObject(transform, out var go))
            {
                go.transform.localPosition = pos;
            }
            else
            {
                RideLog.LogError($"Failed to SetLocalPosition of transform {transform}");
            }
        }

        /// <inheritdoc/>
        public RideVector3 GetLocalPosition(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.localPosition;
            }
            else
            {
                RideLog.LogError($"Failed to GetLocalPosition of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public void SetRotation(RideID transform, RideQuaternion rot)
        {
            if (TryGetGameObject(transform, out var go))
            {
                go.transform.rotation = rot;
            }
            else
            {
                RideLog.LogError($"Failed to SetRotation of transform {transform}");
            }
        }

        /// <inheritdoc/>
        public RideQuaternion GetRotation(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.rotation;
            }
            else
            {
                RideLog.LogError($"Failed to GetRotation {transform}.");
                return RideQuaternion.identity;
            }
        }

        /// <inheritdoc/>
        public void SetLocalRotation(RideID transform, RideQuaternion rot)
        {
            if (TryGetGameObject(transform, out var go))
            {
                go.transform.localRotation = rot;
            }
            else
            {
                RideLog.LogError($"Failed to SetLocalRotation of transform {transform}");
            }
        }

        /// <inheritdoc/>
        public RideQuaternion GetLocalRotation(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.localRotation;
            }
            else
            {
                RideLog.LogError($"Failed to GetLocalRotation {transform}.");
                return RideQuaternion.identity;
            }
        }

        /// <inheritdoc/>
        public void Rotate(RideID transform, RideVector3 eulers)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.Rotate(eulers);
            else
                RideLog.LogError($"Failed to Rotate {transform}. Does not exist");
        }

        /// <inheritdoc/>
        public void RotateAround(RideID transform, RideVector3 point, RideVector3 axis, float angle)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.RotateAround(point, axis, angle);
            else
                RideLog.LogError($"Failed to RotateAround {transform}. Does not exist");
        }

        /// <inheritdoc/>
        public void LookAt(RideID transform, RideVector3 point)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.LookAt(point.ToVector3());
        }

        /// <inheritdoc/>
        public void SetForward(RideID transform, RideVector3 dir)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.forward = dir;
            else
                RideLog.LogError($"Failed to SetForward of transform {transform}");
        }

        /// <inheritdoc/>
        public RideVector3 GetForwardDirection(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.forward;
            }
            else
            {
                RideLog.LogError($"Failed to GetForward of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public RideVector3 GetRightDirection(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.right;
            }
            else
            {
                RideLog.LogError($"Failed to GetRight of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public RideVector3 GetUpDirection(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.up;
            }
            else
            {
                RideLog.LogError($"Failed to GetUp of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public RideVector3 Translate(RideID transform, RideVector3 translation)
        {
            if (TryGetGameObject(transform, out var go))
            {
                go.transform.Translate(translation);
                return go.transform.position;
            }
            else
            {
                RideLog.LogError($"Failed to Translate of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public RideVector3 TranslateLocal(RideID transform, RideVector3 translation)
        {
            if (TryGetGameObject(transform, out var go))
            {
                go.transform.Translate(translation, Space.Self);
                return go.transform.localPosition;
            }
            else
            {
                RideLog.LogError($"Failed to TranslateLocal of transform {transform}");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public void SetParent(RideID parent, RideID child)
        {
            if (TryGetGameObject(parent, out var goParent) && TryGetGameObject(child, out var goChild))
                goChild.transform.SetParent(goParent.transform);
            else
                RideLog.LogError($"Failed to SetParent of child {child} to parent {parent}");
        }

        /// <inheritdoc/>
        public RideID GetParent(RideID child)
        {
            if (!TryGetGameObject(child, out var goChild))
                return RideID.Null;

            var parentTransform = goChild.transform.parent;
            if (parentTransform == null)
                return RideID.Null;

            var goParent = parentTransform.gameObject;

            foreach (var kvp in m_gameObjects)
            {
                if (kvp.Value == goParent)
                    return kvp.Key;
            }

            return RideID.Null;
        }

        /// <inheritdoc/>
        public RideID GetChild(RideID transform, string childName)
        {
            if (!TryGetGameObject(transform, out var go))
            {
                RideLog.LogError($"GameObjectSystemUnity.GetChild() failed: parent {transform} not found");
                return RideID.Null;
            }

            var childTransform = go.transform.Find(childName);
            if (childTransform == null)
            {
                RideLog.LogError($"GameObjectSystemUnity.GetChild() failed: no child named '{childName}' under transform {transform}");
                return RideID.Null;
            }

            return GetObject(childTransform.gameObject.GetInstanceID());
        }

        /// <inheritdoc/>
        public RideID GetChild(RideID transform, int childIndex)
        {
            if (!TryGetGameObject(transform, out var go))
            {
                RideLog.LogError($"GetChild failed: parent {transform} not found");
                return RideID.Null;
            }

            var t = go.transform;
            if (childIndex < 0 || childIndex >= t.childCount)
            {
                RideLog.LogError($"GetChild failed: index {childIndex} out of bounds for transform {transform}. Range: 0–{t.childCount - 1}");
                return RideID.Null;
            }

            var child = t.GetChild(childIndex);
            return GetObject(child.gameObject.GetInstanceID());
        }

        /// <inheritdoc/>
        public int GetChildCount(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
                return go.transform.childCount;
            else
                RideLog.LogError($"Failed to GetChildCount of transform {transform}");

            return 0;
        }

        /// <inheritdoc/>
        public void DestroyChildren(RideID parent)
        {
            if (TryGetGameObject(parent, out var go))
                VHUtils.DestroyChildren(go.transform);
            else
                RideLog.LogError($"Failed to DestroyChildren of parent {parent}");
        }

        /// <inheritdoc/>
        public void SetLocalScale(RideID transform, RideVector3 scale)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.localScale = scale;
            else
                RideLog.LogError($"Failed to SetLocalScale {transform}. Does not exist");
        }

        /// <inheritdoc/>
        public RideVector3 GetLocalScale(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
                return go.transform.localScale;
            else
                RideLog.LogError($"Failed to GetLocalScale {transform}. Does not exist");

            return RideVector3.zero;
        }

        /// <inheritdoc/>
        public RideVector3 GetLossyScale(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
                return go.transform.lossyScale;
            else
                RideLog.LogError($"Failed to GetLossyScale {transform}. Does not exist");

            return RideVector3.zero;
        }

        /// <inheritdoc/>
        public RideVector3 TransformPoint(RideID transform, RideVector3 point)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.TransformPoint(point);
            }
            else
            {
                RideLog.LogError($"Failed to calculate TransformPoint of point {point} using transform {transform}.");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public RideVector3 InverseTransformPoint(RideID transform, RideVector3 point)
        {
            if (TryGetGameObject(transform, out var go))
            {
                return go.transform.InverseTransformPoint(point);
            }
            else
            {
                RideLog.LogError($"Failed to calculate InverseTransformPoint of point {point} using transform {transform}.");
                return RideVector3.zero;
            }
        }

        /// <inheritdoc/>
        public void SetAsFirstSibling(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.SetAsFirstSibling();
            else
                RideLog.LogError($"Failed to SetAsFirstSibling {transform}. Does not exist");
        }

        /// <inheritdoc/>
        public void SetAsLastSibling(RideID transform)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.SetAsLastSibling();
            else
                RideLog.LogError($"Failed to SetAsLastSibling {transform}. Does not exist");
        }

        /// <inheritdoc/>
        public void SetSiblingIndex(RideID transform, int index)
        {
            if (TryGetGameObject(transform, out var go))
                go.transform.SetSiblingIndex(index);
            else
                RideLog.LogError($"Failed to SetSiblingIndex {transform}. Does not exist");
        }

        #endregion

        #region IComponentSystem

        /// <inheritdoc/>
        public T GetComponent<T>(RideID owner)
        {
            if (TryGetGameObject(owner, out var go))
                return go.GetComponent<T>();

            return default;
        }

        /// <inheritdoc/>
        public T GetComponentInChildren<T>(RideID owner, bool includeInactive = false)
        {
            if (TryGetGameObject(owner, out var go))
                return go.GetComponentInChildren<T>(includeInactive);

            return default;
        }

        /// <inheritdoc/>
        public T[] GetComponentsInChildren<T>(RideID owner, bool includeInactive = false)
        {
            if (TryGetGameObject(owner, out var go))
                return go.GetComponentsInChildren<T>(includeInactive);

            return Array.Empty<T>();
        }

        /// <inheritdoc/>
        public T AddComponent<T>(RideID owner) where T : Component
        {
            if (TryGetGameObject(owner, out var go))
            {
                var comp = go.GetComponent<T>();
                if (comp == null)
                    return go.AddComponent<T>();
                else
                    RideLog.Log($"Component {comp.GetType()} already exsists on entity {owner}");
            }

            return default;
        }

        #endregion


        /// <summary>
        /// Removes the provided RideID from the dict but does not destroy the game object in question
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(RideID obj)
        {
            if (Exists(obj))
                m_gameObjects.Remove(obj);
        }

        public void Insert(RideID obj, GameObject go)
        {
            if (!Exists(obj))
                m_gameObjects.Add(obj, go);
        }

        public GameObject GetGameObject(RideID rideId) => TryGetGameObject(rideId, out var go) ? go : null;

        public RideID GetRideID(GameObject go)
        {
            foreach (var kvp in m_gameObjects)
                if (kvp.Value == go)
                    return kvp.Key;

            return RideID.Null;
        }

        private bool TryGetGameObject(RideID id, out GameObject go)
        {
            if (m_gameObjects.TryGetValue(id, out go) && go != null)
                return true;

            go = null;
            return false;
        }

        private GameObject GetObjectInternal(int engineGameObjectInstanceId)
        {
            foreach (var kvp in m_gameObjects)
                if (kvp.Value.GetInstanceID() == engineGameObjectInstanceId)
                    return kvp.Value;

            return null;
        }

        /// <summary>
        /// Registers a new GameObject and returns a RideID handle. Also triggers world state events
        /// for bootstrap components and RideMonoBehaviours attached to the object.
        /// </summary>
        /// <param name="go">The Unity GameObject to register.</param>
        /// <param name="data">Optional custom <see cref="EntityData"/> to include in creation events. If null, it will be auto-populated from the GameObject.</param>
        /// <returns>A new <see cref="RideID"/> uniquely identifying this object within the simulation.</returns>
        private RideID AddGameObject(GameObject go, EntityData data = null)
        {
            RideID id = IdentityFactory.CreateId();
            m_gameObjects.Add(id, go);

            var bootstrappers = go.GetComponents<RideDataUnityBootstrap>();
            foreach (var bootstrap in bootstrappers)
            {
                var eventData = data ?? new EntityData
                {
                    id = id,
                    name = go.name,
                    position = go.transform.position,
                    rotation = go.transform.rotation
                };

                Systems.WorldState?.DispatchEvent(WorldEvent.entityDataCreated, new EntityCreatedEvent(id, eventData, bootstrap.GetData()));
            }

            Systems.WorldState?.DispatchEvent(WorldEvent.entityDataCreationComplete, new EntityEvent(id));

            var rideMonos = go.GetComponents<RideMonoBehaviour>();
            foreach (var rideMono in rideMonos)
                rideMono.id = id;

            go.SetActive(true);

            return id;
        }
    }
}
