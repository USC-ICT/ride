namespace Ride.AI.ML
{
    /// <summary>
    /// Defines runtime policy selection for a Unity ML-Agents-backed agent.
    /// Implementations use it to switch trained models and alternate between heuristic and inference behavior.
    /// </summary>
    public interface IMLAgentsReader
    {
        /// <summary>
        /// Associates the policy reader with a RIDE agent.
        /// </summary>
        /// <param name="agent">The identifier of the agent whose policy is managed.</param>
        void Init(RideID agent);

        //Note: Unity: Use Unity.Barracuda.NNModel; Or make RideNNModel or convert from byte[]

        /// <summary>
        /// Changes the agent's policy to a registered trained model.
        /// </summary>
        /// <param name="newModel">The identifier of the model to activate.</param>
        public void ChangePolicy(RideID newModel);

        /// <summary>Changes the agent to use its hand-authored heuristic behavior.</summary>
        public void ChangeToHeuristicBehaviorType();

        /// <summary>Changes the agent to make decisions by running inference with its active trained model.</summary>
        public void ChangeToInferenceBehaviorType();

        ///// <summary>
        ///// Agents take an action based on the action tensor received from the MLAgents engine
        ///// </summary>
        ///// <param name="vectorAction"></param>
        //public void TakeAction(/*RideActionBuffers vectorAction*/);

        ///// <summary>
        ///// Collect observations from the environment via variables and sensors to send to MLAgents engine
        ///// </summary>
        ///// <param name="sensor"></param>
        //public void CollectObservations(/*RideVectorSensor sensor*/);

        ///// <summary>
        ///// Agent is requested to make a decision to advance the step
        ///// </summary>
        //public void RequestDecision();
    }
}
