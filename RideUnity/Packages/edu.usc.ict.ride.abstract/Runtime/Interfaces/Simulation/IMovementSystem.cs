using System.Collections.Generic;

namespace Ride.Movement
{
    public enum MovementLeg { Dismounted, Wheeled, Tracked }

    /// <summary>
    /// Interface for movement systems that manage pathfinding and locomotion
    /// of entities (movers) in the simulation. This includes basic position
    /// movement, path-following, formation movement, dynamic routing, and 
    /// navigation mesh interaction. Movement commands operate on individual 
    /// entities or groups, and expose both high-level and low-level control.
    /// 
    /// This interface is implemented by multiple backends (e.g., NavMesh or AStar).
    /// </summary>
    public interface IMovementSystem : IRideSystem
    {
        // ---------------------------------------------------------------------
        // System Info
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns the name or identifier of the underlying movement system.
        /// Useful for debugging or selecting between implementations (e.g., "NavMesh", "AStar").
        /// </summary>
        /// <returns>String representing the movement system type.</returns>
        string GetMovementSystemType();


        // ---------------------------------------------------------------------
        // MovementBehaviour Management
        // 
        // Associates IMovementBehaviour implementations with the system,
        // and allows assigning them to movers to control their movement logic.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Registers a movement behavior implementation to be managed by the system.
        /// This allows it to be selected and assigned to movers.
        /// </summary>
        /// <param name="nav">The movement behavior instance to register.</param>
        void AddMovementBehaviour(IMovementBehaviour nav);

        /// <summary>
        /// Unregisters a movement behavior implementation from the system.
        /// </summary>
        /// <param name="nav">The movement behavior instance to remove.</param>
        void RemoveMovementBehaviour(IMovementBehaviour nav);

        /// <summary>
        /// Returns a registered movement behavior by its numeric ID.
        /// </summary>
        /// <param name="id">The ID of the movement behavior.</param>
        /// <returns>The matching <see cref="IMovementBehaviour"/> if found; otherwise null.</returns>
        IMovementBehaviour GetMovementBehaviour(RideID id);

        /// <summary>
        /// Returns a registered movement behavior by its type.
        /// </summary>
        /// <param name="type">The behavior type to retrieve.</param>
        /// <returns>The matching <see cref="IMovementBehaviour"/> if found; otherwise null.</returns>
        IMovementBehaviour GetMovementBehaviour(MovementBehaviour type);

        /// <summary>
        /// Assigns a movement behavior type to a specific mover.
        /// This determines how the mover will move when commanded,
        /// but does not start movement automatically.
        /// </summary>
        /// <param name="mover">The entity ID to assign the behavior to.</param>
        /// <param name="type">The movement behavior type to assign.</param>
        void SetMovementBehaviour(RideID mover, MovementBehaviour type);

        /// <summary>
        /// Assigns a pathing behavior to the specified mover.
        /// This controls how the mover handles path segments,
        /// but does not trigger movement automatically.
        /// </summary>
        /// <param name="mover">The mover to configure.</param>
        /// <param name="type">The pathing behavior to apply.</param>
        void SetPathingBehaviour(RideID mover, PathingBehaviour type);


        // ---------------------------------------------------------------------
        // Move Commands
        //
        // Issues movement instructions to individual movers or groups.
        // Supports direct movement, path-following, and formations.
        // ---------------------------------------------------------------------

        // -- Individual Movement --

        /// <summary>
        /// Moves a single mover to a world-space position using default speed.
        /// </summary>
        /// <param name="mover">The entity to move.</param>
        /// <param name="destination">Target world-space position.</param>
        void MoveToPosition(RideID mover, RideVector3 destination);

        /// <summary>
        /// Moves a single mover to a world-space position at the given speed.
        /// </summary>
        /// <param name="mover">The entity to move.</param>
        /// <param name="destination">Target world-space position.</param>
        /// <param name="speed">Movement speed.</param>
        void MoveToPosition(RideID mover, RideVector3 destination, float speed);


        // -- Group Movement --

        /// <summary>
        /// Moves a group to a shared destination at the specified speed and formation.
        /// </summary>
        /// <param name="group">The group ID representing the movers.</param>
        /// <param name="destination">The world-space position to move to.</param>
        /// <param name="speed">The speed at which to move the group.</param>
        /// <param name="formProc">The formation type to use during movement.</param>
        void MoveGroupToPosition(RideID group, RideVector3 destination, float speed, FormationProcedureType formProc);

