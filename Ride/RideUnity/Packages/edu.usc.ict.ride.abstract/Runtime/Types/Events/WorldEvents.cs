using Ride.Entities;

namespace Ride.WorldState
{
    public interface IWorldEvent { }

    public class WorldEventBase : IWorldEvent
    {
        public override string ToString()
        {
            return RideIO.JsonSerialize(this);
        }
    }


    public class ScenarioStartedEvent : WorldEventBase 
    {
        public readonly RideID scenarioID;
        public ScenarioStartedEvent(RideID scenarioID)
        {
            this.scenarioID = scenarioID;
        }
    }

    public class TeamEvent : WorldEventBase
    {
        public readonly Team team;

        public TeamEvent(Team team)
        {
            this.team = team;
        }
    }

    public class TeamTargetsAcquiredEvent : TeamEvent
    {
        public readonly RideID[] engagements;
        public TeamTargetsAcquiredEvent(Team team, RideID[] engagements) : base(team)
        {
            this.engagements = engagements;
        }
    }

    public class EntityEvent : WorldEventBase
    {
        public readonly RideID entityID;

        public EntityEvent(RideID id)
        {
            entityID = id;
        }
    }

    public class EntityCreatedEvent : EntityEvent
    {
        public readonly EntityData entityData;
        public readonly object entityObjData;
        public EntityCreatedEvent(RideID id, EntityData data, object objData) : base(id)
        {
            entityData = data;
            entityData.id = id;
            entityObjData = objData;
        }
    }

    public class EntityDataEvent : EntityEvent
    {
        public struct EntityDataPoint
        {
            public string category;
            public string value;
        }

        public readonly EntityDataPoint[] dataPoints;

        public EntityDataEvent(RideID id, EntityDataPoint[] dataPts) : base(id)
        {
            dataPoints = dataPts;
        }
    }

    public class ItemEvent : EntityEvent
    {
        public RideID itemId => entityID;
        public ItemEvent(RideID itemId) : base(itemId)
        {
        }
    }

    public class ExplosiveEvent : ItemEvent
    {
        public readonly Explosive explosiveData;

        public ExplosiveEvent(RideID explosiveId) : base(explosiveId)
        {
            explosiveData = Globals.api.equipmentSystem.weaponSystem.GetExplosiveData(explosiveId);
        }
    }

    public class AgentEvent : WorldEventBase
    {
        public readonly RideID agent;
        public override string ToString()
        {
            return base.ToString();
        }
        public AgentEvent(RideID agent) { this.agent = agent; }
    }

    public class AgentAddedEvent : AgentEvent
    {
        public AgentAddedEvent(RideID agent) : base(agent) { }
    }

    public class AgentRemovedEvent : AgentEvent
    {
        public AgentRemovedEvent(RideID agent) : base(agent) { }
    }

    public class BeginMovingEvent : WorldEventBase
    {
        public readonly RideID mover;
        public readonly RideVector3 destination;
        public readonly bool isLocomotionMovment; // if this is false, it's pathing movement

        public BeginMovingEvent(RideID mover) { this.mover = mover; this.isLocomotionMovment = true; }
        public BeginMovingEvent(RideID mover, RideVector3 destination) { this.mover = mover;  this.destination = destination; this.isLocomotionMovment = false; }
    }

    public class AgentJumpEvent : AgentEvent
    {
        public readonly float jumpSpeed;
        public readonly RideVector3 forwardDir;
        public readonly RideVector3 rightDir;

        public AgentJumpEvent(RideID agent, float jSpeed, RideVector3 forw, RideVector3 right) : base(agent)
        {
            jumpSpeed = jSpeed;
            forwardDir = forw;
            rightDir = right;
        }
    }

    public class AgentDiedEvent : AgentEvent
    {
        public AgentDiedEvent(RideID agent) : base(agent) { }
    }

    public class AgentRevivedEvent : AgentEvent
    {
        public AgentRevivedEvent(RideID agent) : base(agent) { }
    }

