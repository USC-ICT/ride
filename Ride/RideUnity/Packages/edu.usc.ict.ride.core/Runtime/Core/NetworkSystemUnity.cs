using System;
using System.Collections;
using System.Collections.Generic;
#if FUSION_NETWORKING
using Fusion;
#endif
using Ride.Entities;
using Ride.WorldState;

namespace Ride.Networking
{
    /// <summary>
    /// Unity-facing base implementation for Ride networking systems that host their runtime state on a <see cref="UnityEngine.MonoBehaviour"/>.
    /// </summary>
    /// <remarks>
    /// This class centralizes the common Ride-side responsibilities shared by concrete networking backends, such as tracking the
    /// relationship between backend actor/view identifiers and <see cref="RideID"/> values, cleaning up spawned network agents when
    /// players leave, and dispatching the standard Ride networking callbacks and world events.
    /// Concrete implementations are responsible for bridging these hooks to a specific transport or session backend such as Photon PUN or Fusion.
    /// </remarks>
    /// <inheritdoc cref="INetworkSystem"/>
    public abstract class NetworkSystemUnity : RideSystemMonoBehaviour, INetworkSystem
    {
        /// <summary>Interval in seconds used to update the local clients ping property</summary>
        protected float m_pingUpdateInterval = 5;
        //float m_pingTimer = 0;
        public bool destroyNetworkUnits = true; // Flag to determine whether or not NetworkSystem should call Photon.Destroy on unit - TODO: Hack for now. Refactor and remove this

        /// <summary>Registers shared network-system callbacks that should exist for the lifetime of the Ride system.</summary>
        public override void SystemInit()
        {
            base.SystemInit();

            this.onDisconnect += OnDisconnected;
        }

        /// <summary>Subscribes to world-state teardown notifications so network-owned entities can be cleaned up when their Ride entity is destroyed.</summary>
        public override void SystemAwake()
        {
            Systems.WorldState.AddListener<EntityEvent>(WorldEvent.entityDataDestroyed, OnEntityDestroyed);

            base.SystemAwake();
        }

        /// <summary>Shuts down the backend connection when the Ride system is torn down.</summary>
        public override void SystemShutdown()
        {
            base.SystemShutdown();

            Disconnect();
        }

        /// <summary>
        /// Updates backend-specific per-frame networking work.
        /// </summary>
        /// <remarks>
        /// The shared base implementation currently leaves the ping replication logic disabled, but the method remains virtual so derived
        /// classes can participate in the standard Ride update loop.
        /// </remarks>
        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            /*if (isConnected && isInRoom && localPlayer != null)
            {
                if (m_pingTimer >= m_pingUpdateInterval)
                {
                    localPlayer.SetCustomProperty(RideDefines.Ping, GetPing());
                    m_pingTimer = 0;

                    //if (CurrentRoom != null)
                    //{
                    //    CalculateRoomPing(CurrentRoom.Name);
                    //}
                }
                m_pingTimer += RideUtils.GetDeltaTime();
            }*/
        }

        /// <summary>
        /// Calculates and stores an average ping value for a named room based on the connected players' replicated ping properties.
        /// </summary>
        /// <param name="roomName">Room whose aggregate ping should be evaluated.</param>
        /// <returns>
        /// The computed room ping, or the local client's ping if the room cannot be found, has no player list, or produces a zero aggregate.
        /// </returns>
        public int CalculateRoomPing(string roomName)
        {
            Room room = null;
            foreach (Room r in rooms )
            {
                if (r.Name == roomName)
                {
                    room = r;
                    break;
                }
            }

            if (room == null)
            {
                RideLog.LogError($"Failed to find room with name {roomName}");
                return GetPing();
            }

            if (room.Players == null)
            {
                RideLog.LogError($"Room player information name available for room {roomName}");
                return GetPing();
            }

            // find the average ping of the players in the room
            int ping = 0;
            foreach (var player in room.Players)
            {
                RideID networkPlayerId = GetRideIDFromPlayer(player.Key);
                INetworkView view = Systems.Component.GetComponent<INetworkView>(networkPlayerId);

#if !FUSION_NETWORKING
                ping += view.GetCustomProperty<int>(RideDefines.Ping);
#endif
            }

            ping /= (int)RideMath.Max(currentRoom.Players.Count, 1);

            if (ping == 0)
                ping = GetPing();  // no one in the room, just use your local ping

            SetRoomCustomProperty(RideDefines.Ping, ping);
            return ping;
        }

