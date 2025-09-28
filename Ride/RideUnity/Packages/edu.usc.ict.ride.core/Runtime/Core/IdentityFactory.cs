
namespace Ride
{
    /// <summary>
    /// Factory for creating <see cref="RideID"/>s.
    /// Use <see cref="CreateId"/> to generate a new, unique ID to use in Ride.
    /// </summary>
    public static class IdentityFactory
    {
        /// <summary>
        /// start at 1 because 0 is RideId.NULL
        /// </summary>
        static int last = 1;

        public static RideID CreateId()
        {
            RideID id = new RideID(last++);
            return id;
        }
    }
}