        /// <summary>
        /// Moves a group to a shared destination at the specified speed and formation.
        /// </summary>
        /// <param name="group">The group ID representing the movers.</param>
        /// <param name="destination">The world-space position to move to.</param>
        /// <param name="speed">The speed at which to move the group.</param>
        /// <param name="formProc">The formation type to use during movement.</param>
        /// <param name="includeSubgroups">True to include subgroups. false to exclude.</param>
        /// <param name="includeSubgroupsRecursively">True to include subgroups recursively. false to exclude.</param>
        void MoveGroupToPosition(RideID group, RideVector3 destination, float speed, FormationProcedureType formProc, bool includeSubgroups = true, bool includeSubgroupsRecursively = true);


        // -- Collection Movement --

        /// <summary>
        /// Moves a collection of movers to a target position using the given formation type.
        /// </summary>
        /// <param name="movers">A collection of mover IDs.</param>
        /// <param name="destination">The world-space destination position.</param>
        /// <param name="formProc">The formation type to use.</param>
        void MoveToPosition(IEnumerable<RideID> movers, RideVector3 destination, FormationProcedureType formProc);

        /// <summary>
        /// Moves a collection of movers to a target position using a custom formation procedure.
        /// </summary>
        /// <param name="movers">A collection of mover IDs.</param>
        /// <param name="destination">The world-space destination position.</param>
        /// <param name="formProc">Custom formation procedure to follow.</param>
        void MoveToPosition(IEnumerable<RideID> movers, RideVector3 destination, IFormationProcedure formProc);


        // -- Path Movement (multiple movers) --

        /// <summary>
        /// Moves a collection of movers along a path using the specified formation type.
        /// </summary>
        /// <param name="movers">The mover IDs to move.</param>
        /// <param name="path">The path to follow as a sequence of positions.</param>
        /// <param name="formProc">The formation type to use during movement.</param>
        void MovePath(IEnumerable<RideID> movers, IEnumerable<RideVector3> path, FormationProcedureType formProc);

        /// <summary>
        /// Moves a collection of movers along a path using a custom formation procedure.
        /// </summary>
        /// <param name="movers">The mover IDs to move.</param>
        /// <param name="path">The path to follow as a sequence of positions.</param>
        /// <param name="formProc">The custom formation procedure.</param>
        void MovePath(IEnumerable<RideID> movers, IEnumerable<RideVector3> path, IFormationProcedure formProc);


        // -- Path Movement (group) --

        /// <summary>
        /// Moves a group of movers along a specified path using the group's formation behavior.
        /// </summary>
        /// <param name="group">The RideID representing the group.</param>
        /// <param name="path">The path to follow as a series of world-space positions.</param>
        /// <param name="formProc">The formation type to apply during movement.</param>
        void MoveGroupPath(RideID group, IEnumerable<RideVector3> path, FormationProcedureType formProc);


        // -- Formation Destination Movement --

        /// <summary>
        /// Moves a group to the given destination using the group's assigned internal formation logic.
        /// This will recursively move subgroups into their own formations as defined.
        /// </summary>
        /// <param name="group">The RideID of the group to move.</param>
        /// <param name="destination">World-space destination position.</param>
        void MoveGroupToPositionInFormation(RideID group, RideVector3 destination);

        /// <summary>
        /// Moves a group of entities to a destination using a built-in formation type and spacing.
        /// </summary>
        /// <param name="group">The RideID of the group to move.</param>
        /// <param name="destination">World-space destination to move to.</param>
        /// <param name="formType">Formation type to use.</param>
        /// <param name="formationDistance">Spacing between movers in the formation (in world units).</param>
        void MoveToPositionInFormation(RideID group, RideVector3 destination, FormationProcedureType formType, float formationDistance = 2.0f);

        /// <summary>
        /// Moves individual movers to a shared destination using a built-in formation type and spacing.
        /// </summary>
        /// <param name="movers">Mover IDs to move.</param>
        /// <param name="destination">World-space target position.</param>
        /// <param name="formType">Formation type to apply.</param>
        /// <param name="formationDistance">Spacing between movers.</param>
        void MoveToPositionInFormation(IEnumerable<RideID> movers, RideVector3 destination, FormationProcedureType formType, float formationDistance = 2.0f);

