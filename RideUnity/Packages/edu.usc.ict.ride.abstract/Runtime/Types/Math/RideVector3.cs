using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a 3D vector class similar to UnityEngine.Vector3, designed to decouple core logic in RIDE from Unity dependencies.
    /// This class provides a consistent math interface usable in both Unity and non-Unity environments, supporting vector math,
    /// scalar operations, comparisons, and conversions to/from Unity's types. Implicit conversions enable easy interoperability,
    /// while preserving a clean and testable abstract layer when desired.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Vector3.html">UnityEngine.Vector3</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideVector3 : IEquatable<RideVector3>
    {
        public float x;
        public float y;
        public float z;

        public RideVector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public RideVector3(RideVector2 vec) : this(vec.x, vec.y, 0) { }

        public RideVector3(UnityEngine.Vector3 vec) : this(vec.x, vec.y, vec.z) { }
        public RideVector3(UnityEngine.Vector2 vec) : this(vec.x, vec.y, 0) { }

        public static readonly RideVector3 zero = new RideVector3(0, 0, 0);
        public static readonly RideVector3 one = new RideVector3(1, 1, 1);
        public static readonly RideVector3 down = new RideVector3(0, -1, 0);
        public static readonly RideVector3 up = new RideVector3(0, 1, 0);
        public static readonly RideVector3 forward = new RideVector3(0, 0, 1);
        public static readonly RideVector3 back = new RideVector3(0, 0, -1);
        public static readonly RideVector3 left = new RideVector3(-1, 0, 0);
        public static readonly RideVector3 right = new RideVector3(1, 0, 0);

        /// <summary>Returns the normalized version of this vector. If magnitude is 0, returns zero vector.</summary>
        public RideVector3 normalized => magnitude == 0 ? RideVector3.zero : this / magnitude;

        /// <summary>Returns the magnitude (length) of the vector.</summary>
        public float magnitude => RideMath.Sqrt(x * x + y * y + z * z);

        /// <summary>Returns the squared magnitude of the vector (avoids square root).</summary>
        public float sqrMagnitude => x * x + y * y + z * z;

        public UnityEngine.Vector3 ToVector3() => new UnityEngine.Vector3(x, y, z);

        public RideVector2 ToRideVector2() => new RideVector2(x, y);

        public static implicit operator RideVector3(RideVector2 pos) => new RideVector3(pos);

        public static implicit operator RideVector3(UnityEngine.Vector3 pos) => new RideVector3(pos.x, pos.y, pos.z);
        public static implicit operator RideVector3(UnityEngine.Vector2 pos) => new RideVector3(pos.x, pos.y, 0);
        public static implicit operator UnityEngine.Vector3(RideVector3 pos) => new UnityEngine.Vector3(pos.x, pos.y, pos.z);

        public static RideVector3 operator +(RideVector3 lhs, RideVector3 rhs) => new RideVector3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        public static RideVector3 operator -(RideVector3 lhs, RideVector3 rhs) => new RideVector3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        public static RideVector3 operator *(RideVector3 lhs, float scalar) => new RideVector3(lhs.x * scalar, lhs.y * scalar, lhs.z * scalar);
        public static RideVector3 operator *(float scalar, RideVector3 rhs) => new RideVector3(rhs.x * scalar, rhs.y * scalar, rhs.z * scalar);

        public static RideVector3 operator /(RideVector3 lhs, float scalar)
        {
            if (scalar == 0f)
                throw new DivideByZeroException("Cannot divide RideVector3 by zero.");
            return new RideVector3(lhs.x / scalar, lhs.y / scalar, lhs.z / scalar);
        }

        public static RideVector3 operator -(RideVector3 op) => new RideVector3(-op.x, -op.y, -op.z);

        public static bool operator ==(RideVector3 lhs, RideVector3 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        public static bool operator !=(RideVector3 lhs, RideVector3 rhs) => !(lhs == rhs);

        public static bool operator ==(RideVector3 lhs, UnityEngine.Vector3 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        public static bool operator !=(RideVector3 lhs, UnityEngine.Vector3 rhs) => !(lhs == rhs);

        public static float Dot(RideVector3 a, RideVector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static RideVector3 Cross(RideVector3 a, RideVector3 b)
        {
            return new RideVector3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        /// <summary>Returns the linear interpolation between a and b based on t.</summary>
        public static RideVector3 Lerp(RideVector3 a, RideVector3 b, float t) => a + ((b - a) * t);

        /// <summary>Returns a vector with the minimum component values from two vectors.</summary>
        public static RideVector3 Min(RideVector3 lhs, RideVector3 rhs)
        {
            return new RideVector3(
                RideMath.Min(lhs.x, rhs.x),
                RideMath.Min(lhs.y, rhs.y),
                RideMath.Min(lhs.z, rhs.z)
            );
        }

        /// <summary>Returns a vector with the maximum component values from two vectors.</summary>
        public static RideVector3 Max(RideVector3 lhs, RideVector3 rhs)
        {
            return new RideVector3(
                RideMath.Max(lhs.x, rhs.x),
                RideMath.Max(lhs.y, rhs.y),
                RideMath.Max(lhs.z, rhs.z)
            );
        }

        public static RideVector3 Clamp(RideVector3 value, RideVector3 min, RideVector3 max)
        {
            return new RideVector3(
                RideMath.Clamp(value.x, min.x, max.x),
                RideMath.Clamp(value.y, min.y, max.y),
                RideMath.Clamp(value.z, min.z, max.z)
            );
        }

        public static float Distance(RideVector3 a, RideVector3 b) => (a - b).magnitude;
        public static float Magnitude(RideVector3 a) => a.magnitude;

        /// <summary>Returns the unsigned angle in degrees between two vectors.</summary>
        public static float Angle(RideVector3 from, RideVector3 to) => UnityEngine.Vector3.Angle(from, to);

        /// <summary>Returns the signed angle in degrees between two vectors, relative to an axis.</summary>
        public static float SignedAngle(RideVector3 from, RideVector3 to, RideVector3 axis) => UnityEngine.Vector3.SignedAngle(from, to, axis);

        /// <summary>Returns the angle in radians between two vectors (manual computation).</summary>
        public static float AngleRad(RideVector3 from, RideVector3 to)
        {
            return RideMath.Acos(Dot(from,to) / (from.magnitude * to.magnitude));
        }

        /// <summary>Returns true if this vector approximately equals another, within a given epsilon.</summary>
        public bool ApproximatelyEquals(RideVector3 other, float epsilon = 1e-5f)
        {
            return RideMath.Abs(x - other.x) < epsilon &&
                   RideMath.Abs(y - other.y) < epsilon &&
                   RideMath.Abs(z - other.z) < epsilon;
        }

        public override bool Equals(object obj) => obj is RideVector3 other && Equals(other);
        public bool Equals(RideVector3 other) => this == other;

        public override int GetHashCode() => HashCode.Combine(x, y, z);

        public override string ToString() => $"({x:f2}, {y:f2}, {z:f2})";
    }
}
