using System.Collections.Generic;
using Ride.Movement;

namespace Ride.Entities
{
    /// <summary>
    /// Provides low-level operations for creating, modifying, and querying groups and their members.
    /// </summary>
    public interface IGroupBuilderSystem : IRideSystem
    {
        // ──────────────── Group Creation / Deletion ────────────────

        /// <summary>
        /// Creates a new group with the specified name.
        /// </summary>
        /// <param name="name">The name of the new group.</param>
        /// <returns>The unique ID of the newly created group.</returns>
        RideID CreateGroup(string name);

        /// <summary>
        /// Creates a new group with the specified name, members, subgroups, and optional formation type.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        /// <param name="members">IDs of the initial group members.</param>
        /// <param name="subgroups">IDs of the initial subgroups.</param>
        /// <param name="formationType">The group's formation strategy (default is Wedge).</param>
        /// <returns>The unique ID of the new group.</returns>
        RideID CreateGroup(
            string name,
            IEnumerable<RideID> members,
            IEnumerable<RideID> subgroups,
            FormationProcedureType formationType = FormationProcedureType.Wedge
        );

        /// <summary>
        /// Removes the group. You can optionally remove its members and subgroups.
        /// </summary>
        /// <param name="group">The ID of the group to remove.</param>
        /// <param name="removeMembers">If true, also remove the group's members.</param>
        /// <param name="recursiveRemove">If true, also remove subgroups recursively.</param>
        /// <returns>True if the group was removed successfully.</returns>
        bool RemoveGroup(RideID group, bool removeMembers = false, bool recursiveRemove = false);

        /// <summary>
        /// Checks whether the given group exists.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <returns>True if the group exists.</returns>
        bool DoesGroupExist(RideID group);

        // ──────────────── Membership Management ────────────────

        /// <summary>
        /// Adds a member to the specified group with the given rank and title.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="memberId">The ID of the member (typically an agent).</param>
        /// <param name="memberRank">The member's rank. Higher values denote more authority.</param>
        /// <param name="memberTitle">The member's role or title within the group.</param>
        /// <returns>The ID of the group the member was added to.</returns>
        RideID AddMember(RideID group, RideID memberId, int memberRank, string memberTitle);

        RideID AddMember(RideID group, RideID memberId, string memberName, int memberRank, string memberTitle);

        /// <summary>
        /// Removes a member from the specified group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="member">The ID of the member to remove.</param>
        /// <returns>True if the member was successfully removed.</returns>
        bool RemoveMember(RideID group, RideID member);

        /// <summary>
        /// Removes all members from the specified group.
        /// </summary>
        /// <param name="group">The ID of the group to clear.</param>
        void RemoveMembers(RideID group);

        /// <summary>
        /// Determines whether the specified member is part of the group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="member">The ID of the member.</param>
        /// <param name="includeSubgroups">Whether to check subgroups recursively.</param>
        /// <returns>True if the member is part of the group or its subgroups.</returns>
        bool IsMember(RideID group, RideID member, bool includeSubgroups = true);

        /// <summary>
        /// Returns the number of members in the specified group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="includeSubgroups">Whether to include members from subgroups recursively.</param>
        /// <returns>The total number of members found.</returns>
        int GetNumMembersInGroup(RideID group, bool includeSubgroups);

        /// <summary>
        /// Returns all members assigned to the group, optionally including subgroups.
        /// Note: Members may be listed more than once if they appear in multiple subgroups.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="includeSubgroups">If true, include subgroup members.</param>
        /// <param name="includeSubgroupsRecursively">If true, recurse into nested subgroups.</param>
        /// <returns>A collection of member IDs.</returns>
        IEnumerable<RideID> GetMembers(RideID group, bool includeSubgroups = true, bool includeSubgroupsRecursively = true);

        /// <summary>
        /// Returns the rank assigned to the specified member.
        /// </summary>
        /// <param name="member">The ID of the member.</param>
        /// <returns>The member's rank.</returns>
        int GetGroupMemberRank(RideID member);

        /// <summary>
        /// Returns all groups that the specified member belongs to.
        /// </summary>
        /// <param name="member">The ID of the member.</param>
        /// <returns>A collection of group IDs.</returns>
        IEnumerable<RideID> GetGroupMemberships(RideID member);

        /// <summary>
        /// Returns the highest-ranking member in the group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <returns>The ID of the group leader.</returns>
        RideID GetGroupLeader(RideID group);

        /// <summary>
        /// Returns the average center position of all group members.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <returns>The mean center position of the group.</returns>
        RideVector3 GetGroupCenter(RideID groupId);

        // ──────────────── Subgroup Management ────────────────

        /// <summary>
        /// Adds the specified subgroup to the given parent group.
        /// </summary>
        /// <param name="group">The ID of the parent group.</param>
        /// <param name="subgroup">The ID of the group to be added as a subgroup.</param>
        /// <returns>The ID of the subgroup that was added.</returns>
        RideID AddSubgroup(RideID group, RideID subgroup);

        /// <summary>
        /// Removes the specified subgroup from the given parent group.
        /// </summary>
        /// <param name="group">The ID of the parent group.</param>
        /// <param name="subgroup">The ID of the subgroup to remove.</param>
        /// <returns>True if the subgroup was removed successfully.</returns>
        bool RemoveSubgroup(RideID group, RideID subgroup);

        /// <summary>
        /// Returns all subgroup IDs contained directly within the specified group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <returns>A collection of subgroup IDs. Returns an empty collection if none exist.</returns>
        IEnumerable<RideID> GetSubgroups(RideID group);

        // ──────────────── Metadata ────────────────

        /// <summary>
        /// Sets the name of the specified group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <param name="name">The new display name to assign.</param>
        void SetName(RideID group, string name);

        /// <summary>
        /// Gets the name of the specified group.
        /// </summary>
        /// <param name="group">The ID of the group.</param>
        /// <returns>The group's display name, or an empty string if unnamed.</returns>
        string GetName(RideID group);
    }
}
