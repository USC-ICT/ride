namespace Ride.AI.ML
{
    /// <summary>
    /// Defines training-area lifecycle operations for episodic machine-learning scenarios.
    /// Training areas use this contract to initialize parallel instances, reset episodes, and report outcomes.
    /// </summary>
    public interface IMLAgentsTrainer
    {
        /// <summary>
        /// Initializes a training-area instance.
        /// </summary>
        /// <param name="instanceID">The identifier that distinguishes this instance during parallel training.</param>
        void Init(RideID instanceID);

        /// <summary>Resets the scenario state and agents for a new training episode.</summary>
        public void SetupAgentEpisode();

        /// <summary>
        /// Gets whether the most recently completed training episode succeeded.
        /// </summary>
        /// <returns><c>true</c> when the episode succeeded; otherwise, <c>false</c>.</returns>
        bool GetEpisodeTrainingResult();
    }
}
