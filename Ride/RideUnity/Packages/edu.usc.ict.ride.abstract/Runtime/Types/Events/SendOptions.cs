namespace Ride.Networking
{
    /// <summary>
    /// Collects transport-level send settings for a raised network event, such as delivery reliability,
    /// encryption, channel selection, and backend delivery mode.
    /// </summary>
    /// <remarks>
    /// Ride uses this alongside <see cref="RaiseEventOptions"/> when dispatching custom network events.
    /// In the current Photon-backed implementation, these fields map directly onto
    /// <c>ExitGames.Client.Photon.SendOptions</c>.
    /// Relevant docs:
    /// https://doc.photonengine.com/realtime/current/reference/dotnet-api/struct_exit_games_1_1_client_1_1_photon_1_1_send_options.html
    /// </remarks>
    public struct SendOptions
    {
        /// <summary>Predefined send options for reliable delivery using the default channel and no encryption.</summary>
        public static readonly SendOptions SendReliable;

        /// <summary>Predefined send options for unreliable delivery using the default channel and no encryption.</summary>
        public static readonly SendOptions SendUnreliable;

        /// <summary>Chose the DeliveryMode for this operation/message. Defaults to Unreliable.</summary>
        public DeliveryMode DeliveryMode;
        
        /// <summary>
        /// If true the operation/message gets encrypted before it's sent. Defaults to false.
        /// 
        /// Before encryption can be used, it must be established. Check PhotonPeer.IsEncryptionAvailable is true. 
        /// </summary>
        public bool Encrypt;

        /// <summary>
        /// The Enet channel to send in. Defaults to 0.
        /// 
        /// Channels in Photon relate to "message channels". Each channel is a sequence of messages.
        /// </summary>
        public byte Channel;
        
        /// <summary>
        /// Sets the DeliveryMode either to true: Reliable or false: Unreliable, overriding any current value.
        /// 
        /// Use this to conveniently select reliable/unreliable delivery.
        /// </summary>
        public bool Reliability { get; set; }
    }
}
