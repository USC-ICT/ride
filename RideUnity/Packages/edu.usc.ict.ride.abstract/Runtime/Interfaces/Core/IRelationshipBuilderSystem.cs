using System;

namespace Ride.Entities
{
    /// <summary>
    /// Provides functionality for creating and managing relationships between two group entities.
    /// </summary>
    public interface IRelationshipBuilderSystem : IRideSystem
    {
        // Creation / Removal

        /// <summary>
        /// Creates a new relationship between two parties.
        /// </summary>
        /// <param name="partyA">The ID of the first group.</param>
        /// <param name="partyB">The ID of the second group.</param>
        /// <param name="friendliness">Initial friendliness value (range [0, 1]).</param>
        /// <returns>The ID of the created or updated relationship.</returns>
        RideID AddRelationship(RideID partyA, RideID partyB, float friendliness);

        /// <summary>
        /// Removes an existing relationship by its ID.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship to remove.</param>
        /// <returns>True if the relationship was successfully removed.</returns>
        bool RemoveRelationship(RideID relationshipId);

        // Existence / Lookup

        /// <summary>
        /// Determines whether a relationship exists between the two specified parties.
        /// </summary>
        /// <param name="partyA">The ID of the first group.</param>
        /// <param name="partyB">The ID of the second group.</param>
        /// <returns>True if a relationship exists.</returns>
        bool DoesRelationshipExist(RideID partyA, RideID partyB);

        /// <summary>
        /// Retrieves the two parties involved in the specified relationship.
        /// </summary>
        /// <param name="relationshipId">The relationship ID.</param>
        /// <returns>A tuple of (partyA, partyB), or (RideID.Null, RideID.Null) if not found.</returns>
        Tuple<RideID, RideID> GetParties(RideID relationshipId);

        // Friendliness Functions

        /// <summary>
        /// Returns the friendliness value of the specified relationship.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship.</param>
        /// <returns>The friendliness value, or -1 if the relationship doesn't exist.</returns>
        float GetFriendliness(RideID relationshipId);

        /// <summary>
        /// Returns the friendliness value between two parties.
        /// </summary>
        /// <param name="partyA">The ID of the first group.</param>
        /// <param name="partyB">The ID of the second group.</param>
        /// <returns>The friendliness value, or -1 if no relationship exists.</returns>
        float GetFriendliness(RideID partyA, RideID partyB);

        /// <summary>
        /// Sets the friendliness value for the specified relationship.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship.</param>
        /// <param name="friendliness">The new friendliness value (range [0, 1]).</param>
        void SetFriendliness(RideID relationshipId, float friendliness);

        /// <summary>
        /// Adjusts the friendliness value of a relationship by the specified delta.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship.</param>
        /// <param name="mod">The amount to add (positive or negative) to the current value.</param>
        void ModifyFriendliness(RideID relationshipId, float mod);

    }
}
