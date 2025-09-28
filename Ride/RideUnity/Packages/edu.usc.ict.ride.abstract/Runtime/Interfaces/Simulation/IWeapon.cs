namespace Ride.Entities
{
    public enum WeaponFiringMode
    {
        Semi = 1,
        Burst = 2,
        Auto = 4
    }

    public interface IWeapon : IItem
    {
        // Deprecated TODO: Remove
        float accuracy { get; }
        float range { get; }
        float damage { get; }
        float attackSpeed { get; }
        float damageRadius { get; }
        float recoilEffect { get; }
        float muzzleVelocity { get; }

        int currentMagazineAmmoCount { get; }
        int currentMagazineCapacity { get; }
        int totalAmmoCount { get; }
        bool isArtillery { get; }
        float destructionRange { get; }
        float destructionForce { get; }
        float suppressionEffect { get; }
        float suppressionRadius { get; }
        float rechamberTime { get; }

        bool isFiring { get; } // deprecated
        bool isReloading { get; } // deprecated
        bool isRechamberingRound { get; set; } // deprecated

        WeaponFiringMode weaponFiringMode { get; set; }
        AmmunitionSize weaponAmmoSize { get; set; }

        float lastFireTime { get; set; }
        int continuousFireCount { get; set; }

        /// <summary>
        /// The last time when a reload was started
        /// </summary>
        float lastReloadTime { get; set; }

        /// <summary>
        /// Time it takes to reload in seconds
        /// </summary>
        float reloadTime { get; }

        RideID attachedMagazine { get; set; }

        bool semiModeAvailable { get; }
        bool burstModeAvailable { get; }
        bool autoModeAvailable { get; }

        RideVector3 weaponPosition { get; }
    }
}