        /// <summary>
        /// Moves individual movers to a shared destination using a custom formation procedure and spacing.
        /// </summary>
        /// <param name="movers">Mover IDs to move.</param>
        /// <param name="destination">World-space target position.</param>
        /// <param name="formProc">Custom formation procedure implementation.</param>
        /// <param name="formationDistance">Spacing between movers.</param>
        void MoveToPositionInFormation(IEnumerable<RideID> movers, RideVector3 destination, IFormationProcedure formProc, float formationDistance = 2.0f);


        // -- Path Movement (individual mover) --

        /// <summary>
        /// Moves the specified mover along their currently assigned path.
        /// The path must have been previously set by a separate command.
        /// </summary>
        /// <param name="mover">The RideID of the mover to follow the assigned path.</param>
        void MovePath(RideID mover);

        /// <summary>
        /// Moves the specified mover along an array of waypoints at the given speed.
        /// </summary>
        /// <param name="mover">The RideID of the entity to move.</param>
        /// <param name="path">An array of waypoints representing the path.</param>
        /// <param name="speed">Movement speed.</param>
        void MovePath(RideID mover, IWaypoint[] path, float speed);

        /// <summary>
        /// Moves the specified mover along a custom IPath at the given speed.
        /// </summary>
        /// <param name="mover">The RideID of the entity to move.</param>
        /// <param name="path">The path object to follow.</param>
        /// <param name="speed">Movement speed.</param>
        void MovePath(RideID mover, IPath path, float speed);

        /// <summary>
        /// Moves the specified mover along a raw array of positions at the given speed.
        /// </summary>
        /// <param name="mover">The RideID of the entity to move.</param>
        /// <param name="path">Array of world-space positions forming a path.</param>
        /// <param name="speed">Movement speed.</param>
        void MovePath(RideID mover, RideVector3[] path, float speed);


        // -- Direct Locomotion --

        /// <summary>
        /// Moves the entity along a velocity vector continuously until stopped.
        /// </summary>
        /// <param name="mover">The entity to move.</param>
        /// <param name="velocity">Direction and speed as a world-space vector.</param>
        void Move(RideID mover, RideVector3 velocity);

        /// <summary>
        /// Moves the specified mover once along the given velocity vector. 
        /// The movement is applied over a single frame only, as an impulse.
        /// </summary>
        /// <param name="mover">The RideID of the mover to apply the impulse to.</param>
        /// <param name="velocity">World-space velocity vector (direction and speed).</param>
        void MoveOnce(RideID mover, RideVector3 velocity);


        // -- Teleport --

        /// <summary>
        /// Instantly places the mover at the specified destination without pathfinding or animation.
        /// Useful for teleportation or reinitialization.
        /// </summary>
        /// <param name="mover">The RideID of the entity to teleport.</param>
        /// <param name="destination">Target world-space destination position.</param>
        void MoveTeleport(RideID mover, RideVector3 destination);


        // -- Stop --

        /// <summary>
        /// Stops any active movement for the specified mover. 
        /// Does not reset paths or behaviors, just halts motion.
        /// </summary>
        /// <param name="mover">The RideID of the mover to stop.</param>
        void Stop(RideID mover);


        // ---------------------------------------------------------------------
        // Path Queries and Creation
        //
        // Provides ways to calculate, validate, and create paths
        // for use in movement commands.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Calculates a navigation path between two points in world space.
        /// </summary>
        /// <param name="start">Starting world-space position.</param>
        /// <param name="end">Ending world-space position.</param>
        /// <param name="leg">Movement leg type to use (e.g., dismounted, wheeled).</param>
        /// <returns>
        /// A valid <see cref="IPath"/> if a route exists; otherwise null if unreachable.
        /// </returns>
        IPath CalculatePath(RideVector3 start, RideVector3 end, MovementLeg leg = MovementLeg.Dismounted);

        /// <summary>
        /// Tests whether a valid path exists between two world-space positions.
        /// </summary>
        /// <param name="start">Start position in world space.</param>
        /// <param name="end">End position in world space.</param>
        /// <returns>True if a path exists; otherwise false.</returns>
        bool IsPathValid(RideVector3 start, RideVector3 end);

        /// <summary>
        /// Creates a path from a list of world-space waypoints.
        /// </summary>
        /// <param name="waypoints">List of positions in world space.</param>
        /// <returns>An <see cref="IPath"/> representing the route.</returns>
        IPath CreatePath(IEnumerable<RideVector3> waypoints);

        /// <summary>
        /// Creates a path from a list of waypoint objects.
        /// </summary>
        /// <param name="waypoints">List of waypoint interfaces.</param>
        /// <returns>An <see cref="IPath"/> object.</returns>
        IPath CreatePath(IEnumerable<IWaypoint> waypoints);


