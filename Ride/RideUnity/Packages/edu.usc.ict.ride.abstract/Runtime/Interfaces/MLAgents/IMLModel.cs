namespace Ride.AI.ML
{
    /// <summary>
    /// Defines loading and saving of a trained machine-learning model associated with a RIDE agent.
    /// Implementations typically adapt serialized neural-network models used for runtime inference.
    /// </summary>
    public interface IMLModel
    {
        /// <summary>
        /// Associates the model with a RIDE agent.
        /// </summary>
        /// <param name="agent">The identifier of the agent that uses the model.</param>
        void Init(RideID agent);

        /// <summary>
        /// Loads a model from its serialized representation.
        /// </summary>
        /// <param name="modelBuffer">The serialized model bytes.</param>
        public void LoadModel(byte[] modelBuffer);

        /// <summary>
        /// Serializes the current model.
        /// </summary>
        /// <returns>The serialized model bytes.</returns>
        public byte[] SaveModel();
    }
}
