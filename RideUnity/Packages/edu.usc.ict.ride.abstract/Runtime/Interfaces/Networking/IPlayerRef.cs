namespace Ride.Networking
{
    /// <summary>
    /// Marker interface representing an abstract reference to a player in a networked simulation context.
    /// 
    /// This interface allows systems to refer to players in a backend-agnostic way, enabling support
    /// for multiple networking frameworks (e.g., Fusion, Photon) without coupling to specific implementations.
    /// 
    /// Implemented by classes such as <c>FusionNetwork</c> and <c>PhotonNetwork</c> to provide player-related functionality
    /// like identity, input tracking, and event routing.
    ///
    /// Typically used in conjunction with <see cref="INetworkSystem"/> and other Ride.Networking components.
    /// </summary>
    public interface IPlayerRef
    {
    }
}
