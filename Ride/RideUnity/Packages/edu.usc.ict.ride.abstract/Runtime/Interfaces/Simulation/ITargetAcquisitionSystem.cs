using System.Collections;
using System.Collections.Generic;

namespace Ride.Combat
{
    /// <summary>Defines how combatants discover targets and create engagements.</summary>
    public interface ITargetAcquisitionSystem : IRideSystem
    {
        /// <summary>
        /// Team 1 acquires team 2 as targets, if possible
        /// </summary>
        /// <param name="team1">The attackers</param>
        /// <param name="team2">The targets</param>
        /// <returns>IDs for all new engagements created</returns>
        IEnumerable<RideID> AcquireTargets(IEnumerable<RideID> team1, IEnumerable<RideID> team2);

        /// <summary>Finds and acquires a suitable target from a candidate set.</summary>
        /// <param name="searcher">The entity searching for a target.</param>
        /// <param name="targets">The candidate target identifiers.</param>
        /// <returns><see langword="true"/> when a target is acquired; otherwise, <see langword="false"/>.</returns>
        bool FindTarget(RideID searcher, IEnumerable<RideID> targets);

        /// <summary>Attempts to create engagements from one team against another.</summary>
        /// <param name="team1">The attacking team.</param>
        /// <param name="team2">The target team.</param>
        void AcquireTargets(Team team1, Team team2);

        /// <summary>
        /// Tests if the searcher can acquire a target
        /// </summary>
        /// <param name="searcher">The entity attempting to acquire a target.</param>
        /// <returns>True if the search can acquire a target</returns>
        bool CanAcquire(RideID searcher);

        /// <summary>
        /// Tests if the target can be acquired
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><see langword="true"/> if the target can be acquired; otherwise, <see langword="false"/>.</returns>
        bool CanBeAcquired(RideID target);

        /// <summary>
        /// Creates an engagement between the searcher and target.
        /// </summary>
        /// <param name="searcher">The attacking entity.</param>
        /// <param name="target">The target entity.</param>
        /// <returns>The Engagement id</returns>
        RideID AcquireTarget(RideID searcher, RideID target);
    }
}
