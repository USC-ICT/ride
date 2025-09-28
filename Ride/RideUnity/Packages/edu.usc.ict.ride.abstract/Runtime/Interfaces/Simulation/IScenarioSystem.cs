using System;
using System.Collections.Generic;
using Ride.Entities;
using Ride.Movement;
using Ride.UI;

namespace Ride.Scenario
{
    /// <summary>
    /// Defines the functionality that can be performed on a scenario
    /// </summary>
    public interface IScenarioSystem : IRideSystem
    {
        /// <summary>
        /// Serializes the data in the scenario and saves it in the filename which gets created and contains the saved data
        /// </summary>
        /// <param name="fileName">the name of the file including path to save the data in</param>
        void SaveScenario(string fileName);

        /// <summary>
        /// Serializes the data in the scenario and saves it in the filename which gets created and contains the saved data
        /// </summary>
        /// <param name="fileName">the name of the file including path to save the data in</param>
        void SaveScenario(RideID scenarioId, string fileName);

        /// <summary>
        /// Loads a scenario into the data structure of the IScenario format
        /// specifically loading agents and a terrain as well as other initial starting data needed for running a simulation
        /// </summary>
        /// <param name="scenarioData">the initial data to load into the IScenario data structure</param>
        void LoadScenario(string scenarioData);

        /// <summary>
        /// Loads a scenario into the data structure of the IScenario format
        /// specifically loading agents and a terrain as well as other initial starting data needed for running a simulation
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="scenarioData">the initial data to load into the IScenario data structure</param>
        void LoadScenario(RideID scenarioId, string scenarioData);

        /// <summary>
        /// a convenience wrapper function to Load a scenario from disk using the path toe file which reads in that file and then calls LoadScenario
        /// </summary>
        /// <param name="scenarioPath">the path to file to load in, it is a json in the msdl structure</param>
        void LoadScenarioFromFile(string scenarioPath);

        /// <summary>
        /// a convenience wrapper function to Load a scenario from disk using the path toe file which reads in that file and then calls LoadScenario
        /// </summary>
        /// <param name="scenarioPath">the path to file to load in, it is a json in the msdl structure</param>
        void LoadScenarioFromFile(RideID scenarioId, string scenarioPath);

        void ClearScenario();

        /// <summary>
        /// deletes all the data in the IScenario so you can run a new clean clear fresh scenario from scratch
        /// </summary>
        void ClearScenario(RideID scenarioId);

        /// <summary>
        /// Insert a generic entity into the scenario
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="tags"></param>
        /// <param name="existingId"></param>
        /// <returns></returns>
        RideID InsertEntity(RideVector3 pos, RideQuaternion rot, IEnumerable<RideParameter> tags, RideID existingId);

        /// <summary>
        /// convenience function to convert the data structure Unit to the main AddAgent function and add the agent
        /// </summary>
        /// <param name="unit">the unit to be added</param>
        /// <returns>the agent which was created</returns>
        RideID AddAgent(Unit unit);

        /// <summary>
        /// convenience function to convert the data structure Unit to the main AddAgent function and add the agent
        /// </summary>
        /// <param name="unit">the unit to be added</param>
        /// <returns>the agent which was created</returns>
        RideID AddAgent(RideID scenarioId, Unit unit);

        /// <summary>
        /// convenience function to convert the data structure Unit to the main AddAgent function and add the agent
        /// </summary>
        /// <param name="agentId">the unit to be added</param>
        /// <returns>the agent which was created</returns>
        void AddAgent(RideID agentId);

        /// <summary>
        /// convenience function to convert the data structure Unit to the main AddAgent function and add the agent
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="agentId">the unit to be added</param>
        /// <returns>the agent which was created</returns>
        void AddAgent(RideID scenarioId, RideID agentId);

        /// <summary>
        /// Removes the agent with the given id from the scenario
        /// </summary>
        /// <param name="id"></param>
        void RemoveAgent(RideID agentId);

        /// <summary>
        /// Removes the agent with the given id from the scenario
        /// </summary>
        /// <param name="id"></param>
        void RemoveAgent(RideID scenarioId, RideID agentId);

        /// <summary>
        /// Adds an area of interest to the scenario
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        RideID AddWaypoint(RideVector3 pos, float radius = 0);

        /// <summary>
        /// Adds an area of interest to the scenario
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        RideID AddWaypoint(RideID scenarioId, RideVector3 pos, float radius = 0);

