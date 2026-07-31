namespace Ride.Conversation
{
    /// <summary>
    /// Local embeddings via <see href="https://ollama.com">Ollama</see>'s OpenAI-compatible
    /// <c>/v1/embeddings</c> endpoint (default port 11434). Nothing leaves the machine, no
    /// per-call cost. The model must be available in the local Ollama instance
    /// (<c>ollama pull nomic-embed-text</c>). Connection/model config is code-authoritative
    /// (not [SerializeField]); for per-deployment overrides, source from RideConfig.
    /// </summary>
    public class EmbeddingsSystemOllama : EmbeddingsSystemOpenAICompatible
    {
        /// <inheritdoc/>
        public override void SystemInit()
        {
            m_endpoint = "http://127.0.0.1:11434/v1/embeddings";
            m_model = "nomic-embed-text";
            m_requestTimeoutSeconds = 0; // first call may load the model
            base.SystemInit();
        }
    }
}
