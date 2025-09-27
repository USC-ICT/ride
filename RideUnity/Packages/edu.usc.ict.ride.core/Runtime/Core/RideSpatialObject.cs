using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Represents a spatial object within the RIDE framework.
    /// This component provides spatial bounds and extents information
    /// by referencing the first child collider found in the hierarchy.
    ///
    /// Implements <see cref="ISpatialObject"/>.
    /// </summary>
    public class RideSpatialObject : RideMonoBehaviour, ISpatialObject
    {
        /// <summary>Gets the world-space bounding box of the object from its child collider.</summary>
        public virtual RideBounds bounds => GetComponentInChildren<Collider>().bounds;

        /// <summary>
        /// Gets the extents of the bounding box from its child collider.
        /// Returns <c>RideVector3.zero</c> if no collider is found.
        /// </summary>
        public virtual RideVector3 extents
        {
            get
            {
                var col = GetComponentInChildren<Collider>();
                return col != null ? col.bounds.extents : RideVector3.zero;
            }
        }
    }
}
