using System;

namespace Ride
{
    /// <summary>
    /// Stores information about the result of a raycast.
    /// This is a parallel class to <a href="https://docs.unity3d.com/ScriptReference/RaycastHit.html">UnityEngine.RaycastHit</a>.
    /// Implemented separately to abstract Ride classes away from UnityEngine specific implementations.
    /// </summary>
    [Serializable]
    public struct RideRaycastHit
    {
        public bool isHit;

        public RideID hitEntity;

        public RideVector3 point;
        public RideVector3 normal;
        public int triangleIndex;
        public float distance;
        public RideVector2 textureCoord;
        [NonSerialized] public object colliderObject;  // UnityEngine.Collider, use collider property below

        public RideRaycastHit(
            bool _isHit,
            RideID _hitEntity,
            RideVector3 _point,
            RideVector3 _normal,
            int _triangleIndex,
            float _distance,
            RideVector2 _textureCoord,
            object _collider)
        {
            isHit = _isHit;
            hitEntity = _hitEntity;
            point = _point;
            normal = _normal;
            triangleIndex = _triangleIndex;
            distance = _distance;
            textureCoord = _textureCoord;
            colliderObject = _collider;
        }

        public RideRaycastHit(bool _isHit, RideID _hitEntity, UnityEngine.RaycastHit hit) : this(_isHit, _hitEntity, hit.point, hit.normal, hit.triangleIndex, hit.distance, hit.textureCoord, hit.collider) { }

        public UnityEngine.Collider collider => colliderObject as UnityEngine.Collider;
        public UnityEngine.Transform transform
        {
            get
            {
                if (rigidbody != null)
                    return rigidbody.transform;
                if (collider != null)
                    return collider.transform;
                return null;
            }
        }
        public UnityEngine.Rigidbody rigidbody => collider != null ? collider.attachedRigidbody : null;

        public static readonly RideRaycastHit Null = new RideRaycastHit(false, RideID.Null, RideVector3.zero, RideVector3.zero, 0, 0, RideVector2.zero, default);
    }
}
