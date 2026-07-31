using System.Collections.Generic;
using Ride.AI;
using Ride.Movement;

namespace Ride.Entities
{
    /// <summary>
    /// all of the functions for IAgents to do things in the world or to effect the data of the agent
    /// </summary>
    public interface IAgentSystem : IRideSystem, IUnitCreatorSystem
    {
        #region Agent Identity and Access

        // ------------------------------------------------------------
        // Agent Identity and Access
        // ------------------------------------------------------------

        /// <summary>
        /// Returns the internal IAgent interface for the given RideID.
        /// </summary>
        /// <param name="agentId">The RideID of the agent to retrieve.</param>
        /// <returns>The IAgent instance representing the agent.</returns>
        IAgent GetIAgent(RideID agentId);

        /// <summary>
        /// Checks whether an agent exists for the given RideID.
        /// </summary>
        /// <param name="agentId">The agent ID to verify.</param>
        /// <returns>True if the agent exists; otherwise, false.</returns>
        bool AgentExists(RideID agentId);

        /// <summary>
        /// Returns all agents currently active in the simulation.
        /// </summary>
        /// <returns>An enumerable of RideIDs representing all agents.</returns>
        IEnumerable<RideID> GetAllAgents();

        /// <summary>
        /// Checks whether the given entity is an agent.
        /// </summary>
        /// <param name="entity">The RideID to check.</param>
        /// <returns>True if the entity is an agent; otherwise, false.</returns>
        bool IsAgent(RideID entity);

        #endregion


        #region Agent Position and Orientation

        // ------------------------------------------------------------
        // Agent Position and Orientation
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the world position of the agent.
        /// </summary>
        /// <param name="agentId">The agent whose position to retrieve.</param>
        /// <returns>The world position of the agent.</returns>
        RideVector3 GetAgentPosition(RideID agentId);

        /// <summary>
        /// Sets the world position of the agent.
        /// </summary>
        /// <param name="agentId">The agent to reposition.</param>
        /// <param name="position">The new world position.</param>
        void SetAgentPosition(RideID agentId, RideVector3 position);

        /// <summary>
        /// Gets the world-space view position of the agent (e.g. eye or head location).
        /// </summary>
        /// <param name="agentId">The agent whose view position to retrieve.</param>
        /// <returns>The view-space position of the agent.</returns>
        RideVector3 GetAgentViewPosition(RideID agentId);

        /// <summary>
        /// Gets the world-space rotation (orientation) of the agent.
        /// </summary>
        /// <param name="agentId">The agent whose rotation to retrieve.</param>
        /// <returns>The rotation as a RideQuaternion.</returns>
        RideQuaternion GetAgentRotation(RideID agentId);

        /// <summary>
        /// Sets the world-space rotation (orientation) of the agent.
        /// </summary>
        /// <param name="agentId">The agent to rotate.</param>
        /// <param name="rotation">The desired world rotation.</param>
        void SetAgentRotation(RideID agentId, RideQuaternion rotation);

        /// <summary>
        /// Rotates the agent to look at a world-space position.
        /// </summary>
        /// <param name="agentId">The agent to orient.</param>
        /// <param name="worldPosition">The position to look at.</param>
        void SetAgentLookAt(RideID agentId, RideVector3 worldPosition);

        #endregion

        #region Agent Health and Suppression

        // ------------------------------------------------------------
        // Agent Health and Suppression
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the agent's current health value.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The current health value.</returns>
        float GetAgentHealth(RideID agentId);

        /// <summary>
        /// Sets the agent's health to an exact value.
        /// </summary>
        /// <param name="agentId">The agent to modify.</param>
        /// <param name="health">The new health value.</param>
        void SetAgentHealth(RideID agentId, float health);

        /// <summary>
        /// Sets the agent's maximum possible health.
        /// </summary>
        /// <param name="agentId">The agent to update.</param>
        /// <param name="maxHealth">The new maximum health.</param>
        void SetAgentMaxHealth(RideID agentId, float maxHealth);

        /// <summary>
        /// Modifies the agent's health by the specified delta (positive or negative).
        /// </summary>
        /// <param name="agentId">The agent whose health is being modified.</param>
        /// <param name="modification">The value to add to the agent's health.</param>
        void ModifyAgentHealth(RideID agentId, float modification);

