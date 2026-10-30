using System.Collections;
using System.Collections.Generic;
using Ride.Combat;
using Ride.Entities;

namespace Ride.Networking
{
    /// <summary>Represents a callback invoked after the local client connects to the network.</summary>
    public delegate void OnNetworkConnect();

    /// <summary>Represents a callback invoked after the local client disconnects from the network.</summary>
    public delegate void OnNetworkDisconnect();

    /// <summary>Represents a callback invoked after the local client joins a room.</summary>
    public delegate void OnJoinRoom();

    /// <summary>Represents a callback invoked after the local client leaves a room.</summary>
    public delegate void OnLeaveRoom();

    /// <summary>Represents a callback that associates a RIDE agent with its network view after joining a room.</summary>
    /// <param name="agent">The RIDE identifier of the agent.</param>
    /// <param name="view">The network view assigned to the agent.</param>
    public delegate void OnSetupNetworkAgent(RideID agent, INetworkView view);

    /// <summary>Represents a callback invoked when a custom network event is received.</summary>
    /// <param name="evCode">The application-defined event code.</param>
    /// <param name="contents">The event payload.</param>
    public delegate void OnNetworkEventRaised(byte evCode, object[] contents);

    /// <summary>Represents a callback invoked when a remote player enters the current room.</summary>
    /// <param name="player">The player that entered.</param>
    public delegate void OnPlayerEnterRoom(Player player);

    /// <summary>Represents a callback invoked when a remote player leaves the current room.</summary>
    /// <param name="player">The player that left.</param>
    public delegate void OnPlayerLeaveRoom(Player player);

    /// <summary>Represents a callback invoked when room authority moves to a new master client.</summary>
    /// <param name="newMasterClient">The new master client.</param>
    public delegate void OnMasterClientSwitched(Player newMasterClient);

    /// <summary>Represents a Fusion room-join callback with backend-independent runner and player handles.</summary>
    /// <param name="runner">The runner managing the session.</param>
    /// <param name="player">The player that joined.</param>
    public delegate void OnJoinRoom_Fusion(INetworkRunner runner, IPlayerRef player);

    /// <summary>Represents a Fusion input callback with backend-independent runner and input handles.</summary>
    /// <param name="runner">The runner requesting input.</param>
    /// <param name="input">The input container to populate.</param>
    public delegate void OnInput_Fusion(INetworkRunner runner, INetworkInput input);

    /// <summary>Represents a request to spawn a named prefab for a player.</summary>
    /// <param name="player">The player that will receive authority over the spawned object.</param>
    /// <param name="prefabName">The registered prefab name.</param>
    public delegate void OnRequestSpawnPrefab(IPlayerRef player, string prefabName);

    /// <summary>Describes the network client's connection and matchmaking state.</summary>
    public enum ClientState
    {
        /// <summary>Peer is created but not used yet.</summary>
        PeerCreated,

        /// <summary>Transition state while connecting to a server. On the Photon Cloud this sends the AppId and AuthenticationValues (UserID).</summary>
        Authenticating,

        /// <summary>Not Used.</summary>
        Authenticated,

        /// <summary>The client sent an OpJoinLobby and if this was done on the Master Server, it will result in. Depending on the lobby, it gets room listings.</summary>
        JoiningLobby,

        /// <summary>The client is in a lobby, connected to the MasterServer. Depending on the lobby, it gets room listings.</summary>
        JoinedLobby,

        /// <summary>Transition from MasterServer to GameServer.</summary>
        DisconnectingFromMasterServer,

        /// <summary>Transition to GameServer (client authenticates and joins/creates a room).</summary>
        ConnectingToGameServer,

        /// <summary>Connected to GameServer (going to auth and join game).</summary>
        ConnectedToGameServer,

        /// <summary>Transition state while joining or creating a room on GameServer.</summary>
        Joining,

        /// <summary>The client entered a room. The CurrentRoom and Players are known and you can now raise events.</summary>
        Joined,

        /// <summary>Transition state when leaving a room.</summary>
        Leaving,

        /// <summary>Transition from GameServer to MasterServer (after leaving a room/game).</summary>
        DisconnectingFromGameServer,

        /// <summary>Connecting to MasterServer (includes sending authentication values).</summary>
        ConnectingToMasterServer,

