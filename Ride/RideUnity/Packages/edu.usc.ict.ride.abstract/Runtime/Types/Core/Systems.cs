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
        public static ISystemAccessSystem m_access;

        /// <summary>The active system access implementation. Typically backed by <see cref="SystemAccessSystem"/>.</summary>
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

        public static void SetSystemAccess(ISystemAccessSystem system) => m_access = system ?? default;

        /// <summary>Retrieves a system of the given type <typeparamref name="T"/> from the active system access layer.</summary>
        public static T Get<T>() where T : IRideSystem => Access.GetSystem<T>();


        // RIDE SYSTEM ACCESSORS
        // These accessors provide a clean, type-safe global entry point for frequently used systems.

        // ride.core
        public static IAudioSystem Audio => Get<IAudioSystem>();
        public static IChatSystem Chat => Get<IChatSystem>();
        public static IComponentSystem Component => Get<IComponentSystem>();
        public static IGameObjectSystem GameObject => Get<IGameObjectSystem>();
        public static IInputSystem Input => Get<IInputSystem>();
        public static ILogSystem Log => Get<ILogSystem>();
        public static INetworkSystem Network => Get<INetworkSystem>();
        public static IParticleEffectSystem Pfx => Get<IParticleEffectSystem>();
        public static ISessionPlaybackSystem SessionPlayback => Get<ISessionPlaybackSystem>();
        public static ISessionRecordingSystem SessionRecording => Get<ISessionRecordingSystem>();
        public static IShaderSystem Shader => Get<IShaderSystem>();

        // ride.core.ui
        public static IViewSystem View => Get<IViewSystem>();

        // ride.agents
        public static IAgentSystem Agent => Get<IAgentSystem>();

        // ride.simulation
        public static RideID ScenarioID => Access.scenario;
        public static IAnimationSystem Animation => Get<IAnimationSystem>();
        public static IAttackSystem Attack => Get<IAttackSystem>();
        public static ICameraSystem Camera => Get<ICameraSystem>();
        public static IEquipmentSystem Equipment => Get<IEquipmentSystem>();
        public static IGroupSystem Group => Get<IGroupSystem>();
        public static IMovementSystem Movement => Get<IMovementSystem>();
        public static IScenarioSystem Scenario => Get<IScenarioSystem>();
        public static IWorldStateSystem WorldState => Get<IWorldStateSystem>();

        // ride.terrain
        public static ITerrainDataModelSystem TerrainDataModel => Get<ITerrainDataModelSystem>();
        public static ITerrainSystem Terrain => Get<ITerrainSystem>();
        public static ITreeSystem Tree => Get<ITreeSystem>();
    }


    /// <summary>
    /// Legacy global access class for backward compatibility.
    /// Use <see cref="Systems"/> instead for all new code.
    /// </summary>
    public static class Globals
    {
        /// <summary>Backward-compatible system access root. Redirects to <see cref="Systems.api"/>.</summary>
        public static ISystemAccessSystem api => Systems.Access;
    }
}
