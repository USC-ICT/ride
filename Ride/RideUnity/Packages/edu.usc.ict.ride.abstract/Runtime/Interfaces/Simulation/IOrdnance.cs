using Ride.Entities;

namespace Ride {
    /// <summary>Specifies optional visual aids displayed for an ordnance object.</summary>
    [System.Flags]
    public enum OrdnanceAppearanceFlags {
        /// <summary>Displays the minimum-safe-distance volume.</summary>
        Sphere = 1 << 0,
        /// <summary>Displays line-of-sight visualization.</summary>
        LOS = 1 << 1,
    }

    /// <summary>
    /// Represents an ordnance with a visual minimum safe distance.
    /// </summary>
    public interface IOrdnance : IEntity {
        /// <summary>
        /// Position in world space
        /// </summary>
        RideVector3 position { get; set; }

        /// <summary>
        /// Rotation in world space
        /// </summary>
        RideQuaternion rotation { get; set; }

        /// <summary>
        /// Appearance of this ordnance
        /// </summary>
        OrdnanceAppearanceFlags flags { get; set; }

        /// <summary>
        /// minimum safe distance without shielding.
        /// </summary>
        double minimumSafeDistance { get; set; }

        /// <summary>
        /// minimum safe distance with shielding.
        /// </summary>
        double minimumSafeDistanceShielded { get; set; }

        /// <summary>Initializes ordnance data without changing its inherited name.</summary>
        /// <param name="id">The ordnance identifier.</param>
        /// <param name="position">The world-space position.</param>
        /// <param name="rotation">The world-space rotation.</param>
        /// <param name="flags">The enabled appearance aids.</param>
        /// <param name="msd">The unshielded minimum safe distance.</param>
        /// <param name="msdShielded">The shielded minimum safe distance.</param>
        void Init(RideID id, RideVector3 position, RideQuaternion rotation, OrdnanceAppearanceFlags flags, float msd, float msdShielded);

        /// <summary>Initializes named ordnance data.</summary>
        /// <param name="id">The ordnance identifier.</param>
        /// <param name="name">The ordnance display name.</param>
        /// <param name="position">The world-space position.</param>
        /// <param name="rotation">The world-space rotation.</param>
        /// <param name="flags">The enabled appearance aids.</param>
        /// <param name="msd">The unshielded minimum safe distance.</param>
        /// <param name="msdShielded">The shielded minimum safe distance.</param>
        void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, OrdnanceAppearanceFlags flags, float msd, float msdShielded);
    }
}