        /// <summary>The client disconnects (from any server). This leads to state Disconnected.</summary>
        Disconnecting,

        /// <summary>The client is no longer connected (to any server). Connect to MasterServer to go on.</summary>
        Disconnected,

        /// <summary>Connected to MasterServer. You might use matchmaking or join a lobby now.</summary>
        ConnectedToMasterServer,

        /// <summary>Client connects to the NameServer. This process includes low level connecting and setting up encryption. When done, state becomes ConnectedToNameServer.</summary>
        ConnectingToNameServer,

        /// <summary>Client is connected to the NameServer and established encryption already. You should call OpGetRegions or ConnectToRegionMaster.</summary>
        ConnectedToNameServer,

        /// <summary>Clients disconnects (specifically) from the NameServer (usually to connect to the MasterServer).</summary>
        DisconnectingFromNameServer
    }

    /// <summary>Describes a player represented in a network room.</summary>
    public class Player
    {
        /// <summary>
        /// Unique value assigned by the network system
        /// </summary>
        public int ActorNumber { get; }

        /// <summary>
        /// Display name of the player. Not Unique
        /// </summary>
        public string NickName { get; }

        /// <summary>
        /// The user id of the player. Available when the room was created
        /// </summary>
        public string UserId { get; }

        /// <summary>Initializes a network player description.</summary>
        /// <param name="actorNumber">The transport-assigned actor number.</param>
        /// <param name="nickName">The player's display name.</param>
        /// <param name="userId">The player's user identifier.</param>
        public Player(int actorNumber, string nickName, string userId) { ActorNumber = actorNumber; NickName = nickName; UserId = userId; }

//#if PHOTON_UNITY_NETWORKING
//        public static explicit operator Player(Photon.Realtime.Player p) => new Player(p.ActorNumber, p.NickName, p.UserId);
//#endif
    }

    /// <summary>Describes a discoverable or joined network room and its players.</summary>
    public class Room
    {
        /// <summary>Gets the room name.</summary>
        public string Name { get; }

        /// <summary>Gets the current number of players in the room.</summary>
        public int PlayerCount { get; }

        /// <summary>Gets the maximum number of players allowed in the room.</summary>
        public int MaxPlayers { get; }

        /// <summary>Gets players keyed by actor number, or <see langword="null"/> when the backend does not expose the roster.</summary>
        public Dictionary<int, Player> Players { get; }  // can be null if this information about the room isn't available.

        /// <summary>Initializes a network room description.</summary>
        /// <param name="name">The room name.</param>
        /// <param name="playerCount">The current player count.</param>
        /// <param name="maxPlayers">The maximum player count.</param>
        /// <param name="players">The optional roster keyed by actor number.</param>
        public Room(string name, int playerCount, int maxPlayers, Dictionary<int, Player> players) { Name = name; PlayerCount = playerCount; MaxPlayers = maxPlayers; Players = players; }
    }

    // needs to match Fusion.GameMode
    /// <summary>Specifies the Fusion-compatible topology used to create or join a session.</summary>
    public enum GameModeFusion
    {
        /// <summary>Runs a local single-player simulation.</summary>
        Single = 1,

        /// <summary>Shares state authority among connected peers.</summary>
        Shared,

        /// <summary>Runs as a dedicated server.</summary>
        Server,

        /// <summary>Runs as both server and local client.</summary>
        Host,

        /// <summary>Runs as a client connected to a server or host.</summary>
        Client,

        /// <summary>Hosts a session when none exists; otherwise joins as a client.</summary>
        AutoHostOrClient
    }

    /// <summary>Collects the backend-independent settings used to start a Fusion-compatible session.</summary>
    public struct SessionRequest
    {
        /// <summary>The RIDE identifier of the requesting user.</summary>
        public RideID UserID;

        /// <summary>The requested network topology.</summary>
        public GameModeFusion GameMode;

        /// <summary>The user's display name.</summary>
        public string DisplayName;

        /// <summary>The session name to create or join.</summary>
        public string SessionName;

        /// <summary>The maximum number of players permitted in the session.</summary>
        public int MaxPlayers;

        /// <summary>The optional custom lobby used for session discovery.</summary>
        public string CustomLobby;