        /// <summary>
        ///  Adds and area of interest to the scenario
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="flags"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        RideID AddWaypoint(RideVector3 pos, WaypointFlags flags, float radius = 0);

        /// <summary>
        ///  Adds and area of interest to the scenario
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="flags"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        RideID AddWaypoint(RideID scenarioId, RideVector3 pos, WaypointFlags flags, float radius = 0);

        /// <summary>
        /// Inserts the waypoint data into the api without creating a visual representation.
        /// Useful for when you place waypoints in the scene prior to starting the scene
        /// </summary>
        /// <param name="wp"></param>
        RideID InsertWaypoint(IWaypoint wp, IEnumerable<RideParameter> tags);

        /// <summary>
        /// Inserts the waypoint data into the api without creating a visual representation.
        /// Useful for when you place waypoints in the scene prior to starting the scene
        /// </summary>
        /// <param name="wp"></param>
        RideID InsertWaypoint(RideID scenarioId, IWaypoint wp, IEnumerable<RideParameter> tags);

        IWaypoint GetWaypointData(RideID waypointId);
        IWaypoint GetWaypointData(RideID scenarioId, RideID waypointId);

        /// <summary>
        /// Creates a path with the given waypoints
        /// </summary>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        IPath AddPath(IEnumerable<IWaypoint> waypoints);

        /// <summary>
        /// Adds a marker onto the terrain that displays a positon's coordinates.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        RideID AddLocationMarker(RideVector3 pos, string text);

        /// <summary>
        /// Adds a marker onto the terrain that displays a positon's coordinates.
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="pos"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        RideID AddLocationMarker(RideID scenarioId, RideVector3 pos, string text);

        /// <summary>
        /// Gets a location marker given its ID.
        /// </summary>
        /// <param name="markerId"></param>
        /// <returns></returns>
        ILocationMarker GetLocationMarker(RideID markerId);

        /// <summary>
        /// Gets all location markers in the default scenario.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILocationMarker> GetLocationMarkers();

        /// <summary>
        /// Gets all location markers in a scenario.
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <returns></returns>
        IEnumerable<ILocationMarker> GetLocationMarkers(RideID scenarioId);

        /// <summary>
        /// Removes a location marker from the default scenario.
        /// </summary>
        /// <param name="markerId"></param>
        void RemoveLocationMarker(RideID markerId);

        /// <summary>
        /// Remove all location markers from a given scenario.
        /// </summary>
        /// <param name="scenarioId"></param>
        void RemoveAllLocationMarkers(RideID scenarioId);

        /// <summary>
        /// Remove all location markers from the default scenario.
        /// </summary>
        void RemoveAllLocationMarkers();

        /// <summary>
        /// Adds ordnance to the terrain that shows its minimum safe distance.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="msd"></param>
        /// <param name="msdShielded"></param>
        /// <returns></returns>
        RideID AddOrdnance(RideVector3 pos, float msd, float msdShielded);

        /// <summary>
        /// Adds ordnance to the terrain that shows its minimum safe distance.
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="pos"></param>
        /// <param name="msd"></param>
        /// <param name="msdShielded"></param>
        /// <returns></returns>
        RideID AddOrdnance(RideID scenarioId, RideVector3 pos, float msd, float msdShielded);

        /// <summary>
        /// Adds ordnance to the terrain that shows its minimum safe distance.
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="prefabPath"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="flags"></param>
        /// <param name="msd"></param>
        /// <param name="msdShielded"></param>
        /// <returns></returns>
        RideID AddOrdnance(RideID scenarioId, string prefabPath, RideVector3 pos, RideQuaternion rot, OrdnanceAppearanceFlags flags, float msd, float msdShielded);

        IOrdnance GetOrdnanceData(RideID ordnanceid);
        IOrdnance GetOrdnanceData(RideID scenarioId, RideID ordnanceid);

        void RemoveOrdnance(RideID ordnanceid);

        /// <summary>
        /// Check if all the agents on a team are dead
        /// </summary>
        /// <param name="team">the team to check</param>
        /// <returns>true if all the agents on a team are dead, false if 1 or more are alive with health > 0 </returns>
        bool AreAllAgentsDead(Team team);

