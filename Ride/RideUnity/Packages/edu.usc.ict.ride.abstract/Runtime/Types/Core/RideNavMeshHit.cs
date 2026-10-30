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
        /// <summary>The sampled position on the navigation mesh.</summary>
        public RideVector3 position;
        /// <summary>The surface normal at the sampled position.</summary>
        public RideVector3 normal;
        /// <summary>The distance between the query origin and sampled position.</summary>
        public float distance;
        /// <summary>The navigation-area mask associated with the result.</summary>
        public int mask;
        /// <summary>Whether the query found a navigation-mesh position.</summary>
        public bool hit;

        /// <summary>Initializes a navigation-mesh query result.</summary>
        /// <param name="_position">The sampled position.</param>
        /// <param name="_normal">The sampled surface normal.</param>
        /// <param name="_distance">The query distance.</param>
        /// <param name="_mask">The navigation-area mask.</param>
        /// <param name="_hit">Whether the query succeeded.</param>
        public RideNavMeshHit(RideVector3 _position, RideVector3 _normal, float _distance, int _mask, bool _hit)
        {
            position = _position;
            normal = _normal;
            distance = _distance;
            mask = _mask;
            hit = _hit;
        }

        /// <summary>Initializes a RIDE navigation hit from a Unity navigation hit.</summary>
        /// <param name="hit">The Unity navigation result.</param>
        public RideNavMeshHit(UnityEngine.AI.NavMeshHit hit) : this(hit.position, hit.normal, hit.distance, hit.mask, hit.hit) { }


        /// <summary>Determines whether two navigation hits contain identical data.</summary>
        public static bool operator ==(RideNavMeshHit left, RideNavMeshHit right) => left.Equals(right);
        /// <summary>Determines whether two navigation hits differ.</summary>
        public static bool operator !=(RideNavMeshHit left, RideNavMeshHit right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(RideNavMeshHit other)
        {
            return hit == other.hit &&
                   position.Equals(other.position) &&
                   normal.Equals(other.normal) &&
                   distance.Equals(other.distance) &&
                   mask == other.mask;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RideNavMeshHit other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(position, normal, distance, mask, hit);
        /// <inheritdoc/>
        public override string ToString() => $"Hit: {hit}, Position: {position}, Normal: {normal}, Distance: {distance}, Mask: {mask}";
    }
}
