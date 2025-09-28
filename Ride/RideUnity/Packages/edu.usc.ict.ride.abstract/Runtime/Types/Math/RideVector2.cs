using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a 2D vector similar to UnityEngine.Vector2, designed to decouple core logic in RIDE from Unity dependencies.
    /// This struct supports vector math, scalar operations, comparisons, and interop with Unity types while remaining usable in non-Unity environments.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Vector2.html">UnityEngine.Vector2</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideVector2 : IEquatable<RideVector2>
    {
        public float x;
        public float y;

        public RideVector2(float _x, float _y)
        {
            x = _x;
            y = _y;
        }

        public RideVector2(UnityEngine.Vector3 vec) : this(vec.x, vec.y) { }


        public static readonly RideVector2 zero = new RideVector2(0, 0);
        public static readonly RideVector2 up = new RideVector2(0, 1);


        /// <summary>Returns the normalized version of this vector.</summary>
        public RideVector2 normalized => magnitude == 0 ? RideVector2.zero : this / magnitude;

        /// <summary>Returns the magnitude (length) of the vector.</summary>
        public float magnitude => RideMath.Sqrt(x * x + y * y);

        /// <summary>Returns the squared magnitude (length^2) of the vector.</summary>
        public float sqrMagnitude => x * x + y * y;

        public UnityEngine.Vector2 ToVector2() => new UnityEngine.Vector2(x, y);
        public UnityEngine.Vector3 ToVector3() => new UnityEngine.Vector3(x, y, 0);

        public static implicit operator RideVector2(RideVector3 pos) => new RideVector2(pos.x, pos.y);

        public static implicit operator RideVector2(UnityEngine.Vector2 pos) => new RideVector2(pos.x, pos.y);
        public static implicit operator RideVector2(UnityEngine.Vector3 pos) => new RideVector2(pos.x, pos.y);
        public static implicit operator UnityEngine.Vector2(RideVector2 pos) => new UnityEngine.Vector2(pos.x, pos.y);

        public static RideVector2 operator +(RideVector2 lhs, RideVector2 rhs) => new RideVector2(lhs.x + rhs.x, lhs.y + rhs.y);
        public static RideVector2 operator -(RideVector2 lhs, RideVector2 rhs) => new RideVector2(lhs.x - rhs.x, lhs.y - rhs.y);
        public static RideVector2 operator *(RideVector2 lhs, float scalar) => new RideVector2(lhs.x * scalar, lhs.y * scalar);
        public static RideVector2 operator *(float scalar, RideVector2 rhs) => new RideVector2(rhs.x * scalar, rhs.y * scalar);
        public static RideVector2 operator /(RideVector2 lhs, float scalar)
        {
            if (scalar == 0)
                throw new DivideByZeroException("Cannot divide RideVector2 by zero.");
            return new RideVector2(lhs.x / scalar, lhs.y / scalar);
        }

        public static RideVector2 operator -(RideVector2 op) => new RideVector2(-op.x, -op.y);

        public static bool operator ==(RideVector2 lhs, RideVector2 rhs) => lhs.x == rhs.x && lhs.y == rhs.y;
        public static bool operator !=(RideVector2 lhs, RideVector2 rhs) => !(lhs == rhs);

        public static bool operator ==(RideVector2 lhs, UnityEngine.Vector2 rhs) => lhs.x == rhs.x && lhs.y == rhs.y;
        public static bool operator !=(RideVector2 lhs, UnityEngine.Vector2 rhs) => !(lhs == rhs);

        /// <summary>Returns the dot product of two vectors.</summary>
        public static float Dot(RideVector2 a, RideVector2 b) => a.x * b.x + a.y * b.y;

        /// <summary>Returns the distance between two vectors.</summary>
        public static float Distance(RideVector2 a, RideVector2 b) => (a - b).magnitude;

        /// <summary>Returns the angle in degrees between two vectors.</summary>
        public static float Angle(RideVector2 from, RideVector2 to) => UnityEngine.Vector2.Angle(from, to);

        /// <summary>Performs linear interpolation between two vectors.</summary>
        public static RideVector2 Lerp(RideVector2 a, RideVector2 b, float t) => a + (b - a) * t;

        /// <summary>Returns true if this vector approximately equals another vector within a tolerance.</summary>
        public bool ApproximatelyEquals(RideVector2 other, float epsilon = 1e-5f)
        {
            return RideMath.Abs(x - other.x) < epsilon && 
                   RideMath.Abs(y - other.y) < epsilon;
        }

        public override bool Equals(object obj) => obj is RideVector2 other && Equals(other);
        public bool Equals(RideVector2 other) => this == other;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x:f2}, {y:f2})";
    }
}
