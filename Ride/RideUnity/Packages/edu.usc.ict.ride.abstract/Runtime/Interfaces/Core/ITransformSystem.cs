namespace Ride
{
    /// <summary>
    /// Defines the system interface responsible for spatial transformation of entities,
    /// including position, rotation, scale, parenting, and world/local coordinate conversion.
    /// 
    /// This system centralizes transform manipulation for all objects identified by <see cref="RideID"/>,
    /// enabling consistent handling of hierarchy, alignment, motion, and direction across simulation layers.
    /// 
    /// It is the authoritative way to modify transform state in RIDE and should be used instead of
    /// directly modifying Unity transforms where abstraction is required.
    /// </summary>
    public interface ITransformSystem : IRideSystem
    {
        #region Position & Rotation

        /// <summary>
        /// Sets the world-space position of the transform.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="pos">The new world-space position.</param>
        void SetPosition(RideID transform, RideVector3 pos);

        /// <summary>
        /// Gets the world-space position of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The current world-space position.</returns>
        RideVector3 GetPosition(RideID transform);

        /// <summary>
        /// Sets the local-space position of the transform. If the transform has no parent, this is equivalent to <see cref="SetPosition"/>.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="pos">The new local-space position.</param>
        void SetLocalPosition(RideID transform, RideVector3 pos);

        /// <summary>
        /// Gets the local-space position of the transform, relative to its parent.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The current local-space position.</returns>
        RideVector3 GetLocalPosition(RideID transform);

        /// <summary>
        /// Sets the world-space rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="rot">The new world-space rotation.</param>
        void SetRotation(RideID transform, RideQuaternion rot);

        /// <summary>
        /// Gets the world-space rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The current world-space rotation.</returns>
        RideQuaternion GetRotation(RideID transform);

        /// <summary>
        /// Sets the local-space rotation of the transform, relative to its parent.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="rot">The new local-space rotation.</param>
        void SetLocalRotation(RideID transform, RideQuaternion rot);

        /// <summary>
        /// Gets the local-space rotation of the transform, relative to its parent.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The current local-space rotation.</returns>
        RideQuaternion GetLocalRotation(RideID transform);

        /// <summary>
        /// Applies an Euler-angle-based rotation to the transform, in world space.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="eulers">Euler angles in degrees.</param>
        void Rotate(RideID transform, RideVector3 eulers);

        /// <summary>
        /// Rotates the transform around a world-space point and axis.
        /// This affects both the position and rotation of the transform.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="point">The pivot point in world space.</param>
        /// <param name="axis">The rotation axis in world space.</param>
        /// <param name="angle">Rotation angle in degrees.</param>
        void RotateAround(RideID transform, RideVector3 point, RideVector3 axis, float angle);

        /// <summary>
        /// Rotates the transform so that its forward vector points at a target world-space point.
        /// </summary>
        /// <param name="transform">The transform to rotate.</param>
        /// <param name="point">The target world-space point to face.</param>
        void LookAt(RideID transform, RideVector3 point);

        /// <summary>
        /// Sets the forward direction of the transform in world space.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="dir">The new forward direction (normalized vector expected).</param>
        void SetForward(RideID transform, RideVector3 dir);

        /// <summary>
        /// Gets the world-space forward direction vector of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The forward direction vector in world space.</returns>
        RideVector3 GetForwardDirection(RideID transform);

        /// <summary>
        /// Gets the world-space right direction vector of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The right direction vector in world space.</returns>
        RideVector3 GetRightDirection(RideID transform);

        /// <summary>
        /// Gets the world-space up direction vector of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The up direction vector in world space.</returns>
        RideVector3 GetUpDirection(RideID transform);

        /// <summary>
        /// Moves the transform in world space by the given offset vector.
        /// </summary>
        /// <param name="transform">The transform to move.</param>
        /// <param name="translation">The movement vector in world space.</param>
        /// <returns>The new world position after translation.</returns>
        RideVector3 Translate(RideID transform, RideVector3 translation);

        /// <summary>
        /// Moves the transform in local space by the given offset vector.
        /// </summary>
        /// <param name="transform">The transform to move.</param>
        /// <param name="translation">The movement vector in local space.</param>
        /// <returns>The new local position after translation.</returns>
        RideVector3 TranslateLocal(RideID transform, RideVector3 translation);

        #endregion

        #region Parent & Hierarchy

        /// <summary>
        /// Attaches the child transform to the specified parent transform.
        /// </summary>
        /// <param name="parent">The parent transform.</param>
        /// <param name="child">The child transform to reparent.</param>
        void SetParent(RideID parent, RideID child);

        /// <summary>
        /// Gets the parent transform of the specified child.
        /// </summary>
        /// <param name="child">The transform whose parent is being queried.</param>
        /// <returns>The parent RideID, or <see cref="RideID.Null"/> if none exists.</returns>
        RideID GetParent(RideID child);

        /// <summary>
        /// Retrieves the child transform by name. This performs a recursive search of the hierarchy.
        /// </summary>
        /// <param name="transform">The parent transform to search under.</param>
        /// <param name="childName">The name of the child transform to find.</param>
        /// <returns>The child's RideID if found, or <see cref="RideID.Null"/> otherwise.</returns>
        /// <remarks>
        /// This method may return <see cref="RideID.Null"/> for children not registered with the system.
        /// </remarks>
        RideID GetChild(RideID transform, string childName);

        /// <summary>
        /// Retrieves the direct child transform at a specific index.
        /// </summary>
        /// <param name="transform">The parent transform to query.</param>
        /// <param name="childIndex">The zero-based index of the direct child.</param>
        /// <returns>The child's RideID if found, or <see cref="RideID.Null"/> otherwise.</returns>
        /// <remarks>
        /// This method does not support recursive hierarchy traversal.
        /// </remarks>
        RideID GetChild(RideID transform, int childIndex);

        /// <summary>
        /// Gets the number of first-generation child transforms under the given parent.
        /// </summary>
        /// <param name="transform">The parent transform.</param>
        /// <returns>The number of immediate children.</returns>
        int GetChildCount(RideID transform);

        /// <summary>
        /// Destroys all child transforms of the given parent.
        /// This removes all direct children from the hierarchy and from the system.
        /// </summary>
        /// <param name="parent">The parent entity whose children should be destroyed.</param>
        void DestroyChildren(RideID parent);

        #endregion

        #region Scale

        /// <summary>
        /// Sets the local scale of the transform.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="scale">The new local scale value.</param>
        void SetLocalScale(RideID transform, RideVector3 scale);

        /// <summary>
        /// Gets the local scale of the transform.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The local scale vector.</returns>
        RideVector3 GetLocalScale(RideID transform);

        /// <summary>
        /// Gets the global (lossy) scale of the transform.
        /// This represents the total scale after accounting for parent transforms.
        /// </summary>
        /// <param name="transform">The transform to query.</param>
        /// <returns>The world-space scale vector.</returns>
        RideVector3 GetLossyScale(RideID transform);

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts a position from local space to world space based on the transform.
        /// </summary>
        /// <param name="transform">The transform providing the local coordinate space.</param>
        /// <param name="point">The local-space point to convert.</param>
        /// <returns>The world-space position.</returns>
        RideVector3 TransformPoint(RideID transform, RideVector3 point);

        /// <summary>
        /// Converts a position from world space to local space based on the transform.
        /// </summary>
        /// <param name="transform">The transform providing the local coordinate space.</param>
        /// <param name="point">The world-space point to convert.</param>
        /// <returns>The local-space position.</returns>
        RideVector3 InverseTransformPoint(RideID transform, RideVector3 point);

        #endregion

        #region Sibling Indexing & Ordering

        /// <summary>
        /// Moves the transform to be the first among its siblings in the hierarchy.
        /// </summary>
        /// <param name="transform">The transform to reorder.</param>
        void SetAsFirstSibling(RideID transform);

        /// <summary>
        /// Moves the transform to be the last among its siblings in the hierarchy.
        /// </summary>
        /// <param name="transform">The transform to reorder.</param>
        void SetAsLastSibling(RideID transform);

        /// <summary>
        /// Sets the sibling index of the transform within its parent's children.
        /// </summary>
        /// <param name="transform">The transform to reorder.</param>
        /// <param name="index">The new sibling index position.</param>
        void SetSiblingIndex(RideID transform, int index);

        #endregion
    }
}
