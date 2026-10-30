using System;

namespace Ride.Entities
{
    /// <summary>Defines the trigger and effect parameters of an explosive item.</summary>
    /// <remarks>
    /// Explosive assets expose this data to the weapon system, and
    /// <see cref="IWeaponSystem.GetExplosiveData"/> returns it for an explosive entity.
    /// </remarks>
    [Serializable]
    public struct Explosive
    {
        /// <summary>Condition that causes the explosive to detonate.</summary>
        public ExplosiveTriggerType triggerType;

        /// <summary>Radius within which the explosion can apply damage.</summary>
        public float explosiveRadius;

        /// <summary>Base damage produced by the explosion.</summary>
        public float explosiveDamage;

        /// <summary>Proximity distance used by proximity-triggered explosives.</summary>
        public float explosiveProximity;

        /// <summary>Delay in seconds used by timed explosives.</summary>
        public float explosiveTimer;
    }
}
