namespace Ride.Networking
{
    public struct SendOptions
    {
        public static readonly SendOptions SendReliable;
        public static readonly SendOptions SendUnreliable;

        /// <summary>
        /// Chose the DeliveryMode for this operation/message. Defaults to Unreliable.
        /// </summary>
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
