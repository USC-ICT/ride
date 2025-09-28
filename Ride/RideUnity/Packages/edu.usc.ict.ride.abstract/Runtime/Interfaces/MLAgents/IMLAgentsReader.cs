namespace Ride.AI.ML
{
    public interface IMLAgentsReader
    {
        /// <summary>
        /// Setup the agent to use ML
        /// </summary>
        /// <param name="agent"></param>
        void Init(RideID agent);

        //Note: Unity: Use Unity.Barracuda.NNModel; Or make RideNNModel or convert from byte[]

        /// <summary>
        /// Change policy to a new model
        /// </summary>
        /// <param name="newModel"></param>
        public void ChangePolicy(RideID newModel);

        /// <summary>
        /// Change behavior to act on a heuristic, which can be simple or complex
        /// </summary>
        public void ChangeToHeuristicBehaviorType();

        /// <summary>
        /// Change behavior to be inference-based where the decisions are made via a trained model
        /// </summary>
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
