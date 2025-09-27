namespace Ride.Networking
{
    public enum DeliveryMode
    {
        //
        // Summary:
        //     The operation/message gets sent just once without acknowledgement or repeat.
        //     The sequence (order) of messages is guaranteed.
        Unreliable = 0,
        //
        // Summary:
        //     The operation/message asks for an acknowledgment. It's resent until an ACK arrived.
        //     The sequence (order) of messages is guaranteed.
        Reliable = 1,
        //
        // Summary:
        //     The operation/message gets sent once (unreliable) and might arrive out of order.
        //     Best for your own sequencing (e.g. for streams).
        UnreliableUnsequenced = 2,
        //
        // Summary:
        //     The operation/message asks for an acknowledgment. It's resent until an ACK arrived
        //     and might arrive out of order. Best for your own sequencing (e.g. for streams).
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

    /// <summary>
    /// GameObjects can be instantiated as "networked GameObjects" with a NetworkView component. It identifies the object and the owner (or controller).
    /// The player who's in control, updates everyone else.
    /// Typically, you would add a NetworkView to a prefab, select the Observed component for it and use TSSNetwork.Instantiate to create an instance.
    /// The observed component of a NetworkView is in charge of writing (and reading) the state of the networked object several times a second.
    /// To do so, a script must implement INetworkObservable, which defines OnNetworkSerializeView
    /// </summary>
    public interface INetworkView
    {
        /// <summary>
        /// true if this is the local client
        /// </summary>
        bool isMine { get; }

        /// <summary>
        /// The network view id (this is unique)
        /// </summary>
        //dynamic viewId { get; }
        int viewId { get; }


        /// <summary>
        /// The network actor id (this is unique)
        /// </summary>
        int actorId { get; }

//#if !FUSION_NETWORKING
        /// <summary>
        /// Remote Procedural call. This allows you to call a function on a connected client
        /// </summary>
        /// <param name="function">The name of the function on the remote machine</param>
        /// <param name="target">The receiver that will call the function</param>
        /// <param name="parameters">The parameters of the function</param>
        void RPC(string function, RpcTarget target, params object[] parameters);

        /// <summary>
        /// Remote Procedural call. This allows you to call a function on a connected client
        /// </summary>
        /// <param name="function">The name of the function on the remote machine</param>
        /// <param name="targetPlayer">The receiving player that will call the function</param>
        /// <param name="parameters">The parameters of the function</param>
        void RPC(string function, Player targetPlayer, params object[] parameters);


        /// <summary>
        /// Sets a value on this view that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetCustomProperty(string property, int value);

        /// <summary>
        /// Sets a value on this view that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetCustomProperty(string property, float value);

        /// <summary>
        /// Sets a value on this view that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetCustomProperty(string property, bool value);

        /// <summary>
        /// Sets a value on this view that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetCustomProperty(string property, string value);

        /// <summary>
        /// Returns a network synced property value of the view
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="property">The name of the property</param>
        /// <returns>The network stored value of the property</returns>
        T GetCustomProperty<T>(string property);

        /// <summary>
        /// Returns a network synced property value of the view if it exists
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The network stored value of the property</param>
        /// <returns>Returns true if property exists, false if it does not</returns>
        bool TryGetCustomProperty<T>(string property, out T value);
//#elif FUSION_NETWORKING
        void SetTeam(Team team);
        //#endif

        void RegisterRoomJoinRequest(int playerRef);
    }
}
