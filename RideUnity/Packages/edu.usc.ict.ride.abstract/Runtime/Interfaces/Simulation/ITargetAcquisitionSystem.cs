using System.Collections;
using System.Collections.Generic;

namespace Ride.Combat
{
    /// <summary>
    /// Interface for defining the fules for how targets can be acquired
    /// </summary>
    public interface ITargetAcquisitionSystem : IRideSystem
    {
        /// <summary>
        /// Team 1 acquires team 2 as targets, if possible
        /// </summary>
        /// <param name="team1">The attackers</param>
        /// <param name="team2">The targets</param>
        /// <returns>IDs for all new engagements created</returns>
        IEnumerable<RideID> AcquireTargets(IEnumerable<RideID> team1, IEnumerable<RideID> team2);

        bool FindTarget(RideID searcher, IEnumerable<RideID> targets);

        void AcquireTargets(Team team1, Team team2);

        /// <summary>
        /// Tests if the searcher can acquire a target
        /// </summary>
        /// <param name="searcher"></param>
        /// <returns>True if the search can acquire a target</returns>
        bool CanAcquire(RideID searcher);

        /// <summary>
        /// Tests if the target can be acquired
        /// </summary>
        /// <param name="target"></param>
        /// <returns>Teuf if the target can be acquired</returns>
        bool CanBeAcquired(RideID target);

        /// <summary>
        /// Creates and engagement between the searcher and the target
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="target"></param>
        /// <returns>The Engagement id</returns>
        RideID AcquireTarget(RideID searcher, RideID target);
    }
}
