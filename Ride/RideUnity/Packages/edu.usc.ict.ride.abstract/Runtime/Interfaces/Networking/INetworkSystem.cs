using System.Collections;
using System.Collections.Generic;
using Ride.Combat;
using Ride.Entities;

namespace Ride.Networking
{
    public delegate void OnNetworkConnect();
    public delegate void OnNetworkDisconnect();
    public delegate void OnJoinRoom();
    public delegate void OnLeaveRoom();
    public delegate void OnSetupNetworkAgent(RideID agent, INetworkView view);
    public delegate void OnNetworkEventRaised(byte evCode, object[] contents);
    public delegate void OnPlayerEnterRoom(Player player);
    public delegate void OnPlayerLeaveRoom(Player player);
    public delegate void OnMasterClientSwitched(Player newMasterClient);
    public delegate void OnJoinRoom_Fusion(INetworkRunner runner, IPlayerRef player);
    public delegate void OnInput_Fusion(INetworkRunner runner, INetworkInput input);
    public delegate void OnRequestSpawnPrefab(IPlayerRef player, string prefabName);

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

    /// <summary>
    /// A player that is represented on the network
    /// </summary>
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

        public Player(int actorNumber, string nickName, string userId) { ActorNumber = actorNumber; NickName = nickName; UserId = userId; }

//#if PHOTON_UNITY_NETWORKING
//        public static explicit operator Player(Photon.Realtime.Player p) => new Player(p.ActorNumber, p.NickName, p.UserId);
//#endif
    }

    public class Room
    {
        public string Name { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public Dictionary<int, Player> Players { get; }  // can be null if this information about the room isn't available.

        public Room(string name, int playerCount, int maxPlayers, Dictionary<int, Player> players) { Name = name; PlayerCount = playerCount; MaxPlayers = maxPlayers; Players = players; }
    }

    // needs to match Fusion.GameMode
    public enum GameModeFusion
    {
        Single = 1,
        Shared,
        Server,
        Host,
        Client,
        AutoHostOrClient
    }

    public struct SessionRequest
    {
        public RideID UserID;
        public GameModeFusion GameMode;
        public string DisplayName;
        public string SessionName;
        public int MaxPlayers;
        public string CustomLobby;
        public string IPAddress;
        public ushort Port;
    }

    public interface INetworkAgent
    {
        void SendReceivingDamage(RideID entityId, float damage);
        void SendInflictingDamage(IAttackResult attackResult);
    }


    /// <summary>
    /// Interface that allows functionality for clients to communicate over the network
    /// </summary>
    public interface INetworkSystem : IRideSystem
    {
        void AddNetworkAgent(RideID id, INetworkAgent agent);

        bool IsNetworkAgent(RideID id);

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
        /// <param name="roomName"></param>
        /// <returns></returns>
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
        /// Creates a networked session in Fusion environment. (Session is equivalent to room in PUN)
        /// </summary>
        /// <param name="name">The display name of the room</param>
        void CreateRoom(SessionRequest request);

        /// <summary>
        /// Makes a request for the local player to join a networked room. If successful, OnJoinedRoom is dispatched
        /// </summary>
        /// <param name="name">The name of the room to join</param>
        void JoinRoom(string name);

        /// <summary>
        /// The local player leaves their current rroom
        /// </summary>
        void LeaveRoom();

        /// <summary>
        /// Sets a value on a networ room that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetRoomCustomProperty(string property, int value);

        /// <summary>
        /// Sets a value on a networ room that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetRoomCustomProperty(string property, float value);

        /// <summary>
        /// Sets a value on a networ room that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetRoomCustomProperty(string property, bool value);

        /// <summary>
        /// Sets a value on a networ room that is automatically synced over the network
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the proptery</param>
        void SetRoomCustomProperty(string property, string value);

        /// <summary>
        /// Returns a network synced property value of the room
        /// </summary>
        /// <typeparam name="T">Allowed values: string, int, float, bool</typeparam>
        /// <param name="property">The name of the property</param>
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
        /// <param name="unit"></param>
        /// <returns>Id of the created network agent</returns>
        RideID CreateNetworkObject(Unit unit);

        /// <summary>
        /// Create an object in Fusion Network
        /// </summary>
        /// <param name="playerRef">RideID assigned to PlayerRef (Fusion only)</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
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
        /// Performs TSS setup on the networked agent
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        RideID SetupNetworkObject(IAgent agent);

        /// <summary>
        /// Returns the RideID associated with the network player id.
        /// </summary>
        /// <param name="playerId">The network player id</param>
        /// <returns>The RideID. RideID.NULL if not found</returns>
        RideID GetRideIDFromPlayer(int playerId);

        /// <summary>
        /// Returns the unique network player id associated with the TSSid
        /// </summary>
        /// <param name="agent">The tssid of the network player</param>
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
        /// <returns></returns>
        bool DoesRideIDViewMappingExist(int view);

        /// <summary>
        /// The RideID associated with the view id
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
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
        /// <param name="raiseEventOptions"></param>
        /// <param name="sendOptions">Information about who to send it to</param>
        /// <param name="content">The data you want to send</param>
        void RaiseEvent(byte evCode, RaiseEventOptions raiseEventOptions, SendOptions sendOptions, object[] content);

        /// <summary>
        /// The current amount of network time that has passed. This time is mostly identical across all connected clients
        /// </summary>
        /// <returns></returns>
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

        OnInput_Fusion onInput_Fusion { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via CreateRoom() or JoinRoom()
        /// </summary>
        OnLeaveRoom onLeaveRoom { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via CreateRoom() or JoinRoom(). Invoked after onJoinRoom
        /// </summary>
        OnSetupNetworkAgent onSetupNetworkAgent { get; set; }

        /// <summary>
        /// Listeners that are invoked after successfully joining a network room via RaiseEvent()
        /// </summary>
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

        /// <summary>
        /// Intended for Fusion only. Register existing players in current room
        /// </summary>
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
