namespace Ride.Entities
{
    /// <summary>Specifies the firing modes supported by a weapon.</summary>
    public enum WeaponFiringMode
    {
        /// <summary>Fires one round per use action.</summary>
        Semi = 1,
        /// <summary>Fires a fixed burst of rounds per use action.</summary>
        Burst = 2,
        /// <summary>Continues firing while the use action remains active.</summary>
        Auto = 4
    }

    /// <summary>Describes a weapon item consumed by the attack, equipment, and weapon systems.</summary>
    public interface IWeapon : IItem
    {
        // Deprecated TODO: Remove
        /// <summary>Gets the legacy weapon accuracy value.</summary>
        float accuracy { get; }
        /// <summary>Gets the legacy maximum effective range.</summary>
        float range { get; }
        /// <summary>Gets the legacy base damage.</summary>
        float damage { get; }
        /// <summary>Gets the legacy attack rate.</summary>
        float attackSpeed { get; }
        /// <summary>Gets the legacy radial damage distance.</summary>
        float damageRadius { get; }
        /// <summary>Gets the legacy recoil magnitude.</summary>
        float recoilEffect { get; }
        /// <summary>Gets the legacy projectile muzzle velocity.</summary>
        float muzzleVelocity { get; }

        /// <summary>Gets the number of rounds remaining in the attached magazine.</summary>
        int currentMagazineAmmoCount { get; }
        /// <summary>Gets the capacity of the attached magazine.</summary>
        int currentMagazineCapacity { get; }
        /// <summary>Gets the total ammunition available to the weapon's owner.</summary>
        int totalAmmoCount { get; }
        /// <summary>Gets whether the weapon uses artillery behavior.</summary>
        bool isArtillery { get; }
        /// <summary>Gets the distance over which the weapon can destruct terrain or objects.</summary>
        float destructionRange { get; }
        /// <summary>Gets the force applied during destruction effects.</summary>
        float destructionForce { get; }
        /// <summary>Gets the suppression magnitude applied by the weapon.</summary>
        float suppressionEffect { get; }
        /// <summary>Gets the radius within which suppression is applied.</summary>
        float suppressionRadius { get; }
        /// <summary>Gets the time in seconds required to chamber the next round.</summary>
        float rechamberTime { get; }

        /// <summary>Gets the legacy indication that the weapon is currently firing.</summary>
        bool isFiring { get; } // deprecated
        /// <summary>Gets the legacy indication that the weapon is currently reloading.</summary>
        bool isReloading { get; } // deprecated
        /// <summary>Gets or sets the legacy indication that the weapon is chambering a round.</summary>
        bool isRechamberingRound { get; set; } // deprecated

        /// <summary>Gets or sets the active firing mode.</summary>
        WeaponFiringMode weaponFiringMode { get; set; }
        /// <summary>Gets or sets the ammunition size accepted by the weapon.</summary>
        AmmunitionSize weaponAmmoSize { get; set; }

        /// <summary>Gets or sets the simulation time of the most recent shot.</summary>
        float lastFireTime { get; set; }
        /// <summary>Gets or sets the number of consecutive shots in the current firing sequence.</summary>
        int continuousFireCount { get; set; }

        /// <summary>Gets or sets the simulation time when the latest reload began.</summary>
        float lastReloadTime { get; set; }

        /// <summary>Gets the time required to reload, in seconds.</summary>
        float reloadTime { get; }

        /// <summary>Gets or sets the identifier of the attached magazine.</summary>
        RideID attachedMagazine { get; set; }

        /// <summary>Gets whether semi-automatic mode is supported.</summary>
        bool semiModeAvailable { get; }
        /// <summary>Gets whether burst mode is supported.</summary>
        bool burstModeAvailable { get; }
        /// <summary>Gets whether automatic mode is supported.</summary>
        bool autoModeAvailable { get; }

        /// <summary>Gets the weapon's world-space position.</summary>
        RideVector3 weaponPosition { get; }
    }
}
