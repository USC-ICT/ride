using System;

namespace Ride
{
    /// <summary>
    /// Represents an axis-aligned bounding box similar to UnityEngine.Bounds, designed to decouple
    /// RIDE logic from Unity dependencies. Stores center and extents (half-size) as <see cref="RideVector3"/>,
    /// and provides convenience accessors for size. Implicit conversions enable interoperability with
    /// Unity's Bounds, while preserving a clean and testable abstract layer when needed.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Bounds.html">UnityEngine.Bounds</a>.
    /// </summary>
    public struct RideBounds : IEquatable<RideBounds>
    {
        /// <summary>The center point of the volume</summary>
        private RideVector3 center;

        /// <summary>The length of the volume on each axis</summary>
        private RideVector3 extents;

        public RideVector3 Center { get => center; set => center = value; }
        public RideVector3 Extents { get => extents; set => extents = value; }
        public RideVector3 Size { get => extents * 2; set => extents = value * 0.5f; }

        public RideBounds(RideVector3 _center, RideVector3 _size)
        {
            center = _center;
            extents = _size * 0.5f;
        }

        public UnityEngine.Bounds ToBounds() => new UnityEngine.Bounds(Center, Size);

        static public implicit operator UnityEngine.Bounds(RideBounds bounds) => bounds.ToBounds();
        static public implicit operator RideBounds(UnityEngine.Bounds bounds) => new RideBounds(bounds.center, bounds.size);

        public static bool operator ==(RideBounds a, RideBounds b) => a.Equals(b);
        public static bool operator !=(RideBounds a, RideBounds b) => !a.Equals(b);

        public RideVector3 ClosestPoint(RideVector3 pos) => ToBounds().ClosestPoint(pos);
        public bool Contains(RideVector3 pos) => ToBounds().Contains(pos);

        public bool Equals(RideBounds other) => center.Equals(other.center) && extents.Equals(other.extents);
        public override bool Equals(object obj) => obj is RideBounds other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(center, extents);
        public override string ToString() => ToBounds().ToString();
    }
}