        /// <summary>Clears the cached player-to-entity mappings after the backend disconnects.</summary>
        void OnDisconnected()
        {
            ClearPlayerMap();
        }

        /// <summary>Gets the backend's current network time value.</summary>
        public abstract double GetNetworkTime();

        public abstract int numRooms { get; }
        public abstract int numPlayers { get; }
        public abstract int numPlayersInRooms { get; }
        public abstract int numPlayersLookingForRooms { get; }
        public abstract bool isMasterClient { get; }
        public abstract bool isServer { get; }

        public abstract IEnumerable<Room> rooms { get; }
        public INetworkView localPlayer { get; private set; }

        public abstract ClientState networkClientState { get; }
        public abstract bool isConnected { get; }

        public abstract bool isInRoom { get; }
        public abstract Room currentRoom { get; }

        public abstract string playerNickName { get; set; }


        Dictionary<int, RideID> m_ActorIdToRideIdMap = new();
        Dictionary<int, RideID> m_ViewToRideIdMap = new();
        //Dictionary<int, RideID> m_ViewToRideIdMap = new();


        Dictionary<int, HashSet<RideID>> m_ActorIdToRideIdSetMap = new();
        //Dictionary<int, HashSet<RideID>> m_ViewToRideIdSetMap = new();
        Dictionary<int, HashSet<RideID>> m_ViewToRideIdSetMap = new();

        protected Dictionary<RideID, INetworkAgent> m_NetworkAgents = new();

        /// <summary>The views created on this client</summary>
        List<INetworkView> m_myViews = new();


        /// <summary>Connects the local client to the underlying networking backend.</summary>
        public abstract void Connect();

        /// <summary>Disconnects the local client from the underlying networking backend.</summary>
        public abstract void Disconnect();

        /// <summary>Joins the backend lobby service so available rooms can be discovered.</summary>
        public abstract void JoinLobby();

        /// <summary>Leaves the currently joined backend lobby.</summary>
        public abstract void LeaveLobby();

        /// <summary>Creates a new network room using a simple room name.</summary>
        /// <param name="name">User-facing room name to create.</param>
        public abstract void CreateRoom(string name);

        /// <summary>Creates a new network room from a richer session request payload.</summary>
        /// <param name="request">Session configuration used by the backend to create the room.</param>
        public abstract void CreateRoom(SessionRequest request);

        /// <summary>Joins an existing room by name.</summary>
        /// <param name="name">Name of the room to join.</param>
        public abstract void JoinRoom(string name);

        /// <summary>Leaves the currently joined room.</summary>
        public abstract void LeaveRoom();

