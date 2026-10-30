using System.Collections.Generic;

namespace Ride.Entities
{
    /// <summary>Coordinates weapon state, ammunition, effects, and explosive behavior for RIDE entities.</summary>
    /// <remarks>Implemented by Simulation's weapon system and used by attack, equipment, agent, and input systems.</remarks>
    public interface IWeaponSystem : IRideSystem
    {
        /// <summary>Determines whether a weapon has no usable ammunition.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns><see langword="true"/> when the weapon is out of ammunition; otherwise, <see langword="false"/>.</returns>
        bool IsOutOfAmmo(RideID weaponId);

        /// <summary>Gets weapons owned by an entity.</summary>
        /// <param name="owner">The owning entity.</param>
        /// <returns>The owned weapon identifiers.</returns>
        IEnumerable<RideID> GetWeapons(RideID owner);

        /// <summary>Gets an entity's primary weapon.</summary>
        /// <param name="owner">The owning entity.</param>
        /// <returns>The primary weapon identifier, or <see cref="RideID.Null"/> when unavailable.</returns>
        RideID GetPrimaryWeapon(RideID owner);

        /// <summary>Stops an active weapon-use action.</summary>
        /// <param name="id">The weapon identifier.</param>
        /// <param name="sendMsg">Whether to send the corresponding synchronization message.</param>
        /// <returns><see langword="true"/> when use was stopped; otherwise, <see langword="false"/>.</returns>
        bool StopUseWeapon(RideID id, bool sendMsg = true);

        /// <summary>Determines whether a weapon fires a simulated projectile.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns><see langword="true"/> for projectile weapons; otherwise, <see langword="false"/>.</returns>
        bool WeaponUsesProjectile(RideID weaponId);

        /// <summary>Determines whether a weapon is attached to a weapon mount.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns><see langword="true"/> when mounted; otherwise, <see langword="false"/>.</returns>
        bool IsMountedWeapon(RideID weaponId);

        /// <summary>Gets a weapon by identifier.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The weapon, or <see langword="null"/> when unavailable.</returns>
        IWeapon GetWeapon(RideID weaponId);

        /// <summary>Gets a weapon's maximum effective range.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The range in simulation distance units.</returns>
        float GetWeaponRange(RideID weaponId);

        /// <summary>Gets a weapon's base damage.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The base damage value.</returns>
        float GetWeaponDamage(RideID weaponId);

        /// <summary>Gets a weapon's radial damage distance.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The damage radius.</returns>
        float GetWeaponDamageRadius(RideID weaponId);

        /// <summary>Gets a weapon's projectile muzzle velocity.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The muzzle velocity.</returns>
        float GetWeaponMuzzleVelocity(RideID weaponId);

        /// <summary>Gets the current number of rounds in a weapon's attached magazine.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The current round count.</returns>
        float GetWeaponAmmoCount(RideID weaponId);

        /// <summary>Gets the capacity of a weapon's attached magazine.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The magazine capacity.</returns>
        float GetWeaponAmmoCapacity(RideID weaponId);

        /// <summary>Gets a weapon's suppression magnitude.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The suppression effect value.</returns>
        float GetWeaponSuppressionEffect(RideID weaponId);

        /// <summary>Gets a weapon's suppression radius.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The suppression radius.</returns>
        float GetWeaponSuppressionRadius(RideID weaponId);

        /// <summary>Gets the delay required to chamber a weapon's next round.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The rechamber time in seconds.</returns>
        float GetWeaponRechamberTime(RideID weaponId);

        /// <summary>Gets a weapon's current firing mode.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The active firing mode.</returns>
        WeaponFiringMode GetWeaponFiringMode(RideID weaponId);

        /// <summary>Gets the ammunition size accepted by a weapon.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The accepted ammunition size.</returns>
        AmmunitionSize GetWeaponAmmoSize(RideID weaponId);

        /// <summary>Gets the magazine attached to a weapon.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The magazine identifier, or <see cref="RideID.Null"/> when none is attached.</returns>
        RideID GetAttachedWeaponMagazine(RideID weaponId);

        /// <summary>Gets the armor-piercing rating of a weapon's ammunition.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The armor-piercing rating.</returns>
        float GetArmorPiercingRating(RideID weaponId);

        /// <summary>Determines whether a mounted weapon is aimed at its assigned target.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns><see langword="true"/> when aligned with the target; otherwise, <see langword="false"/>.</returns>
        bool WeaponPointedAtTarget(RideID weaponId);

        /// <summary>Resets transient state for all registered weapons and explosives.</summary>
        void ResetWeaponSystem();

        /// <summary>Gets the world-space ray along which a weapon is aimed.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <returns>The weapon aim ray.</returns>
        RideRay GetWeaponPointedDirection(RideID weaponId);

        /// <summary>Determines whether an explosive has been triggered.</summary>
        /// <param name="explosiveId">The explosive identifier.</param>
        /// <returns><see langword="true"/> when triggered; otherwise, <see langword="false"/>.</returns>
        bool HasExplosiveTriggered(RideID explosiveId);

        /// <summary>Determines whether an explosive has detonated.</summary>
        /// <param name="explosiveId">The explosive identifier.</param>
        /// <returns><see langword="true"/> when detonated; otherwise, <see langword="false"/>.</returns>
        bool HasExplosiveExploded(RideID explosiveId);

        /// <summary>Gets the remaining or configured timer for an explosive.</summary>
        /// <param name="explosiveId">The explosive identifier.</param>
        /// <returns>The timer value in seconds.</returns>
        float GetExplosiveTimer(RideID explosiveId);

        /// <summary>Gets the runtime state associated with an explosive.</summary>
        /// <param name="explosiveId">The explosive identifier.</param>
        /// <returns>The explosive state data.</returns>
        Explosive GetExplosiveData(RideID explosiveId);

        /// <summary>Plays effects for a weapon using its current position and aim.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <param name="projectileDamage">Whether projectile effects should apply damage.</param>
        void WeaponFX(RideID weaponId, bool projectileDamage = true);

        /// <summary>Plays effects for a weapon at an explicit position and direction.</summary>
        /// <param name="weaponId">The weapon identifier.</param>
        /// <param name="weaponPos">The world-space effect origin.</param>
        /// <param name="weaponDir">The world-space firing direction.</param>
        /// <param name="projectileDamage">Whether projectile effects should apply damage.</param>
        void WeaponFX(RideID weaponId, RideVector3 weaponPos, RideVector3 weaponDir, bool projectileDamage = true);

        /// <summary>Removes weapon-system items owned by an entity.</summary>
        /// <param name="owner">The owning entity.</param>
        void RemoveItems(RideID owner);
    }
}
