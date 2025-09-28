using System.Collections.Generic;

namespace Ride.Entities
{
    public interface IWeaponSystem : IRideSystem
    {
        bool IsOutOfAmmo(RideID weaponId);

        IEnumerable<RideID> GetWeapons(RideID owner);

        RideID GetPrimaryWeapon(RideID owner);

        bool StopUseWeapon(RideID id, bool sendMsg = true);

        bool WeaponUsesProjectile(RideID weaponId);

        bool IsMountedWeapon(RideID weaponId);

        IWeapon GetWeapon(RideID weaponId);

        float GetWeaponRange(RideID weaponId);

        float GetWeaponDamage(RideID weaponId);

        float GetWeaponDamageRadius(RideID weaponId);

        float GetWeaponMuzzleVelocity(RideID weaponId);

        float GetWeaponAmmoCount(RideID weaponId);

        float GetWeaponAmmoCapacity(RideID weaponId);

        float GetWeaponSuppressionEffect(RideID weaponId);

        float GetWeaponSuppressionRadius(RideID weaponId);

        float GetWeaponRechamberTime(RideID weaponId);

        WeaponFiringMode GetWeaponFiringMode(RideID weaponId);

        AmmunitionSize GetWeaponAmmoSize(RideID weaponId);

        RideID GetAttachedWeaponMagazine(RideID weaponId);

        float GetArmorPiercingRating(RideID weaponId);

        bool WeaponPointedAtTarget(RideID weaponId);

        void ResetWeaponSystem();

        RideRay GetWeaponPointedDirection(RideID weaponId);

        bool HasExplosiveTriggered(RideID explosiveId);

        bool HasExplosiveExploded(RideID explosiveId);

        float GetExplosiveTimer(RideID explosiveId);

        Explosive GetExplosiveData(RideID explosiveId);

        void WeaponFX(RideID weaponId, bool projectileDamage = true);

        void WeaponFX(RideID weaponId, RideVector3 weaponPos, RideVector3 weaponDir, bool projectileDamage = true);

        void RemoveItems(RideID owner);
    }
}