        // ---------------------------------------------------------------------
        // Position and Bounds
        //
        // Retrieves world-space positions and bounding volumes for movers.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Gets the current world-space position of the specified mover.
        /// </summary>
        /// <param name="mover">The RideID of the mover.</param>
        /// <returns>The world-space position of the mover.</returns>
        RideVector3 GetMoverPosition(RideID mover);

        /// <summary>
        /// Gets the world-space positions of multiple movers.
        /// </summary>
        /// <param name="movers">A collection of mover RideIDs.</param>
        /// <returns>An enumerable of positions matching the input order.</returns>
        IEnumerable<RideVector3> GetMoverPositions(IEnumerable<RideID> movers);

        /// <summary>
        /// Gets the world-space bounding volume (axis-aligned box) of a single mover.
        /// </summary>
        /// <param name="mover">The RideID of the mover.</param>
        /// <returns>The bounding box of the mover in world space.</returns>
        RideBounds GetMoverBounds(RideID mover);

        /// <summary>
        /// Gets the world-space bounding volumes for a collection of movers.
        /// </summary>
        /// <param name="movers">A collection of mover RideIDs.</param>
        /// <returns>An enumerable of bounding boxes matching the input order.</returns>
        IEnumerable<RideBounds> GetMoverBounds(IEnumerable<RideID> movers);


        // ---------------------------------------------------------------------
        // Movement Settings
        //
        // Sets physical or control-related parameters that affect
        // how movers navigate, steer, and are resolved by the pathing system.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Sets the mover’s pathing radius, which affects collision checks and path clearance.
        /// </summary>
        /// <param name="mover">The RideID of the mover.</param>
        /// <param name="radius">Radius in simulation units.</param>
        void SetMoverRadius(RideID mover, float radius);

        /// <summary>
        /// Sets the mover’s pathing height, typically used in vertical clearance tests.
        /// </summary>
        /// <param name="mover">The RideID of the mover.</param>
        /// <param name="height">Height in simulation units.</param>
        void SetMoverHeight(RideID mover, float height);

        /// <summary>
        /// Sets the turning speed for the mover as it steers toward destinations.
        /// A value of 0 disables turning.
        /// </summary>
        /// <param name="mover">The RideID of the mover.</param>
        /// <param name="speed">Angular turn speed (degrees per second, or similar units).</param>
        void SetMoverTurnSpeed(RideID mover, float speed);

        /// <summary>
        /// Sets the current movement speed of the mover.
        /// This may override defaults or previous values.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <param name="speed">Speed value to apply.</param>
        void SetMoverSpeed(RideID moverId, float speed);

        /// <summary>
        /// Sets the maximum movement speed the mover can be allowed to reach.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <param name="maxSpeed">Maximum speed cap.</param>
        void SetMoverMaxSpeed(RideID moverId, float maxSpeed);


        // ---------------------------------------------------------------------
        // State Queries
        //
        // Reports mover status, capabilities, or identity.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Checks whether the specified mover or group is currently in motion.
        /// If the ID represents a group, returns true only if all members are moving.
        /// </summary>
        /// <param name="mover">Mover or group RideID.</param>
        /// <returns>True if the mover(s) are moving; false otherwise.</returns>
        bool IsMoving(RideID mover);

        /// <summary>
        /// Returns a list of all known movers currently managed by the system.
        /// </summary>
        /// <returns>Enumerable of valid mover RideIDs.</returns>
        IEnumerable<RideID> GetAllMovers();

        /// <summary>
        /// Checks whether the given RideID corresponds to a mover in the system.
        /// </summary>
        /// <param name="moverId">The RideID to check.</param>
        /// <returns>True if the ID represents a mover.</returns>
        bool IsMover(RideID moverId);

        /// <summary>
        /// Checks whether the given RideID can be controlled via user input.
        /// This is typically relevant for player-controlled units.
        /// </summary>
        /// <param name="controllableId">The RideID to check.</param>
        /// <returns>True if the mover supports user input control.</returns>
        bool IsInputControllable(RideID controllableId);

        /// <summary>
        /// Returns the movement leg type (e.g., Dismounted, Wheeled, Tracked)
        /// used by the specified mover.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>The mover’s <see cref="MovementLeg"/> classification.</returns>
        MovementLeg GetMoverLeg(RideID moverId);


