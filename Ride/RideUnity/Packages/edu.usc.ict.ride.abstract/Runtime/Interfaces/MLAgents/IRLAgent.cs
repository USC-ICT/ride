namespace Ride.AI.ML
{
    /// <summary>
    /// Defines reinforcement-learning operations layered on a RIDE machine-learning agent.
    /// The Unity adapter maps opaque <see cref="RideID"/> handles to ML-Agents action buffers and observation sensors.
    /// </summary>
    public interface IRLAgent : IMLAgent
    {
        /// <summary>Advances the agent by one learning step, including any implementation-specific reward updates.</summary>
        public void Step();

        /// <summary>
        /// Processes an action buffer received from the learning policy.
        /// </summary>
        /// <param name="actions">The opaque identifier of the action buffer.</param>
        public void OnActionReceived(RideID actions /*ActionBuffers actions*/);

        /// <summary>
        /// Adds the agent's current observations to an observation sensor.
        /// </summary>
        /// <param name="sensor">The opaque identifier of the observation sensor.</param>
        public void CollectObservations(RideID sensor /*VectorSensor sensor*/);

        /// <summary>
        /// Writes heuristic actions to an action buffer.
        /// </summary>
        /// <param name="actionsOut">The opaque identifier of the action buffer to populate.</param>
        public void Heuristic(RideID actionsOut /*in ActionBuffers actionsOut*/);

        /// <summary>
        /// Applies the actions contained in an action buffer to the agent.
        /// </summary>
        /// <param name="actionBuffers">The opaque identifier of the action buffer to apply.</param>
        public void TakeAction(RideID actionBuffers /*ActionBuffers actionBuffers*/);
    }
}
