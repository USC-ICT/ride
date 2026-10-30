using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.WorldState
{
    /// <summary>Represents a typed listener invoked when the world-state system dispatches an event.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="simulationEvent">The marker recorded in world-state history.</param>
    /// <param name="eventData">The event payload.</param>
    public delegate void WorldSimulationEvent<T>(WorldEventMarker simulationEvent, T eventData);

    /// <summary>Provides a base payload type for simulation events that do not require specialized data.</summary>
    public class SimulationEventData { }

    /// <summary>Stores world-event history and simulation clock configuration for <see cref="IWorldStateSystem"/>.</summary>
    public interface IWorldState
    {
        /// <summary>Gets or sets the chronological event markers recorded by the simulation.</summary>
        List<WorldEventMarker> worldEvents { get; set; }
        //Dictionary<WorldEvent, List<WorldSimulationEvent>> worldSimulationEvents { get; set; }
        /// <summary>Gets or sets the simulation clock's starting date and time.</summary>
        DateTime timeOfDay { get; set; }
        /// <summary>Gets or sets the number of simulation seconds advanced per Unity game-time second.</summary>
        float timeIncrement { get; set; }
    }
}
