using System;
using Ride.Entities;
using Ride.Movement;

namespace Ride
{
    /// <summary>
    /// Represents the team affiliation of a unit.
    /// </summary>
    public enum Team
    {
        Blue,
        Red,
        Civilian
    }

    /// <summary>
    /// Represents a simulated scenario entity with position, attributes, team alignment,
    /// prefab identity, optional items, and movement behavior.
    ///
    /// Units are commonly spawned from scenario definitions and serve as core agents
    /// or interactive objects in both gameplay and simulation layers. Each unit has
    /// spatial properties (position, rotation), health and stamina, team identity,
    /// and can be customized via prefab overrides or SIDC-based symbology.
    ///
    /// This class is serialized as part of scenario data and instantiated into live
    /// GameObjects at runtime by system logic or scenario loaders.
    /// </summary>
    [Serializable]
    public class Unit : IIdentity
    {
        /// <inheritdoc/>
        public RideID id { get; set; }

        /// <summary>
        /// The display name of the unit. Usually matches the prefab name.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The prefab used to instantiate the unit in Unity.
        /// </summary>
        public string prefab;

        /// <summary>
        /// An optional unit classification (e.g., type ID). Not used in core systems.
        /// </summary>
        public int type;

        /// <summary>
        /// The initial world position of the unit.
        /// </summary>
        public RideVector3 pos;

        /// <summary>
        /// The initial world rotation of the unit. Defaults to identity.
        /// </summary>
        public RideQuaternion rot = RideQuaternion.identity;

        /// <summary>
        /// The team affiliation of the unit (Blue, Red, Civilian).
        /// </summary>
        public Team team;

        /// <summary>
        /// Inventory bin ID used by pathfinding or inventory logic.
        /// 13 = default weight; 14 = fully loaded.
        /// </summary>
        public int bin = 13;

        /// <summary>
        /// The current health of the unit.
        /// </summary>
        public float health = 100;

        /// <summary>
        /// The maximum health value the unit can have.
        /// </summary>
        public float maxHealth = 100;

        /// <summary>
        /// The current stamina level of the unit.
        /// </summary>
        public float stamina = 100;

        /// <summary>
        /// The maximum stamina the unit can store.
        /// </summary>
        public float maxStamina = 100;

        /// <summary>
        /// The vertical jump speed or strength of the unit.
        /// </summary>
        public float jumpSpeed = 5;

        /// <summary>
        /// The base walking/running speed of the unit.
        /// </summary>
        public float defaultSpeed = 5;

        /// <summary>
        /// Movement multiplier when the unit is boosted or sprinting.
        /// </summary>
        public float speedBoostMultiplier = 2;

        /// <summary>
        /// Movement multiplier when slowed by effects or environment.
        /// </summary>
        public float speedSlowMultiplier = 0.5f;

        /// <summary>
        /// Reduction rate for suppression effects. Higher values reduce suppression faster.
        /// </summary>
        public float suppressionReductionRate = 0.1f;

        /// <summary>
        /// A normalized skill rating used for AI decision-making.
        /// </summary>
        public float skillLevel = 1.0f;

        /// <summary>
        /// The MIL-STD-2525 SIDC string identifying the symbol to display for this unit.
        /// </summary>
        public string sidc = string.Empty;

        /// <summary>
        /// An array of items assigned to the unit at spawn.
        /// </summary>
        public ItemType[] items;

        /// <summary>
        /// The movement behavior assigned to this unit.
        /// </summary>
        public Movement.Movement moveBehaviour;

        /// <summary>
        /// Bit flags describing unit capabilities (e.g., agent, sensor, target).
        /// </summary>
        public EntityAttributes attributes = EntityAttributes.agent;


        /// <summary>
        /// Default constructor for serialization or manual setup.
        /// </summary>
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
