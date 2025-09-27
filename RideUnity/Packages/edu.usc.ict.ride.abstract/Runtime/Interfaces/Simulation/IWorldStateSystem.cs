using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.WorldState
{
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


    public interface IWorldStateSystem : IRideSystem
    {
        /// <summary>
        /// The data that IWorldStateSystem modifies
        /// </summary>
        IWorldState worldState { get; set; }

        /// <summary>
        /// Adds a callback to be triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The world event that triggers the listner</param>
        /// <param name="cb">The callback to invoke when the worldEvent is dispatched</param>
        void AddListener<T>(WorldEvent worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Adds a callback to be triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The world event that triggers the listner</param>
        /// <param name="cb">The callback to invoke when the worldEvent is dispatched</param>
        void AddListener<T>(string worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Removes a callback from being triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The world event associated with the listner</param>
        /// <param name="cb">The removed callback</param>
        void RemoveListener<T>(WorldEvent worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Removes a callback from being triggered when the given world event is dispatched
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The world event associated with the listner</param>
        /// <param name="cb">The removed callback</param>
        void RemoveListener<T>(string worldEvent, WorldSimulationEvent<T> cb);

        /// <summary>
        /// Dispatches an event and invokes all listeners associated with the given event and the given type of data received in the callback
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The triggered event</param>
        /// <param name="eventData">The data associated with the event</param>
        void DispatchEvent<T>(WorldEvent worldEvent, T eventData);

        /// <summary>
        /// Dispatches an event and invokes all listeners associated with the given event and the given type of data received in the callback
        /// </summary>
        /// <typeparam name="T">The expected data to be passed into the listner</typeparam>
        /// <param name="worldEvent">The triggered event</param>
        /// <param name="eventData">The data associated with the event</param>
        void DispatchEvent<T>(string worldEvent, T eventData);

        /// <summary>
        /// Removes all past world events from the cache
        /// </summary>
        void ClearWorldEvents();

        /// <summary>
        /// Removes all listeners from all world events
        /// </summary>
        void ClearListeners();

        /// <summary>
        /// returns all of the events that have occurred so far
        /// </summary>
        /// <param name="worldState">a list of all the simulation events that have occurred as of yet that you can iterate over</param>
        /// <returns></returns>
        IEnumerable<WorldEventMarker> GetWorldEvents();

        /// <summary>
        /// Checks whether the WorldEvent has ever happened
        /// </summary>
        /// <param name="worldEvent">the worldEvent to check if it occured</param>
        /// <returns>true if the worldEvent happened</returns>
        bool HasWorldEventOccurred(WorldEvent worldEvent);

        /// <summary>
        /// Sets the date time for the simulation to keep track of time
        /// </summary>
        /// <param name="dateTime"></param>
        void SetTimeOfDay(DateTime dateTime);

        /// <summary>
        /// sets the time increment which is the multiple of the Unity Time.time which is game time
        /// so if timeIncrement is 1 then 1 unity second = 1 simulation second
        /// and if timeIncrement is 10 then 1 unity second = 10 simulation seconds
        /// </summary>
        /// <param name="timeIncrement">the multiple for the unity Time.time</param>
        void SetTimeIncrement(float timeIncrement);

        /// <summary>
        /// the time of the simulation at the moment it is called which is the SetTimeOfDay + the unity time.tim * time increment
        /// </summary>
        /// <returns>a DateTime representing the current simulation time</returns>
        DateTime GetTimeOfDay();

        /// <summary>
        /// Get the local real world time of the computer running the simulation
        /// </summary>
        /// <returns></returns>
        DateTime GetLocalTime();
    }
}
