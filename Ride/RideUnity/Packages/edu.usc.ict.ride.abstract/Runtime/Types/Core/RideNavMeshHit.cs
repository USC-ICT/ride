using System;

namespace Ride
{
    /// <summary>
    /// Represents a NavMesh hit result similar to UnityEngine.AI.NavMeshHit, designed to decouple core logic in RIDE
    /// from UnityEngine.AI dependencies. This struct encapsulates hit position, surface normal, query distance,
    /// area mask, and a validity flag.
    ///
    /// This type enables consistent handling of navigation mesh queries in both Unity and non-Unity environments,
    /// using RideVector3 instead of UnityEngine.Vector3 for portability and testability.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/AI.NavMeshHit.html">UnityEngine.AI.NavMeshHit</a>
    /// </summary>
    [Serializable]
    public struct RideNavMeshHit : IEquatable<RideNavMeshHit>
    {
        public RideVector3 position;
        public RideVector3 normal;
        public float distance;
        public int mask;
        public bool hit;

        public RideNavMeshHit(RideVector3 _position, RideVector3 _normal, float _distance, int _mask, bool _hit)
        {
            position = _position;
            normal = _normal;
            distance = _distance;
            mask = _mask;
            hit = _hit;
        }

        public RideNavMeshHit(UnityEngine.AI.NavMeshHit hit) : this(hit.position, hit.normal, hit.distance, hit.mask, hit.hit) { }


        public static bool operator ==(RideNavMeshHit left, RideNavMeshHit right) => left.Equals(right);
        public static bool operator !=(RideNavMeshHit left, RideNavMeshHit right) => !left.Equals(right);

        public bool Equals(RideNavMeshHit other)
        {
            return hit == other.hit &&
                   position.Equals(other.position) &&
                   normal.Equals(other.normal) &&
                   distance.Equals(other.distance) &&
                   mask == other.mask;
        }

        public override bool Equals(object obj) => obj is RideNavMeshHit other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(position, normal, distance, mask, hit);
        public override string ToString() => $"Hit: {hit}, Position: {position}, Normal: {normal}, Distance: {distance}, Mask: {mask}";
    }
}
