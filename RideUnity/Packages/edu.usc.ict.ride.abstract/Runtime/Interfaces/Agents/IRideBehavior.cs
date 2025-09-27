namespace Ride.AI
{
    /// <summary>
    /// Defines the interface for agent-level runtime behaviors in RIDE.
    /// 
    /// A behavior represents a modular unit of simulation logic—such as patrol, idle, or follow—that is 
    /// attached to an entity and executed based on context. Implementations are managed per-agent and 
    /// support full lifecycle control, similar in spirit to Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.
    /// </summary>
    public interface IRideBehavior : IIdentity
    {
        /// <summary>
        /// Gets the current execution status of the behavior.
        /// See <see cref="BehaviorStatus"/> for possible states.
        /// </summary>
        BehaviorStatus Status { get; }

        /// <summary>
        /// Initializes the behavior with the specified entity context.
        /// This method should be called before <see cref="Start"/>.
        /// </summary>
        /// <param name="entity">The entity that owns or is executing this behavior.</param>
        void Init(RideID entity);

        /// <summary>
        /// Starts the behavior's execution.
        /// Transitioning into this state typically triggers a <see cref="BehaviorEvent.Started"/> event.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the behavior's execution.
        /// May trigger a <see cref="BehaviorEvent.Stopped"/> event depending on implementation.
        /// </summary>
        void Stop();

        /// <summary>
        /// Called once per frame while <see cref="Status"/> is <see cref="BehaviorStatus.Running"/>.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</see> method.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since the last frame, in seconds.</param>
        void Update(float deltaTime);

        /// <summary>
        /// Called once per frame, after all <see cref="Update"/> calls, while <see cref="Status"/> is <see cref="BehaviorStatus.Running"/>.
        /// Mirrors Unity's <see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html">LateUpdate</see> method.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since the last frame, in seconds.</param>
        void LateUpdate(float deltaTime);

        /// <summary>
        /// Registers a callback to be invoked when a behavior event occurs.
        /// Events include <see cref="BehaviorEvent.Started"/>, <see cref="BehaviorEvent.Stopped"/>, and <see cref="BehaviorEvent.Finished"/>.
        /// </summary>
        /// <param name="callback">The callback to invoke when a behavior event is raised.</param>
        void AddOnBehaviorEventListener(OnBehaviorEvent callback);

        /// <summary>
        /// Returns the internal state machine controlling this behavior.
        /// This is typically used for inspection or debugging; implementations may return <c>null</c> if not applicable.
        /// </summary>
        /// <returns>The behavior's internal state machine.</returns>
        IStateMachine<string, int, string> GetStateMachine();
    }
}
