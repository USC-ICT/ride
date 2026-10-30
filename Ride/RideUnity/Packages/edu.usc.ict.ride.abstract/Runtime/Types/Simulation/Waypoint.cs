using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ride.Entities;

namespace Ride.Movement
{
    /// <summary>Provides a serializable implementation of <see cref="IWaypoint"/>.</summary>
    /// <remarks>
    /// Scenario and movement systems use waypoints to represent destinations, spawn locations,
    /// areas of interest, and points within an <see cref="IPath"/>.
    /// </remarks>
    public class Waypoint : IWaypoint
    {
        /// <inheritdoc/>
        public RideVector3 position {get; set; }

        /// <inheritdoc/>
        public RideQuaternion rotation { get; set; }

        /// <inheritdoc/>
        public WaypointFlags flags { get; set; }

        /// <inheritdoc/>
        public EntityAttributes attributes { get; set; }

        /// <inheritdoc/>
        public float radius { get; set; }

        /// <summary>Gets or sets non-serialized metadata associated with the waypoint.</summary>
        [JsonIgnore]
        public Dictionary<string, object> tags { get; set; }

        /// <inheritdoc/>
        public RideID id { get; set; }

        /// <inheritdoc/>
        public string name { get; set; }

        /// <summary>Initializes an empty waypoint for serialization or later initialization.</summary>
        public Waypoint() { }

        /// <summary>Initializes a waypoint whose name is derived from its identifier.</summary>
        /// <param name="id">Unique waypoint identifier.</param>
        /// <param name="position">World-space position.</param>
        /// <param name="rotation">World-space rotation.</param>
        /// <param name="flags">Waypoint behavior flags.</param>
        /// <param name="radius">Radius of the represented area.</param>
        public Waypoint(RideID id, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius) : this(id, id.ToString(), position, rotation, flags, radius) { }

        /// <summary>Initializes a named waypoint.</summary>
        /// <param name="id">Unique waypoint identifier.</param>
        /// <param name="name">Display name.</param>
        /// <param name="position">World-space position.</param>
        /// <param name="rotation">World-space rotation.</param>
        /// <param name="flags">Waypoint behavior flags.</param>
        /// <param name="radius">Radius of the represented area.</param>
        public Waypoint(RideID id, string name, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius)
        {
            Init(id, name, position, rotation, flags, radius);
        }

        /// <inheritdoc/>
        public void Init(RideID id, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius) => Init(id, id.ToString(), position, rotation, flags, radius);

        /// <inheritdoc/>
        public void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, WaypointFlags flags, float radius)
        {
            this.id = id;
            this.name = name;
            this.position = position;
            this.rotation = rotation;
            this.flags = flags;
            this.radius = radius;
        }

        /// <inheritdoc/>
        public bool HasAttributes(EntityAttributes att)
        {
            return (attributes & att) == att;
        }
    }
}
