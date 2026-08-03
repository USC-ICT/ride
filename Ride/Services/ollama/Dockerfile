# Ollama LLM server with on-demand model loading — clients hot-swap models by name, no restart.
# Unlike vLLM (one model per process), Ollama can hold multiple models resident and switch per request.
# Pinned engine (Ollama ships frequently). entrypoint.sh pre-pulls the two configured models.
FROM ollama/ollama:0.30.10

# Bind all interfaces inside the container so the published port works.
ENV OLLAMA_HOST=0.0.0.0:11434
# Two models pre-pulled at startup. Both Qwen3 = Apache-2.0 (redistributable). Override via compose/.env.
ENV OLLAMA_MODEL_A=qwen3:1.7b
ENV OLLAMA_MODEL_B=qwen3:4b
# Keep both models resident so switching between them is instant (no reload/eviction).
ENV OLLAMA_MAX_LOADED_MODELS=2
ENV OLLAMA_KEEP_ALIVE=-1

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 11434
ENTRYPOINT ["/entrypoint.sh"]
