using Ride.Entities;

namespace Ride {
    [System.Flags]
    public enum OrdnanceAppearanceFlags {
        Sphere = 1 << 0,
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

        void Init(RideID id, RideVector3 position, RideQuaternion rotation, OrdnanceAppearanceFlags flags, float msd, float msdShielded);

        void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, OrdnanceAppearanceFlags flags, float msd, float msdShielded);
    }
}
