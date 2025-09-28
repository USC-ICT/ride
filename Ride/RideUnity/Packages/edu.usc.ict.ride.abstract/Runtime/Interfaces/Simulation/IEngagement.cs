using System.Collections.Generic;

namespace Ride.Combat
{
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

        public static readonly Engagement Null = new Engagement() { attacker = RideID.Null, attackee = RideID.Null, weapon = RideID.Null };

        static public bool operator ==(Engagement lhs, Engagement rhs)
        {
            return lhs.attacker == rhs.attacker && lhs.attackee == rhs.attackee && lhs.weapon == rhs.weapon;
        }

        static public bool operator !=(Engagement lhs, Engagement rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            return obj is Engagement engagement &&
                   EqualityComparer<RideID>.Default.Equals(attacker, engagement.attacker) &&
                   EqualityComparer<RideID>.Default.Equals(attackee, engagement.attackee) &&
                   EqualityComparer<RideID>.Default.Equals(weapon, engagement.weapon);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(attacker, attackee, weapon);
        }
    }
}
