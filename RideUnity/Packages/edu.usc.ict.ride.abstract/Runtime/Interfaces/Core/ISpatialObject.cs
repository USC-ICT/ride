namespace Ride
{
    /// <summary>
    /// Represents an object (visible or invisible) that takes up space in the world.
    /// This includes a position, rotation, and scale via <see cref="ITransform"/>, and provides
    /// additional bounding information (<see cref="Bounds"/> and <see cref="RideVector3">extents</see>) used for physics, selection, and visibility.
    /// </summary>
    public interface ISpatialObject : ITransform
    {
        /// <summary>
        /// Gets the full bounding volume that encloses this object in world space.
        /// This is typically used for spatial queries, occlusion checks, or physics tests.
        /// </summary>
        RideBounds bounds { get; }

        /// <summary>
        /// Gets the extents of the bounding volume.
        /// This represents the half-size of the object on each axis, measured from the bounds center.
        /// Equivalent to <c>bounds.extents</c>.
        /// </summary>
        RideVector3 extents { get; }
    }
}