        /// <summary>
        /// Fully restores the agent's health to the maximum value.
        /// </summary>
        /// <param name="agentId">The agent to heal.</param>
        void SetAgentHealthFull(RideID agentId);

        /// <summary>
        /// Checks whether the agent is dead.
        /// </summary>
        /// <param name="agentId">The agent to check.</param>
        /// <returns>True if the agent is dead; otherwise, false.</returns>
        bool IsAgentDead(RideID agentId);

        /// <summary>
        /// Shows or hides the agent's health bar.
        /// </summary>
        /// <param name="agentId">The agent whose health bar to modify.</param>
        /// <param name="visible">True to show, false to hide.</param>
        void SetHealthBarVisible(RideID agentId, bool visible);

        /// <summary>
        /// Enables or disables the agent's health bar entirely.
        /// </summary>
        /// <param name="agentId">The agent whose health bar to control.</param>
        /// <param name="enable">True to enable; false to disable.</param>
        void SetHealthBarEnable(RideID agentId, bool enable);

        /// <summary>
        /// Shows or hides health bars for multiple agents.
        /// </summary>
        /// <param name="agentIds">The agents to update.</param>
        /// <param name="visible">True to show; false to hide.</param>
        void SetHealthBarsVisible(IEnumerable<RideID> agentIds, bool visible);

        /// <summary>
        /// Gets the agent's current suppression value (0 = none, 1 = fully suppressed).
        /// </summary>
        /// <param name="agentId">The agent to check.</param>
        /// <returns>Suppression level between 0.0 and 1.0.</returns>
        float GetAgentSuppression(RideID agentId);

        /// <summary>
        /// Gets the rate at which the agent's suppression reduces (per second).
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The suppression recovery rate.</returns>
        float GetAgentSuppressionReductionRate(RideID agentId);

        /// <summary>
        /// Sets the rate at which the agent's suppression reduces (per second).
        /// </summary>
        /// <param name="agentId">The agent to update.</param>
        /// <param name="suppressionReductionRate">The suppression recovery rate.</param>
        void SetAgentSuppressionReductionRate(RideID agentId, float suppressionReductionRate);

        /// <summary>
        /// Adds suppression to the agent manually.
        /// </summary>
        /// <param name="agentId">The agent to suppress.</param>
        /// <param name="suppression">The amount of suppression to add.</param>
        void SuppressAgent(RideID agentId, float suppression);

        /// <summary>
        /// Adds suppression to the agent based on the properties of a weapon.
        /// </summary>
        /// <param name="agentId">The agent to suppress.</param>
        /// <param name="weaponId">The weapon whose suppression value to apply.</param>
        void SuppressAgent(RideID agentId, RideID weaponId);

        #endregion

        #region Agent Weapons and Equipment

        // ------------------------------------------------------------
        // Agent Weapons and Equipment
        // ------------------------------------------------------------

        /// <summary>
        /// Adds a logical (non-visual) weapon to the agent using an IWeapon instance.
        /// </summary>
        /// <param name="agentId">The agent receiving the weapon.</param>
        /// <param name="weapon">The logical weapon to add.</param>
        /// <param name="priority">
        /// The weapon's priority for the agent. 0 = highest priority.
        /// Use -1 if priority is not important.
        /// </param>
        /// <returns>The RideID of the added weapon.</returns>
        RideID AddWeapon(RideID agentId, IWeapon weapon, int priority = -1);

        /// <summary>
        /// Adds a logical (non-visual) weapon to the agent using a concrete Weapon instance.
        /// </summary>
        /// <param name="agentId">The agent receiving the weapon.</param>
        /// <param name="weapon">The logical weapon to add.</param>
        /// <param name="priority">
        /// The weapon's priority for the agent. 0 = highest priority.
        /// Use -1 if priority is not important.
        /// </param>
        /// <returns>The RideID of the added weapon.</returns>
        RideID AddWeapon(RideID agentId, Weapon weapon, int priority = -1);

        /// <summary>
        /// Returns the highest priority weapon currently assigned to the agent.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The RideID of the primary weapon.</returns>
        RideID GetPrimaryWeapon(RideID agentId);

        /// <summary>
        /// Returns all weapons currently associated with the agent.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>A list of RideIDs representing each weapon.</returns>
        IList<RideID> GetWeapons(RideID agentId);

