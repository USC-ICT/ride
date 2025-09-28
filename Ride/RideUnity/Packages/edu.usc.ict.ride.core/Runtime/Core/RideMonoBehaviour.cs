using System;
using UnityEngine;
using Ride.Entities;

namespace Ride
{
    /// <summary>
    /// Base class for all RIDE scene objects, combining Unity's MonoBehaviour with the RIDE-specific <see cref="IEntity"/> and <see cref="ITransform"/> interfaces.
    /// Provides access to the object's globally unique <see cref="RideID"/>, attribute flags (<see cref="EntityAttributes"/>), and RIDE-style transform operations.
    /// 
    /// This class acts as the foundation for all systems that need to register, query, or manipulate objects using RIDE's API layer.
    /// Typical usages include:
    /// - Entity identification via ID or attributes
    /// - Transform access in <see cref="RideVector3"/> and <see cref="RideQuaternion"/> space
    /// - System queries using <c>GetEntity&lt;T&gt;()</c> or <c>HasAttributes()</c>
    /// 
    /// For more advanced registration workflows, this class optionally participates in the ConvertToRide pattern:
    /// See <see cref="ConvertToRide(RideID)"/> for details.
    /// 
    /// See usage in <c>ConvertToRide.cs</c>    
    /// </summary>
    public class RideMonoBehaviour : MonoBehaviour, IEntity, ITransform
    {
        public EntityAttributes attributes { get; set; }

        public RideID id { get; set; } = IdentityFactory.CreateId();


        protected virtual void Start() { }
        protected virtual void Update() { }


        #region IEntity

        public virtual T GetEntity<T>() where T : IEntity => GetComponent<T>();
        public virtual bool HasAttributes(EntityAttributes att) => (attributes & att) == att;

        #endregion

        #region ITransform

        public virtual RideVector3 position { get => transform.position; set => transform.position = value; }
        public virtual RideVector3 localPosition { get => transform.localPosition; set => transform.localPosition = value; }
        public virtual RideQuaternion rotation { get => transform.rotation; set => transform.rotation = value; }
        public virtual RideQuaternion localRotation { get => transform.localRotation; set => transform.localRotation = value; }
        public virtual RideVector3 eulerAngles { get => transform.eulerAngles; set => transform.eulerAngles = value; }
        public virtual RideVector3 localEulerAngles { get => transform.localEulerAngles; set => transform.localEulerAngles = value; }
        public virtual RideVector3 forward { get => transform.forward; set => transform.forward = value; }
        public virtual RideVector3 right { get => transform.right; set => transform.right = value; }
        public virtual void LookAt(RideVector3 target) => transform.LookAt(target);
        public virtual void RotateAround(RideVector3 point, RideVector3 axis, float angle) => transform.RotateAround(point, axis, angle);
        public virtual void SetParent(ITransform parent) => transform.SetParent(((RideMonoBehaviour)parent).transform);

        #endregion

        /// <summary>
        /// Optional hook called by <c>ConvertToRide</c> (see ConvertToRide.cs) to associate this scene object with a specific <see cref="RideID"/> at runtime.
        /// 
        /// This function is typically invoked when scanning the scene for MonoBehaviours that should become part of the RIDE entity system.
        /// Override this method to:
        /// - Set the object's RideID explicitly
        /// - Register with a RIDE subsystem
        /// - Trigger system-specific initialization
        /// 
        /// This is only called when explicitly converting objects; normal runtime usage does not require overriding this.
        /// </summary>
        public virtual void ConvertToRide(RideID id) { }
    }
}
