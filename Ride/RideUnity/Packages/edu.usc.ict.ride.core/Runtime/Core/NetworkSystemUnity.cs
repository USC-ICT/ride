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
    /// Abstract base class that handles a lot of the boiler plate code. if your networking system USES Monobehaviours, it should implment this class
    /// </summary>
    /// <inheritdoc cref="INetworkSystem"/>
    public abstract class NetworkSystemMono : RideSystemMonoBehaviour, INetworkSystem
    {
        /// <summary>
        /// Interval in seconds used to update the local clients ping property
        /// </summary>
        protected float m_pingUpdateInterval = 5;
        //float m_pingTimer = 0;
        public bool destroyNetworkUnits = true; // Flag to determine whether or not NetworkSystem should call Photon.Destroy on unit - TODO: Hack for now. Refactor and remove this

        public override void SystemInit()
        {
            base.SystemInit();

            this.onDisconnect += OnDisconnected;
        }

        public override void SystemAwake()
        {
            Globals.api.worldStateSystem.AddListener<EntityEvent>(WorldEvent.entityDataDestroyed, OnEntityDestroyed);

            base.SystemAwake();
        }

        public override void SystemShutdown()
        {
            base.SystemShutdown();

            Disconnect();
        }

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
                m_pingTimer += TSSUtils.GetDeltaTime();
            }*/
        }

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
            foreach (KeyValuePair<int, Player> player in room.Players)
            {
                RideID networkPlayerId = GetRideIDFromPlayer(player.Key);
                INetworkView view = Globals.api.componentSystem.GetComponent<INetworkView>(networkPlayerId);
#if !FUSION_NETWORKING
                ping += view.GetCustomProperty<int>(RideDefines.Ping);
#endif
            }

            ping /= (int)RideMath.Max(currentRoom.Players.Count, 1);

            if (ping == 0)
            {
                // no one in the room, just use your local ping
                ping = GetPing();
            }

            SetRoomCustomProperty(RideDefines.Ping, ping);
            return ping;
        }

        void OnDisconnected()
        {
            ClearPlayerMap();
        }

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

        Dictionary<int, RideID> m_ActorIdToTSSIdMap = new Dictionary<int, RideID>();
        Dictionary<int, RideID> m_ViewToTSSidMap = new Dictionary<int, RideID>();
        //Dictionary<int, RideID> m_ViewToTSSidMap = new Dictionary<int, RideID>();


        Dictionary<int, HashSet<RideID>> m_ActorIdToTSSIdSetMap = new Dictionary<int, HashSet<RideID>>();
        //Dictionary<int, HashSet<RideID>> m_ViewToTSSIdSetMap = new Dictionary<int, HashSet<RideID>>();
        Dictionary<int, HashSet<RideID>> m_ViewToTSSIdSetMap = new Dictionary<int, HashSet<RideID>>();

        protected Dictionary<RideID, INetworkAgent> m_NetworkAgents = new Dictionary<RideID, INetworkAgent>();

        /// <summary>
        /// The views created on this client
        /// </summary>
        List<INetworkView> m_myViews = new List<INetworkView>();
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void JoinLobby();

        public abstract void LeaveLobby();
        public abstract void CreateRoom(string name);
        public abstract void CreateRoom(SessionRequest request);
        public abstract void JoinRoom(string name);
        public abstract void LeaveRoom();

        public abstract RideID CreateNetworkObject(string objectName, RideVector3 position, RideQuaternion rotation);
        public abstract RideID CreateNetworkObject(Unit unit);
        public abstract RideID CreateNetworkRoomObject(string objectName, RideVector3 position, RideQuaternion rotation);
        public abstract RideID CreateNetworkObject(IPlayerRef playerRef, string objectName, RideVector3 position, RideQuaternion rotation);

        public abstract void RegisterExistingPlayers();
#if FUSION_NETWORKING
        public abstract NetworkPlayer GetNetworkPlayer();
