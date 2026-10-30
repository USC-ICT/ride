using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a strongly-typed wrapper around UnityEngine.LayerMask for use within the RIDE framework.
    ///
    /// Provides type safety, logging for invalid layer usage, and convenience methods for converting between
    /// layer names and bitmask values. This struct is intended to be used in place of raw integers or UnityEngine.LayerMask
    /// to ensure consistency and maintainability across RIDE subsystems that rely on layer-based logic.
    ///
    /// For more details, see Unity's documentation:
    /// https://docs.unity3d.com/ScriptReference/LayerMask.html
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideLayerMask : IEquatable<RideLayerMask>
    {
        /// <summary>Gets or sets the underlying layer bitmask.</summary>
        public int value { get; set; }

        /// <summary>Initializes a layer mask from its integer bitmask.</summary>
        /// <param name="value">The layer bitmask.</param>
        public RideLayerMask(int value) => this.value = value;

        /// <summary>A mask containing every layer.</summary>
        public static readonly RideLayerMask AllLayers = new RideLayerMask() { value = ~0 };

        /// <summary>Returns the integer bitmask represented by a RIDE layer mask.</summary>
        public static implicit operator int(RideLayerMask mask) => mask.value;
        /// <summary>Wraps an integer bitmask as a RIDE layer mask.</summary>
        public static implicit operator RideLayerMask(int value) => new RideLayerMask(value);

        /// <summary>Creates a bitmask containing the named Unity layers.</summary>
        /// <param name="layers">The Unity layer names.</param>
        /// <returns>The combined layer bitmask.</returns>
        public static int GetMask(params string[] layers)
        {
            int mask = UnityEngine.LayerMask.GetMask(layers);
            if (mask == 0)
                RideLog.LogError($"RideLayerMask.GetMask - The layer(s) do not exist in the project: {string.Join(" ", layers)}");

            return mask;
        }

        /// <summary>Gets the Unity layer name associated with an index.</summary>
        /// <param name="layer">The layer index.</param>
        /// <returns>The layer name, or an empty string when the layer is undefined.</returns>
        public static string LayerToName(int layer) => UnityEngine.LayerMask.LayerToName(layer);

        /// <inheritdoc/>
        public bool Equals(RideLayerMask other) => value == other.value;
        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RideLayerMask other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => value.GetHashCode();
        /// <summary>Determines whether two layer masks contain the same bitmask.</summary>
        public static bool operator ==(RideLayerMask lhs, RideLayerMask rhs) => lhs.value == rhs.value;
        /// <summary>Determines whether two layer masks differ.</summary>
        public static bool operator !=(RideLayerMask lhs, RideLayerMask rhs) => !(lhs == rhs);

        /// <inheritdoc/>
        public override string ToString() => $"RideLayerMask({value})";
    }
}
