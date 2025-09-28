namespace Ride.Entities
{
    /// <summary>
    /// Indicates the intended echelon size for a unit. Helps with naming, AI setup, and behavior scaling.
    /// </summary>
    public enum EchelonSize
    {
        Individual,
        Fireteam,
        Squad,
        Platoon,
        Company,
        Battalion,
        Brigade,
        Division,
        Corps
    }

    /// <summary>
    /// Specifies how a unit should be created, including layout, team, position, and prefab overrides.
    /// </summary>
    public struct UnitCreationParams
    {
        /// <summary>Name assigned to the unit (e.g. "2nd Squad Alpha").</summary>
        public string name;

        /// <summary> World space position for the unit's origin (centered on the commander).</summary>
        public RideVector3 position;

        /// <summary>World space rotation for the unit's orientation.</summary>
        public RideQuaternion rotation;

        /// <summary>Number of rows in the agent formation (only applies if num_agents is zero).</summary>
        public int rows;

        /// <summary>Number of columns in the agent formation (only applies if num_agents is zero).</summary>
        public int cols;

        /// <summary>Overrides automatic rows × cols calculation if set. Used in CreateCustomGroup() to spawn an exact number of agents regardless of layout.</summary>
        public int numAgents;

        /// <summary>Team to which this unit belongs.</summary>
        public Team team;

        /// <summary>Separation (in world units) between adjacent members.</summary>
        public float memberSeparation;

        /// <summary>Optional prefab path or identifier for the agent to use.</summary>
        public string prefab;

        public UnitCreationParams(string _name, string _prefab = "")
        {
            name = _name;
            position = RideVector3.zero;
            rotation = RideQuaternion.identity;
            rows = 2;
            cols = 2;
            numAgents = 0;  // Only override if needed
            team = Team.Blue;
            memberSeparation = 1;
            prefab = _prefab;
        }

        public bool UseExplicitAgentCount => numAgents > 0;
    }

    /// <summary>
    /// Defines structured creation of military units such as fireteams, squads, and platoons.
    /// These units are composed of agents and are initialized in the scene with spatial and logical grouping.
    /// </summary>
    public interface IUnitCreatorSystem : IRideSystem
    {
        /// <summary>
        /// Creates a fireteam with standard composition (typically 4 agents).
        /// Commander's Rank: Staff Sgt
        /// </summary>
        /// <param name="p">Parameters defining team, position, layout, and name.</param>
        /// <returns>The RideID of the newly created fireteam.</returns>
        RideID CreateFireTeam(UnitCreationParams p);

        /// <summary>
        /// Creates a squad unit, generally consisting of 4–10 agents.
        /// Commander's Rank: Sgt or Staff Sgt
        /// </summary>
        /// <param name="p">Parameters defining team, position, layout, and name.</param>
        /// <returns>The RideID of the newly created squad.</returns>
        RideID CreateSquad(UnitCreationParams p);

        /// <summary>
        /// Creates a platoon made up of multiple squads (typically 16–40 agents).
        /// Commander's Rank: Lieutenant
        /// </summary>
        /// <param name="p">Parameters defining team, position, layout, and name.</param>
        /// <returns>The RideID of the newly created platoon.</returns>
        RideID CreatePlatoon(UnitCreationParams p);

        /// <summary>
        /// Creates a company-level unit composed of multiple (3-5) platoons (typically 100–200 agents).
        /// Commander's Rank: Captain
        /// </summary>
        /// <param name="p">Parameters defining team, position, layout, and name.</param>
        /// <returns>The RideID of the newly created company.</returns>
        RideID CreateCompany(UnitCreationParams p);

        /// <summary>
        /// Creates a unit with a custom number of agents. Used for testing or flexible formations.
        /// Commander's Rank: Staff Sgt
        /// </summary>
        /// <param name="p">Parameters including exact number of agents to spawn.</param>
        /// <returns>The RideID of the created unit.</returns>
        RideID CreateCustomGroup(UnitCreationParams p);
    }
}