#endif

        public virtual RideID SetupNetworkObject(IAgent agent)
        {
            RideID id = RideID.Null;
            if (agent != null)
            {
                id = Globals.api.agentSystem.AddAgentExisting(agent);
                Globals.api.scenarioSystem.AddAgent(id);
                INetworkView view = Globals.api.componentSystem.GetComponent<INetworkView>(id);
                if (view != null)
                {
                    if (view.isMine)
                    {
                        m_myViews.Add(view);
                    }

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
                    Globals.api.worldStateSystem.DispatchEvent<NetworkViewCreatedEvent>(WorldEvent.networkViewCreated,
                        new NetworkViewCreatedEvent(id, view.viewId, view.actorId, view.isMine, localPlayer == view));
                }
                else
                {
                    RideLog.LogError("There is no INetworkView associated with networked agent");
                }
            }

            return id;
        }

        void MapIds(RideID id, int actorId, /*int viewId*/int viewId)
        {
            if (!m_ActorIdToTSSIdMap.ContainsKey(actorId)) m_ActorIdToTSSIdMap.Add(actorId, id);
            else m_ActorIdToTSSIdMap[actorId] = id;

            if (!m_ViewToTSSidMap.ContainsKey(viewId)) m_ViewToTSSidMap.Add(viewId, id);
            else m_ViewToTSSidMap[viewId] = id;

            // Since it is possible for a player to spawn team members,
            // we also create map from actor/view to Ride agent IDs to keep track of that.
            if (!m_ActorIdToTSSIdSetMap.ContainsKey(actorId))
            {
                m_ActorIdToTSSIdSetMap.Add(actorId, new HashSet<RideID>());
            }

            if (!m_ViewToTSSIdSetMap.ContainsKey(viewId))
            {
                m_ViewToTSSIdSetMap.Add(viewId, new HashSet<RideID>());
            }

            m_ActorIdToTSSIdSetMap[actorId].Add(id);
            m_ViewToTSSIdSetMap[viewId].Add(id);
        }

        public virtual void OnLeftRoom()
        {
            ClearPlayerMap();
        }

        public RideID GetRideIDFromPlayer(int playerId)
        {
            RideID id = RideID.Null;
            if (m_ActorIdToTSSIdMap.ContainsKey(playerId))
            {
                id = m_ActorIdToTSSIdMap[playerId];
            }
            else
            {
                RideLog.LogError($"Failed to find TSSid associated with player {playerId}");
            }
            return id;
        }

        public int GetPlayerFromRideID(RideID agent)
        {
            foreach(int player in m_ActorIdToTSSIdMap.Keys)
            {
                if (m_ActorIdToTSSIdMap[player] == agent)
                    return player;
            }

            return -1;
        }

        protected void ClearPlayerMap()
        {
            foreach (KeyValuePair<int, RideID> actorMap in m_ActorIdToTSSIdMap)
            {
                //api.scenarioSystem.RemoveAgent(actorMap.Value);
                //RemoveTSSEntity(actorMap.Key);
                RemoveTSSEntities(actorMap.Key);
            }
            CleanUpViews();
            m_ActorIdToTSSIdMap.Clear();
            m_ViewToTSSidMap.Clear();
            m_ActorIdToTSSIdSetMap.Clear();
            m_ViewToTSSIdSetMap.Clear();
            m_myViews.Clear();
        }

        protected void RemoveTSSEntities(int playerId)
        {
            // This is to handle the case when a player spawns team mates as AI bots.
            if (DoesRideIDPlayeridMappingExist(playerId))
            {
                HashSet<RideID> agents = m_ActorIdToTSSIdSetMap[playerId];
                foreach (var agent in agents)
                {
                    int viewId = GetViewFromRideID(agent);
                    int index = m_myViews.FindIndex(v => v.ToString() != "null" && v.viewId == viewId);
                    if (index != -1)
                    {
                        if (m_myViews[index].isMine)
                        {
                            INetworkView view = m_myViews[index];
                            Globals.api.worldStateSystem.DispatchEvent<NetworkViewDestoyedEvent>(WorldEvent.networkViewDestroyed,
                                new NetworkViewDestoyedEvent(agent, view.viewId, view.actorId, view.isMine, view == localPlayer));

                            CleanUpViews();
                        }

                        if (index < m_myViews.Count)
                        {
                            m_myViews.RemoveAt(index);
                        }
                    }
                    Globals.api.scenarioSystem.RemoveAgent(agent);
                    //m_ActorIdToTSSIdMap.Remove(playerId);
                    m_ViewToTSSidMap.Remove(GetViewFromRideID(agent));
                }
            }
        }

        protected void RemoveTSSEntity(int playerId)
        {
            if (DoesRideIDPlayeridMappingExist(playerId))
            {
                RideID agent = m_ActorIdToTSSIdMap[playerId];
                int viewId = GetViewFromRideID(agent);
                int index = m_myViews.FindIndex(v => v.ToString() != "null" && v.viewId == viewId);
                if (index != -1)
                {
                    if (m_myViews[index].isMine)
                    {
                        INetworkView view = m_myViews[index];
                        Globals.api.worldStateSystem.DispatchEvent<NetworkViewDestoyedEvent>(WorldEvent.networkViewDestroyed,
                            new NetworkViewDestoyedEvent(agent, view.viewId, view.actorId, view.isMine, view == localPlayer));

                        CleanUpViews();
                    }

                    if (index < m_myViews.Count)
                    {
                        m_myViews.RemoveAt(index);
                    }
                }


                Globals.api.scenarioSystem.RemoveAgent(agent);
                //m_ActorIdToTSSIdMap.Remove(playerId);

                m_ViewToTSSidMap.Remove(GetViewFromRideID(agent));
            }
        }

        protected void CleanUpViews()
        {
            for (int i = 0; i < m_myViews.Count; i++)
            {
                //if (m_allViews[i].isMine)
                if (m_myViews[i].ToString() != "null")
                {
                    Globals.api.scenarioSystem.RemoveAgent(GetRideIDFromView(m_myViews[i].viewId));
                    m_myViews.RemoveAt(i--);
                }
            }

            m_myViews.Clear();
        }

        public bool DoesRideIDPlayeridMappingExist(int playerId)
        {
            return m_ActorIdToTSSIdMap.ContainsKey(playerId);
        }

        public bool DoesRideIDViewMappingExist(int view)
        {
            return m_ViewToTSSidMap.ContainsKey(view);
        }

