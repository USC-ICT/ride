using System.Collections.Generic;

namespace Ride.WorldState
{
    /// <summary>
    /// Meta data about a meaningful event that occured during the scenario
    /// </summary>
    public struct WorldEventMarker
    {
        public string worldEvent;
        public float timeOfEvent;
        public List<RideID> rideIDs;


        public WorldEventMarker(string worldEvent, float time, List<RideID>rideIDs)
        {
            this.worldEvent = worldEvent;
            this.timeOfEvent = time;
            this.rideIDs = rideIDs;
        }

        public override string ToString()
        {
            string rideIdList = string.Join(",", rideIDs?.ConvertAll(r => r.id.ToString()) ?? new List<string>());
            return $"WorldEventMarker(worldEvent={worldEvent}, timeOfEvent={timeOfEvent}, rideIDs=[{rideIdList}])";
        }
    }
}