        /// <summary>
        /// Check if all the agents on a team are dead
        /// </summary>
        /// <param name="team">the team to check</param>
        /// <returns>true if all the agents on a team are dead, false if 1 or more are alive with health > 0 </returns>
        bool AreAllAgentsDead(RideID scenarioId, Team team);

        /// <summary>
        /// Returns an traversable container of all the agents in a scenario
        /// </summary>
        /// <returns>a list of all agents</returns>
        IEnumerable<RideID> GetAgents();

        /// <summary>
        /// Returns an traversable container of all the agents in a scenario
        /// </summary>
        /// <returns>a list of all agents</returns>
        IEnumerable<RideID> GetAgents(RideID scenarioId);

        /// <summary>
        /// gets all the agents on one specific team
        /// </summary>
        /// <param name="team">the team to get the agents on</param>
        /// <returns>all of the agents from the team passed in</returns>
        IEnumerable<RideID> GetAgents(Team team);

        /// <summary>
        /// gets all the agents on one specific team
        /// </summary>
        /// <param name="team">the team to get the agents on</param>
        /// <returns>all of the agents from the team passed in</returns>
        IEnumerable<RideID> GetAgents(RideID scenarioId, Team team);

        /// <summary>
        /// Returns all the agents within a certain range
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        IEnumerable<RideID> GetAgents(RideVector3 center, float radius);

        /// <summary>
        /// Returns all the agents within a certain range
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        IEnumerable<RideID> GetAgents(RideID scenarioId, RideVector3 center, float radius);


        /// <summary>
        /// Return all agents of a certain team within a range
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        IEnumerable<RideID> GetAgents( RideVector3 center, float radius, Team team);

        /// <summary>
        /// Return all agents of a certain team within a range
        /// </summary>
        /// <param name="scenarioId"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        IEnumerable<RideID> GetAgents(RideID scenarioId, RideVector3 center, float radius, Team team);

        /// <summary>
        /// Find and return all entities in the scene that match all of the given attributes
        /// </summary>
        /// <param name="attributes">The attribute flags that the entity is required to have</param>
        /// <returns>Entities matching the given attributes</returns>
        IEnumerable<RideID> GetEntitiesByAttributes(EntityAttributes attributes);

        /// <summary>
        /// Set the name of the entity
        /// </summary>
        /// <param name="entity">The unique id of the entity</param>
        /// <param name="name">The name. Does not have to be unique</param>
        void SetEntityName(RideID entity, string name);

        /// <summary>
        /// Returns the entity's name
        /// </summary>
        /// <param name="entity">The unique id of the entity</param>
        /// <returns></returns>
        string GetEntityName(RideID entity);

        /// <summary>
        /// Add the set of tags to the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tags"></param>
        void AddTagsToEntity(RideID entity, IEnumerable<RideParameter> tags);

        /// <summary>
        /// Adds metadata of the given object to the entity using the tag
        /// </summary>
        /// <param name="entity">The entity to receive the tag data</param>
        /// <param name="tag">The id of the value</param>
        /// <param name="value">The metadata</param>
        void AddTagToEntity(RideID entity, string tag, object value = null);

        /// <summary>
        /// Adds metadata of the given object to the entity using the tag
        /// </summary>
        /// <param name="entity">The entity to receive the tag data</param>
        /// <param name="tag">The id of the value</param>
        /// <param name="value">The metadata</param>
        void AddTagsToEntity(RideID entity, string[] tags, object value = null);

        /// <summary>
        /// Removes the metadata from the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tag"></param>
        void RemoveTagFromEntity(RideID entity, string tag);

        /// <summary>
        /// Gets the entity tag value
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="entity">The unique entity id</param>
        /// <param name="tag"></param>
        /// <returns>The metadata value. Default(T) is used if the tag isn't found</returns>
        T GetEntityTagValue<T>(RideID entity, string tag);

        /// <summary>
        /// Returns all the tags associated with the given entity
        /// </summary>
        /// <param name="entity">The unique id of the entity</param>
        /// <returns>All the entity's tags. Null if there aren't any</returns>
        string[] GetEntityTags(RideID entity);

        /// <summary>
        /// Modifies the tag metadata
        /// </summary>
        /// <param name="entity">The entity that has the tag</param>
        /// <param name="tag">The id of the metadata</param>
        /// <param name="newValue">The new value to replace the old metadata</param>
        void ModifyTagValue(RideID entity, string tag, float newValue);