        /// <summary>The optional server or host address.</summary>
        public string IPAddress;

        /// <summary>The optional server or host port.</summary>
        public ushort Port;
    }

    /// <summary>Represents the network-facing operations exposed by a RIDE simulation agent.</summary>
    public interface INetworkAgent
    {
        /// <summary>
        /// Notifies the remote agent that an entity received damage.
        /// </summary>
        /// <param name="entityId">The damaged entity.</param>
        /// <param name="damage">The damage amount.</param>
        void SendReceivingDamage(RideID entityId, float damage);

        /// <summary>
        /// Sends the result of damage inflicted by the agent.
        /// </summary>
        /// <param name="attackResult">The attack result to synchronize.</param>
        void SendInflictingDamage(IAttackResult attackResult);
    }


    /// <summary>Provides backend-independent connection, room, object, and event networking for RIDE simulations.</summary>
    /// <remarks>Implemented by RIDE Core network systems and concrete Photon or Fusion adapters.</remarks>
    public interface INetworkSystem : IRideSystem
    {
        /// <summary>
        /// Registers the network operations associated with a RIDE agent.
        /// </summary>
        /// <param name="id">The agent's RIDE identifier.</param>
        /// <param name="agent">The network agent implementation.</param>
        void AddNetworkAgent(RideID id, INetworkAgent agent);

        /// <summary>Determines whether a network agent is registered for a RIDE identifier.</summary>
        /// <param name="id">The RIDE identifier to test.</param>
        /// <returns><see langword="true"/> when an agent is registered; otherwise, <see langword="false"/>.</returns>
        bool IsNetworkAgent(RideID id);

        /// <summary>Gets the network agent registered for a RIDE identifier.</summary>
        /// <param name="id">The RIDE identifier to resolve.</param>
        /// <returns>The registered network agent.</returns>
        INetworkAgent GetNetworkAgent(RideID id);

        /// <summary>
        /// The number of network rooms created
        /// </summary>
        int numRooms { get; }

        /// <summary>
        /// The number of players in the room or the lobby
        /// </summary>
        int numPlayers { get; }

        /// <summary>
        /// The number of players in network rooms
        /// </summary>
        int numPlayersInRooms { get; }

        /// <summary>
        /// The number of players in the lobby that haven't joined a room
        /// </summary>
        int numPlayersLookingForRooms { get; }

        /// <summary>
        /// True if this machine is the master client in a network room
        /// </summary>
        bool isMasterClient { get; }

        /// <summary>
        /// The list of network rooms
        /// </summary>
        IEnumerable<Room> rooms { get; }

        /// <summary>
        /// The current connection state of this machine
        /// </summary>
        ClientState networkClientState { get; }

        /// <summary>
        /// The networked player this machine represents
        /// </summary>
        INetworkView localPlayer { get; }

        /// <summary>
        /// Returns true if the client is ready for match making. This does not mean that the client is in a game
        /// </summary>
        bool isConnected { get; }

        /// <summary>
        /// True if the local player is in a networked room
        /// </summary>
        bool isInRoom { get; }

        /// <summary>
        /// True if this instance is the server
        /// </summary>
        bool isServer { get; }

        /// <summary>
        /// The current networked room that you are in. Null if you are not in a room
        /// </summary>
        Room currentRoom { get; }

        /// <summary>
        /// Calculates and returns the latency of the room in ms
        /// </summary>
        /// <param name="roomName">The room to measure.</param>
        /// <returns>The estimated latency in milliseconds.</returns>
        int CalculateRoomPing(string roomName);

        /// <summary>
        /// The local player's nickname so that other clients know who they are
        /// </summary>
        string playerNickName { get; set; }

        /// <summary>
        /// Connect to the network
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the network
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Join the lobby to see the available rooms
        /// </summary>
        void JoinLobby();

        /// <summary>
        /// Leave the lobby
        /// </summary>
        void LeaveLobby();

        /// <summary>
        /// Creates a networked room for clients to join
        /// </summary>
        /// <param name="name">The display name of the room</param>
        void CreateRoom(string name);


        /// <summary>
        /// Creates a networked session using Fusion-compatible settings. A session is equivalent to a room in PUN.
        /// </summary>
        /// <param name="request">The session settings.</param>
        void CreateRoom(SessionRequest request);

