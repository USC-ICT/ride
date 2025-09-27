using UnityEngine;

namespace Ride
{
    public struct VisibleObject
    {
        //GameObject gameObject;
        public float distance;
        public RideVector3 direction;
        public RideBounds bounds;
        //string[] attributes;

        // TODO: add bitfield for object type flags (building, tree, etc)
        // TODO: add gameobject index for lookup
    }
}
