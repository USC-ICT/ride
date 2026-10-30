namespace Ride.Combat
{
    /// <summary>Calculates hit probability, damage, visibility, and ballistic trajectories for attacks.</summary>
    /// <remarks>Used by the Simulation attack system to resolve direct-fire, projectile, and artillery attacks.</remarks>
    public interface IAttackResultCalculator
    {
        /// <summary>
        /// Tests if the attackee is in range of the attacker using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The weapon used by the attacker.</param>
        /// <param name="attackee">The entity receiving the attack.</param>
        /// <returns>True if the attackee is in attack range of the attacker using weapon</returns>
        bool IsInRange(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Tests if the attackee is hit by the attacker using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The weapon used by the attacker.</param>
        /// <param name="attackee">The entity receiving the attack.</param>
        /// <returns>True if the attackee is hit by the attacker using weapon</returns>
        bool IsHit(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Performs a raycast using the given attackRay and weapon data
        /// </summary>
        /// <param name="attackRay">The ray that is used to intersect geometry</param>
        /// <param name="weapon">The weapon being used</param>
        /// <param name="mask">The collision layers to test</param>
        /// <returns>The information about the first over hit by the ray. Can be empty is the ray didn't hit anything</returns>
        RideRaycastHit GetHitData(RideRay attackRay, RideID weapon, RideLayerMask mask);

        /// <summary>
        /// Calculates the accuracy of an attack
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The weapon used by the attacker.</param>
        /// <param name="attackee">The entity receiving the attack.</param>
        /// <returns>Final accuracy of attack based on a series of variables</returns>
        float CalculateAccuracy(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Calculates the amount of damage the attacker will do to the attackee using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The weapon used by the attacker.</param>
        /// <param name="attackee">The entity receiving the attack.</param>
        /// <returns>Positive value between 0 and X</returns>
        float CalculateDamage(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Calculates the time it will take to hit a target.
        /// </summary>
        /// <param name="attacker">The transform from which the projectile is fired.</param>
        /// <param name="attack">The attack and target position.</param>
        /// <param name="projectileSpeed">The projectile's initial speed.</param>
        /// <param name="segmentScale">The trajectory sampling interval used for visualization.</param>
        /// <param name="isArtillery">Whether to use the high ballistic arc.</param>
        /// <param name="projectileMass">The projectile mass used in drag calculations.</param>
        /// <returns>The estimated flight time in seconds.</returns>
        float CalculateTimeToHitTarget(ITransform attacker, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>
        /// Calculates the time it will take to hit a target.
        /// </summary>
        /// <param name="firePos">The world-space firing position.</param>
        /// <param name="fireDir">The initial world-space firing direction.</param>
        /// <param name="attack">The attack and target position.</param>
        /// <param name="projectileSpeed">The projectile's initial speed.</param>
        /// <param name="segmentScale">The trajectory sampling interval used for visualization.</param>
        /// <param name="isArtillery">Whether to use the high ballistic arc.</param>
        /// <param name="projectileMass">The projectile mass used in drag calculations.</param>
        /// <returns>The estimated flight time in seconds.</returns>
        float CalculateTimeToHitTarget(RideVector3 firePos, RideVector3 fireDir, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>Calculates the high and low launch angles that can reach an attack position.</summary>
        /// <param name="firePoint">The projectile firing transform.</param>
        /// <param name="attack">The attack and target position.</param>
        /// <param name="projectileSpeed">The projectile's initial speed.</param>
        /// <param name="theta1">For storing the high "artillery" firing angle.</param>
        /// <param name="theta2">For storing the low "bullet" firing angle.</param>
        /// <returns><see langword="true"/> when a ballistic solution exists; otherwise, <see langword="false"/>.</returns>
        bool CalculateAngleToHitTarget(ITransform firePoint, IAttack attack, float projectileSpeed, out float theta1, out float theta2);

        /// <summary>Calculates sampled ballistic trajectory positions from a firing transform.</summary>
        /// <param name="attacker">The attacking entity.</param>
        /// <param name="firePoint">The projectile firing transform.</param>
        /// <param name="attack">The attack and target position.</param>
        /// <param name="projectileSpeed">The projectile's initial speed.</param>
        /// <param name="segmentScale">The trajectory sampling interval.</param>
        /// <param name="isArtillery">Whether to use the high ballistic arc.</param>
        /// <param name="projectileMass">The projectile mass.</param>
        /// <returns>The sampled world-space trajectory positions.</returns>
        RideVector3[] CalculateTrajectoryToHitTarget(RideID attacker, ITransform firePoint, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>Calculates sampled ballistic trajectory positions from an explicit position and direction.</summary>
        /// <param name="attacker">The attacking entity.</param>
        /// <param name="firePos">The world-space firing position.</param>
        /// <param name="fireDir">The initial world-space firing direction.</param>
        /// <param name="attack">The attack and target position.</param>
        /// <param name="projectileSpeed">The projectile's initial speed.</param>
        /// <param name="segmentScale">The trajectory sampling interval.</param>
        /// <param name="isArtillery">Whether to use the high ballistic arc.</param>
        /// <param name="projectileMass">The projectile mass.</param>
        /// <returns>The sampled world-space trajectory positions.</returns>
        RideVector3[] CalculateTrajectoryToHitTarget(RideID attacker, RideVector3 firePos, RideVector3 fireDir, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>Calculates the drag applied to a projectile velocity.</summary>
        /// <param name="velocityVec">The current velocity.</param>
        /// <param name="mass">The projectile mass.</param>
        /// <returns>The drag vector.</returns>
        RideVector3 CalculateDrag(RideVector3 velocityVec, float mass);

        /// <summary>Calculates line-of-sight visibility between an attacker and target position.</summary>
        /// <param name="attackerPos">The attacker position.</param>
        /// <param name="targetPos">The target position.</param>
        /// <returns>The visibility factor.</returns>
        float GetVisibility(RideVector3 attackerPos, RideVector3 targetPos);
    }
}
