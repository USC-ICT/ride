namespace Ride.AI.ML
{

    public interface IRideMLModel
    {
        /// <summary>
        /// Setup the agent to use ML
        /// </summary>
        /// <param name="agent"></param>
        void Init(RideID agent);
    }
}
