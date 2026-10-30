namespace Ride.AI.ML
{
    /// <summary>
    /// Defines a machine-learning behavior associated with a RIDE agent.
    /// The Unity integration adapts this contract to the
    /// <see href="https://unity-technologies.github.io/ml-agents/Learning-Environment-Design-Agents/">Unity ML-Agents Agent API</see>.
    /// </summary>
    public interface IMLAgent
    {
        /// <summary>
        /// Associates this machine-learning behavior with a RIDE agent.
        /// </summary>
        /// <param name="agent">The identifier of the agent that uses the behavior.</param>
        void Init(RideID agent);

        /// <summary>Gets the name that distinguishes this behavior from other machine-learning behaviors.</summary>
        string behaviourName { get; }
    }
}
