namespace Ride.AI.ML
{
    /// <summary>
    /// Defines access to the machine-learning agents and activation state associated with a behavior object.
    /// ML behavior components use this contract to initialize and locate their concrete agent implementations.
    /// </summary>
    public interface IMLBehaviourSettings
    {
        /// <summary>Gets the name of the configured machine-learning behavior.</summary>
        string behaviourName { get; }

        /// <summary>
        /// Initializes the machine-learning agent associated with an entity.
        /// </summary>
        /// <param name="entity">The identifier of the entity that owns the behavior.</param>
        void Init(RideID entity);

        /// <summary>
        /// Activates or deactivates the behavior object.
        /// </summary>
        /// <param name="active"><c>true</c> to activate the behavior; <c>false</c> to deactivate it.</param>
        void SetActive(bool active);

        /// <summary>
        /// Gets the associated machine-learning agent as a requested implementation type.
        /// </summary>
        /// <typeparam name="T">The machine-learning agent type to retrieve.</typeparam>
        /// <returns>The matching agent, or <c>null</c> when none exists.</returns>
        T GetMLAgent<T>() where T : IMLAgent;

        /// <summary>
        /// Gets the associated reinforcement-learning agent as a requested implementation type.
        /// </summary>
        /// <typeparam name="T">The reinforcement-learning agent type to retrieve.</typeparam>
        /// <returns>The matching agent, or <c>null</c> when none exists.</returns>
        T GetRLAgent<T>() where T : IRLAgent;
    }
}
