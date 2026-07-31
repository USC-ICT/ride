using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.Combat
{
    /// <summary>
    /// Attributes that can be used to modify how combat works
    /// </summary>
    [Flags]
    public enum AttackFlags
    {
        None = 1,
        Dead_Can_Attack = 1,
        Dead_Can_Take_Damage = 1 << 1,
    }

    [Flags]
    public enum AttackSystemFlags
    {
        None = 0,
        Acquire_Targets = 1,
        Auto_Attack = 1 << 1,
    }

    /// <summary>
    /// Manages combat interactions between agents including attacks, damage resolution,
    /// weapon-based engagement, suppression effects, and projectile visualization.
    /// Provides APIs for both immediate and simulated attacks using detailed parameters,
    /// supports AI and player-driven combat, and integrates with target acquisition and engagement systems.
    /// </summary>
    public interface IAttackSystem : IRideSystem
    {
        // -----------------------------------------------------------------------------
        // Core Attack APIs
        //
        // These methods perform immediate attacks by one agent against another agent or
        // position. Overloads support default weapons, explicit weapon selection,
        // attack metadata (like timing and aim), and dependency-injected calculators
        // for simulation-based scenarios.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Commands the attacker to perform a basic attack on the target using their primary weapon.
        /// </summary>
        /// <param name="attacker">The agent initiating the attack.</param>
        /// <param name="target">The target agent being attacked.</param>
        /// <returns>The result of the attack operation.</returns>
        IAttackResult AttackAgent(RideID attacker, RideID target);

        /// <summary>
        /// Commands the attacker to use a specific weapon to attack a target agent.
        /// </summary>
        /// <param name="attacker">The attacking agent.</param>
        /// <param name="weapon">The weapon to be used for the attack.</param>
        /// <param name="target">The agent being attacked.</param>
        /// <returns>The result of the attack.</returns>
        IAttackResult AttackAgent(RideID attacker, RideID weapon, RideID target);

        /// <summary>
        /// Executes a combat action using a provided IAttack data structure and the attacker's default weapon.
        /// </summary>
        /// <param name="attack">Attack metadata including location and timing.</param>
        /// <param name="attacker">The agent initiating the attack.</param>
        /// <param name="target">The target agent being attacked.</param>
        /// <returns>The computed result of the attack.</returns>
        IAttackResult AttackAgent(IAttack attack, RideID attacker, RideID target);

        /// <summary>
        /// Executes an attack using specified weapon and attack metadata, allowing direct control over targeting and simulation parameters.
        /// </summary>
        /// <param name="attack">Attack parameters including position and style.</param>
        /// <param name="attacker">Agent performing the attack.</param>
        /// <param name="weapon">Weapon used for the attack.</param>
        /// <param name="attackee">Agent being attacked.</param>
        /// <returns>The detailed outcome of the attack.</returns>
        IAttackResult AttackAgent(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Performs an attack with custom simulation logic, using a provided calculator to determine hit chance, range, and damage.
        /// </summary>
        /// <param name="attack">Attack parameters to apply.</param>
        /// <param name="attacker">Attacking agent.</param>
        /// <param name="target">Target agent.</param>
        /// <param name="attackCalc">Custom calculator to simulate the attack.</param>
        /// <returns>The result of the attack with simulation values applied.</returns>
        IAttackResult AttackAgent(IAttack attack, RideID attacker, RideID target, IAttackResultCalculator attackCalc);

        /// <summary>
        /// Executes a fully parameterized attack using a specific weapon and calculator to resolve the combat outcome.
        /// </summary>
        /// <param name="attack">Detailed attack input parameters.</param>
        /// <param name="attacker">Agent initiating the attack.</param>
        /// <param name="weapon">Weapon used in the attack.</param>
        /// <param name="attackee">Target agent.</param>
        /// <param name="attackCalc">Calculator used for hit, range, and damage computation.</param>
        /// <returns>The final attack result.</returns>
        IAttackResult AttackAgent(IAttack attack, RideID attacker, RideID weapon, RideID attackee, IAttackResultCalculator attackCalc);

        /// <summary>
        /// Orders an agent to attack a fixed position on the terrain using a specified weapon,
        /// regardless of whether a valid enemy is present at that location.
        /// </summary>
        /// <param name="attack">Attack data including position and style.</param>
        /// <param name="agent">The agent performing the attack.</param>
        /// <param name="weapon">Weapon used for this attack.</param>
        /// <returns>The result of the attack, including damage to any entities at the location.</returns>
        IAttackResult AttackPosition(IAttack attack, RideID agent, RideID weapon);

        /// <summary>
        /// Executes an area attack, where the agent randomly selects targets within a defined radius from the given center point.
        /// </summary>
        /// <param name="attack">Attack setup information.</param>
        /// <param name="agent">Agent launching the area attack.</param>
        /// <param name="weapon">Weapon used for the attack.</param>
        /// <param name="radius">Radius of the area to be affected.</param>
        /// <returns>Aggregated result of all valid attacks within the area.</returns>
        IAttackResult AttackArea(IAttack attack, RideID agent, RideID weapon, float radius);

        /// <summary>
        /// Continually attacks the given enemy as long as it remains within range of the agent's weapon.
        /// This method supports automatic behavior such as sentry guns or AI overwatch.
        /// </summary>
        /// <param name="attack">Attack definition and timing.</param>
        /// <param name="agent">The agent performing repeated attacks.</param>
        /// <param name="weapon">The weapon used by the agent.</param>
        /// <param name="enemy">The enemy to be attacked while in range.</param>
        /// <returns>The most recent result of the attack.</returns>
        IAttackResult AttackWhileInRange(IAttack attack, RideID agent, RideID weapon, RideID enemy);


        // -----------------------------------------------------------------------------
        // Calculation Utilities
        //
        // These methods allow computation of damage, hit accuracy, and attack results
        // without necessarily applying the attack in-game. Useful for AI decision-making,
        // UI previews, simulation testing, and deferred combat resolution.
        // They support both NPC-to-NPC calculations and player-like ray-based targeting.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Calculates the result of a ray-based attack, typically used for player-controlled
        /// shooting or direct-hit weapon systems (like lasers or bullets).
        /// </summary>
        /// <param name="attackRay">Ray representing the aim direction and origin.</param>
        /// <param name="weapon">The weapon used to perform the ray-based attack.</param>
        /// <returns>The computed result of the attack, including any hit information.</returns>
        IAttackResult CalculateRayAttack(RideRay attackRay, RideID weapon);

        /// <summary>
        /// Computes the damage that would be inflicted by this attack on the target, without
        /// applying any effects. Takes into account attacker, weapon, and target attributes.
        /// </summary>
        /// <param name="attack">Attack parameters used to influence damage.</param>
        /// <param name="attacker">Agent performing the attack.</param>
        /// <param name="weapon">Weapon used in the attack.</param>
        /// <param name="attackee">Target receiving the damage.</param>
        /// <returns>Amount of damage that would be dealt.</returns>
        float CalculateDamage(IAttack attack, RideID attacker, RideID weapon, RideID attackee);

        /// <summary>
        /// Computes the accuracy of an attack given specific parameters, representing the
        /// likelihood of a successful hit. This is useful for AI or UI predictions.
        /// </summary>
        /// <param name="attack">Attack input data such as distance, spread, etc.</param>
        /// <param name="attacker">Agent initiating the attack.</param>
        /// <param name="weapon">Weapon used by the attacker.</param>
        /// <param name="target">Target agent being evaluated for hit chance.</param>
        /// <returns>Probability or normalized value indicating likelihood of a hit.</returns>
        float CalculateAccuracy(IAttack attack, RideID attacker, RideID weapon, RideID target);

        /// <summary>
        /// Simulates an attack using the provided attack parameters and a custom calculator.
        /// Does not apply the result in-game. Useful for AI systems or pre-execution logic.
        /// </summary>
        /// <param name="attack">The input parameters for the attack, including location and flags.</param>
        /// <param name="target">The agent being evaluated as the potential target.</param>
        /// <param name="weapon">The weapon to simulate the attack with.</param>
        /// <param name="attackee">The actual recipient of the simulated attack (usually the same as target).</param>
        /// <param name="attackCalc">The calculator used to compute hit chance, damage, and other results.</param>
        /// <returns>An IAttackResult containing all relevant outcome data.</returns>
        IAttackResult CalculateAttack(IAttack attack, RideID target, RideID weapon, RideID attackee, IAttackResultCalculator attackCalc);


        // -----------------------------------------------------------------------------
        // Range Checking
        //
        // These methods determine whether a given target position or entity is within
        // range of an attacker's weapon. Useful for validating targetability before
        // triggering an attack or to inform UI/AI behavior.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Determines whether the target agent is within range of the specified weapon,
        /// based on the current world positions of both attacker and target.
        /// </summary>
        /// <param name="attacker">Agent initiating the attack; its position is used.</param>
        /// <param name="attackee">Target agent; its position is used.</param>
        /// <param name="weapon">Weapon being evaluated for range.</param>
        /// <returns>True if the target is in range, otherwise false.</returns>
        bool IsInAttackRange(RideID attacker, RideID attackee, RideID weapon);

        /// <summary>
        /// Determines whether a world-space position is within range of another position,
        /// given a specific weapon's range characteristics.
        /// </summary>
        /// <param name="start">World position of the attacker or weapon origin.</param>
        /// <param name="target">World position of the intended target.</param>
        /// <param name="weapon">Weapon used to determine range.</param>
        /// <returns>True if the target is in range, otherwise false.</returns>
        bool IsInAttackRange(RideVector3 start, RideVector3 target, RideID weapon);

        /// <summary>
        /// Determines whether a world-space position is within a given raw distance
        /// (bypassing weapon logic). Useful for debug/testing.
        /// </summary>
        /// <param name="start">Origin point of the attack.</param>
        /// <param name="target">Target point being tested.</param>
        /// <param name="range">Maximum range to test against.</param>
        /// <returns>True if the distance is within the specified range.</returns>
        bool IsInAttackRange(RideVector3 start, RideVector3 target, float range);


        // -----------------------------------------------------------------------------
        // Engagement / Target Management
        //
        // These methods manage combat relationships between agents, including engagement
        // creation, target tracking, and cleanup. Engagements are typically used by AI and
        // systems managing ongoing combat state or behaviors.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Creates an engagement record between the attacking and defending agents,
        /// allowing the system to track active combat relationships.
        /// </summary>
        /// <param name="attacker">The agent initiating the engagement.</param>
        /// <param name="attackee">The agent being engaged (target).</param>
        /// <returns>An IEngagement instance representing the relationship.</returns>
        IEngagement AddEngagement(RideID attacker, RideID attackee);

        /// <summary>
        /// Returns whether the specified agent currently has an active combat target.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <returns>True if a target is set and valid, otherwise false.</returns>
        bool HasTarget(RideID agent);

        /// <summary>
        /// Gets the agent's currently assigned combat target.
        /// </summary>
        /// <param name="agent">The agent whose target is being queried.</param>
        /// <returns>The RideID of the engaged target, or RideID.Null if none.</returns>
        RideID GetTarget(RideID agent);

        /// <summary>
        /// Removes any active target or engagement currently associated with the given agent.
        /// </summary>
        /// <param name="attacker">The agent whose target relationship should be cleared.</param>
        void RemoveTarget(RideID attacker);



        // -----------------------------------------------------------------------------
        // Trajectory, Aiming, and Projectile Launch
        //
        // These methods visualize, prepare, and execute projectile-based attacks.
        // Includes trajectory prediction, aiming indicators, and logic to fire or throw
        // projectiles using configured ballistic or physics-based models.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Draws a predicted trajectory line from the attacker's weapon to the target position.
        /// Used for visual feedback or AI aiming indication.
        /// </summary>
        /// <param name="attacker">Agent initiating the aim operation.</param>
        /// <param name="target">World-space position to aim at.</param>
        /// <param name="weapon">Weapon used to compute the trajectory.</param>
        void AimAtTarget(RideID attacker, RideVector3 target, RideID weapon);

        /// <summary>
        /// Draws a trajectory arc for a throwable object aimed at a target position.
        /// Used for visual preview of grenade-like objects.
        /// </summary>
        /// <param name="attacker">Agent initiating the throw aim.</param>
        /// <param name="target">Target location to throw toward.</param>
        /// <param name="throwableId">ID of the throwable object.</param>
        void AimThrowAtTarget(RideID attacker, RideVector3 target, RideID throwableId);

        /// <summary>
        /// Clears any visual aiming trajectory line currently drawn for the specified agent.
        /// </summary>
        /// <param name="attacker">Agent whose aim visualization should be removed.</param>
        void StopAimingAtTarget(RideID attacker);

        /// <summary>
        /// Instantiates and launches a projectile toward the target using the current
        /// aiming solution for the given weapon.
        /// </summary>
        /// <param name="attacker">Agent performing the projectile fire.</param>
        /// <param name="target">Target position to fire at.</param>
        /// <param name="weapon">Weapon used to fire the projectile.</param>
        void FireProjectile(RideID attacker, RideVector3 target, RideID weapon);

        /// <summary>
        /// Instantiates and throws a throwable object toward the target position.
        /// </summary>
        /// <param name="thrower">Agent performing the throw.</param>
        /// <param name="target">World-space position to throw at.</param>
        /// <param name="throwable">The throwable item to be launched.</param>
        /// <returns>True if the throw was successfully initiated, false otherwise.</returns>
        bool ThrowProjectile(RideID thrower, RideVector3 target, RideID throwable);

        /// <summary>
        /// Rotates the firing point (e.g., gun barrel or turret) to align with the trajectory
        /// needed to hit the target with a projectile. Supports both flat and high-arc aiming.
        /// </summary>
        /// <param name="weaponPoint">Transform for the rotating weapon element.</param>
        /// <param name="firePoint">Transform representing the projectile origin point.</param>
        /// <param name="attack">Attack parameters describing the target location.</param>
        /// <param name="projectileSpeed">Speed of the projectile in meters per second.</param>
        /// <param name="isArtillery">Whether to use high-arc (artillery-style) aiming.</param>
        /// <returns>True if the rotation succeeded and the weapon aimed correctly.</returns>
        bool RotateGun(ITransform weaponPoint, ITransform firePoint, IAttack attack, float projectileSpeed, bool isArtillery);


        // -----------------------------------------------------------------------------
        // Hit Box & Health Utilities
        //
        // These methods manage entity-level health and hit box interactions.
        // They are useful for querying and modifying health values during attacks,
        // and for calculating weapon effectiveness against armor.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Checks whether the specified entity has a registered hit box that can receive damage.
        /// </summary>
        /// <param name="entityId">The ID of the entity being checked.</param>
        /// <returns>True if the entity has a hit box, otherwise false.</returns>
        bool HasEntityHitBox(RideID entityId);

        /// <summary>
        /// Returns the current health value of the entity's hit box.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <returns>The current hit box health value.</returns>
        float GetEntityHitBoxHealth(RideID entityId);

        /// <summary>
        /// Returns the maximum health value of the entity's hit box.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <returns>The maximum possible hit box health.</returns>
        float GetEntityHitBoxMaxHealth(RideID entityId);

        /// <summary>
        /// Modifies the entity's hit box health by adding or subtracting the given value.
        /// </summary>
        /// <param name="entityId">The entity to modify.</param>
        /// <param name="mod">The value to add to current health (can be negative).</param>
        void ModifyEntityHitHealth(RideID entityId, float mod);

        /// <summary>
        /// Sets the hit box health of the entity to a specific value.
        /// </summary>
        /// <param name="entityId">The entity whose health to modify.</param>
        /// <param name="health">The new health value to assign.</param>
        void SetEntityHitHealth(RideID entityId, float health);

        /// <summary>
        /// Returns a modifier that scales weapon damage based on the target's armor properties.
        /// </summary>
        /// <param name="entityArmor">The armor RideID of the target entity.</param>
        /// <param name="weaponId">The weapon being used to attack.</param>
        /// <returns>A multiplier applied to incoming damage (e.g., 0.5 for 50% reduction).</returns>
        float GetArmorDamageModifier(RideID entityArmor, RideID weaponId);


        // -----------------------------------------------------------------------------
        // System Configuration
        //
        // These members allow configuration of core systems used by the attack logic,
        // including setting global behavior flags, injecting external calculation logic,
        // and assigning companion systems like engagement or targeting managers.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Global flags that control attack system behavior, such as auto-attack
        /// and automatic target acquisition. Use bitwise OR to combine values.
        /// </summary>
        AttackSystemFlags flags { get; set; }

        /// <summary>
        /// Assigns a custom calculator to use for computing range, hit, and damage
        /// across all applicable attacks.
        /// </summary>
        /// <param name="calc">The calculator instance to use for all attack calculations.</param>
        void SetAttackResultCalculator(IAttackResultCalculator calc);

        /// <summary>
        /// Assigns the engagement system responsible for managing combat relationships
        /// between agents (e.g., tracking who is attacking whom).
        /// </summary>
        /// <param name="engagementSystem">The system to use for engagements.</param>
        void SetEngagementSystem(IEngagementSystem engagementSystem);

        /// <summary>
        /// Assigns the target acquisition system used to determine what agents
        /// should target during autonomous or guided attacks.
        /// </summary>
        /// <param name="targetAcquisitionSystem">The system used for selecting targets.</param>
        void SetTargetAcquisitionSystem(ITargetAcquisitionSystem targetAcquisitionSystem);


        // -----------------------------------------------------------------------------
        // Effects and Raycasting
        //
        // Provides utility functions for combat visualization and low-level raycasting.
        // Used to display effects, simulate impact hits, and calculate suppression
        // based on where rounds land relative to agents.
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Performs a physics raycast based on the attack ray and weapon definition,
        /// optionally using a layer mask to filter collisions. Typically used to
        /// determine where a projectile would hit.
        /// </summary>
        /// <param name="attackRay">The ray that defines the origin and direction of the attack.</param>
        /// <param name="weapon">The weapon used to determine hit behavior and properties.</param>
        /// <param name="mask">A layer mask to restrict what the ray can hit.</param>
        /// <returns>Information about the first object hit by the ray, or an empty hit if nothing was hit.</returns>
        RideRaycastHit GetHitData(RideRay attackRay, RideID weapon, RideLayerMask mask);

        /// <summary>
        /// Calculates the suppression effect on a target agent based on the position of
        /// a projectile impact. Can be used to simulate psychological or tactical effects
        /// without applying direct damage.
        /// </summary>
        /// <param name="targetPos">The position of the agent being evaluated for suppression.</param>
        /// <param name="roundPos">The location where the projectile or round landed.</param>
        /// <param name="weaponId">The weapon used in the attack (used to scale suppression).</param>
        /// <returns>A suppression value from 0.0 (no effect) to 1.0 (max suppression).</returns>
        float GetSuppressionEffect(RideVector3 targetPos, RideVector3 roundPos, RideID weaponId);

        /// <summary>
        /// Spawns a visual or audio effect at a given position in the world. Can be
        /// delayed and parented to another object for continuous or location-based effects.
        /// </summary>
        /// <param name="fxName">The name of the effect (e.g., explosion, tracer).</param>
        /// <param name="position">World position to spawn the effect.</param>
        /// <param name="duration">Optional duration before the effect is removed. Use -1 for persistent effects.</param>
        /// <param name="delay">Optional delay before the effect is shown.</param>
        /// <param name="parentedObject">Optional parent object for spatial attachment.</param>
        /// <returns>True if the effect was successfully spawned, false otherwise.</returns>
        bool SpawnFX(string fxName, RideVector3 position, float duration = -1.0f, float delay = 0.0f, object parentedObject = null);
    }
}