        /// <summary>
        /// Equips a visible item on the agent using a predefined item type.
        /// </summary>
        /// <param name="agentId">The agent receiving the item.</param>
        /// <param name="type">The type of item to equip.</param>
        /// <param name="priority">
        /// If the item is a weapon, sets its priority.
        /// 0 = highest priority, -1 = no preference.
        /// </param>
        /// <returns>The RideID of the equipped item.</returns>
        RideID EquipItem(RideID agentId, ItemType type, int priority = -1);

        /// <summary>
        /// Returns true if the specified weapon is out of ammo for the given agent.
        /// </summary>
        /// <param name="agentId">The agent to check.</param>
        /// <param name="weaponId">The weapon to check.</param>
        /// <returns>True if the weapon is out of ammo; otherwise, false.</returns>
        bool IsOutOfAmmo(RideID agentId, RideID weaponId);

        #endregion

        #region Agent State Machine

        // ------------------------------------------------------------
        // Agent State Machine
        // ------------------------------------------------------------

        /// <summary>
        /// Initializes the agent's state machine using its default configuration.
        /// Typically used after agent creation.
        /// ref: StateMachineUtils.cs
        /// </summary>
        /// <param name="agentId">The agent to initialize.</param>
        void InitializeAgentStateMachine(RideID agentId);

        /// <summary>
        /// Sets the agent's current state by directly assigning a state name.
        /// </summary>
        /// <param name="agentId">The agent whose state to set.</param>
        /// <param name="state">The name of the state to assign.</param>
        void SetAgentState(RideID agentId, string state);

        /// <summary>
        /// Gets the agent's current state by name.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The name of the agent's current state.</returns>
        string GetAgentState(RideID agentId);

        /// <summary>
        /// Fires a trigger on the agent's state machine. If valid for the current state,
        /// the trigger may cause a state transition.
        /// </summary>
        /// <param name="agentId">The agent whose state machine to trigger.</param>
        /// <param name="trigger">The numeric trigger ID.</param>
        void FireAgentStateMachineTrigger(RideID agentId, int trigger);

        /// <summary>
        /// Gets a reference to the agent's underlying state machine.
        /// </summary>
        /// <param name="agentId">The agent whose state machine to retrieve.</param>
        /// <returns>The state machine instance controlling this agent.</returns>
        IStateMachine<string, int, string> GetAgentStateMachine(RideID agentId);

        /// <summary>
        /// Replaces the agent's state machine with a new one.
        /// Use with caution - this overrides internal behavior logic.
        /// </summary>
        /// <param name="agentID">The agent whose state machine to replace.</param>
        /// <param name="sm">The new state machine instance.</param>
        void ReplaceAgentStateMachine(RideID agentID, IStateMachine<string, int, string> sm);

        #endregion

        #region Agent Behavior System

        // ------------------------------------------------------------
        // Agent Behavior System
        // ------------------------------------------------------------

        /// <summary>
        /// Assigns a single behavior to the agent, clearing any existing behaviors.
        /// </summary>
        /// <param name="agent">The agent to control.</param>
        /// <param name="behaviour">The behavior to assign.</param>
        /// <returns>The RideID of the assigned behavior.</returns>
        RideID SetAgentBehaviour(RideID agent, IRideBehavior behaviour);

        /// <summary>
        /// Adds a behavior to the agent without removing existing ones.
        /// </summary>
        /// <param name="agent">The agent to modify.</param>
        /// <param name="behaviour">The behavior to add.</param>
        /// <returns>The RideID of the added behavior.</returns>
        RideID AddAgentBehaviour(RideID agent, IRideBehavior behaviour);

        /// <summary>
        /// Removes a specific behavior from the agent.
        /// </summary>
        /// <param name="agent">The agent to modify.</param>
        /// <param name="behaviour">The behavior to remove.</param>
        void RemoveAgentBehaviour(RideID agent, RideID behaviour);

        /// <summary>
        /// Removes all behaviors from the agent.
        /// </summary>
        /// <param name="agent">The agent to clear.</param>
        void ClearAgentBehaviours(RideID agent);

        /// <summary>
        /// Starts execution of the specified behavior on the agent.
        /// </summary>
        /// <param name="agent">The agent to control.</param>
        /// <param name="behaviour">The behavior to start.</param>
        void StartAgentBehaviour(RideID agent, RideID behaviour);

        /// <summary>
        /// Starts all assigned behaviors on the agent.
        /// </summary>
        /// <param name="agent">The agent to control.</param>
        void StartAgentBehaviours(RideID agent);