    public class MoverStoppedEvent : WorldEventBase
    {
        public readonly RideID mover;
        public MoverStoppedEvent(RideID mover)  { this.mover = mover; }
    }

    public class AgentHealthModifiedEvent : AgentEvent
    {
        public readonly float modification;
        public AgentHealthModifiedEvent(RideID agent, float modification) : base(agent) { this.modification = modification; }
    }

    public class AgentEngagedEvent : AgentEvent
    {
        public readonly RideID engagee;
        public AgentEngagedEvent(RideID engager, RideID engagee) :base(engager) {  this.engagee = engagee; }
    }

    public class AgentDisengagedEvent : AgentEvent
    {
        public readonly RideID engagee;
        public AgentDisengagedEvent(RideID engager, RideID engagee) : base(engager) { this.engagee = engagee; }
    }

    public class AgentKilledByAgentEvent : AgentEvent
    {
        public readonly RideID killer;
        public readonly RideID killerWeapon;
        public readonly bool allowRespawn;
        public readonly bool removeOnDestruction;

        public AgentKilledByAgentEvent(RideID deceased, RideID killer, RideID killerWeapon) : base(deceased) 
        { 
            this.killer = killer;
            this.killerWeapon = killerWeapon;
            this.allowRespawn = true;
            this.removeOnDestruction = true;
        }

        public AgentKilledByAgentEvent(RideID deceased, RideID killer, RideID killerWeapon, bool allowRespawn, bool removeOnDestruction) : base(deceased)
        {
            this.killer = killer;
            this.killerWeapon = killerWeapon;
            this.allowRespawn = allowRespawn;
            this.removeOnDestruction = removeOnDestruction;
        }
    }

    public class AgentAttackedByAgentEvent : AgentEvent
    {
        public readonly RideID attackee;
        public readonly RideID weapon;
        public readonly float damage;
        public readonly bool isHit;
        public AgentAttackedByAgentEvent(RideID attacker, RideID attackee, RideID weapon, float damage, bool isHit) : base(attacker)
        {
            this.attackee = attackee;
            this.weapon = weapon;
            this.damage = damage;
            this.isHit = isHit;
        }
    }
    
    public class AgentItemEvent: AgentEvent
    {
        public readonly RideID itemId;

        public AgentItemEvent(RideID agent, RideID item) : base(agent)
        {
            itemId = item;
        }
    }

    public class AgentThrowEvent : AgentEvent
    {
        public readonly RideID throwable;
        public readonly RideVector3 throwDirection;
        public readonly float throwStrength;

        public AgentThrowEvent(RideID agent, RideID throwObj, RideVector3 throwDir, float throwStrngth) : base(agent)
        {
            throwable = throwObj;
            throwDirection = throwDir;
            throwStrength = throwStrngth;
        }
    }

    public class AgentPostureChange : AgentEvent
    {
        public readonly AgentPosture agentPosture;

        public AgentPostureChange(RideID agent, AgentPosture posture) : base(agent)
        {
            agentPosture = posture;
        }
    }

    public class AgentIdleEmoteChange : AgentEvent
    {
        public readonly int agentIdleChange;

        public AgentIdleEmoteChange(RideID agent, int agentIdleChange) : base(agent)
        {
            this.agentIdleChange = agentIdleChange;
        }
    }

    public class AgentClassChange : AgentEvent 
    {
        public readonly bool isMilitary;

        public AgentClassChange(RideID agent, bool isMilitary) : base(agent)
        {
            this.isMilitary = isMilitary;
        }
    }

    public class WeaponEvent : AgentEvent
    {
        public readonly RideID weapon;

        public WeaponEvent(RideID attacker, RideID weapon) : base(attacker)
        {
            this.weapon = weapon;
        }
    }

    public class RoundLandingEvent : WeaponEvent
    {
        public readonly RideVector3 roundPos;

        public RoundLandingEvent(RideID attacker, RideID weapon, RideVector3 pos) : base(attacker, weapon)
        {
            roundPos = pos;
        }
    }
    
    public class BallisticHitEvent : RoundLandingEvent
    {
        public readonly float destructionRange;
        public readonly float destructionForce;

