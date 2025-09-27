using System;
using Ride.Entities;

namespace Ride
{
    [Serializable]
    public class EntityData : IEntity
    {
        public RideID id { get; set; } = RideID.Null;
        public string name { get; set; } = string.Empty;
        public string prefab = string.Empty;
        public RideVector3 position = RideVector3.zero;
        public RideQuaternion rotation = RideQuaternion.identity;
        public EntityAttributes attributes { get; set; } = 0;

        public EntityData() { }

        public EntityData(RideID entId, string entName = "", string entPrefab = "", EntityAttributes entAtt = 0)
        {
            id = entId;
            name = entName;
            prefab = entPrefab;
            attributes = entAtt;
        }

        public bool HasAttributes(EntityAttributes att) => (attributes & att) == att;

        public override string ToString() => $"EntityData(Name={name}, Prefab={prefab}, ID={id}, Pos={position}, Rot={rotation}, Attr={attributes})";
    }
}