        /// <summary>Spawns a networked object by prefab name at the given pose.</summary>
        public abstract RideID CreateNetworkObject(string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>Spawns and initializes a networked object from existing Ride unit data.</summary>
        public abstract RideID CreateNetworkObject(Unit unit);

        /// <summary>Spawns a room-owned network object that is not tied to a specific player owner.</summary>
        public abstract RideID CreateNetworkRoomObject(string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>Spawns a networked object associated with an explicit backend player reference.</summary>
        public abstract RideID CreateNetworkObject(IPlayerRef playerRef, string objectName, RideVector3 position, RideQuaternion rotation);

        /// <summary>Registers players that already exist in the backend session when Ride attaches to an in-progress networking state.</summary>
        public abstract void RegisterExistingPlayers();

#if FUSION_NETWORKING
        public abstract NetworkPlayer GetNetworkPlayer();
#endif

        /// <summary>
        /// Registers a spawned network agent with Ride systems, caches its backend identifiers, and dispatches the standard creation event.
        /// </summary>
        /// <param name="agent">Agent component attached to the spawned network object.</param>
        /// <returns>The Ride entity identifier assigned to the network object, or <see cref="RideID.Null"/> if setup fails.</returns>
        public virtual RideID SetupNetworkObject(IAgent agent)
        {
            RideID id = RideID.Null;
            if (agent != null)
            {
                id = Systems.Agent.AddAgentExisting(agent);
                Systems.Scenario.AddAgent(id);
                INetworkView view = Systems.Component.GetComponent<INetworkView>(id);
                if (view != null)
                {
                    if (view.isMine)
                        m_myViews.Add(view);

                    MapIds(id, view.actorId, view.viewId);

                    if (view.isMine)
                    {
                        // We do this to prevent AI bots from being the localPlayer.
                        // This code assumes the localPlayer is the first view found
                        // with isMine set to true. This needs to be updated.
                        if (localPlayer == null)
                        {
                            localPlayer = view;
#if !FUSION_NETWORKING
                            localPlayer.SetCustomProperty(RideDefines.Ping, GetPing());
#endif
                        }
                    }

                    onSetupNetworkAgent?.Invoke(id, view);
                    Systems.WorldState.DispatchEvent<NetworkViewCreatedEvent>(WorldEvent.networkViewCreated,
                        new NetworkViewCreatedEvent(id, view.viewId, view.actorId, view.isMine, localPlayer == view));
                }
                else
                {
                    RideLog.LogError("There is no INetworkView associated with networked agent");
                }
            }

            return id;
        }

        /// <summary>
        /// Caches the relationship between backend actor/view identifiers and the Ride entity created for that network object.
        /// </summary>
        /// <param name="id">Ride entity identifier assigned to the network object.</param>
        /// <param name="actorId">Backend player or actor identifier that owns the view.</param>
        /// <param name="viewId">Backend view identifier for the spawned object.</param>
        void MapIds(RideID id, int actorId, /*int viewId*/int viewId)
        {
            if (!m_ActorIdToRideIdMap.ContainsKey(actorId))
                m_ActorIdToRideIdMap.Add(actorId, id);
            else
                m_ActorIdToRideIdMap[actorId] = id;

            if (!m_ViewToRideIdMap.ContainsKey(viewId))
                m_ViewToRideIdMap.Add(viewId, id);
            else
                m_ViewToRideIdMap[viewId] = id;

            // Since it is possible for a player to spawn team members,
            // we also create map from actor/view to Ride agent IDs to keep track of that.
            if (!m_ActorIdToRideIdSetMap.ContainsKey(actorId))
                m_ActorIdToRideIdSetMap.Add(actorId, new HashSet<RideID>());

            if (!m_ViewToRideIdSetMap.ContainsKey(viewId))
                m_ViewToRideIdSetMap.Add(viewId, new HashSet<RideID>());

            m_ActorIdToRideIdSetMap[actorId].Add(id);
            m_ViewToRideIdSetMap[viewId].Add(id);
        }

        /// <summary>Resets Ride-side player and view mappings after leaving a room.</summary>
        public virtual void OnLeftRoom()
        {
            ClearPlayerMap();
        }

        /// <summary>
        /// Resolves a backend player identifier to the corresponding Ride entity identifier.
        /// </summary>
        /// <param name="playerId">Backend player or actor identifier.</param>
        /// <returns>The associated Ride entity identifier, or <see cref="RideID.Null"/> if no mapping exists.</returns>
        public RideID GetRideIDFromPlayer(int playerId)
        {
            RideID id = RideID.Null;
            if (m_ActorIdToRideIdMap.ContainsKey(playerId))
                id = m_ActorIdToRideIdMap[playerId];
            else
                RideLog.LogError($"Failed to find RideID associated with player {playerId}");

            return id;
        }

        /// <summary>
        /// Resolves a Ride entity identifier back to its owning backend player identifier.
        /// </summary>
        /// <param name="agent">Ride entity identifier to resolve.</param>
        /// <returns>The backend player identifier, or <c>-1</c> when the mapping is not known.</returns>
        public int GetPlayerFromRideID(RideID agent)
        {
            foreach (int player in m_ActorIdToRideIdMap.Keys)
            {
                if (m_ActorIdToRideIdMap[player] == agent)
                    return player;
            }

            return -1;
        }

        /// <summary>Removes all cached player and view mappings and tears down any Ride agents that were created for them.</summary>
        protected void ClearPlayerMap()
        {
            foreach (var actorMap in m_ActorIdToRideIdMap)
            {
                //api.scenarioSystem.RemoveAgent(actorMap.Value);
                //RemoveRideEntity(actorMap.Key);
                RemoveRideEntities(actorMap.Key);
            }

            CleanUpViews();
            m_ActorIdToRideIdMap.Clear();
            m_ViewToRideIdMap.Clear();
            m_ActorIdToRideIdSetMap.Clear();
            m_ViewToRideIdSetMap.Clear();
            m_myViews.Clear();
        }

        /// <summary>Removes every Ride entity associated with the supplied backend player identifier.</summary>
        /// <param name="playerId">Backend player identifier whose owned entities should be removed.</param>
        protected void RemoveRideEntities(int playerId)
        {
            // This is to handle the case when a player spawns team mates as AI bots.
            if (DoesRideIDPlayeridMappingExist(playerId))
            {
                var agents = m_ActorIdToRideIdSetMap[playerId];
                foreach (var agent in agents)
                {
                    int viewId = GetViewFromRideID(agent);
                    int index = m_myViews.FindIndex(v => v.ToString() != "null" && v.viewId == viewId);
                    if (index != -1)
                    {
                        if (m_myViews[index].isMine)
                        {
                            INetworkView view = m_myViews[index];
                            Systems.WorldState.DispatchEvent<NetworkViewDestoyedEvent>(WorldEvent.networkViewDestroyed,
                                new NetworkViewDestoyedEvent(agent, view.viewId, view.actorId, view.isMine, view == localPlayer));

                            CleanUpViews();
                        }

                        if (index < m_myViews.Count)
                            m_myViews.RemoveAt(index);
                    }

                    Systems.Scenario.RemoveAgent(agent);
                    //m_ActorIdToRideIdMap.Remove(playerId);
                    m_ViewToRideIdMap.Remove(GetViewFromRideID(agent));
                }
            }
        }

        /// <summary>Removes a single Ride entity associated with the supplied backend player identifier.</summary>
        /// <param name="playerId">Backend player identifier whose primary Ride entity should be removed.</param>
        protected void RemoveRideEntity(int playerId)
        {
            if (DoesRideIDPlayeridMappingExist(playerId))
            {
                RideID agent = m_ActorIdToRideIdMap[playerId];
                int viewId = GetViewFromRideID(agent);
                int index = m_myViews.FindIndex(v => v.ToString() != "null" && v.viewId == viewId);
                if (index != -1)
                {
                    if (m_myViews[index].isMine)
                    {
                        INetworkView view = m_myViews[index];
                        Systems.WorldState.DispatchEvent<NetworkViewDestoyedEvent>(WorldEvent.networkViewDestroyed,
                            new NetworkViewDestoyedEvent(agent, view.viewId, view.actorId, view.isMine, view == localPlayer));

                        CleanUpViews();
                    }

                    if (index < m_myViews.Count)
                        m_myViews.RemoveAt(index);
                }

                Systems.Scenario.RemoveAgent(agent);
                //m_ActorIdToRideIdMap.Remove(playerId);

                m_ViewToRideIdMap.Remove(GetViewFromRideID(agent));
            }
        }

        /// <summary>Removes any locally tracked network views that still have Ride agents attached to them.</summary>
        protected void CleanUpViews()
        {
            for (int i = 0; i < m_myViews.Count; i++)
            {
                //if (m_allViews[i].isMine)
                if (m_myViews[i].ToString() != "null")
                {
                    Systems.Scenario.RemoveAgent(GetRideIDFromView(m_myViews[i].viewId));
                    m_myViews.RemoveAt(i--);
                }
            }

            m_myViews.Clear();
        }

        public bool DoesRideIDPlayeridMappingExist(int playerId)
        {
            return m_ActorIdToRideIdMap.ContainsKey(playerId);
        }

        public bool DoesRideIDViewMappingExist(int view)
        {
            return m_ViewToRideIdMap.ContainsKey(view);
        }

#if FUSION_NETWORKING
        /// <summary>
        /// Resolves a backend view identifier to the corresponding Ride entity identifier.
        /// </summary>
        /// <param name="viewId">Backend view identifier.</param>
        /// <returns>The associated Ride entity identifier, or <see cref="RideID.Null"/> if no mapping exists.</returns>
        public int GetRideIDFromView(int viewId)
        {
            RideID id = RideID.Null;
            if (m_ViewToRideIdMap.ContainsKey(viewId))
                id = m_ViewToRideIdMap[viewId];
            else
                RideLog.LogError($"Failed to find RideID associated with view {viewId}");

            return id;
        }
#else
        /// <summary>
        /// Resolves a backend view identifier to the corresponding Ride entity identifier.
        /// </summary>
        /// <param name="viewId">Backend view identifier.</param>
        /// <returns>The associated Ride entity identifier, or <see cref="RideID.Null"/> if no mapping exists.</returns>
        public RideID GetRideIDFromView(int viewId)
        {
            RideID id = RideID.Null;
            if (m_ViewToRideIdMap.ContainsKey(viewId))
                id = m_ViewToRideIdMap[viewId];
            else
                RideLog.LogError($"Failed to find RideID associated with view {viewId}");

            return id;
        }

        //public int GetRideIDFromView(int view)
        //{
        //    RideID id = RideID.Null;
        //    if (m_ViewToRideIdMap.ContainsKey(view))
        //    {
        //        id = m_ViewToRideIdMap[view];
        //    }
        //    else
        //    {
        //        RideLogSystem.LogErrorFormat("Failed to find RideID associated with view {0}", view);
        //    }
        //    return id;
        //}
#endif

        /// <summary>
        /// Resolves a Ride entity identifier back to its backend view identifier.
        /// </summary>
        /// <param name="agent">Ride entity identifier to resolve.</param>
        /// <returns>The backend view identifier, or <c>-1</c> when the mapping is not known.</returns>
        public int GetViewFromRideID(RideID agent)
        {
            foreach (int view in m_ViewToRideIdMap.Keys)
            {
                if (m_ViewToRideIdMap[view] == agent)
                    return view;
            }

            return -1;
        }

        /// <summary>
        /// Dispatches a Ride event across the active networking backend.
        /// </summary>
        /// <param name="evCode">Range 1 - 199 inclusive. See <see cref="NetworkEventCode"/>.</param>
        /// <param name="raiseEventOptions">Backend-agnostic routing and caching options for the raised event.</param>
        /// <param name="sendOptions">Transport delivery options used when sending the event payload.</param>
        /// <param name="content">Serialized event payload entries to send.</param>
        public abstract void RaiseEvent(byte evCode, RaiseEventOptions raiseEventOptions, SendOptions sendOptions, object[] content);

        /// <summary>Sets an integer custom property on the active room.</summary>
        public abstract void SetRoomCustomProperty(string property, int value);

        /// <summary>Sets a floating-point custom property on the active room.</summary>
        public abstract void SetRoomCustomProperty(string property, float value);

        /// <summary>Sets a Boolean custom property on the active room.</summary>
        public abstract void SetRoomCustomProperty(string property, bool value);

        /// <summary>Sets a string custom property on the active room.</summary>
        public abstract void SetRoomCustomProperty(string property, string value);

        /// <summary>
        /// Gets a typed room custom property from the backend room cache.
        /// </summary>
        /// <typeparam name="T">Expected property value type.</typeparam>
        /// <param name="room">Room name whose property should be read.</param>
        /// <param name="property">Custom property key to retrieve.</param>
        /// <returns>The stored property value, or the backend's default value for <typeparamref name="T"/> when unavailable.</returns>
        public abstract T GetRoomCustomProperty<T>(string room, string property);

        /// <summary>Gets the backend-reported network ping for the local client.</summary>
        public abstract int GetPing();

        public void AddNetworkAgent(RideID id, INetworkAgent agent)
        {
            m_NetworkAgents.Add(id, agent);
        }

        public bool IsNetworkAgent(RideID id)
        {
            return m_NetworkAgents.ContainsKey(id);
        }

        public INetworkAgent GetNetworkAgent(RideID id)
        {
            return m_NetworkAgents.ContainsKey(id) ? m_NetworkAgents[id] : null;
        }

        /// <summary>
        /// Handles Ride entity-destruction notifications for network-owned entities.
        /// </summary>
        /// <param name="simulationEvent">World event marker that triggered the destruction callback.</param>
        /// <param name="e">Entity destruction payload.</param>
        void OnEntityDestroyed(WorldEventMarker simulationEvent, EntityEvent e)
        {
            if (destroyNetworkUnits)
            {
#if false
                Photon.Pun.PhotonView photonView = Globals.api.componentSystem.GetComponent<Photon.Pun.PhotonView>(e.entityID);
                if (photonView != null && photonView.IsMine)
                    Photon.Pun.PhotonNetwork.Destroy(photonView);
#else
                UnityEngine.Debug.LogError($"AttackSystemMono.HandleWeaponFired() - TODO - Ride Refactor - {simulationEvent} - {e.entityID}");
#endif
            }
        }

        public OnNetworkConnect onConnect { get; set; }
        public OnNetworkDisconnect onDisconnect { get; set; }
        public OnJoinRoom onJoinRoom { get; set; }
        public OnLeaveRoom onLeaveRoom { get; set; }
        public OnSetupNetworkAgent onSetupNetworkAgent { get; set; }
        public OnNetworkEventRaised onEventRaised { get; set; }
        public OnPlayerEnterRoom onPlayerEnterRoom { get; set; }
        public OnPlayerLeaveRoom onPlayerLeaveRoom { get; set; }
        public OnMasterClientSwitched onMasterClientSwitched { get; set; }
        public OnJoinRoom_Fusion onJoinRoom_Fusion { get; set; }
        public OnInput_Fusion onInput_Fusion { get; set; }
        public OnRequestSpawnPrefab onRequestSpawnPrefab { get; set; }


        //[MenuItem("Ride/Networking/Fusion")]
        //public static void UseFusion()
        //{
        //    SetScriptingDefineSymbol("PHOTON_FUSION_NETWORKING", "PHOTON_UNITY_NETWORKING");
        //}

        //[MenuItem("Ride/Networking/PUN")]
        //public static void UsePUN()
        //{
        //    SetScriptingDefineSymbol("PHOTON_UNITY_NETWORKING", "PHOTON_FUSION_NETWORKING");
        //}

        //private static void SetScriptingDefineSymbol(string addDefines, string subDefines)
        //{
        //    string currentDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone);

        //    string newDefines = currentDefines.Replace(subDefines, string.Empty);
        //    newDefines += $";{addDefines};";

        //    PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, newDefines);
        //    Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        //}

#if FUSION_NETWORKING
        //public abstract NetworkObject GetNetworkObject(RideID id);
        public abstract PlayerRef GetLocalPlayer();
#endif
    }
}
