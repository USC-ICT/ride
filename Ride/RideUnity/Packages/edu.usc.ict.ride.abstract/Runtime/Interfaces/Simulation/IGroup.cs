using System;
using Ride.Movement;

namespace Ride.Entities
{
    /// <summary>
    /// Interface that should be implemented for defining a group with GroupMembers
    /// </summary>
    public interface IGroup : IIdentity
    {
        new string name { get; set; }
        FormationProcedureType formation { get; set; }
    }

    /// <summary>
    /// Defines data for a group
    /// </summary>
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

        public FormationProcedureType formation { get; set; }

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