        /// <summary>
        /// Stops a specific behavior on the agent.
        /// </summary>
        /// <param name="agent">The agent to control.</param>
        /// <param name="behaviour">The behavior to stop.</param>
        void StopAgentBehaviour(RideID agent, RideID behaviour);

        /// <summary>
        /// Stops all behaviors currently active on the agent.
        /// </summary>
        /// <param name="agent">The agent to stop.</param>
        void StopAgentBehaviours(RideID agent);

        /// <summary>
        /// Gets a reference to a specific behavior currently assigned to the agent.
        /// </summary>
        /// <param name="agent">The agent using the behavior.</param>
        /// <param name="behaviour">The RideID of the behavior.</param>
        /// <returns>The behavior object, or null if not found.</returns>
        IRideBehavior GetAgentBehaviour(RideID agent, RideID behaviour);

        /// <summary>
        /// Gets all behaviors currently assigned to the agent.
        /// </summary>
        /// <param name="agent">The agent to query.</param>
        /// <returns>All IRideBehavior instances assigned to the agent.</returns>
        IEnumerable<IRideBehavior> GetAgentBehaviours(RideID agent);

        /// <summary>
        /// Assigns a behavior to all members of a group, replacing existing behaviors.
        /// </summary>
        /// <param name="group">The group of agents.</param>
        /// <param name="behaviour">The behavior to assign.</param>
        /// <returns>The RideID of the assigned behavior.</returns>
        RideID SetAgentGroupBehaviour(RideID group, IRideBehavior behaviour);

        /// <summary>
        /// Adds a behavior to all members of a group, preserving existing behaviors.
        /// </summary>
        /// <param name="group">The group of agents.</param>
        /// <param name="behaviour">The behavior to add.</param>
        /// <returns>The RideID of the added behavior.</returns>
        RideID AddAgentGroupBehaviour(RideID group, IRideBehavior behaviour);

        /// <summary>
        /// Removes a specific behavior from all members of a group.
        /// </summary>
        /// <param name="group">The group of agents.</param>
        /// <param name="behaviour">The behavior to remove.</param>
        void RemoveAgentGroupBehaviour(RideID group, RideID behaviour);

        /// <summary>
        /// Removes all behaviors from all members of a group.
        /// </summary>
        /// <param name="group">The group of agents.</param>
        void ClearAgentGroupBehaviours(RideID group);

        /// <summary>
        /// Stops all behaviors on all agents in the group.
        /// </summary>
        /// <param name="group">The group of agents to stop.</param>
        void StopAgentGroupBehaviours(RideID group);

        /// <summary>
        /// Gets a specific behavior from a group of agents by behavior ID.
        /// </summary>
        /// <param name="group">The group to query.</param>
        /// <param name="behaviour">The behavior to retrieve.</param>
        /// <returns>The behavior instance if found; otherwise null.</returns>
        IRideBehavior GetAgentGroupBehaviour(RideID group, RideID behaviour);

        #endregion

        #region Agent Selection and Group Queries

        // ------------------------------------------------------------
        // Agent Selection and Group Queries
        // ------------------------------------------------------------

        /// <summary>
        /// Sets whether an individual agent is selected.
        /// </summary>
        /// <param name="agentId">The agent to update.</param>
        /// <param name="isSelected">True to select; false to deselect.</param>
        void SetAgentSelected(RideID agentId, bool isSelected);

        /// <summary>
        /// Sets the selection state for multiple agents at once.
        /// </summary>
        /// <param name="agentIds">The agents to update.</param>
        /// <param name="isSelected">True to select; false to deselect.</param>
        void SetAgentsSelected(IEnumerable<RideID> agentIds, bool isSelected);

        /// <summary>
        /// Gets all agents currently marked as selected.
        /// </summary>
        /// <returns>An array of RideIDs representing selected agents.</returns>
        RideID[] GetSelectedAgents();

        /// <summary>
        /// Returns a list of agents within a given radius of a world position.
        /// </summary>
        /// <param name="position">The center position to search from.</param>
        /// <param name="radius">The search radius in world units.</param>
        /// <returns>A list of RideIDs within the specified radius.</returns>
        IEnumerable<RideID> GetAgentsNearPosition(RideVector3 position, float radius = 1.0f);

        #endregion

        #region Agent Skills, Posture, and Speed

