using Ride.Animations;
using Ride.Audio;
using Ride.Combat;
using Ride.Effects;
using Ride.Entities;
using Ride.IO;
using Ride.Movement;
using Ride.Networking;
using Ride.Scenario;
using Ride.Terrain;
using Ride.UI;
using Ride.WorldState;

namespace Ride
{
    /// <summary>
    /// Global access point for RIDE systems.
    /// This class provides static, type-safe access to all registered systems via lazy lookup through <see cref="ISystemAccessSystem"/>.
    /// Systems are automatically discovered and cached on first use. This class is safe to use from any part of the application.
    /// </summary>
    public static class Systems
    {
        /// <summary>The backing system-access implementation.</summary>
        public static ISystemAccessSystem m_access;

        /// <summary>The active system access implementation. Typically backed by <c>SystemAccessSystem</c>.</summary>
        public static ISystemAccessSystem Access
        {
            get
            {
#if UNITY_EDITOR
                if (m_access == default)
                    RideLog.LogWarning("Systems.Access was accessed before initialization.");
#endif
                return m_access;
            }
        }

        /// <summary>Sets the system-access implementation used by all static accessors.</summary>
        /// <param name="system">The system-access implementation, or <see langword="null"/> to clear it.</param>
        public static void SetSystemAccess(ISystemAccessSystem system) => m_access = system ?? default;

        /// <summary>Retrieves a system of the given type <typeparamref name="T"/> from the active system access layer.</summary>
        public static T Get<T>() where T : IRideSystem => Access.GetSystem<T>();


        // RIDE SYSTEM ACCESSORS
        // These accessors provide a clean, type-safe global entry point for frequently used systems.

        // ride.core
        /// <summary>Gets the registered audio system.</summary>
        public static IAudioSystem Audio => Get<IAudioSystem>();
        /// <summary>Gets the registered chat system.</summary>
        public static IChatSystem Chat => Get<IChatSystem>();
        /// <summary>Gets the registered component system.</summary>
        public static IComponentSystem Component => Get<IComponentSystem>();
        /// <summary>Gets the registered game-object system.</summary>
        public static IGameObjectSystem GameObject => Get<IGameObjectSystem>();
        /// <summary>Gets the registered input system.</summary>
        public static IInputSystem Input => Get<IInputSystem>();
        /// <summary>Gets the registered logging system.</summary>
        public static ILogSystem Log => Get<ILogSystem>();
        /// <summary>Gets the registered networking system.</summary>
        public static INetworkSystem Network => Get<INetworkSystem>();
        /// <summary>Gets the registered particle-effect system.</summary>
        public static IParticleEffectSystem Pfx => Get<IParticleEffectSystem>();
        /// <summary>Gets the registered session-playback system.</summary>
        public static ISessionPlaybackSystem SessionPlayback => Get<ISessionPlaybackSystem>();
        /// <summary>Gets the registered session-recording system.</summary>
        public static ISessionRecordingSystem SessionRecording => Get<ISessionRecordingSystem>();
        /// <summary>Gets the registered shader system.</summary>
        public static IShaderSystem Shader => Get<IShaderSystem>();

        // ride.core.ui
        /// <summary>Gets the registered view system.</summary>
        public static IViewSystem View => Get<IViewSystem>();

        // ride.agents
        /// <summary>Gets the registered agent system.</summary>
        public static IAgentSystem Agent => Get<IAgentSystem>();

        // ride.simulation
        /// <summary>Gets the active scenario identifier.</summary>
        public static RideID ScenarioID => Access.scenario;
        /// <summary>Gets the registered animation system.</summary>
        public static IAnimationSystem Animation => Get<IAnimationSystem>();
        /// <summary>Gets the registered attack system.</summary>
        public static IAttackSystem Attack => Get<IAttackSystem>();
        /// <summary>Gets the registered camera system.</summary>
        public static ICameraSystem Camera => Get<ICameraSystem>();
        /// <summary>Gets the registered equipment system.</summary>
        public static IEquipmentSystem Equipment => Get<IEquipmentSystem>();
        /// <summary>Gets the registered group system.</summary>
        public static IGroupSystem Group => Get<IGroupSystem>();
        /// <summary>Gets the registered movement system.</summary>
        public static IMovementSystem Movement => Get<IMovementSystem>();
        /// <summary>Gets the registered scenario system.</summary>
        public static IScenarioSystem Scenario => Get<IScenarioSystem>();
        /// <summary>Gets the registered world-state system.</summary>
        public static IWorldStateSystem WorldState => Get<IWorldStateSystem>();

        // ride.terrain
        /// <summary>Gets the registered terrain data-model system.</summary>
        public static ITerrainDataModelSystem TerrainDataModel => Get<ITerrainDataModelSystem>();
        /// <summary>Gets the registered terrain system.</summary>
        public static ITerrainSystem Terrain => Get<ITerrainSystem>();
        /// <summary>Gets the registered tree system.</summary>
        public static ITreeSystem Tree => Get<ITreeSystem>();
    }


    /// <summary>
    /// Legacy global access class for backward compatibility.
    /// Use <see cref="Systems"/> instead for all new code.
    /// </summary>
    public static class Globals
    {
        /// <summary>Backward-compatible system access root. Redirects to <see cref="Systems.Access"/>.</summary>
        public static ISystemAccessSystem api => Systems.Access;
    }
}
