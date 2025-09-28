using System.Collections;
using System.Collections.Generic;
using Ride.Combat;
using Ride.Effects;
using Ride.Entities;
using Ride.Movement;
using Ride.Networking;
using Ride.Scenario;
using Ride.Terrain;
using Ride.UI;
using Ride.WorldState;
using Ride.IO;
using Ride.Audio;
using Ride.Animations;

namespace Ride
{
    /// <summary>
    /// Defines the RIDE system access interface for querying, registering, and managing shared subsystems.
    /// All systems registered via this interface implement <see cref="IRideSystem"/> and are accessible globally
    /// through the <see cref="Systems"/> static class. This interface supports both singleton-style lookups (e.g. `GetSystem&lt;T&gt;`)
    /// and enumeration of multiple systems via `GetAddedSystems&lt;T&gt;`.
    ///
    /// Most developers will use the global `Systems` or `Globals.api` accessors for convenience,
    /// which internally route through this interface.
    /// </summary>
    public interface ISystemAccessSystem : IRideSystem
    {
        // ride.core
        IAudioSystem audioSystem { get; }
        IChatSystem chatSystem { get; }
        IComponentSystem componentSystem { get; }
        IGameObjectSystem gameObjectSystem { get; }
        IInputSystem inputSystem { get; }
        ILogSystem logSystem { get; }
        INetworkSystem networkSystem { get; }
        IParticleEffectSystem pfxSystem { get; }
        ISessionPlaybackSystem sessionPlaybackSystem { get; }
        ISessionRecordingSystem sessionRecordingSystem { get; }
        IShaderSystem shaderSystem { get; }

        // ride.core.ui
        IViewSystem viewSystem { get; }

        // ride.agents
        IAgentSystem agentSystem { get; }

        // ride.simulation
        RideID scenario { get; set; }
        IAnimationSystem animationSystem { get; }
        IAttackSystem attackSystem { get; }
        ICameraSystem cameraSystem { get; }
        IEquipmentSystem equipmentSystem { get; }
        IGroupSystem groupSystem { get; }
        IMovementSystem movementSystem { get; }
        IScenarioSystem scenarioSystem { get; }
        IWorldStateSystem worldStateSystem { get; }

        // ride.terrain
        ITerrainDataModelSystem terrainDataModelSystem { get; }
        ITerrainSystem terrainSystem { get; }
        ITreeSystem treeSystem { get; }

        /// <summary>
        /// Registers a system for global access.
        /// All supported interfaces on the system are mapped for fast lookup via <see cref="GetSystem{T}"/>.
        /// </summary>
        /// <param name="system">The system to register.</param>
        /// <returns>The <see cref="RideID"/> of the system.</returns>
        RideID AddSystem(IRideSystem system);

        /// <summary>
        /// Unregisters the system associated with the specified <paramref name="id"/>.
        /// Removes all cached type and interface mappings for the system.
        /// </summary>
        /// <param name="id">The unique ID of the system to remove.</param>
        void RemoveSystem(RideID id);

        /// <summary>
        /// Returns the system instance registered under the specified <paramref name="id"/> if it implements <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The system interface or type expected.</typeparam>
        /// <param name="id">The unique ID of the system to look up.</param>
        /// <returns>The matching system of type <typeparamref name="T"/>, or default if not found or of wrong type.</returns>
        T GetSystem<T>(RideID id) where T : IRideSystem;

        /// <summary>
        /// Returns the first registered system of type <typeparamref name="T"/>.
        /// This is the primary method used by the <see cref="Systems"/> class for singleton-style lookups.
        /// Automatically searches the scene and registers a matching system if none are cached.
        /// </summary>
        /// <typeparam name="T">The interface or concrete type of the system to retrieve.</typeparam>
        /// <returns>The system instance of type <typeparamref name="T"/>, or default if not found.</returns>
        T GetSystem<T>() where T : IRideSystem;

        /// <summary>
        /// Returns the first registered system that is assignable to <typeparamref name="T"/>.
        /// Unlike <see cref="GetSystem{T}"/>, this works with any class or interface, not just IRideSystem.
        /// No scene search is performed.
        /// </summary>
        /// <typeparam name="T">The base type or interface to match.</typeparam>
        /// <returns>The first matching registered object, or default if not found.</returns>
        T GetAddedSystemOfType<T>() where T : class;

        /// <summary>
        /// Returns all registered systems of type <typeparamref name="T"/>.
        /// This does not search the scene and only returns systems explicitly registered via <see cref="AddSystem"/>.
        /// </summary>
        /// <typeparam name="T">The system interface or type to search for.</typeparam>
        /// <returns>An enumerable of matching systems; empty if none are found.</returns>
        IEnumerable<T> GetAddedSystems<T>() where T : IRideSystem;
    }
}
