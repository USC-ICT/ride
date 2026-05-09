namespace Ride
{
    /// <summary>
    /// Marker interface for a Ride system that exposes an on-screen debug log implementation.
    /// </summary>
    /// <remarks>
    /// This abstraction exists so Ride can reference an on-screen debug-log capability without taking
    /// a direct compile-time dependency on a specific UI/logging implementation. In the current core
    /// implementation, the concrete system is a thin wrapper around <c>VHAssets.DebugOnScreenLog</c>.
    /// </remarks>
    public interface IDebugOnScreenLog : IRideSystem
    {
    }
}