        // ------------------------------------------------------------
        // Agent Skills, Posture, and Speed
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the agent's current skill level (0.0 = untrained, 1.0 = expert).
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The skill level value between 0.0 and 1.0.</returns>
        float GetAgentSkill(RideID agentId);

        /// <summary>
        /// Sets the agent's skill level.
        /// </summary>
        /// <param name="agentId">The agent to update.</param>
        /// <param name="skillLevel">The new skill level (0.0 to 1.0).</param>
        void SetAgentSkill(RideID agentId, float skillLevel);

        /// <summary>
        /// Sets the posture of the agent (standing, crouching, prone, etc.).
        /// </summary>
        /// <param name="agentId">The agent to modify.</param>
        /// <param name="posture">The desired posture.</param>
        void SetAgentPosture(RideID agentId, AgentPosture posture);

        /// <summary>
        /// Gets the current posture of the agent.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The agent's posture.</returns>
        AgentPosture GetAgentPosture(RideID agentId);

        /// <summary>
        /// Sets the movement speed values for the agent.
        /// </summary>
        /// <param name="agent">The agent to update.</param>
        /// <param name="targetSpeed">The desired average speed to move at.</param>
        /// <param name="maxSpeed">The maximum allowed speed.</param>
        void SetAgentSpeed(RideID agent, float targetSpeed, float maxSpeed);

        /// <summary>
        /// Gets the agent's configured movement speed.
        /// </summary>
        /// <param name="agent">The agent to query.</param>
        /// <returns>The agent's target movement speed.</returns>
        float GetAgentSpeed(RideID agent);

        /// <summary>
        /// Gets the agent's current actual speed (based on velocity).
        /// </summary>
        /// <param name="agent">The agent to query.</param>
        /// <returns>The agent's current movement speed.</returns>
        float GetAgentCurrentSpeed(RideID agent);

        /// <summary>
        /// Gets the agent's configured maximum speed.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The maximum speed the agent may move at.</returns>
        float GetAgentMaxSpeed(RideID agentId);

        /// <summary>
        /// Gets the agent's facing direction in world space.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The agent's forward direction as a vector.</returns>
        RideVector3 GetAgentForwardDirection(RideID agentId);

        /// <summary>
        /// Gets the agent's current velocity relative to their local coordinate system.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The local velocity vector.</returns>
        RideVector3 GetAgentLocalVelocity(RideID agentId);

        /// <summary>
        /// Gets the current rotation speed of the agent (e.g. for turn rate analysis).
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>Rotation speed in degrees per second.</returns>
        float GetAgentRotationSpeed(RideID agentId);

        /// <summary>
        /// Gets the agent's visual range, typically used in targeting logic.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The visual detection range of the agent.</returns>
        float GetAgentRange(RideID agentId);

        /// <summary>
        /// Sets the agent's visual range, used in auto-targeting and engagement logic.
        /// </summary>
        /// <param name="agentId">The agent to modify.</param>
        /// <param name="range">The new visual range.</param>
        void SetAgentRange(RideID agentId, float range);

        /// <summary>
        /// Applies a pathing behavior override to the agent.
        /// </summary>
        /// <param name="agentId">The agent to configure.</param>
        /// <param name="pathingBehaviour">The pathing behavior to apply.</param>
        void SetPathingBehaviour(RideID agentId, PathingBehaviour pathingBehaviour);

        #endregion

        #region Agent Spawning and Removal

        // ------------------------------------------------------------
        // Agent Spawning and Removal
        // ------------------------------------------------------------

        /// <summary>
        /// Adds a new agent using preconfigured Unit data.
        /// </summary>
        /// <param name="unit">The unit data describing the agent to create.</param>
        /// <returns>The RideID of the newly created agent.</returns>
        RideID AddAgent(Unit unit);

