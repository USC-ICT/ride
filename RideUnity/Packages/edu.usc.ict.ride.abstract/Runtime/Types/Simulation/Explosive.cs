using System;

namespace Ride.Entities
{
    [Serializable]
    public struct Explosive
    {
        public ExplosiveTriggerType triggerType;
        public float explosiveRadius;
        public float explosiveDamage;
        public float explosiveProximity;
        public float explosiveTimer;
    }
}
