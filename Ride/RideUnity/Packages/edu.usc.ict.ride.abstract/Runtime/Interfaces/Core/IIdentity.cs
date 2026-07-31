
namespace Ride
{
    /// <summary>
    /// Defines an interface for any object that can be uniquely identified by a <see cref="RideID"/>.
    /// 
    /// This is commonly implemented by systems, entities, and other runtime objects
    /// to support lookup, registration, or persistence across simulation layers.
    /// </summary>
    public interface IIdentity
    {
        /// <summary>
        /// Gets the globally unique identifier for this object.
        /// See <see cref="RideID"/> for implementation details.
        /// </summary>
        RideID id { get; }

        /// <summary>
        /// Gets the human-readable name for this object.
        /// This is typically used for debugging, UI labels, or logging - and may not be unique.
        /// </summary>
        string name { get; }
    }
}
