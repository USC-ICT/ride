namespace Ride.AI.ML
{
    public interface IMLAgentsTrainer
    {
        /// <summary>
        /// Setup the agent to use ML
        /// </summary>
        /// <param name="instanceID">Training instance ID in case of multiple parallel training instances</param>
        void Init(RideID instanceID);

        /// <summary>
        /// Code to reset scenario for a new episode. Typically this includes things like setting agent initial positions, which are often randomized.
        /// </summary>
        public void SetupAgentEpisode();

        /// <summary>
        /// Determine if episode resulted in a success
        /// </summary>
        /// <param name="success"></param>
        bool GetEpisodeTrainingResult();
    }
}
