using System.Collections.Generic;
using Ride.Entities;

namespace Ride.Movement
{
    /// <summary>
    /// Defines the structure for data returned by a formation procedure,
    /// representing a single mover and its target destination in world space.
    /// </summary>
    public interface IUnitFormationData
    {
        /// <summary>
        /// The unique ID of the mover assigned to this formation position.
        /// </summary>
        RideID moverId { get; }

        /// <summary>
        /// The world position that the mover should move to as part of the formation.
        /// </summary>
        RideVector3 destination { get; }
    }

    /// <summary>
    /// Enumerates the supported types of unit formations. These are used to
    /// identify and instantiate specific formation procedures via factory logic.
    /// </summary>
    public enum FormationProcedureType
    {
        MaintainRelativeDistance,
        Cluster,
        Column,
        Wedge,
        Line,
        Arrow,
        //V,
        NUM_STANDARD_FORMATIONS
    }

    /// <summary>
    /// Interface for formation procedure logic. A formation procedure calculates
    /// the destination positions for a group of movers based on a given shape,
    /// destination, and direction. It may also support recursive subgroup layout.
    /// </summary>
    public interface IFormationProcedure : IIdentity
    {
        /// <summary>
        /// Gets the specific formation type implemented by this procedure.
        /// Used for tracking and display purposes.
        /// </summary>
        FormationProcedureType formationType { get; }


        // -------------------------------------------------------------
        // High-level layout
        // -------------------------------------------------------------

        /// <summary>
        /// Calculates the formation layout for the specified unit or group,
        /// resolving any nested subgroups recursively.
        /// </summary>
        /// <param name="unitId">The root RideID of the unit or group to move.</param>
        /// <param name="destination">The target world position for the formation center.</param>
        /// <param name="direction">The world-space forward direction the formation should face.</param>
        /// <param name="spaceDistance">Spacing between units or subgroups, default is 2.0 meters.</param>
        /// <returns>A collection of formation data entries with assigned destinations for each mover.</returns>
        IEnumerable<IUnitFormationData> CalculatePositionsForUnit(
            RideID unitId,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );


        // -------------------------------------------------------------
        // Basic layout
        // -------------------------------------------------------------

        /// <summary>
        /// Calculates the position for each mover in a flat list of RideIDs,
        /// given a target destination and facing direction.
        /// </summary>
        /// <param name="movers">The movers to be placed in formation.</param>
        /// <param name="destination">The world-space destination to center the formation on.</param>
        /// <param name="direction">The direction the formation should face in world space.</param>
        /// <param name="spaceDistance">The base spacing between each unit.</param>
        /// <returns>A list of unit formation assignments (mover + target position).</returns>
        IEnumerable<IUnitFormationData> CalculatePositions(
            IEnumerable<RideID> movers,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );

        /// <summary>
        /// Calculates positions for each subgroup of movers, producing one set of
        /// formation data per group. Useful for multi-level or hierarchical formations.
        /// </summary>
        /// <param name="movers">A list of mover groups, each a collection of RideIDs.</param>
        /// <param name="destination">The shared world-space destination for all groups.</param>
        /// <param name="direction">The direction all formations should face.</param>
        /// <param name="spaceDistance">Spacing between movers in each group.</param>
        /// <returns>A collection of formation data lists, one per group.</returns>
        IEnumerable<IEnumerable<IUnitFormationData>> CalculatePositions(
            IEnumerable<IEnumerable<RideID>> movers,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );


        // -------------------------------------------------------------
        // Utilities
        // -------------------------------------------------------------

        /// <summary>
        /// Rotates the specified movers from one world-space direction to another,
        /// maintaining their relative formation layout around a computed center.
        /// </summary>
        /// <param name="movers">The movers to rotate.</param>
        /// <param name="fromDirection">The original formation direction.</param>
        /// <param name="toDirection">The target world-space direction.</param>
        /// <returns>Updated destination data for each mover after rotation.</returns>
        IEnumerable<IUnitFormationData> RotateFormation(
            IEnumerable<RideID> movers,
            RideVector3 fromDirection,
            RideVector3 toDirection
        );

        /// <summary>
        /// Computes the geometric center (average position) of all given movers.
        /// Used to establish formation pivot and alignment.
        /// </summary>
        /// <param name="movers">The movers whose positions will be averaged.</param>
        /// <returns>The center point of the group in world space.</returns>
        RideVector3 GetFormationCenter(IEnumerable<RideID> movers);


        // -------------------------------------------------------------
        // Built-in shape builders (for recursive layout)
        // -------------------------------------------------------------

        /// <summary>
        /// Generates a wedge-shaped formation layout for the given movers.
        /// </summary>
        IEnumerable<IUnitFormationData> GenerateFormationWedge(
            IEnumerable<RideID> movers,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );

        /// <summary>
        /// Generates a vertical line formation layout (units arranged back-to-front).
        /// </summary>
        IEnumerable<IUnitFormationData> GenerateFormationVerticalLine(
            IEnumerable<RideID> movers,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );

        /// <summary>
        /// Generates a horizontal line formation layout (units arranged side-by-side).
        /// </summary>
        IEnumerable<IUnitFormationData> GenerateFormationHorizontalLine(
            IEnumerable<RideID> movers,
            RideVector3 destination,
            RideVector3 direction,
            float spaceDistance = 2.0f
        );
    }
}
