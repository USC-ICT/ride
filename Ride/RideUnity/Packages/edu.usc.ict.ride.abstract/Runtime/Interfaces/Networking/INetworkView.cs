namespace Ride.Networking
{
    /// <summary>Specifies the reliability and ordering guarantees used when sending a network message.</summary>
    public enum DeliveryMode
    {
        /// <summary>Sends once without acknowledgement while preserving message order.</summary>
        Unreliable = 0,

        /// <summary>Resends until acknowledged while preserving message order.</summary>
        Reliable = 1,

        /// <summary>Sends once without acknowledgement or ordering guarantees.</summary>
        UnreliableUnsequenced = 2,

        /// <summary>Resends until acknowledged without enforcing message order.</summary>
        ReliableUnsequenced = 3
    }

    /// <summary>Enum of "target" options for RPCs. These define which remote clients get your RPC call. </summary>
    public enum RpcTarget
    {
        /// <summary>Sends the RPC to everyone else and executes it immediately on this client. Player who join later will not execute this RPC.</summary>
        All,

        /// <summary>Sends the RPC to everyone else. This client does not execute the RPC. Player who join later will not execute this RPC.</summary>
        Others,

        /// <summary>Sends the RPC to MasterClient only. Careful: The MasterClient might disconnect before it executes the RPC and that might cause dropped RPCs.</summary>
        MasterClient,

        /// <summary>Sends the RPC to everyone else and executes it immediately on this client. New players get the RPC when they join as it's buffered (until this client leaves).</summary>
        AllBuffered,

        /// <summary>Sends the RPC to everyone. This client does not execute the RPC. New players get the RPC when they join as it's buffered (until this client leaves).</summary>
        OthersBuffered,

        /// <summary>Sends the RPC to everyone (including this client) through the server.</summary>
        /// <remarks>
        /// This client executes the RPC like any other when it received it from the server.
        /// Benefit: The server's order of sending the RPCs is the same on all clients.
        /// </remarks>
        AllViaServer,

        /// <summary>Sends the RPC to everyone (including this client) through the server and buffers it for players joining later.</summary>
        /// <remarks>
        /// This client executes the RPC like any other when it received it from the server.
        /// Benefit: The server's order of sending the RPCs is the same on all clients.
        /// </remarks>
        AllBufferedViaServer
    }


    /// <summary>
    /// Lite - OpRaiseEvent lets you chose which actors in the room should receive events.
    /// By default, events are sent to "Others" but you can overrule this.
    /// </summary>
    public enum ReceiverGroup : byte
    {
        /// <summary>Default value (not sent). Anyone else gets my event.</summary>
        Others = 0,

        /// <summary>Everyone in the current room (including this peer) will get this event.</summary>
        All = 1,

        /// <summary>The server sends this event only to the actor with the lowest actorNumber.</summary>
        /// <remarks>The "master client" does not have special rights but is the one who is in this room the longest time.</remarks>
        MasterClient = 2,
    }

    /// <summary>Provides backend-independent ownership, identity, RPC, and custom-property access for a networked object.</summary>
    /// <remarks>Used by RIDE network agents and implemented by adapters such as Photon and Fusion network views.</remarks>
    public interface INetworkView
    {
        /// <summary>Gets whether the local client owns or controls this network view.</summary>
        bool isMine { get; }

        /// <summary>Gets the unique network view identifier.</summary>
        //dynamic viewId { get; }
        int viewId { get; }


        /// <summary>Gets the unique actor identifier that owns this view.</summary>
        int actorId { get; }

//#if !FUSION_NETWORKING
        /// <summary>
        /// Invokes a remote procedure on the clients selected by <paramref name="target"/>.
        /// </summary>
        /// <param name="function">The name of the function on the remote machine</param>
        /// <param name="target">The receiver that will call the function</param>
        /// <param name="parameters">The parameters of the function</param>
        void RPC(string function, RpcTarget target, params object[] parameters);

        /// <summary>
        /// Invokes a remote procedure on a specific player.
        /// </summary>
        /// <param name="function">The name of the function on the remote machine</param>
        /// <param name="targetPlayer">The receiving player that will call the function</param>
        /// <param name="parameters">The parameters of the function</param>
        void RPC(string function, Player targetPlayer, params object[] parameters);


        /// <summary>
        /// Sets an integer custom property that is synchronized with this view.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetCustomProperty(string property, int value);

        /// <summary>
        /// Sets a floating-point custom property that is synchronized with this view.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetCustomProperty(string property, float value);

        /// <summary>
        /// Sets a Boolean custom property that is synchronized with this view.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetCustomProperty(string property, bool value);

        /// <summary>
        /// Sets a string custom property that is synchronized with this view.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetCustomProperty(string property, string value);

        /// <summary>
        /// Gets a synchronized custom property from this view.
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="property">The name of the property</param>
        /// <returns>The network stored value of the property</returns>
        T GetCustomProperty<T>(string property);

        /// <summary>
        /// Attempts to get a synchronized custom property from this view.
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="property">The name of the property</param>
        /// <param name="value">When this method returns, contains the stored property value if found.</param>
        /// <returns><see langword="true"/> if the property exists; otherwise, <see langword="false"/>.</returns>
        bool TryGetCustomProperty<T>(string property, out T value);
//#elif FUSION_NETWORKING
        /// <summary>
        /// Assigns this view to a simulation team.
        /// </summary>
        /// <param name="team">The team to assign.</param>
        void SetTeam(Team team);
        //#endif

        /// <summary>
        /// Registers a request for the specified player to join the room represented by this view.
        /// </summary>
        /// <param name="playerRef">The backend player-reference identifier.</param>
        void RegisterRoomJoinRequest(int playerRef);
    }
}
