using System.Collections.Generic;

namespace Ride.Combat
{
    /// <summary>Associates an attacker, target, and weapon in an active combat engagement.</summary>
    public interface IEngagement
    {
        /// <summary>
        /// Agent doing the attacking
        /// </summary>
        RideID attacker { get; set; }

        /// <summary>
        /// Agent being attacked
        /// </summary>
        RideID attackee { get; set; }

        /// <summary>
        /// Weapon being used by the attacker
        /// </summary>
        RideID weapon { get; set; }
    }

    /// <summary>Provides value-type engagement data maintained by <see cref="IEngagementSystem"/>.</summary>
    public struct Engagement : IEngagement
    {
        /// <summary>
        /// Agent doing the attacking
        /// </summary>
        public RideID attacker { get; set; }

        /// <summary>
        /// Agent being attacked
        /// </summary>
        public RideID attackee { get; set; }

        /// <summary>
        /// Weapon being used by the attacker
        /// </summary>
        public RideID weapon { get; set; }

        /// <summary>Represents an unavailable or nonexistent engagement.</summary>
        public static readonly Engagement Null = new Engagement() { attacker = RideID.Null, attackee = RideID.Null, weapon = RideID.Null };

        /// <summary>Determines whether two engagement values identify the same attacker, target, and weapon.</summary>
        static public bool operator ==(Engagement lhs, Engagement rhs)
        {
            return lhs.attacker == rhs.attacker && lhs.attackee == rhs.attackee && lhs.weapon == rhs.weapon;
        }

        /// <summary>Determines whether two engagement values differ.</summary>
        static public bool operator !=(Engagement lhs, Engagement rhs)
        {
            return !(lhs == rhs);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Engagement engagement &&
                   EqualityComparer<RideID>.Default.Equals(attacker, engagement.attacker) &&
                   EqualityComparer<RideID>.Default.Equals(attackee, engagement.attackee) &&
                   EqualityComparer<RideID>.Default.Equals(weapon, engagement.weapon);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return System.HashCode.Combine(attacker, attackee, weapon);
        }
    }
}
