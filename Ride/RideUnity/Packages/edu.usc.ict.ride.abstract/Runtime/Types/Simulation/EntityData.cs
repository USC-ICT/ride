using System;
using Ride.Entities;

namespace Ride
{
    /// <summary>Stores the serializable identity, transform, prefab, and attribute data for a simulation entity.</summary>
    /// <remarks>
    /// Entity creation code uses this data with <see cref="Ride.WorldState.EntityCreatedEvent"/> to describe logical
    /// entities independently of their engine-specific objects. It is also used by camera and
    /// GameObject integration code when publishing entity lifecycle events.
    /// </remarks>
    [Serializable]
    public class EntityData : IEntity
    {
        /// <inheritdoc/>
        public RideID id { get; set; } = RideID.Null;

        /// <inheritdoc/>
        public string name { get; set; } = string.Empty;

        /// <summary>Prefab or resource name used to create the entity's visual representation.</summary>
        public string prefab = string.Empty;

        /// <summary>World-space position of the entity.</summary>
        public RideVector3 position = RideVector3.zero;

        /// <summary>World-space rotation of the entity.</summary>
        public RideQuaternion rotation = RideQuaternion.identity;

        /// <inheritdoc/>
        public EntityAttributes attributes { get; set; } = 0;

        /// <summary>Initializes empty entity data for serialization or object-initializer use.</summary>
        public EntityData() { }

        /// <summary>Initializes entity data with identity and optional resource metadata.</summary>
        /// <param name="entId">Unique entity identifier.</param>
        /// <param name="entName">Display name of the entity.</param>
        /// <param name="entPrefab">Prefab or resource name used to represent the entity.</param>
        /// <param name="entAtt">Attribute flags assigned to the entity.</param>
        public EntityData(RideID entId, string entName = "", string entPrefab = "", EntityAttributes entAtt = 0)
        {
            id = entId;
            name = entName;
            prefab = entPrefab;
            attributes = entAtt;
        }

        /// <inheritdoc/>
        public bool HasAttributes(EntityAttributes att) => (attributes & att) == att;

        /// <inheritdoc/>
        public override string ToString() => $"EntityData(Name={name}, Prefab={prefab}, ID={id}, Pos={position}, Rot={rotation}, Attr={attributes})";
    }
}
