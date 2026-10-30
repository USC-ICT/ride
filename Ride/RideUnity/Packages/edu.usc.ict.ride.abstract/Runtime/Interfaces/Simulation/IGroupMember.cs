using System;

namespace Ride.Entities
{
    /// <summary>
    /// Interface that should be implemented for Defining data required for a single group member
    /// </summary>
    public interface IGroupMember : IIdentity
    {
        /// <summary>
        /// Position of power in the group. The higher the rank, the more powerful
        /// </summary>
        int rank { get; set; }

        /// <summary>
        /// Title of the member (i.e. Commanding Officer)
        /// </summary>
        string title { get; }
    }

    /// <summary>
    /// Defines data for a single group member
    /// </summary>
    [Serializable]
    public struct GroupMember : IGroupMember
    {
        /// <summary>
        /// Identifier of the entity in this group
        /// </summary>
        public RideID id { get; set; }

        /// <summary>
        /// Identifier of the entity in this group
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Position of power in the group. The higher the rank, the more powerful
        /// </summary>
        public int rank { get; set; }

        /// <summary>
        /// Title of the member (i.e. Commanding Officer)
        /// </summary>
        public string title { get; }

        /// <summary>Initializes a group member.</summary>
        /// <param name="id">The member identifier.</param>
        /// <param name="name">The member display name.</param>
        /// <param name="rank">The member's authority rank.</param>
        /// <param name="title">The member's role within the group.</param>
        public GroupMember(RideID id, string name, int rank, string title)
        {
            this.id = id;
            this.name = name;
            this.rank = rank;
            this.title = title;
        }
    }
}
