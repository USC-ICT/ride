using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.WorldState
{
    /// <summary>Identifies standard events published through the RIDE world-state event bus.</summary>
    [Serializable]
    public enum WorldEvent
    {
        unknown,
        scenarioStarted,
        fireTeamInContact,
        fireTeamNotInContact,
        gameOver,
        flagCaptured,
        agentEngaged,
        agentDisengaged,
        agentAttackedAgent,
        agentDied,
        agentRevived,
        agentHealthModified,
        agentReachedGoal,
        agentTrainingEpisodeBegin,
        agentTrainingEpisodeEnd,
        agentTrainingEpisodeResult,
        beginMoving,
        enemiesDestroyed,
        agentCreated,
        agentRemoved,
        waypointReached,
        pathFinished,
        destinationReached,
        iedTriggered,
        agentStateChanged,
        entityBehaviourChanged,
        entityBehaviourStarted,
        entityBehaviourStopped,
        entityBehaviourFinished,
        moverStopped,
        terrainLoaded,
        terrainCleared,
        terrainDestructed,
        agentJumped,
        agentLanded,
        agentKilledByAgent,
        weaponFired,
        agentFiringWeapon,
        agentStopFiringWeapon,
        weaponFiringModeChange,
        weaponRoundLanded,
        agentAimingWeapon,
        agentStopAimingWeapon,
        weaponOutOfAmmo,
        weaponMagazineEmpty,
        gameObjectCreated,
        gameObjectDestroyed,
        materialCreated,
        networkViewCreated,
        networkViewDestroyed,
        inputLayersModified,
        agentThrowObject,
        agentStartThrowObject,
        agentEndThrowObject,
        agentPostureChange,
        billboardSelected,
        billboardUnselected,
        scenarioEvent,
        entityDataCreated,
        agentIdleEmoteChanged,
        agentClassChanged,
        entityDataSetup,
        entityDataCreationComplete,
        entityDataDestroyed,
        weaponReload,
        entitySelectionChanged,
        observabilityUpdated,
        ballisticHit,
        groupCreated,
        groupDestroyed,
        targetsAcquired,
        entityDisabled,
        entityDataRequest,
        entityDataUpdate,
        agentCoverReached,
        agentCoverLeft,
        behaviorTreeRootUpdated,
        leafNodeVisited,
        exitingStateMachine,
        exitingBehaviorTree,
        agentStateChangedGUID,
        NAT_NodeResolved,
        NAT_EnterFunctionNode,
        NAT_EnterFinishNode,
        NAT_EnterNode,
        NAT_NLP_Responded,
        explosiveExploded,
        entityReset,
        navMeshCut,
    }


    /// <summary>Records simulation events, dispatches typed event callbacks, and maintains the simulation clock.</summary>
    /// <remarks>Used throughout Simulation, agent logic, scenarios, UI, and behavior systems as the shared event bus.</remarks>
    public interface IWorldStateSystem : IRideSystem
    {
        /// <summary>Gets or sets the world-state data modified by this system.</summary>
        IWorldState worldState { get; set; }

        /// <summary>
        /// Adds a callback to be triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The payload type expected by the listener.</typeparam>
        /// <param name="worldEvent">The event that triggers the listener.</param>
        /// <param name="cb">The callback to invoke when the event is dispatched.</param>
        void AddListener<T>(WorldEvent worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Adds a callback to be triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The payload type expected by the listener.</typeparam>
        /// <param name="worldEvent">The custom event name that triggers the listener.</param>
        /// <param name="cb">The callback to invoke when the event is dispatched.</param>
        void AddListener<T>(string worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Removes a callback from being triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The payload type expected by the listener.</typeparam>
        /// <param name="worldEvent">The event associated with the listener.</param>
        /// <param name="cb">The callback to remove.</param>
        void RemoveListener<T>(WorldEvent worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Removes a callback from being triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The payload type expected by the listener.</typeparam>
        /// <param name="worldEvent">The custom event name associated with the listener.</param>
        /// <param name="cb">The callback to remove.</param>
        void RemoveListener<T>(string worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Dispatches an event and invokes all listeners associated with the given event and the given type of data received in the callback
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="worldEvent">The event to dispatch.</param>
        /// <param name="eventData">The event payload.</param>
        void DispatchEvent<T>(WorldEvent worldEvent, T eventData);

        /// <summary>
        /// Dispatches an event and invokes all listeners associated with the given event and the given type of data received in the callback
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="worldEvent">The custom event name to dispatch.</param>
        /// <param name="eventData">The event payload.</param>
        void DispatchEvent<T>(string worldEvent, T eventData);

        /// <summary>Removes all recorded world-event markers.</summary>
        void ClearWorldEvents();

        /// <summary>Removes all registered listeners from all world events.</summary>
        void ClearListeners();

        /// <summary>Gets the world events recorded so far.</summary>
        /// <returns>The chronological event markers.</returns>
        IEnumerable<WorldEventMarker> GetWorldEvents();

        /// <summary>
        /// Checks whether the WorldEvent has ever happened
        /// </summary>
        /// <param name="worldEvent">The event to find.</param>
        /// <returns><see langword="true"/> if the event has occurred; otherwise, <see langword="false"/>.</returns>
        bool HasWorldEventOccurred(WorldEvent worldEvent);

        /// <summary>
        /// Sets the date time for the simulation to keep track of time
        /// </summary>
        /// <param name="dateTime">The simulation clock's starting date and time.</param>
        void SetTimeOfDay(DateTime dateTime);

        /// <summary>
        /// sets the time increment which is the multiple of the Unity Time.time which is game time
        /// so if timeIncrement is 1 then 1 unity second = 1 simulation second
        /// and if timeIncrement is 10 then 1 unity second = 10 simulation seconds
        /// </summary>
        /// <param name="timeIncrement">The number of simulation seconds advanced per Unity game-time second.</param>
        void SetTimeIncrement(float timeIncrement);

        /// <summary>
        /// Gets the current simulation time by applying elapsed game time and the configured increment to the starting time.
        /// </summary>
        /// <returns>The current simulation date and time.</returns>
        DateTime GetTimeOfDay();

        /// <summary>
        /// Gets the local system time of the computer running the simulation.
        /// </summary>
        /// <returns>The current local date and time.</returns>
        DateTime GetLocalTime();
    }
}