        /// <summary>
        /// Makes a request for the local player to join a networked room. If successful, OnJoinedRoom is dispatched
        /// </summary>
        /// <param name="name">The name of the room to join</param>
        void JoinRoom(string name);

        /// <summary>Leaves the room currently occupied by the local player.</summary>
        void LeaveRoom();

        /// <summary>
        /// Sets an integer custom property that is synchronized with the room.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetRoomCustomProperty(string property, int value);

        /// <summary>
        /// Sets a floating-point custom property that is synchronized with the room.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetRoomCustomProperty(string property, float value);

        /// <summary>
        /// Sets a Boolean custom property that is synchronized with the room.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetRoomCustomProperty(string property, bool value);

        /// <summary>
        /// Sets a string custom property that is synchronized with the room.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The property value.</param>
        void SetRoomCustomProperty(string property, string value);

        /// <summary>
        /// Returns a synchronized property value from a room.
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="room">The room name.</param>
        /// <param name="property">The property name.</param>
        /// <returns>The network stored value of the property on the room</returns>
        T GetRoomCustomProperty<T>(string room, string property);

        /// <summary>
        /// Create an object that sends data across the network
        /// </summary>
        /// <param name="objectName">Prefab name</param>
        /// <param name="position">World position to spawn the object</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>Id of the created network entity</returns>
        RideID CreateNetworkObject(string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Create an agent that sends data across the network
        /// </summary>
        /// <param name="unit">The simulation unit to network.</param>
        /// <returns>Id of the created network agent</returns>
        RideID CreateNetworkObject(Unit unit);

        /// <summary>
        /// Creates a networked object with authority associated with a backend player reference.
        /// </summary>
        /// <param name="playerRef">The backend player reference that receives authority.</param>
        /// <param name="objectName">The registered prefab name.</param>
        /// <param name="position">The world position.</param>
        /// <param name="rotation">The world rotation.</param>
        /// <returns>The RIDE identifier of the created network entity.</returns>
        RideID CreateNetworkObject(IPlayerRef playerRef, string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Create an object that sends data across the network. This object has the lifetime of the room and
        /// exists even after the creator leaves the room.
        /// </summary>
        /// <param name="objectName">Prefab name</param>
        /// <param name="position">World position to spawn the object</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>Id of the created network entity</returns>
        RideID CreateNetworkRoomObject(string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>
        /// Performs RIDE setup on a networked agent.
        /// </summary>
        /// <param name="agent">The simulation agent to configure.</param>
        /// <returns>The RIDE identifier assigned to the networked agent.</returns>
        RideID SetupNetworkObject(IAgent agent);

        /// <summary>
        /// Returns the RideID associated with the network player id.
        /// </summary>
        /// <param name="playerId">The network player id</param>
        /// <returns>The associated RIDE identifier, or <see cref="RideID.Null"/> when no mapping exists.</returns>
        RideID GetRideIDFromPlayer(int playerId);

        /// <summary>
        /// Returns the unique network player identifier associated with a RIDE identifier.
        /// </summary>
        /// <param name="agent">The RIDE identifier of the network player.</param>
        /// <returns>The network player id</returns>
        int GetPlayerFromRideID(RideID agent);

        /// <summary>
        /// Tests if there is a RideID associated with the network player id
        /// </summary>
        /// <param name="playerId">The network player id</param>
        /// <returns>True if there is a RideID associated with the playerId</returns>
        bool DoesRideIDPlayeridMappingExist(int playerId);

        /// <summary>
        /// Tests if there is a RideID associated with the network view id
        /// </summary>
        /// <param name="view">The network view id</param>
        /// <returns><see langword="true"/> when a mapping exists; otherwise, <see langword="false"/>.</returns>
        bool DoesRideIDViewMappingExist(int view);

        /// <summary>
        /// The RideID associated with the view id
        /// </summary>
        /// <param name="view">The network view identifier.</param>
        /// <returns>The associated RIDE identifier, or <see cref="RideID.Null"/> when no mapping exists.</returns>
        //#if FUSION_NETWORKING
        //        int GetRideIDFromView(NetworkId viewId);
        //#else
        //        int GetRideIDFromView(int view);
        //#endif
        RideID GetRideIDFromView(int view);

        /// <summary>
        /// Returns the unique network view id associated with the TSSid
        /// </summary>
        /// <param name="agent">The RideID of the network player</param>
        /// <returns>The network view id</returns>
        int GetViewFromRideID(RideID agent);

        /// <summary>
        /// Dispatch an event across the network
        /// </summary>
        /// <param name="evCode">Range 1 - 199 inclusive.  See NetworkEventCodes</param>
        /// <param name="raiseEventOptions">The target and caching options for the event.</param>
        /// <param name="sendOptions">The delivery guarantees to use.</param>
        /// <param name="content">The event payload.</param>
        void RaiseEvent(byte evCode, RaiseEventOptions raiseEventOptions, SendOptions sendOptions, object[] content);

        /// <summary>
        /// The current amount of network time that has passed. This time is mostly identical across all connected clients
        /// </summary>
        /// <returns>The synchronized network time in seconds.</returns>
        double GetNetworkTime();

        /// <summary>
        /// Returns current round trip time in milliseconds to the server and back.
        /// </summary>
        /// <returns>Time in milliseconds for a response from the server</returns>
        int GetPing();

        /// <summary>
        /// Listeners that are invoked after successfully connecting to the network via Connect()
        /// </summary>
        OnNetworkConnect onConnect { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully connecting to the network via Disconnect()
        /// </summary>
        OnNetworkDisconnect onDisconnect { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via CreateRoom() or JoinRoom()
        /// </summary>
        OnJoinRoom onJoinRoom { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via CreateRoom() or JoinRoom()
        /// </summary>
        OnJoinRoom_Fusion onJoinRoom_Fusion { get; set; }

        /// <summary>Gets or sets Fusion input listeners invoked when the runner requests local input.</summary>
        OnInput_Fusion onInput_Fusion { get; set; }

        /// <summary>Gets or sets listeners invoked after successfully leaving a network room.</summary>
        OnLeaveRoom onLeaveRoom { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via CreateRoom() or JoinRoom(). Invoked after onJoinRoom
        /// </summary>
        OnSetupNetworkAgent onSetupNetworkAgent { get; set; }

        /// <summary>Gets or sets listeners invoked when a custom network event is received.</summary>
        OnNetworkEventRaised onEventRaised { get; set; }

        /// <summary>
        /// Listeners that are invoked after a remote player enters the room.
        /// </summary>
        OnPlayerEnterRoom onPlayerEnterRoom { get; set; }

        /// <summary>
        /// Listeners that are invoked after a remote player leaves the room.
        /// </summary>
        OnPlayerLeaveRoom onPlayerLeaveRoom { get; set; }

        /// <summary>
        /// Listeners that are invoked when switching to a new MasterClient when the current one leaves.
        /// </summary>
        OnMasterClientSwitched onMasterClientSwitched { get; set; }

        /// <summary>
        /// Intended for Fusion only. Listeners that are invoked when clients request server for prefab spawn. 
        /// </summary>
        //OnRequestSpawnPrefab onRequestSpawnPrefab { get; set; }

#if FUSION_NETWORKING
        /// <summary>
        /// Returns networked object associated with given id.
        /// </summary>
        //NetworkObject GetNetworkObject(RideID id);

        /// <summary>
        /// Intended for Fusion only. Get networked player associated with local instance of network.
        /// </summary>
        NetworkPlayer GetNetworkPlayer();

        /// <summary>
        /// Intended for Fusion only. Returns PlayerRef for local simulation. 
        /// IMPORTANT: Not to be confused with RIDE's localPlayer (InetworkView) 
        /// </summary>
        PlayerRef GetLocalPlayer();
#endif

        /// <summary>Registers existing players in the current room for Fusion-compatible implementations.</summary>
        void RegisterExistingPlayers();

        /// <summary>
        /// Intended for Fusion only. Send request to server to join the game room.
        /// </summary>
        //void RegisterRoomJoinRequest(dynamic playerRef);

        /// <summary>
        /// Intended for Fusion only. Send request to server to spawn prefab with input authority given to 'playerRef'.
        /// </summary>
        //void RequestPrefabSpawn(PlayerRef playerRef, string prefabName, RideVector3 position, RideQuaternion rotation);
    }
}