        /// <summary>
        /// Adds a new agent by duplicating a scene object.
        /// </summary>
        /// <param name="sceneObjectName">The name of the scene object to clone.</param>
        /// <param name="position">The world position to place the new agent.</param>
        /// <param name="rotation">The world rotation to apply to the agent.</param>
        /// <returns>The RideID of the cloned agent.</returns>
        RideID AddAgentFromScene(string sceneObjectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Adds an agent using an existing IAgent instance.
        /// </summary>
        /// <param name="agent">The agent object to register.</param>
        /// <returns>The RideID of the registered agent.</returns>
        RideID AddAgentExisting(IAgent agent);

        /// <summary>
        /// Removes an agent from the simulation.
        /// </summary>
        /// <param name="agent">The agent to remove.</param>
        void RemoveAgent(RideID agent);

        /// <summary>
        /// Restores a previously incapacitated agent to full functionality.
        /// </summary>
        /// <param name="agentId">The agent to revive.</param>
        void ReviveAgent(RideID agentId);

        /// <summary>
        /// Stops any active movement coroutines on the agent.
        /// </summary>
        /// <param name="agentId">The agent to stop.</param>
        void StopAgentMovingCoroutine(RideID agentId);

        #endregion

        #region Agent Metadata and Attributes

        // ------------------------------------------------------------
        // Agent Metadata and Attributes
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the name assigned to the agent.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The agent's name.</returns>
        string GetAgentName(RideID agentId);

        /// <summary>
        /// Sets the agent's name.
        /// </summary>
        /// <param name="agent">The agent to modify.</param>
        /// <param name="name">The name to assign.</param>
        void SetAgentName(RideID agent, string name);

        /// <summary>
        /// Sets the internal Unit data associated with the agent.
        /// </summary>
        /// <param name="agent">The agent to modify.</param>
        /// <param name="data">The Unit data to assign.</param>
        void SetAgentData(RideID agent, Unit data);

        /// <summary>
        /// Attaches a custom object reference to the agent. Use with caution - no serialization.
        /// </summary>
        /// <param name="agentId">The agent to attach to.</param>
        /// <param name="obj">The object to associate with the agent.</param>
        void AttachObjectToAgent(RideID agentId, object obj);

        /// <summary>
        /// Returns a JSON string representing the agent's Unit data.
        /// </summary>
        /// <param name="agentId">The agent to serialize.</param>
        /// <returns>JSON string describing the agent's data.</returns>
        string GetAgentJSON(RideID agentId);

        /// <summary>
        /// Returns the team the agent is assigned to.
        /// </summary>
        /// <param name="agentId">The agent to query.</param>
        /// <returns>The team enum value.</returns>
        Team GetAgentTeam(RideID agentId);

        /// <summary>
        /// Returns the current EntityStatus flags assigned to the agent.
        /// </summary>
        /// <param name="agent">The agent to query.</param>
        /// <returns>The current status flags.</returns>
        EntityStatus GetAgentStatus(RideID agent);

        /// <summary>
        /// Sets the full EntityStatus flag set for the agent.
        /// </summary>
        /// <param name="agent">The agent to update.</param>
        /// <param name="stat">The new status value.</param>
        void SetAgentStatus(RideID agent, EntityStatus stat);

        /// <summary>
        /// Returns true if the agent has the given status flag.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <param name="stat">The status flag to verify.</param>
        /// <returns>True if the flag is set; otherwise, false.</returns>
        bool HasStatus(RideID agent, EntityStatus stat);

        /// <summary>
        /// Returns all EntityAttributes currently assigned to the agent.
        /// </summary>
        /// <param name="agent">The agent to query.</param>
        /// <returns>The attribute flags.</returns>
        EntityAttributes GetAgentAttributes(RideID agent);

        /// <summary>
        /// Returns true if the agent has all of the specified attributes.
        /// </summary>
        /// <param name="entity">The agent to check.</param>
        /// <param name="att">The attributes to verify.</param>
        /// <returns>True if all specified attributes are set.</returns>
        bool HasAttributes(RideID entity, EntityAttributes att);

        /// <summary>
        /// Overwrites all current attributes with the provided set.
        /// </summary>
        /// <param name="entity">The agent to update.</param>
        /// <param name="att">The new attribute flags.</param>
        void SetAttributes(RideID entity, EntityAttributes att);

        /// <summary>
        /// Adds one or more attribute flags to the agent. Uses bitwise OR.
        /// </summary>
        /// <param name="entity">The agent to update.</param>
        /// <param name="att">The attributes to add.</param>
        void AddAttributes(RideID entity, EntityAttributes att);

        /// <summary>
        /// Removes one or more attribute flags from the agent. Uses bitwise AND NOT.
        /// </summary>
        /// <param name="entity">The agent to update.</param>
        /// <param name="att">The attributes to remove.</param>
        void RemoveAttributes(RideID entity, EntityAttributes att);

        #endregion
    }
}
