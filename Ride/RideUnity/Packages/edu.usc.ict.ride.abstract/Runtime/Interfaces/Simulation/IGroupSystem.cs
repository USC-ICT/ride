using Ride.Movement;

namespace Ride.Entities
{
    /// <summary>
    /// Provides higher-level operations for managing groups,
    /// including formation behaviors, member hierarchy, and reverse lookups.
    /// </summary>
    public interface IGroupSystem : IGroupBuilderSystem, IRelationshipBuilderSystem
    {
        /// <summary>
        /// Returns the depth level of the group in the hierarchy.
        /// A group with no subgroups has level 0.
        /// Each additional nested subgroup increases the level by 1.
        /// </summary>
        /// <param name="groupId">The unique ID of the group.</param>
        /// <returns>The group's hierarchical depth (0 = leaf group).</returns>
        int GetGroupLevel(RideID groupId);

        /// <summary>
        /// Gets the current formation procedure assigned to the group.
        /// </summary>
        /// <param name="groupId">The unique ID of the group.</param>
        /// <returns>The group's <see cref="FormationProcedureType"/> value.</returns>
        FormationProcedureType GetFormationType(RideID groupId);

        /// <summary>
        /// Sets the formation procedure to be used by the group.
        /// </summary>
        /// <param name="groupId">The unique ID of the group.</param>
        /// <param name="formType">The desired <see cref="FormationProcedureType"/>.</param>
        void SetFormationType(RideID groupId, FormationProcedureType formType);

        /// <summary>
        /// Sets the rank of a given group member. Higher ranks take precedence when determining leadership.
        /// </summary>
        /// <param name="member">The unique ID of the member.</param>
        /// <param name="rank">The rank to assign (higher means more authority).</param>
        void SetGroupMemberRank(RideID member, int rank);

        /// <summary>
        /// Returns the group ID that the specified member belongs to.
        /// If the member is not found in any group, returns <see cref="RideID.Null"/>.
        /// </summary>
        /// <param name="member">The unique ID of the member.</param>
        /// <returns>The group ID of the member, or <see cref="RideID.Null"/>.</returns>
        RideID GetGroup(RideID member);
    }
}
