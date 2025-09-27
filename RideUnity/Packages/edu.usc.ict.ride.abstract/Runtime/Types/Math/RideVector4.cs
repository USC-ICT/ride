using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a 4D vector similar to UnityEngine.Vector4, designed to decouple core logic in RIDE from Unity dependencies.
    /// This struct supports vector math, scalar operations, comparisons, and interop with Unity types while remaining usable in non-Unity environments.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Vector4.html">UnityEngine.Vector4</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideVector4 : IEquatable<RideVector4>
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public RideVector4(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public RideVector4(RideVector2 vec) : this(vec.x, vec.y, 0, 0) { }
        public RideVector4(RideVector3 vec) : this(vec.x, vec.y, vec.z, 0) { }

        public RideVector4(UnityEngine.Vector4 vec) : this(vec.x, vec.y, vec.z, vec.w) { }
        public RideVector4(UnityEngine.Vector2 vec) : this(vec.x, vec.y, 0, 0) { }
        public RideVector4(UnityEngine.Vector3 vec) : this(vec.x, vec.y, vec.z, 0) { }

        public static readonly RideVector4 negativeInfinity = new RideVector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        public static readonly RideVector4 positiveInfinity = new RideVector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static readonly RideVector4 zero = new RideVector4(0, 0, 0, 0);
        public static readonly RideVector4 one = new RideVector4(1, 1, 1, 1);

        /// <summary>Returns the normalized version of this vector. If magnitude is 0, returns zero vector.</summary>
        public RideVector4 normalized => magnitude == 0 ? RideVector4.zero : this / magnitude;

        /// <summary>Returns the magnitude (length) of the vector.</summary>
        public float magnitude => RideMath.Sqrt(w * w + x * x + y * y + z * z);

        /// <summary>Returns the squared magnitude of the vector (avoids square root).</summary>
        public float sqrMagnitude => w * w + x * x + y * y + z * z;

        public static implicit operator RideVector4(RideVector3 pos) => new RideVector4(pos);

        public static implicit operator RideVector4(UnityEngine.Vector2 pos) => new RideVector4(pos.x, pos.y, 0, 0);
        public static implicit operator RideVector4(UnityEngine.Vector4 pos) => new RideVector4(pos.x, pos.y, pos.z, pos.w);
        public static implicit operator RideVector4(UnityEngine.Quaternion quat) => new RideVector4(quat.x, quat.y, quat.z, quat.w);
        public static implicit operator UnityEngine.Vector4(RideVector4 pos) => new UnityEngine.Vector4(pos.x, pos.y, pos.z, pos.w);

        public static RideVector4 operator +(RideVector4 lhs, RideVector4 rhs) => new RideVector4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
        public static RideVector4 operator -(RideVector4 lhs, RideVector4 rhs) => new RideVector4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);
        public static RideVector4 operator *(RideVector4 lhs, float scalar) => new RideVector4(lhs.x * scalar, lhs.y * scalar, lhs.z * scalar, lhs.w * scalar);
        public static RideVector4 operator *(float scalar, RideVector4 rhs) => new RideVector4(rhs.x * scalar, rhs.y * scalar, rhs.z * scalar, rhs.w * scalar);

        public static RideVector4 operator /(RideVector4 lhs, float scalar)
        {
            if (scalar == 0f)
                throw new DivideByZeroException("Cannot divide RideVector4 by zero.");
            return new RideVector4(lhs.x / scalar, lhs.y / scalar, lhs.z / scalar, lhs.w / scalar);
        }

        public static RideVector4 operator -(RideVector4 op) => new RideVector4(-op.x, -op.y, -op.z, -op.w);

        public static bool operator ==(RideVector4 lhs, RideVector4 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        public static bool operator !=(RideVector4 lhs, RideVector4 rhs) => !(lhs == rhs);

        public static bool operator ==(RideVector4 lhs, UnityEngine.Vector4 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        public static bool operator !=(RideVector4 lhs, UnityEngine.Vector4 rhs) => !(lhs == rhs);

        public static float Dot(RideVector4 a, RideVector4 b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        public static float Distance(RideVector4 a, RideVector4 b) => (a - b).magnitude;
        public static RideVector4 Lerp(RideVector4 a, RideVector4 b, float t) => a + ((b - a) * t);

        public static RideVector4 Min(RideVector4 lhs, RideVector4 rhs) => UnityEngine.Vector4.Min(lhs, rhs);
        public static RideVector4 Max(RideVector4 lhs, RideVector4 rhs) => UnityEngine.Vector4.Max(lhs, rhs);

        /// <summary>Returns true if this vector approximately equals another, within a given epsilon.</summary>
        public bool ApproximatelyEquals(RideVector4 other, float epsilon = 1e-5f)
        {
            return RideMath.Abs(x - other.x) < epsilon &&
                   RideMath.Abs(y - other.y) < epsilon &&
                   RideMath.Abs(z - other.z) < epsilon &&
                   RideMath.Abs(w - other.w) < epsilon;
        }

        public override bool Equals(object obj) => obj is RideVector4 other && Equals(other);
        public bool Equals(RideVector4 other) => this == other;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() => $"({x:f2}, {y:f2}, {z:f2}, {w:f2})";
    }
}
