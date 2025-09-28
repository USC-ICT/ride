using System;

namespace Ride.Entities
{
    [Serializable]
    public struct Weapon
    {
        public float damage;
        public float attackSpeed;
        public float accuracy; // Deprecated TODO: Remove
        public float range;
        public float damageRadius;
        public float reloadTime;
        public float recoilEffect;
        public float muzzleVelocity;
        public float rechamberTime;
        public WeaponFiringMode firingMode;
        public bool isArtillery;
        public float destructionRange;
        public float destructionForce;
        public float suppressionEffect;
        public float suppressionRadius;
        public AmmunitionSize usableAmmoSize;
        public bool semiModeAvailable;
        public bool burstModeAvailable;
        public bool autoModeAvailable;
    }
}
