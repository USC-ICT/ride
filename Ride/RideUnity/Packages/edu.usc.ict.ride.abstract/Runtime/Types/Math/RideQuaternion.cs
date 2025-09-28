using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a quaternion rotation struct similar to UnityEngine.Quaternion, designed to decouple core logic in RIDE from Unity dependencies.
    /// This struct provides a consistent rotation interface usable in both Unity and non-Unity environments, supporting quaternion math,
    /// scalar operations, comparisons, and conversions to/from Unity's types. Implicit conversions enable easy interoperability,
    /// while preserving a clean and testable abstract layer when desired.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Quaternion.html">UnityEngine.Quaternion</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideQuaternion : IEquatable<RideQuaternion>
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public RideQuaternion(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public RideQuaternion(UnityEngine.Quaternion quat) : this(quat.x, quat.y, quat.z, quat.w) { }

        public static readonly RideQuaternion identity = new RideQuaternion(0, 0, 0, 1);

        public RideVector3 eulerAngles => ToQuaternion().eulerAngles;

        public UnityEngine.Quaternion ToQuaternion() => new UnityEngine.Quaternion(x, y, z, w);

        static public explicit operator RideVector4(RideQuaternion rot) => new RideVector4(rot.x, rot.y, rot.z, rot.w);

        static public implicit operator RideQuaternion(UnityEngine.Quaternion rot) => new RideQuaternion(rot.x, rot.y, rot.z, rot.w);
        static public implicit operator UnityEngine.Quaternion(RideQuaternion rot) => new UnityEngine.Quaternion(rot.x, rot.y, rot.z, rot.w);

        public static RideQuaternion operator * (RideQuaternion lhs, RideQuaternion rhs) => lhs.ToQuaternion() * rhs.ToQuaternion();
        static public RideVector3 operator *(RideQuaternion quat, RideVector3 vec) => quat.ToQuaternion() * vec.ToVector3();

        public static bool operator ==(RideQuaternion lhs, RideQuaternion rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        public static bool operator !=(RideQuaternion lhs, RideQuaternion rhs) => !(lhs == rhs);

        /// <summary>Returns true if this vector approximately equals another, within a given epsilon.</summary>
        public bool ApproximatelyEquals(RideQuaternion other, float epsilon = 1e-5f)
        {
            return RideMath.Abs(x - other.x) < epsilon &&
                   RideMath.Abs(y - other.y) < epsilon &&
                   RideMath.Abs(z - other.z) < epsilon &&
                   RideMath.Abs(w - other.w) < epsilon;
        }

        public static RideQuaternion Euler(float x, float y, float z) => UnityEngine.Quaternion.Euler(x, y, z);
        public static RideQuaternion Euler(RideVector3 euler) => UnityEngine.Quaternion.Euler(euler);
        public static float Angle(RideQuaternion a, RideQuaternion b) => UnityEngine.Quaternion.Angle(a, b);
        public static RideQuaternion Lerp(RideQuaternion a, RideQuaternion b, float t) => UnityEngine.Quaternion.Lerp(a, b, t);
        public static RideQuaternion LookRotation(RideVector3 forward) => UnityEngine.Quaternion.LookRotation(forward);

        public override bool Equals(object obj) => obj is RideQuaternion other && Equals(other);
        public bool Equals(RideQuaternion other) => this == other;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() => ToQuaternion().ToString();
    }
}