        public BallisticHitEvent(RideID attacker, RideID weapon, RideVector3 pos, float destructiveRange, float destructiveForce) : base(attacker, weapon, pos)
        {
            destructionRange = destructiveRange;
            destructionForce = destructiveForce;
        }
    }

    public class WeaponFiringModeChangeEvent : AgentEvent
    {
        public readonly RideID weapon;
        public readonly Entities.WeaponFiringMode firingMode;

        public WeaponFiringModeChangeEvent(RideID weaponOwner, RideID weapon, Entities.WeaponFiringMode firingMode) : base(weaponOwner)
        {
            this.weapon = weapon;
            this.firingMode = firingMode;
        }
    }

    public enum EntityBehaviourChangeType { Set, Add, Remove }
    public enum EntityBehaviourUserType { Agent, Group }

    public class EntityBehaviourEvent : WorldEventBase
    {
        /// <summary>
        /// The entity involved in the behavioural change
        /// </summary>
        public readonly RideID entity;

        /// <summary>
        /// The behaviour involved in the change
        /// </summary>
        public readonly RideID behaviour;

        /// <summary>
        /// Human readable name of the behaviour
        /// </summary>
        public readonly string name;

        public EntityBehaviourEvent(RideID entity, RideID behaviour, string name) 
        {
            this.entity = entity;
            this.behaviour = behaviour;
            this.name = name;
        }
    }

    public class EntityBehaviourChangedEvent : EntityBehaviourEvent
    {
        /// <summary>
        /// The previous behaviour used by the entity. Equal to RideID.Null unless BehaviourChangeType is equal to Set 
        /// </summary>
        public readonly RideID prevBehaviour = RideID.Null;

        /// <summary>
        /// The type of entity involved in the change
        /// </summary>
        public readonly EntityBehaviourUserType userType;

        /// <summary>
        /// The type of change that happend
        /// </summary>
        public readonly EntityBehaviourChangeType changeType;

        public EntityBehaviourChangedEvent(RideID entity, RideID behaviour,
            string name, EntityBehaviourUserType userType, EntityBehaviourChangeType changeType) 
            : base(entity, behaviour, name)
        {
            this.userType = userType;
            this.changeType = changeType;
        }

        public EntityBehaviourChangedEvent(RideID entity, RideID behaviour, string name, RideID prevBehaviour,
            EntityBehaviourUserType userType, EntityBehaviourChangeType changeType)
            : base(entity, behaviour, name)
        {
            this.prevBehaviour = prevBehaviour;
            this.userType = userType;
            this.changeType = changeType;
        }
    }

    public class EntityBehaviourStartedEvent : EntityBehaviourEvent
    {
        public EntityBehaviourStartedEvent(RideID entity, RideID behaviour, string name) 
            : base(entity, behaviour, name) { }
    }

    public class EntityBehaviourStoppedEvent : EntityBehaviourEvent
    {
        public EntityBehaviourStoppedEvent(RideID entity, RideID behaviour, string name)
            : base(entity, behaviour, name) { }
    }

    public class EntityBehaviourFinishedEvent : EntityBehaviourEvent
    {
        public EntityBehaviourFinishedEvent(RideID entity, RideID behaviour, string name)
            : base(entity, behaviour, name) { }
    }

    public class AgentStateChangedEvent : AgentEvent
    {
        public readonly string fromState;
        public readonly string toState;
        public AgentStateChangedEvent(RideID agent, string from, string to) : base(agent) { this.fromState = from; this.toState = to; }
    }

    public class AgentReachedGoalEvent : AgentEvent
    {
        public readonly RideID goal;
        public readonly float reward;               
        public AgentReachedGoalEvent(RideID agent, RideID goal, float reward) : base(agent) { this.goal = goal;  this.reward = reward; }
    }

    public class AgentTrainingEpisodeBeginEvent : AgentEvent
    {
        public AgentTrainingEpisodeBeginEvent(RideID agent) : base(agent) { }
    }

