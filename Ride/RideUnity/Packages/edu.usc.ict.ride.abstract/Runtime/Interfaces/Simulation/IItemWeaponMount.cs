namespace Ride.Entities
{
    /// <summary>Configures the axes and speed available when rotating a weapon mount.</summary>
    [System.Serializable]
    public struct RideWeaponMountControl
    {
        /// <summary>Whether rotation around the local X axis is enabled.</summary>
        public bool x;
        /// <summary>Whether rotation around the local Y axis is enabled.</summary>
        public bool y;
        /// <summary>Whether rotation around the local Z axis is enabled.</summary>
        public bool z;
        /// <summary>The mount rotation speed.</summary>
        public float rotationSpeed;
    }

    /// <summary>Represents a rotatable equipment anchor used by the weapon and equipment systems.</summary>
    public interface IItemWeaponMount : IItemAnchor
    {
        /// <summary>Gets or sets the transform rotated to aim the mount.</summary>
        ITransform mountTransform { get; set; }
        /// <summary>Gets or sets the permitted mount rotation axes and speed.</summary>
        RideWeaponMountControl weaponMountRotationControl { get; set; }
        /// <summary>Gets or sets nested weapon mounts attached to this mount.</summary>
        IItemWeaponMount[] subMounts { get; set; }
        /// <summary>Gets or sets the weapon attached to this mount.</summary>
        IWeapon mountedWeapon { get; set; }
        /// <summary>Gets or sets whether a weapon is currently mounted.</summary>
        bool isMounted { get; set; }
        /// <summary>Gets the world-space point from which the mounted weapon fires.</summary>
        RideVector3 firePoint { get; }
    }
}
