namespace Ride.Combat
{
    /// <summary>Describes the lifecycle state or failure reason of an attack.</summary>
    public enum AttackStatus
    {
        /// <summary>No attack has been attempted.</summary>
        NoAttack,
        /// <summary>The attack failed because the attacker is dead.</summary>
        Failed_AttackerIsDead,
        /// <summary>The attack failed because the target is dead.</summary>
        Failed_TargetIsDead,
        /// <summary>The attack failed because the weapon could not fire.</summary>
        Failed_WeaponFailed,
        /// <summary>The attack failed because the attacker was suppressed.</summary>
        Failed_Suppressed,
        /// <summary>The attack is currently in progress.</summary>
        Engaging,
        /// <summary>The attack completed.</summary>
        Complete,
        /// <summary>The attack failed because no ammunition was available.</summary>
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

        /// <summary>Initializes an empty attack result.</summary>
        public AttackResult()
        {
            attacker = RideID.Null;
            attackee = RideID.Null;
            isDead = false;
            damage = 0.0f;
            weapon = RideID.Null;
            status = AttackStatus.NoAttack;
        }

        /// <summary>Initializes an attack result.</summary>
        /// <param name="attcker">The attacking entity.</param>
        /// <param name="attckee">The attacked entity.</param>
        /// <param name="dead">Whether the attacked entity died.</param>
        /// <param name="dmg">The applied damage.</param>
        /// <param name="wpn">The weapon used.</param>
        /// <param name="state">The resulting attack status.</param>
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
