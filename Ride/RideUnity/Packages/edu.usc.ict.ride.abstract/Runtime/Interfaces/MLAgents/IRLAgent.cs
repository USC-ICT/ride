namespace Ride.AI.ML
{
    /// <summary>
    /// A reinforcement learning agent 
    /// </summary>
    public interface IRLAgent : IMLAgent
    {
        /// <summary>
        /// Function to be performed at every step. This can typically include doling out a reward.
        /// </summary>
        public void Step();

        public void OnActionReceived(RideID actions /*ActionBuffers actions*/);

        public void CollectObservations(RideID sensor /*VectorSensor sensor*/);

        public void Heuristic(RideID actionsOut /*in ActionBuffers actionsOut*/);

        public void TakeAction(RideID actionBuffers /*ActionBuffers actionBuffers*/);
    }
}
