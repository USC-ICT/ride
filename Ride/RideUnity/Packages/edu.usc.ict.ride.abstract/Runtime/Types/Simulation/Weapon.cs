using System;

namespace Ride.Entities
{
    /// <summary>Defines the serialized combat, ballistics, ammunition, and firing-mode data for a weapon.</summary>
    /// <remarks>
    /// Weapon assets expose this structure through <see cref="IWeapon"/> implementations, while
    /// <see cref="IEquipmentSystem"/>, <see cref="IWeaponSystem"/>, and <see cref="Ride.Combat.IAttackSystem"/>
    /// consume it during equipping and combat. The accuracy field's removal TODO is retained, but
    /// current attack-system code still uses that value when calculating missed shot positions.
    /// </remarks>
    [Serializable]
    public struct Weapon
    {
        /// <summary>Base damage applied by the weapon.</summary>
        public float damage;

        /// <summary>Legacy attack-rate value.</summary>
        public float attackSpeed;

        /// <summary>Legacy accuracy value used to calculate missed shot positions.</summary>
        public float accuracy; // Deprecated TODO: Remove

        /// <summary>Legacy maximum effective range.</summary>
        public float range;

        /// <summary>Radius within which radial damage is applied.</summary>
        public float damageRadius;

        /// <summary>Time in seconds required to reload the weapon.</summary>
        public float reloadTime;

        /// <summary>Magnitude of the weapon's recoil effect.</summary>
        public float recoilEffect;

        /// <summary>Initial projectile velocity.</summary>
        public float muzzleVelocity;

        /// <summary>Time in seconds required to chamber the next round.</summary>
        public float rechamberTime;

        /// <summary>Initial or preferred firing mode.</summary>
        public WeaponFiringMode firingMode;

        /// <summary>Whether the weapon uses artillery trajectory behavior.</summary>
        public bool isArtillery;

        /// <summary>Distance over which the weapon can destruct terrain or objects.</summary>
        public float destructionRange;

        /// <summary>Force applied by the weapon's destruction effect.</summary>
        public float destructionForce;

        /// <summary>Suppression magnitude applied by the weapon.</summary>
        public float suppressionEffect;

        /// <summary>Radius within which suppression is applied.</summary>
        public float suppressionRadius;

        /// <summary>Ammunition size accepted by the weapon.</summary>
        public AmmunitionSize usableAmmoSize;

        /// <summary>Whether semi-automatic firing mode is supported.</summary>
        public bool semiModeAvailable;

        /// <summary>Whether burst firing mode is supported.</summary>
        public bool burstModeAvailable;

        /// <summary>Whether automatic firing mode is supported.</summary>
        public bool autoModeAvailable;
    }
}