        // ---------------------------------------------------------------------
        // Velocity and Speed Queries
        //
        // Retrieves current or max velocity/speed-related data for movers.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Gets the current velocity vector of the specified mover in world space.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>Current world-space velocity vector.</returns>
        RideVector3 GetMoverVelocity(RideID moverId);

        /// <summary>
        /// Gets the current movement speed of the specified mover.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>Current speed value.</returns>
        float GetMoverSpeed(RideID moverId);

        /// <summary>
        /// Gets the maximum allowed movement speed of the specified mover.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>Maximum speed cap.</returns>
        float GetMoverMaxSpeed(RideID moverId);

        /// <summary>
        /// Gets the current angular rotation speed (turning speed) of the mover.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>Current angular speed (e.g., degrees per second).</returns>
        float GetMoverAngularSpeed(RideID moverId);

        /// <summary>
        /// Gets the maximum allowed angular turning speed of the mover.
        /// </summary>
        /// <param name="moverId">The RideID of the mover.</param>
        /// <returns>Maximum angular speed.</returns>
        float GetMoverMaxAngularSpeed(RideID moverId);


        // ---------------------------------------------------------------------
        // Boosters
        //
        // Provides access to optional movement boosters (e.g., sprint or dash).
        // ---------------------------------------------------------------------

        /// <summary>
        /// Enables or disables boost mode for the specified entity.
        /// </summary>
        /// <param name="boosterId">The RideID of the mover with boost capability.</param>
        /// <param name="toggle">True to enable boost; false to disable it.</param>
        void ToggleBooster(RideID boosterId, bool toggle);

        /// <summary>
        /// Gets the current boost value for the specified mover.
        /// </summary>
        /// <param name="boosterId">The RideID of the mover.</param>
        /// <returns>Current boost amount (runtime-defined units).</returns>
        float GetBoost(RideID boosterId);

        /// <summary>
        /// Gets the total available boost capacity for the specified mover.
        /// </summary>
        /// <param name="boosterId">The RideID of the mover.</param>
        /// <returns>Maximum boost capacity.</returns>
        float GetMaxBoost(RideID boosterId);

        /// <summary>
        /// Gets the amount of boost currently remaining for the mover.
        /// </summary>
        /// <param name="boosterId">The RideID of the mover.</param>
        /// <returns>Remaining boost capacity.</returns>
        float GetBoostAmount(RideID boosterId);

        /// <summary>
        /// Returns true if the specified RideID has booster functionality.
        /// </summary>
        /// <param name="boosterId">The RideID to test.</param>
        /// <returns>True if boosters are supported; otherwise false.</returns>
        bool BoosterExist(RideID boosterId);

        /// <summary>
        /// Returns true if the specified mover is currently boosting.
        /// </summary>
        /// <param name="boosterId">The RideID of the mover.</param>
        /// <returns>True if actively boosting; otherwise false.</returns>
        bool IsMoverBoosting(RideID boosterId);


        // ---------------------------------------------------------------------
        // Navigation Helper & Graph Setup
        //
        // Utility methods for locating nearby valid positions and initializing pathing data.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Finds the closest reachable position to the target, starting from a given point.
        /// If the target is unreachable, it searches iteratively closer to the start.
        /// </summary>
        /// <param name="start">Starting position for search.</param>
        /// <param name="target">Desired destination position (may be unreachable).</param>
        /// <param name="numAttempts">Number of attempts before falling back to start position.</param>
        /// <returns>A reachable world-space position, or the start if none is found.</returns>
        RideVector3 GetClosestAvailablePosition(RideVector3 start, RideVector3 target, int numAttempts = 10);

        /// <summary>
        /// Finds the closest reachable position to a given point within a search radius.
        /// </summary>
        /// <param name="position">The starting position to evaluate.</param>
        /// <param name="distance">Radius to search for a valid position.</param>
        /// <returns>A valid world-space position; returns (-∞, -∞, -∞) if none found.</returns>
        RideVector3 GetClosestAvailablePosition(RideVector3 position, float distance = 1.0f);

        /// <summary>
        /// Loads or updates the terrain graph used by A* movement systems.
        /// </summary>
        /// <param name="astarGraph">Serialized graph data.</param>
        /// <param name="penalties">Serialized penalty data.</param>
        /// <param name="terrainOffset">World offset to apply to the graph data.</param>
        void ScanTerrain(byte[] astarGraph, byte[] penalties, RideVector3 terrainOffset);
    }
}
