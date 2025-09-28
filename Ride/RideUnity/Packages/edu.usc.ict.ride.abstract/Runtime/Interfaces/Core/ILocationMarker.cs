using Ride;
using Ride.Entities;
namespace Ride.UI {
    /// <summary>
    /// Represents a coordinate display marker in the world.
    /// </summary>
    public interface ILocationMarker : IEntity {
        /// <summary>
        /// Position in world space
        /// </summary>
        RideVector3 position { get; set; }

        /// <summary>
        /// Rotation in world space
        /// </summary>
        RideQuaternion rotation { get; set; }

        /// <summary>
        /// Displayed coordinate text.
        /// </summary>
        string text { get; set; }

        void Init(RideID id, RideVector3 position, RideQuaternion rotation, string text);

        void Init(RideID id, string name, RideVector3 position, RideQuaternion rotation, string text);
    }
}
