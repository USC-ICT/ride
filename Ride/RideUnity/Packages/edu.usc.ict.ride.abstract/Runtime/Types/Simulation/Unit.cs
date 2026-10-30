using System;
using Ride.Entities;
using Ride.Movement;

namespace Ride
{
    /// <summary>Represents the team affiliation of a unit.</summary>
    public enum Team
    {
        /// <summary>Blue or friendly team.</summary>
        Blue,

        /// <summary>Red or opposing team.</summary>
        Red,

        /// <summary>Civilian or noncombatant team.</summary>
        Civilian
    }

    /// <summary>Describes a serializable simulated unit and its initial state.</summary>
    /// <remarks>
    /// <see cref="Ride.Scenario.IScenarioSystem"/> and <see cref="IAgentSystem"/> use this model when creating
    /// agents from scenario definitions. It contains engine-independent identity, transform,
    /// team, health, equipment, symbology, and movement configuration data.
    /// </remarks>
    [Serializable]
    public class Unit : IIdentity
    {
        /// <inheritdoc/>
        public RideID id { get; set; }

        /// <summary>Display name of the unit, which usually matches its prefab name.</summary>
        public string name { get; set; }

        /// <summary>Prefab used to instantiate the unit in Unity.</summary>
        public string prefab;

        /// <summary>Optional unit classification identifier that is not interpreted by core systems.</summary>
        public int type;

        /// <summary>Initial world position of the unit.</summary>
        public RideVector3 pos;

        /// <summary>Initial world rotation of the unit.</summary>
        public RideQuaternion rot = RideQuaternion.identity;

        /// <summary>Team affiliation of the unit.</summary>
        public Team team;

        /// <summary>Load-state bin, where 13 is the default and 14 represents fully loaded.</summary>
        public int bin = 13;

        /// <summary>Current health of the unit.</summary>
        public float health = 100;

        /// <summary>Maximum health of the unit.</summary>
        public float maxHealth = 100;

        /// <summary>Current stamina of the unit.</summary>
        public float stamina = 100;

        /// <summary>Maximum stamina of the unit.</summary>
        public float maxStamina = 100;

        /// <summary>Vertical jump speed or strength of the unit.</summary>
        public float jumpSpeed = 5;

        /// <summary>Base walking or running speed of the unit.</summary>
        public float defaultSpeed = 5;

        /// <summary>Movement multiplier applied while the unit is boosted or sprinting.</summary>
        public float speedBoostMultiplier = 2;

        /// <summary>Movement multiplier applied while the unit is slowed.</summary>
        public float speedSlowMultiplier = 0.5f;

        /// <summary>Rate at which suppression is reduced.</summary>
        public float suppressionReductionRate = 0.1f;

        /// <summary>Normalized skill rating used for AI decision-making.</summary>
        public float skillLevel = 1.0f;

        /// <summary>MIL-STD-2525 symbol identification code used to display the unit.</summary>
        public string sidc = string.Empty;

        /// <summary>Items assigned to the unit when it is spawned.</summary>
        public ItemType[] items;

        /// <summary>Movement configuration assigned to the unit.</summary>
        public Movement.Movement moveBehaviour;

        /// <summary>Attribute flags describing the unit's simulation capabilities.</summary>
        public EntityAttributes attributes = EntityAttributes.agent;


        /// <summary>Initializes an empty unit for serialization or manual setup.</summary>
        public Unit() { }

        /// <summary>
        /// Constructs a new <see cref="Unit"/> with a specified team, position, and optional prefab override.
        /// Assigns default items and prefabs based on team identity.
        /// </summary>
        /// <param name="team">The team to which the unit belongs.</param>
        /// <param name="position">The initial world position of the unit.</param>
        /// <param name="overridePrefab">Optional prefab name override. If not supplied, uses team default.</param>
        public Unit(Team team, RideVector3 position, string overridePrefab = "")
            : this()
        {
            this.prefab = !string.IsNullOrEmpty(overridePrefab) ? overridePrefab : GetDefaultPrefab(team);
            this.name = this.prefab;
            this.pos = position;
            this.team = team;
            this.items = GetDefaultItems(team);
            this.moveBehaviour = new Movement.Movement(MovementBehaviour.Unrestricted, PathingBehaviour.Loop, RideVector3.zero, 0, 2, false, null);
        }

        /// <summary>
        /// Returns the default prefab name for a unit based on team affiliation.
        /// </summary>
        /// <param name="team">The team the unit belongs to.</param>
        /// <returns>String name of the default prefab.</returns>
        private static string GetDefaultPrefab(Team team)
        {
            switch (team)
            {
                case Team.Blue: return "ChrUsaArmyInfantryAcu01Prefab";
                case Team.Red: return "ChrIrqInsurgentMleAdultAvg01Prefab";
                case Team.Civilian:
                default: return "ChrIrqCivilianMleAdultAvg01Prefab";
            }
        }

        /// <summary>
        /// Returns the default items assigned to a unit based on team.
        /// </summary>
        /// <param name="team">The team the unit belongs to.</param>
        /// <returns>An array of default item types, or null.</returns>
        private static ItemType[] GetDefaultItems(Team team)
        {
            switch (team)
            {
                case Team.Blue: return new[] { ItemType.m4 };
                case Team.Red: return new[] { ItemType.ak47 };
                case Team.Civilian:
                default: return null;
            }
        }
    }
}
