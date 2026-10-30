namespace Ride.AI.ML
{
    /// <summary>
    /// Defines the minimal RIDE-facing contract for a machine-learning model component.
    /// The Unity model adapter uses it to associate model assets with their owning agents.
    /// </summary>
    public interface IRideMLModel
    {
        /// <summary>
        /// Associates the model component with a RIDE agent.
        /// </summary>
        /// <param name="agent">The identifier of the agent that owns the model.</param>
        void Init(RideID agent);
    }
}
