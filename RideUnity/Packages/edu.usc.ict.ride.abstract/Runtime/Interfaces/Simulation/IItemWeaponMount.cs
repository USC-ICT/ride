namespace Ride.Entities
{
    [System.Serializable]
    public struct RideWeaponMountControl
    {
        public bool x;
        public bool y;
        public bool z;
        public float rotationSpeed;
    }

    public interface IItemWeaponMount : IItemAnchor
    {
        ITransform mountTransform { get; set; }
        RideWeaponMountControl weaponMountRotationControl { get; set; }
        IItemWeaponMount[] subMounts { get; set; }
        IWeapon mountedWeapon { get; set; }
        bool isMounted { get; set; }
        RideVector3 firePoint { get; }
    }
}