using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ride.Entities;

namespace Ride.Movement
{
    /// <summary>
    /// Concrete simple implementation of IWaypoint
    /// </summary>
    public class Waypoint : IWaypoint
    {
        public RideVector3 position {get; set; }
        public RideQuaternion rotation { get; set; }
        public WaypointFlags flags { get; set; }
        public EntityAttributes attributes { get; set; }
        public float radius { get; set; }
        [JsonIgnore]
        public Dictionary<string, object> tags { get; set; }
        public RideID id { get; set; }
        public string name { get; set; }

        public Waypoint() { }

        public Waypoint(RideID id, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius) : this(id, id.ToString(), position, rotation, flags, radius) { }

        public Waypoint(RideID id, string name, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius)
        {
            Init(id, name, position, rotation, flags, radius);
        }

        public void Init(RideID id, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius) => Init(id, id.ToString(), position, rotation, flags, radius);

        public void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius)
        {
            this.id = id;
            this.name = name;
            this.position = position;
            this.rotation = rotation;
            this.flags = flags;
            this.radius = radius;
        }

        public bool HasAttributes(EntityAttributes att)
        {
            return (attributes & att) == att;
        }
    }
}
