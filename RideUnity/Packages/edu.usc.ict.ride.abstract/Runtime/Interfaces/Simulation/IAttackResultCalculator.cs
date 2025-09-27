namespace Ride.Combat
{
    public interface IAttackResultCalculator
    {
        /// <summary>
        /// Tests if the attackee is in range of the attacker using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The entity receiving the attack</param>
        /// <param name="attackee">The weapon used by the attacker</param>
        /// <returns>True if the attackee is in attack range of the attacker using weapon</returns>
        bool IsInRange(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Tests if the attackee is hit by the attacker using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The entity receiving the attack</param>
        /// <param name="attackee">The weapon used by the attacker</param>
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
        /// <param name="weapon">The entity receiving the attack</param>
        /// <param name="attackee">The weapon used by the attacker</param>
        /// <returns>Final accuracy of attack based on a series of variables</returns>
        float CalculateAccuracy(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Calculates the amount of damage the attacker will do to the attackee using weapon
        /// </summary>
        /// <param name="attack">Information about the attack</param>
        /// <param name="attacker">The entity doing the attacking</param>
        /// <param name="weapon">The entity receiving the attack</param>
        /// <param name="attackee">The weapon used by the attacker</param>
        /// <returns>Positive value between 0 and X</returns>
        float CalculateDamage(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Calculates the time it will take to hit a target.
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attack"></param>
        /// <param name="projectileSpeed"></param>
        /// <param name="segmentScale">determines smoothness of LineRenderer used for displaying trajectory.</param>
        /// <param name="isArtillery"></param>
        /// <param name="projectileMass"></param>
        /// <returns></returns>
        float CalculateTimeToHitTarget(ITransform attacker, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>
        /// Calculates the time it will take to hit a target.
        /// </summary>
        /// <param name="firePos"></param>
        /// <param name="fireDir"></param>
        /// <param name="attack"></param>
        /// <param name="projectileSpeed"></param>
        /// <param name="segmentScale">determines smoothness of LineRenderer used for displaying trajectory.</param>
        /// <param name="isArtillery"></param>
        /// <param name="projectileMass"></param>
        /// <returns></returns>
        float CalculateTimeToHitTarget(RideVector3 firePos, RideVector3 fireDir, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        /// <summary>
        ///
        /// </summary>
        /// <param name="firePoint"></param>
        /// <param name="attack"></param>
        /// <param name="projectileSpeed"></param>
        /// <param name="theta1">For storing the high "artillery" firing angle.</param>
        /// <param name="theta2">For storing the low "bullet" firing angle.</param>
        /// <returns></returns>
        bool CalculateAngleToHitTarget(ITransform firePoint, IAttack attack, float projectileSpeed, out float theta1, out float theta2);

        RideVector3[] CalculateTrajectoryToHitTarget(RideID attacker, ITransform firePoint, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        RideVector3[] CalculateTrajectoryToHitTarget(RideID attacker, RideVector3 firePos, RideVector3 fireDir, IAttack attack, float projectileSpeed, float segmentScale, bool isArtillery, float projectileMass);

        RideVector3 CalculateDrag(RideVector3 velocityVec, float mass);

        float GetVisibility(RideVector3 attackerPos, RideVector3 targetPos);
    }
}
