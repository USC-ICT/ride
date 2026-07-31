using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride.Audio;
using Ride.Combat;
using Ride.Effects;
using Ride.Entities;
using Ride.Movement;
using Ride.Scenario;
using Ride.Terrain;
using Ride.UI;
using Ride.WorldState;
using Ride.Networking;
using Ride.IO;
using Ride.Animations;

namespace Ride
{
    /// <summary>
    /// Default implementation of <see cref="ISystemAccessSystem"/> used by RIDE to manage global access to systems.
    /// Systems can be registered manually or discovered in the scene and are made globally accessible through
    /// the <see cref="Systems"/> static class (or <see cref="Globals.api"/> for legacy compatibility).
    ///
    /// This class maintains mappings for fast O(1) lookup by type or RideID and is typically not accessed directly
    /// outside of bootstrapping or testing contexts.
    /// </summary>
    public class SystemAccessSystem : RideSystemMonoBehaviour, ISystemAccessSystem
    {
        [SerializeField] private LogSystemUnity m_logSystem;

        /// <summary>
        /// Maps each registered system's unique RideID to its instance.
        /// Used for fast O(1) lookup by ID (e.g., GetSystem<T>(RideID)) and reliable removal.
        /// </summary>
        private readonly Dictionary<RideID, IRideSystem> m_systemsByID = new();

        /// <summary>
        /// Caches the first system instance for each interface or concrete type.
        /// Used by GetSystem<T>() for fast O(1) type-based access (singleton-style).
        /// This dictionary assumes only one system is needed per interface for this use case.
        /// </summary>
        private readonly Dictionary<Type, IRideSystem> m_systemsByType = new();

        public RideID scenario { get; set; }

        // ride.core
        public IAudioSystem audioSystem => GetSystem<IAudioSystem>();
        public IChatSystem chatSystem => GetSystem<IChatSystem>();
        public IComponentSystem componentSystem => GetSystem<IComponentSystem>();
        public IGameObjectSystem gameObjectSystem => GetSystem<IGameObjectSystem>();
        public IInputSystem inputSystem => GetSystem<IInputSystem>();
        public ILogSystem logSystem => GetSystem<ILogSystem>();
        public INetworkSystem networkSystem => GetSystem<INetworkSystem>();
        public IParticleEffectSystem pfxSystem => GetSystem<IParticleEffectSystem>();
        public ISessionPlaybackSystem sessionPlaybackSystem => GetSystem<ISessionPlaybackSystem>();
        public ISessionRecordingSystem sessionRecordingSystem => GetSystem<ISessionRecordingSystem>();
        public IShaderSystem shaderSystem => GetSystem<IShaderSystem>();

        // ride.core.ui
        public IViewSystem viewSystem => GetSystem<IViewSystem>();

        // ride.agents
        public IAgentSystem agentSystem => GetSystem<IAgentSystem>();

        // ride.simulation
        public IAnimationSystem animationSystem => GetSystem<IAnimationSystem>();
        public IAttackSystem attackSystem => GetSystem<IAttackSystem>();
        public ICameraSystem cameraSystem => GetSystem<ICameraSystem>();
        public IEquipmentSystem equipmentSystem => GetSystem<IEquipmentSystem>();
        public IGroupSystem groupSystem => GetSystem<IGroupSystem>();
        public IMovementSystem movementSystem => GetSystem<IMovementSystem>();
        public IScenarioSystem scenarioSystem => GetSystem<IScenarioSystem>();
        public IWorldStateSystem worldStateSystem => GetSystem<IWorldStateSystem>();

        // ride.terrain
        public ITerrainDataModelSystem terrainDataModelSystem => GetSystem<ITerrainDataModelSystem>();
        public ITerrainSystem terrainSystem => GetSystem<ITerrainSystem>();
        public ITreeSystem treeSystem => GetSystem<ITreeSystem>();

        /// <inheritdoc />
        protected override void Awake()
        {
            Systems.SetSystemAccess(this);
            RideLog.SetLogSystem(m_logSystem);

            base.Awake();
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            // this alerts the ride api about the entities that are in the scene prior to hitting play
            var conversionObjects = RideUtils.FindObjectsByType<ConvertToRide>();
            foreach (var conversionObject in conversionObjects)
                conversionObject.Convert();
        }

        /// <inheritdoc />
        public RideID AddSystem(IRideSystem system)
        {
            if (system == null || system.id == RideID.Null)
            {
                RideLog.LogError("Cannot register system: invalid or null.");
                return RideID.Null;
            }

            m_systemsByID[system.id] = system;
            m_systemsByType[system.GetType()] = system;
            foreach (var iface in system.GetType().GetInterfaces())
                if (typeof(IRideSystem).IsAssignableFrom(iface))
                    m_systemsByType[iface] = system;

            return system.id;
        }

        /// <inheritdoc />
        public void RemoveSystem(RideID id)
        {
            if (!m_systemsByID.TryGetValue(id, out var system))
            {
                RideLog.LogWarning($"[SystemAccessSystem] Tried to remove unknown system with ID {id}");
                return;
            }

            m_systemsByID.Remove(id);

            var toRemove = new List<Type>();
            foreach (var kvp in m_systemsByType)
                if (kvp.Value.id == id)
                    toRemove.Add(kvp.Key);

            foreach (var type in toRemove)
                m_systemsByType.Remove(type);

            RideLog.Log($"[SystemAccessSystem] Removed system {system.GetType().Name} with ID {id}");
        }

        /// <inheritdoc />
        public T GetSystem<T>(RideID id) where T : IRideSystem
        {
            if (m_systemsByID.TryGetValue(id, out var system))
            {
                if (system is T typed)
                    return typed;

                RideLog.LogError($"[SystemAccessSystem] System with ID {id} is not of type {typeof(T)}");
            }
            else
            {
                RideLog.LogError($"[SystemAccessSystem] No system registered with ID {id}");
            }

            return default;
        }

        /// <inheritdoc />
        public T GetSystem<T>() where T : IRideSystem
        {
            Type type = typeof(T);
            if (m_systemsByType.TryGetValue(type, out var cached))
                return (T)cached;

            foreach (var kvp in m_systemsByID)
            {
                if (kvp.Value is T match)
                {
                    AddSystem(match);
                    return match;
                }
            }

            var objects = RideUtils.FindObjectsByType<RideSystemMonoBehaviour>();
            foreach (var obj in objects)
            {
                if (obj is T match)
                {
                    AddSystem(match);
                    return match;
                }
            }

            //RideLog.LogError($"[SystemAccessSystem] Could not find system of type {type}");
            return default;
        }

        /// <inheritdoc />
        public T GetAddedSystemOfType<T>() where T : class
        {
            foreach (var kvp in m_systemsByID)
                if (kvp.Value is T match)
                    return match;

            RideLog.LogError($"Couldn't find system of type {typeof(T)}");
            return default;
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAddedSystems<T>() where T : IRideSystem
        {
            foreach (var kvp in m_systemsByID)
                if (kvp.Value is T match)
                    yield return match;
        }
    }
}
