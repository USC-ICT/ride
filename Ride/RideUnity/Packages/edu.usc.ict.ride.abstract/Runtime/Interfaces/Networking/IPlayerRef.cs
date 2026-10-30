namespace Ride.Networking
{
    /// <summary>
    /// Represents an abstract reference to a player in a networked simulation. It lets RIDE pass player identity
    /// through room callbacks, input and event routing, and network object creation without exposing a networking
    /// backend's native player-reference type. Concrete integrations for frameworks such as Fusion or Photon wrap
    /// their player handle behind this interface, which is used primarily with <see cref="INetworkSystem"/> and
    /// <see cref="INetworkRunner"/>.
    /// </summary>
    public interface IPlayerRef
    {
    }
}
