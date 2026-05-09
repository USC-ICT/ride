namespace Ride.AI.ML
{
    /// <summary>
    /// Interface for handling trained machine learning models, typically neural network models.
    /// This typically covers both inference, a la Barracuda, and loading models a la ONNX
    /// Corresponds more to NNModel than Model in Unity MLAgents
    /// </summary>
    public interface IMLModel
    {
        /// <summary>
        /// Setup the agent to use ML
        /// </summary>
        /// <param name="agent"></param>
        void Init(RideID agent);

        /// <summary>
        ///  TODO: Convert to RIDEModel
        /// </summary>
        /// <param name="modelBuffer"></param>
        public void LoadModel(byte[] modelBuffer);

        public byte[] SaveModel();
    }
}
