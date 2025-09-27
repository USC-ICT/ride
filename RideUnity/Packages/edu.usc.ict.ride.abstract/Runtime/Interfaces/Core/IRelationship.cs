namespace Ride.Entities
{
    /// <summary>
    /// Interface that should be implemented for defining the relationship between 2 entities or groups
    /// </summary>
    public interface IRelationship
    {
        /// <summary>
        /// ID of one of the entities or groups in this relationship
        /// </summary>
        RideID partyA { get; set; }

        /// <summary>
        /// ID of the other entity or group in this relationship
        /// </summary>
        RideID partyB { get; set; }

        /// <summary>
        /// The higher the value, the better the relationship. Range: [0-1]
        /// </summary>
        float friendliness { get; set; }
    }

    /// <summary>
    /// Defines data for a relationship between 2 entities or groups
    /// </summary>
    [System.Serializable]
    public struct Relationship : IRelationship
    {
        /// <summary>
        /// ID of one of the entities or groups in this relationship
        /// </summary>
        public RideID partyA { get; set; }

        /// <summary>
        /// ID of the other entity or group in this relationship
        /// </summary>
        public RideID partyB { get; set; }

        /// <summary>
        /// The higher the value, the better the relationship. Range: [0-1]
        /// </summary>
        public float friendliness { get; set; }

        public Relationship(RideID partyA, RideID partyB, float friendliness)
        {
            this.partyA = partyA;
            this.partyB = partyB;
            this.friendliness = friendliness;
        }
    }
}
