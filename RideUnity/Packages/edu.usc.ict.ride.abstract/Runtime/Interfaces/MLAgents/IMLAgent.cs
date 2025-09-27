namespace Ride.AI.ML
{
    /// <summary>
    /// A Machine Learning agent that can use many types of approaches for
    /// implementing an agent
    /// </summary>
    public interface IMLAgent
    {
        /// <summary>
        /// Setup the agent to use ML
        /// </summary>
        /// <param name="agent"></param>
        void Init(RideID agent);

        /// <summary>
        /// BehaviourName for this behavior to differentiate it from other behaviors in scene
        /// </summary>
        string behaviourName { get; }
    }
}
