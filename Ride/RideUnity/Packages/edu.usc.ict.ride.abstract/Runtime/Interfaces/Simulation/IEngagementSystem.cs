using System.Collections.Generic;

namespace Ride.Combat
{
    /// <summary>
    /// Defines how engagments between entities are created and maintained
    /// </summary>
    public interface IEngagementSystem : IRideSystem
    {
        /// <summary>
        /// Checks if the engagement should be lost
        /// </summary>
        /// <param name="engagement"></param>
        /// <returns>True if the engagement is lost, otherwise false</returns>
        bool IsLost(Engagement engagement);

        /// <summary>
        /// Updates and removes lost engagements
        /// </summary>
        /// <returns>List of engagers that lost their engagements</returns>
        IEnumerable<RideID> UpdateForLostEngagements();

        /// <summary>
        /// Creates a new engagement
        /// </summary>
        /// <param name="engager">The engager of the engagement</param>
        /// <param name="engagee">The target of the engager</param>
        /// <returns>The id of the engagement</returns>
        RideID Create(RideID engager, RideID engagee);

        /// <summary>
        /// Creates a new engagement
        /// </summary>
        /// <param name="engager">The engager of the engagement</param>
        /// <param name="attackee">The target of the engager</param>
        /// <param name="weapon">The weapon of the engager used in the engagement</param>
        /// <returns>The id of the engagement</returns>
        RideID Create(RideID engager, RideID attackee, RideID weapon);

        /// <summary>
        /// Destroy the engagement
        /// </summary>
        /// <param name="engagement"></param>
        void Destroy(RideID engagement);

        /// <summary>
        /// Destroy the engagement in which the given engager is involved
        /// </summary>
        /// <param name="engager"></param>
        void DestroyAttackerEngagement(RideID engager);

        /// <summary>
        /// Destroy all engagements in which the engagee is involved
        /// </summary>
        /// <param name="engagee"></param>
        void DestroyAttackeeEngagements(RideID engagee);

        /// <summary>
        /// Returns true if the given engager is an engager (not an engagee) in an engagement
        /// </summary>
        /// <param name="engager"></param>
        /// <returns>True if the given engager is an engager (not an engagee) in an engagement</returns>
        bool IsEngaged(RideID engager);

        /// <summary>
        /// Tests if the engagement exists
        /// </summary>
        /// <param name="engagement">The engagement</param>
        /// <returns>True if the engagement exists</returns>
        bool Exists(RideID engagement);

        /// <summary>
        /// Tests if an engagement exists between an engager and engagee
        /// </summary>
        /// <param name="engager">The engager</param>
        /// <param name="engagee">The engagee</param>
        /// <param name="weapon">A specific weapon (Use RideID.Null to ignore this parameter)</param>
        /// <returns>True if the engagement exists</returns>
        bool Exists(RideID engager, RideID engagee, RideID weapon);

        /// <summary>
        /// Gets the engagement id and engagement from the engager (if it exists)
        /// </summary>
        /// <param name="attacker"></param>
        /// <returns>The engagement. Engagement.Null if it doesn't exist</returns>
        (RideID, Engagement) GetEngagementFromAttacker(RideID attacker);

        /// <summary>
        /// Returns the engagement
        /// </summary>
        /// <param name="engagement"></param>
        /// <returns>The engagement. Engagement.Null if it doesn't exist</returns>
        Engagement GetEngagement(RideID engagement);

        /// <summary>
        /// Returns all current engagements
        /// </summary>
        /// <returns>All current engagement</returns>
        IEnumerable<Engagement> GetEngagements();
    }
}
