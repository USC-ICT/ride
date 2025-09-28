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
        public int value { get; set; }

        public RideLayerMask(int value) => this.value = value;

        public static readonly RideLayerMask AllLayers = new RideLayerMask() { value = ~0 };

        public static implicit operator int(RideLayerMask mask) => mask.value;
        public static implicit operator RideLayerMask(int value) => new RideLayerMask(value);

        public static int GetMask(params string[] layers)
        {
            int mask = UnityEngine.LayerMask.GetMask(layers);
            if (mask == 0)
                RideLog.LogError($"RideLayerMask.GetMask - The layer(s) do not exist in the project: {string.Join(" ", layers)}");

            return mask;
        }

        public static string LayerToName(int layer) => UnityEngine.LayerMask.LayerToName(layer);

        public bool Equals(RideLayerMask other) => value == other.value;
        public override bool Equals(object obj) => obj is RideLayerMask other && Equals(other);
        public override int GetHashCode() => value.GetHashCode();
        public static bool operator ==(RideLayerMask lhs, RideLayerMask rhs) => lhs.value == rhs.value;
        public static bool operator !=(RideLayerMask lhs, RideLayerMask rhs) => !(lhs == rhs);

        public override string ToString() => $"RideLayerMask({value})";
    }
}