#if FUSION_NETWORKING
        public int GetRideIDFromView(int viewId)
        {
            RideID id = RideID.Null;
            if (m_ViewToTSSidMap.ContainsKey(viewId))
            {
                id = m_ViewToTSSidMap[viewId];
            }
            else
            {
                RideLogSystem.LogErrorFormat("Failed to find TSSid associated with view {0}", viewId);
            }

            return id;
        }
#else
        public RideID GetRideIDFromView(int viewId)
        {
            RideID id = RideID.Null;
            if (m_ViewToTSSidMap.ContainsKey(viewId))
                id = m_ViewToTSSidMap[viewId];
            else
                RideLog.LogError($"Failed to find TSSid associated with view {viewId}");

            return id;
        }
        //public int GetRideIDFromView(int view)
        //{
        //    RideID id = RideID.Null;
        //    if (m_ViewToTSSidMap.ContainsKey(view))
        //    {
        //        id = m_ViewToTSSidMap[view];
        //    }
        //    else
        //    {
        //        RideLogSystem.LogErrorFormat("Failed to find TSSid associated with view {0}", view);
        //    }

        //    return id;
        //}
#endif

        public int GetViewFromRideID(RideID agent)
        {
            foreach (int view in m_ViewToTSSidMap.Keys)
            {
                if (m_ViewToTSSidMap[view] == agent)
                    return view;
            }

            return -1;
        }

        /// <summary>
        /// Dispatch an event across the network
        /// </summary>
        /// <param name="evCode">Range 1 - 199 inclusive.  See NetworkEventCodes</param>
        /// <param name="raiseEventOptions"></param>
        /// <param name="sendOptions">Information about who to send it to</param>
        /// <param name="content">The data you want to send</param>
        public abstract void RaiseEvent(byte evCode, RaiseEventOptions raiseEventOptions, SendOptions sendOptions, object[] content);

        public abstract void SetRoomCustomProperty(string property, int value);
        public abstract void SetRoomCustomProperty(string property, float value);
        public abstract void SetRoomCustomProperty(string property, bool value);
        public abstract void SetRoomCustomProperty(string property, string value);
        public abstract T GetRoomCustomProperty<T>(string room, string property);
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
