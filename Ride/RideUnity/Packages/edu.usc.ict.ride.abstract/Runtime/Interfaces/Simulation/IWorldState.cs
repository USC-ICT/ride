using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.WorldState
{
    /// <summary>
    /// This delegate is a function which is passed in to the IWorldState worldSimulationEvents Dictionary List which is a list of functions which are called when the WorldEvent occurs
    /// </summary>
    /// <param name="simulationEvent">The data of the event which is also sent to the user delegate function</param>
    public delegate void WorldSimulationEvent<T>(WorldEventMarker simulationEvent, T eventData);

    public class SimulationEventData { }
    public interface IWorldState
    {
        List<WorldEventMarker> worldEvents { get; set; }
        //Dictionary<WorldEvent, List<WorldSimulationEvent>> worldSimulationEvents { get; set; }
        DateTime timeOfDay { get; set; }
        float timeIncrement { get; set; }
    }
}
