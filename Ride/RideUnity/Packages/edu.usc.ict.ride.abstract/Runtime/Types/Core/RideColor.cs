using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents an RGBA color struct, decoupled from UnityEngine.Color for improved testability and abstraction.
    /// Used across systems where Unity's types may not be accessible or desirable. Includes implicit conversions for
    /// convenience, equality checks, interpolation, and string formatting.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Color.html">UnityEngine.Color</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideColor : IEquatable<RideColor>
    {
        public float r;
        public float g;
        public float b;
        public float a;  // Alpha component of the color (0 is transparent, 1 is opaque).


        public RideColor(float _r, float _g, float _b, float _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        public RideColor(UnityEngine.Color c) : this(c.r, c.g, c.b, c.a) { }


        public static readonly RideColor black = new RideColor(0, 0, 0, 1);
        public static readonly RideColor white = new RideColor(1, 1, 1, 1);
        public static readonly RideColor clear = new RideColor(0, 0, 0, 0);


        public UnityEngine.Color ToColor() => new UnityEngine.Color(r, g, b, a);


        public static implicit operator RideColor(UnityEngine.Color c) => new RideColor(c);
        public static implicit operator UnityEngine.Color(RideColor color) => color.ToColor();


        public static bool operator ==(RideColor lhs, RideColor rhs) => lhs.Equals(rhs);
        public static bool operator !=(RideColor lhs, RideColor rhs) => !lhs.Equals(rhs);


        public static RideColor Lerp(RideColor a, RideColor b, float t) => UnityEngine.Color.Lerp(a, b, t);


        public bool Equals(RideColor other) => r == other.r && g == other.g && b == other.b && a == other.a;
        public override bool Equals(object obj) => obj is RideColor other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public override string ToString() => ToColor().ToString();
    }
}