    public class AgentTrainingEpisodeEndEvent : AgentEvent
    {
        public AgentTrainingEpisodeEndEvent(RideID agent) : base(agent) { }
    }

    public class AgentTrainingEpisodeResultEvent : AgentEvent
    {
        public readonly bool successful;
        public AgentTrainingEpisodeResultEvent(RideID agent, bool successful) : base(agent) { this.successful = successful; }
    }

    public class EnemyDestroyedEvent : AgentEvent
    {
        public EnemyDestroyedEvent(RideID agent) : base(agent) { }
    }

    public class FlagCapturedEvent : AgentEvent
    {
        public FlagCapturedEvent(RideID agent) : base(agent) { }
    }

    public class AgentTrajectoryEvent: AgentEvent
    {
        public RideVector3[] positions;
        public AgentTrajectoryEvent(RideID agent, RideVector3[] positions) : base(agent) { this.positions = positions; }
    }

    public class InputLayerModifiedEvent: WorldEventBase
    {
        /// <summary>
        /// Which layer was modified?
        /// </summary>
        public IO.RideInputLayer layer;
        /// <summary>
        /// Was the layer activated or deactivated?
        /// </summary>
        public bool isOn;
        public InputLayerModifiedEvent(IO.RideInputLayer layer, bool isOn) { this.layer = layer; this.isOn = isOn; }
    }

    public class BillboardSelectedEvent : WorldEventBase {
        public RideID ordnanceId;
        public BillboardSelectedEvent(RideID ordnanceId) { this.ordnanceId = ordnanceId; }
    }
    public class BillboardUnselectedEvent : WorldEventBase {
        public RideID ordnanceId;
        public BillboardUnselectedEvent(RideID ordnanceId) { this.ordnanceId = ordnanceId; }
    }

    public class IEDTriggeredEvent : WorldEventBase
    {
        public readonly RideVector3 position;
        public IEDTriggeredEvent(RideVector3 position) { this.position = position; }
    }

    public class WaypointReachedEvent : WorldEventBase
    {
        public readonly RideID wp;
        public readonly RideID mover;
        public WaypointReachedEvent(RideID wp, RideID mover) { this.wp = wp; this.mover = mover; }
    }

    public class PathFinishedEvent : WorldEventBase
    {
        public readonly RideID mover;
        public PathFinishedEvent(RideID mover) { this.mover = mover; }
    }

    public class DestinationReachedEvent : WorldEventBase
    {
        public readonly RideVector3 destination;
        public readonly RideID mover;

        /// <summary>
        /// if true, the mover RideID is a group
        /// </summary>
        public readonly bool isGroup;

        public DestinationReachedEvent(RideID mover, RideVector3 destination, bool isGroup) 
            { this.mover = mover; this.destination = destination; this.isGroup = isGroup; }
    }

    [System.Serializable]
    public struct TerrainDestructEventData
    {
        public RideVector3 point;
        public float radius;
        public float power;
    }

    public class TerrainDestructedEvent : WorldEventBase
    {
        public readonly TerrainDestructEventData data;

        public TerrainDestructedEvent(RideVector3 point, float radius, float power)
        {
            this.data = new TerrainDestructEventData() { point = point, radius = radius, power = power };
        }
        public TerrainDestructedEvent(TerrainDestructEventData data)
        {
            this.data = data;
        }
    }

    public class TerrainLoadedEvent : WorldEventBase
    {
        public readonly Ride.Terrain.LoadTerrainParams loadParams;
        public TerrainLoadedEvent(Ride.Terrain.LoadTerrainParams loadParams) { this.loadParams = loadParams; }
    }

    public class TerrainClearedEvent : WorldEventBase
    {
        public TerrainClearedEvent() { }
    }

    public class GameObjectEvent : WorldEventBase
    {
        public readonly RideID gameObject;
        public GameObjectEvent(RideID gameObject) { this.gameObject = gameObject; }
    }

    public class GameObjectCreatedEvent : GameObjectEvent
    {
        public GameObjectCreatedEvent(RideID gameObject) : base(gameObject) { }
    }