        /// <summary>
        /// Searches all entities in the scnario that have the given tag
        /// </summary>
        /// <param name="tag">The tag to search for in the entities</param>
        /// <returns>A collection of entity ids</returns>
        IEnumerable<RideID> GetEntityByTag(string tag);

        /// <summary>
        /// Searches all entities in the scenario that have the given tag and meet the given condition
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <param name="condition"></param>
        /// <returns>A collection of entity ids</returns>
        IEnumerable<RideID> GetEntityByTagValue(string tag, float value, Func<float, float, bool> condition);

        /// <summary>
        /// Searches all entities in the scenario that have the given tag and are within the given distance
        /// </summary>
        /// <param name="position">The center point</param>
        /// <param name="tag">The id requirement of the entity within the distance</param>
        /// <param name="distance">The furthest distance from the center point that an entity can be before being ignored</param>
        /// <returns>A collection of entity ids</returns>
        IEnumerable<RideID> GetEntityByLocation(RideVector3 position, string tag, float distance = 0);

        /// <summary>
        /// Searches all entities in the scenario that have the given tag and are within the given distance
        /// </summary>
        /// <param name="position">The center point</param>
        /// <param name="tag">The id requirement of the entity within the distance</param>
        /// <param name="distance">The furthest distance from the center point that an entity can be before being ignored</param>
        /// <returns>A collection of entity ids</returns>
        IEnumerable<RideID> GetEntityByLocation(RideID scenarioId, RideVector3 position, string tag, float distance = 0);

        /// <summary>
        /// Adds the given event to the main scenario
        /// </summary>
        /// <param name="scenarioEvent">The event to add</param>
        /// <returns>The scenario event id</returns>
        RideID AddScenarioEvent(IScenarioEvent scenarioEvent);

        /// <summary>
        /// Adds the given event to the given scenario
        /// </summary>
        /// <param name="scenarioId">The scenario to use the event</param>
        /// <param name="e">The event to add</param>
        /// <returns>The scenario event id</returns>
        RideID AddScenarioEvent(RideID scenarioId, IScenarioEvent e);

        /// <summary>
        /// Starts the given scenario, enabling its scenario events and initializing its start time
        /// </summary>
        /// <param name="scenarioId">The scenario to start</param>
        void StartScenario(RideID scenarioId);

        /// <summary>
        /// Starts the main sceanrio, enabling its scenario events and initializing its start time
        /// </summary>
        void StartScenario();
 
        /// <summary>
        /// Returns the elapsed time of the given scenario
        /// </summary>
        /// <param name="scenarioId">The scenario</param>
        /// <returns>The elapsed time span, or null if scenario has not been started</returns>
        TimeSpan GetScenarioElapsedTime(RideID scenarioId);
 
        /// <summary>
        /// Returns the elapsed time of the main scenario
        /// </summary>
        /// <returns>The elapsed time span, or null if scenario has not been started</returns>
        TimeSpan GetScenarioElapsedTime();

        /// <summary>
        /// Is the given scenario running?
        /// </summary>
        /// <param name="scenarioId">Scenario to check</param>
        /// <returns>True if the given scenario has been started and not stopped, false otherwise</returns>
        bool IsScenarioRunning(RideID scenarioId);

        /// <summary>
        /// Is the current scenario running?
        /// </summary>
        /// <returns>True if the main scenario has been started and not stopped, false otherwise</returns>
        bool IsScenarioRunning();

        /// <summary>
        /// Stops the given scenario, disabling its scenario events
        /// </summary>
        /// <param name="scenarioId">Scenario to stop</param>
        void StopScenario(RideID scenarioId);

        /// <summary>
        /// Stops the main scenario, disabling its scenario events
        /// </summary>
        void StopScenario();

        /// <summary>
        /// Removes the given waypoint from the scenario.
        /// </summary>
        /// <param name="waypointId">RideID of the waypoint to be removed.</param>
        void RemoveWaypoint(RideID waypointId);

        /// <summary>
        /// Removes the given waypoint from the scenario.
        /// </summary>
        /// <param name="scenarioId">RideID of the scenario the waypoint exists in.</param>
        /// <param name="waypointId">RideID of the waypoint to be removed</param>
        void RemoveWaypoint(RideID scenarioId, RideID waypointId);

        /// <summary>
        /// Remove all the agents from all scenarios
        /// </summary>
        void RemoveAllAgents();
    }
}
