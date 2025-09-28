using Ride.Entities;

namespace Ride.Combat
{
    /// <summary>
    /// All of the data associated with an attack
    /// </summary>
    public interface IAttack
    {
        /// <summary>
        ///  The point of attack. Examples: where a mortar or IED explodes
        /// </summary>
        RideVector3 position { get; set; }

        /// <summary>
        /// Flags to control how combat is calculated
        /// </summary>
        AttackFlags flags { get; set; }
    }

    /// <summary>
    /// Data associated with an attack
    /// </summary>
    public struct Attack : IAttack
    {
        public RideVector3 position { get; set; }
        public AttackFlags flags { get; set; }

        public static readonly Attack None = new Attack();
    }
}