    public class GameObjectDestroyedEvent : GameObjectEvent
    {
        public GameObjectDestroyedEvent(RideID gameObject) : base(gameObject) { }
    }

    public class MaterialEvent : WorldEventBase
    {
        public readonly RideID material;
        public MaterialEvent(RideID material) { this.material = material; }
    }

    public class MaterialAddedEvent : MaterialEvent
    {
        public MaterialAddedEvent(RideID material) : base(material) { }
    }

    public class NetworkViewEvent : WorldEventBase
    {
        readonly public RideID id;
        readonly public /*int*/dynamic viewId;
        readonly public int actorId;
        readonly public bool isMine;
        readonly public bool isLocalPlayer;

        public NetworkViewEvent(RideID id, /*int viewId*/ dynamic viewId, int actorId, bool isMine, bool isLocalPlayer)
        {
            this.id = id;
            this.viewId = viewId;
            this.actorId = actorId;
            this.isMine = isMine;
            this.isLocalPlayer = isLocalPlayer;
        }
    }

    public class NetworkViewCreatedEvent : NetworkViewEvent
    {
        public NetworkViewCreatedEvent(RideID id, int viewId, int actorId, bool isMine, bool isLocalPlayer) 
            : base(id, viewId, actorId, isMine, isLocalPlayer) { }
    }

    public class NetworkViewDestoyedEvent : NetworkViewEvent
    {
        public NetworkViewDestoyedEvent(RideID id, int viewId, int actorId, bool isMine, bool isLocalPlayer)
            : base(id, viewId, actorId, isMine, isLocalPlayer) { }
    }

    /// <summary>
    /// World event data dispatched by IRegionService, 
    /// Accompanies world events idenified as "agentEnterRegion", "agentExitRegion", "groupEnterRegion", "groupExitRegion"
    /// </summary>
    public class RegionChangeEvent : AgentEvent 
    {
        public RideID region;
        public RegionChangeEvent(RideID agent, RideID r) : base(agent)
        {
            region = r;
        }
    }
    
    
    /// <summary>
    /// World event data dispatched by IRegionService, 
    /// Accompanies world events idenified as "agentEnterRegion", "agentExitRegion", "groupEnterRegion", "groupExitRegion"
    /// </summary>
    public class TeamLeaderCommandEvent : AgentEvent 
    {
        public string leaderCommand = "Nothing";
        public string targetGroup = "all";
        public RideVector3 targetPos = RideVector3.zero;
        public TeamLeaderCommandEvent(RideID agent, string command, string group = "all") : base(agent)
        {
            leaderCommand = command;
            targetGroup = group;
        }
    }
    
    public class TeamMemberReportEvent : AgentEvent 
    {
        public string teamMemberStatus = "Nothing";
        public RideID leaderId = RideID.Null;
        public string teamName = "all";
        public TeamMemberReportEvent(RideID agent, RideID leaderId, string status, string teamName) : base(agent)
        {
            teamMemberStatus = status;
            this.leaderId = leaderId;
            this.teamName = teamName;
        }
        public static void parseStatus(string status, out string stateName, out string eventName)
        {
            stateName = "None";
            eventName = "None";
            string[] words = status.Split(':');
            if (words.Length >= 2)
            {
                stateName = words[0];
                eventName = words[1];
            }
        }
    }

    public class AgentCoverReachedEvent : AgentEvent {
        public float coverRating;

        public AgentCoverReachedEvent(RideID agent, float coverRating) : base(agent) {
            this.coverRating = coverRating;
        }
    }

    public class AgentCoverLeftEvent : AgentEvent {
        public float coverRating;

        public AgentCoverLeftEvent(RideID agent, float coverRating) : base(agent) {
            this.coverRating = coverRating;
        }
    }

    public class SimulationResetEvent : WorldEventBase 
    {
        public string debugMsg;
        public SimulationResetEvent(string debugMsg = "")
        {
            this.debugMsg = debugMsg;
        }
    }
    
