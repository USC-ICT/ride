using UnityEngine;

namespace Ride
{
    /// <summary>Describes an object found within a viewer's visibility volume.</summary>
    /// <remarks>
    /// <see cref="RideUtils.GetVisibleObjects(float, float, float, float, RideVector3, RideQuaternion)"/>
    /// produces these records for visibility and perception queries. The current record contains
    /// spatial information only; the retained TODOs identify planned type flags and an object lookup
    /// index, while the commented fields show earlier identity and attribute approaches.
    /// </remarks>
    public struct VisibleObject
    {
        //GameObject gameObject;

        /// <summary>Distance from the viewer to the object.</summary>
        public float distance;

        /// <summary>Normalized world-space direction from the viewer to the object.</summary>
        public RideVector3 direction;

        /// <summary>World-space bounds of the visible object.</summary>
        public RideBounds bounds;
        //string[] attributes;

        // TODO: add bitfield for object type flags (building, tree, etc)
        // TODO: add gameobject index for lookup
    }
}
