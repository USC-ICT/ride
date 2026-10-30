using System;
using Ride.Movement;

namespace Ride.Entities
{
    /// <summary>Represents an identified entity group with a name and movement formation.</summary>
    /// <remarks>Membership is managed by <see cref="IGroupSystem"/> rather than exposed directly by this contract.</remarks>
    public interface IGroup : IIdentity
    {
        /// <summary>Gets or sets the group display name.</summary>
        new string name { get; set; }
        /// <summary>Gets or sets the formation used when arranging group members.</summary>
        FormationProcedureType formation { get; set; }
    }

    /// <summary>Provides the serializable group data created and maintained by the group system.</summary>
    [Serializable]
    public class Group : IGroup
    {
        /// <summary>
        /// Group ID. Use this to find the group
        /// </summary>
        public RideID id { get; private set; }

        /// <summary>
        /// Group name
        /// </summary>
        public string name { get; set; }

        /// <inheritdoc/>
        public FormationProcedureType formation { get; set; }

        /// <summary>Initializes a group.</summary>
        /// <param name="groupId">The unique group identifier.</param>
        /// <param name="groupName">The group display name.</param>
        /// <param name="form">The initial member formation.</param>
        public Group(RideID groupId, string groupName, FormationProcedureType form = FormationProcedureType.Wedge)
        {
            id = groupId;
            name = groupName;
            formation = form;
        }

        /*
        /// <summary>
        /// Child groups
        /// </summary>
        public IEnumerable<IGroupMember> members { get; }

        /// <summary>
        /// Child groups
        /// </summary>
        public IEnumerable<IGroup> subgroups { get; }

        public Group(int id, string name, IEnumerable<IGroupMember> members, IEnumerable<IGroup> subgroups)
        {
            this.id = id;
            this.name = name;
            this.members = members;
            this.subgroups = subgroups;
        }
        */
    }
}
