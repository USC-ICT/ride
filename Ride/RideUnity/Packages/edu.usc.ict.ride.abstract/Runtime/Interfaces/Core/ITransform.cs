namespace Ride
{
    /// <summary>
    /// Represents the position and orientation of an object.
    /// This is a parallel class to <a href="https://docs.unity3d.com/ScriptReference/Transform.html">UnityEngine.Transform</a>.
    /// Implemented separately to abstract Ride classes away from UnityEngine specific implementations.
    /// </summary>
    public interface ITransform
    {
        /// <summary>The world position of the object.</summary>
        RideVector3 position { get; set; }

        /// <summary>The local position of the object, relative to its parent transform.</summary>
        RideVector3 localPosition { get; set; }

        /// <summary>The world rotation of the object.</summary>
        RideQuaternion rotation { get; set; }

        /// <summary>The local rotation of the object, relative to its parent transform.</summary>
        RideQuaternion localRotation { get; set; }

        /// <summary>The world euler Angles of the object.</summary>
        RideVector3 eulerAngles { get; set; }

        /// <summary>The local Euler angles of the object, relative to its parent.</summary>
        RideVector3 localEulerAngles { get; set; }

        /// <summary>The forward direction of the object in world space.</summary>
        RideVector3 forward { get; set; }

        /// <summary>Gets or sets the right direction of the object in world space.</summary>
        RideVector3 right { get; set; }

        /// <summary>
        /// Rotates the object to face a specified world-space target point.
        /// </summary>
        /// <param name="target">The world position to face.</param>
        void LookAt(RideVector3 target);

        /// <summary>
        /// Rotates the transform around a given axis and point in world space.
        /// This affects both the position and rotation of the transform.
        /// </summary>
        /// <param name="point">The pivot point in world space.</param>
        /// <param name="axis">The axis to rotate around (in world space).</param>
        /// <param name="angle">The rotation angle in degrees.</param>
        void RotateAround(RideVector3 point, RideVector3 axis, float angle);

        /// <summary>
        /// Sets the parent transform for this object, making it a child of the given transform.
        /// The local position and rotation are updated accordingly.
        /// </summary>
        /// <param name="parent">The parent transform to attach to.</param>
        void SetParent(ITransform parent);
    }
}
