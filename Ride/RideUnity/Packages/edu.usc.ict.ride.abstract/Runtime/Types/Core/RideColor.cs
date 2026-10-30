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
        /// <summary>The red component.</summary>
        public float r;
        /// <summary>The green component.</summary>
        public float g;
        /// <summary>The blue component.</summary>
        public float b;
        /// <summary>The alpha component, where zero is transparent and one is opaque.</summary>
        public float a;  // Alpha component of the color (0 is transparent, 1 is opaque).


        /// <summary>Initializes a color from red, green, blue, and alpha components.</summary>
        /// <param name="_r">The red component.</param>
        /// <param name="_g">The green component.</param>
        /// <param name="_b">The blue component.</param>
        /// <param name="_a">The alpha component.</param>
        public RideColor(float _r, float _g, float _b, float _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        /// <summary>Initializes a RIDE color from a Unity color.</summary>
        /// <param name="c">The Unity color.</param>
        public RideColor(UnityEngine.Color c) : this(c.r, c.g, c.b, c.a) { }


        /// <summary>Opaque black.</summary>
        public static readonly RideColor black = new RideColor(0, 0, 0, 1);
        /// <summary>Opaque white.</summary>
        public static readonly RideColor white = new RideColor(1, 1, 1, 1);
        /// <summary>Fully transparent black.</summary>
        public static readonly RideColor clear = new RideColor(0, 0, 0, 0);


        /// <summary>Converts this value to a Unity color.</summary>
        /// <returns>The equivalent Unity color.</returns>
        public UnityEngine.Color ToColor() => new UnityEngine.Color(r, g, b, a);


        /// <summary>Converts a Unity color to a RIDE color.</summary>
        public static implicit operator RideColor(UnityEngine.Color c) => new RideColor(c);
        /// <summary>Converts a RIDE color to a Unity color.</summary>
        public static implicit operator UnityEngine.Color(RideColor color) => color.ToColor();


        /// <summary>Determines whether two colors have identical components.</summary>
        public static bool operator ==(RideColor lhs, RideColor rhs) => lhs.Equals(rhs);
        /// <summary>Determines whether two colors differ.</summary>
        public static bool operator !=(RideColor lhs, RideColor rhs) => !lhs.Equals(rhs);


        /// <summary>Linearly interpolates between two colors.</summary>
        /// <param name="a">The starting color.</param>
        /// <param name="b">The ending color.</param>
        /// <param name="t">The interpolation factor.</param>
        /// <returns>The interpolated color.</returns>
        public static RideColor Lerp(RideColor a, RideColor b, float t) => UnityEngine.Color.Lerp(a, b, t);


        /// <inheritdoc/>
        public bool Equals(RideColor other) => r == other.r && g == other.g && b == other.b && a == other.a;
        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RideColor other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        /// <inheritdoc/>
        public override string ToString() => ToColor().ToString();
    }
}
