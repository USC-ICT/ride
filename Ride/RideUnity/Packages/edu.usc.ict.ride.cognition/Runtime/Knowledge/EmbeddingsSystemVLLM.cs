namespace Ride.Conversation
{
    /// <summary>
    /// Local embeddings via <see href="https://docs.vllm.ai">vLLM</see>'s OpenAI-compatible
    /// <c>/v1/embeddings</c> endpoint. Nothing leaves the machine and there is no per-call
    /// cost, so this suits deployments that must keep conversation data on-premises.
    ///
    /// Requires a vLLM instance serving an embedding model under the model name below. vLLM
    /// serves one model per process, so a deployment that also uses vLLM for language
    /// generation runs a second instance for embeddings on its own port. Connection and model
    /// config is code-authoritative (not [SerializeField]) so changing a default here takes
    /// effect without a prefab or scene edit; for per-deployment overrides, source from
    /// <see cref="RideConfig"/>.
    /// </summary>
    public class EmbeddingsSystemVLLM : EmbeddingsSystemOpenAICompatible
    {
        /// <inheritdoc/>
        public override void SystemInit()
        {
            m_endpoint = "http://127.0.0.1:8001/v1/embeddings";
            m_model = "vhtoolkit-embed";
            m_requestTimeoutSeconds = 0; // first call may wait for the model to load
            base.SystemInit();
        }
    }
}