    public class FormationChangeEvent : WorldEventBase 
    {
        public string prevFormation, curFormation, debugMsg;
        public FormationChangeEvent(string prevFormation, string curFormation, string debugMsg = "")
        {
            this.prevFormation = prevFormation;
            this.curFormation = curFormation;
            this.debugMsg = debugMsg;
        }
    }

    /// <summary>
    /// World Event dispatched by the Scenario System when a Scenario Event is executed
    /// </summary>
    public class ScenarioEventEvent : WorldEventBase
    {
        public RideID scenario;
        public RideID id;
        public ScenarioEventEvent(RideID scenario, RideID id)
        {
            this.scenario = scenario;
            this.id = id;
        }

    }
    
    public class SelectionEvent : WorldEventBase
    {
        public RideID[] deselected;
        public RideID[] selected;

        public SelectionEvent(RideID[] deselectedIds, RideID[] selectedIds)
        {
            deselected = new RideID[(deselectedIds != null) ? deselectedIds.Length : 0];
            selected = new RideID[(selectedIds != null) ? selectedIds.Length : 0];

            if (deselectedIds != null)
            {
                for (int i = 0; i < deselectedIds.Length; i++)
                    deselected[i] = deselectedIds[i];
            }

            if(selectedIds != null)
            {
                for (int i = 0; i < selectedIds.Length; i++)
                    selected[i] = selectedIds[i];
            }
        }
    }

    public class ObservabilityUpdatedEvent : WorldEventBase
    {

    }

    public class BTNodeVisited : WorldEventBase
    {
        public string guid;
        public BTNodeVisited(string guid)
        {
            this.guid = guid;
        }
    }

    public class ExitingStateMachine : WorldEventBase
    {
        public RideID agent;
        public string parentStateName;  //--Name of the parent state that finish state was substate of.
        public string destinationName;  //--Name of the state that current substate is trying to exit to. 

        public ExitingStateMachine(RideID agent, string parentStateName, string destinationName)
        {
            this.agent = agent;
            this.parentStateName = parentStateName;
            this.destinationName = destinationName;
        }
    }

    public class ExitingBehaviorTree : WorldEventBase
    {
        public RideID agentID;
        public string destinationName;
        public int trigger;

        public ExitingBehaviorTree(RideID agentID, string destinationName, int trigger)
        {
            this.agentID = agentID;
            this.destinationName = destinationName;
            this.trigger = trigger;
        }
    }

    public class AgentStateChangedEventGUID : AgentEvent
    {
        public readonly string fromGUID;
        public readonly string fromState;
        public readonly string toGUID;
        public readonly string toState;
        public AgentStateChangedEventGUID(RideID agent, string fromGUID, string fromState, string toGUID, string toState) : base(agent) 
        { this.fromGUID = fromGUID; this.fromState = fromState; this.toGUID = toGUID; this.toState = toState; }
    }

    public class NAT_NodeResolved : WorldEventBase
    {
        public string resolvedNodeGUID;

        public NAT_NodeResolved(string resolvedNodeGUID)
        {
            this.resolvedNodeGUID = resolvedNodeGUID;
        }
    }

    public class NAT_EnterFunction : WorldEventBase
    {
        public string functionNodeGuid;
        public NAT_EnterFunction(string functionNodeGuid)
        {
            this.functionNodeGuid = functionNodeGuid;
        }
    }
    
    public class NAT_EnterNode : WorldEventBase
    {
        public string nodeGuid;
        public NAT_EnterNode(string nodeGuid) { this.nodeGuid = nodeGuid; }
    }

    public class NAT_EnterFinish : WorldEventBase
    {
        public string finishNodeGuid;
        public string parentNodeGuid;
        public NAT_EnterFinish(string finishNodeGuid, string parentNodeGuid)
        {
            this.finishNodeGuid = finishNodeGuid;
            this.parentNodeGuid = parentNodeGuid;
        }
    }

    public class NAT_ReceivingNLPResponse : WorldEventBase
    {
        public string response;
        public NAT_ReceivingNLPResponse(string response) { this.response = response; }
    }
}
