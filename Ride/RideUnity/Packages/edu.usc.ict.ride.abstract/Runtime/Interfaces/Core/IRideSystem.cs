using System;

namespace Ride
{
    /// <summary>
    /// Defines a standardized lifecycle interface for all core RIDE systems.
    ///
    /// This interface mirrors Unity's MonoBehaviour pattern 
    /// (<see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html">Awake</see>,
    /// <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html">Start</see>,
    /// <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</see>, etc.),
    /// but allows for manual control of initialization and update phases by the simulation
    /// framework. Systems implementing this interface are typically managed through
    /// <see cref="RideSystemMonoBehaviour"/> and discovered via <see cref="SystemAccessSystem"/>.
    /// </summary>
    /// <remarks>
    /// Most implementations of <see cref="IRideSystem"/> should derive from <see cref="RideSystemMonoBehaviour"/>,
    /// which provides default method handling and automatic registration with the system framework.
    /// Manual implementation is only recommended for advanced or low-level use cases.
    /// </remarks>
    public interface IRideSystem : IIdentity
    {
        /// <summary>
        /// Gets whether <see cref="SystemAwake"/> has been called.
        /// Typically invoked during Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html">Awake</see> phase.
        /// </summary>
        bool SystemAwakeCalled { get; }

        /// <summary>
        /// Gets whether <see cref="SystemInit"/> has been called.
        /// Typically invoked during Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html">Start</see> phase or equivalent system bootstrap.
        /// </summary>
        bool SystemInitCalled { get; }

        /// <summary>
        /// Gets whether the system is currently active.
        /// An active system is expected to process updates and perform simulation duties.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Called during the early initialization phase.
        /// Use this to set up internal references and one-time configuration
        /// before the system is used or accessed.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html">Awake</see> behavior.
        /// </summary>
        void SystemAwake();

        /// <summary>
        /// Called when the system is ready to begin execution or participate in simulation.
        /// This is the primary entry point for initialization and dependency acquisition.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html">Start</see> behavior.
        /// </summary>
        void SystemInit();

        /// <summary>
        /// Called once per frame to advance simulation logic.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</see> phase.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last frame.</param>
        void SystemUpdate(float deltaTime);

        /// <summary>
        /// Called once per frame after all <see cref="SystemUpdate"/> calls.
        /// Useful for finalizing frame logic or responding to state changes.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html">LateUpdate</see> phase.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last frame.</param>
        void SystemLateUpdate(float deltaTime);

        /// <summary>
        /// Called at a fixed timestep, typically used for physics or deterministic logic.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html">FixedUpdate</see> phase.
        /// </summary>
        /// <param name="fixedDeltaTime">Fixed time interval in seconds since the last call.</param>
        void SystemFixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// Called when the system is shutting down or being removed.
        /// Use this to clean up resources or unregister from shared services.
        /// </summary>
        void SystemShutdown();

        /// <summary>
        /// Activates the system, making it eligible to receive update calls.
        /// Mirrors Unity's pattern of enabling a component or GameObject.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates the system, suspending update activity until reactivated.
        /// Mirrors Unity's pattern of disabling a component or GameObject.
        /// </summary>
        void Deactivate();
    }
}
