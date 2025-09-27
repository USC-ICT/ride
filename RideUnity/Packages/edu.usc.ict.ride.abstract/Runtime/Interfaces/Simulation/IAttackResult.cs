namespace Ride.Combat
{
    public enum AttackStatus
    {
        NoAttack,
        Failed_AttackerIsDead,
        Failed_TargetIsDead,
        Failed_WeaponFailed,
        Failed_Suppressed,
        Engaging,
        Complete,
        Failed_OutOfAmmo
    }

    /// <summary>
    /// The result of an agent to agent attack
    /// </summary>
    public interface IAttackResult
    {
        /// <summary>
        /// Status of the attack
        /// </summary>
        AttackStatus status { get; set; }

        /// <summary>
        /// The Agent doing the attacking
        /// </summary>
        RideID attacker { get; set; }

        /// <summary>
        /// The Agent receiving the attack
        /// </summary>
        RideID attackee { get; set; }

        /// <summary>
        /// Returns true if the attackee is dead
        /// </summary>
        bool isDead { get; }

        /// <summary>
        /// Returns true if the damage done was greater than 0
        /// </summary>
        bool isHit { get; }

        /// <summary>
        /// The amount of damage the attacker did to the attackee
        /// </summary>
        float damage { get; set; }

        /// <summary>
        /// the weapon used in the attack
        /// </summary>
        RideID weapon { get; set; }

        /// <summary>
        /// The position of the attack
        /// </summary>
        RideVector3 hitPos { get; set; }
    }

    /// <summary>
    /// The result of an agent to agent attack
    /// </summary>
    public class AttackResult : IAttackResult
    {
        /// <summary>
        /// Status of the attack
        /// </summary>
        public AttackStatus status { get; set; }

        /// <summary>
        /// The Agent doing the attacking
        /// </summary>
        public RideID attacker { get; set; }

        /// <summary>
        /// The Agent receiving the attack
        /// </summary>
        public RideID attackee { get; set; }

        /// <summary>
        /// Returns true if the attackee is dead
        /// </summary>
        public bool isDead { get; set; }

        /// <summary>
        /// Returns true if the damage done was greater than 0
        /// </summary>
        public bool isHit { get { return (damage > 0); } }

        /// <summary>
        /// The amount of damage the attacker did to the attackee
        /// </summary>
        public float damage { get; set; }

        /// <summary>
        /// the weapon used in the attack
        /// </summary>
        public RideID weapon { get; set; }

        /// <summary>
        /// The position of the attack
        /// </summary>
        public RideVector3 hitPos { get; set; }

        public AttackResult()
        {
            attacker = RideID.Null;
            attackee = RideID.Null;
            isDead = false;
            damage = 0.0f;
            weapon = RideID.Null;
            status = AttackStatus.NoAttack;
        }

        public AttackResult(RideID attcker, RideID attckee, bool dead, float dmg, RideID wpn, AttackStatus state)
        {
            attacker = attcker;
            attackee = attckee;
            isDead = dead;
            damage = dmg;
            weapon = wpn;
            status = state;
        }
    }
}
