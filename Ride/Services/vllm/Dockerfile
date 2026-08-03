# Pin the engine: vLLM ships a minor release roughly every two weeks and regularly changes CLI flags.
# v0.23.0 is the last version that works on WSL2 -- v0.24.0 introduced a V2 Model Runner that
# requires UVA (Unified Virtual Addressing), which is not available in WSL2/Docker on Windows.
FROM vllm/vllm-openai:v0.23.0

# Default model. Override by setting VLLM_MODEL in .env.
# Phi-4-mini-instruct = MIT license (redistributable, commercial use OK); 3.8B, 7.69 GB; V1 runner (expected).
# --served-model-name keeps the Unity client alias stable across model swaps.
# --max-model-len 8192 caps the context window (default is 128K which OOMs on smaller GPUs).
ENV VLLM_MODEL=microsoft/Phi-4-mini-instruct

# Both entrypoints ship in the image; the compose service picks one. entrypoint.sh serves the
# chat model on 8000, entrypoint-embed.sh serves an embedding model on 8001 for semantic
# retrieval (vLLM serves one model per process, so RAG needs both running).
COPY entrypoint.sh /entrypoint.sh
COPY entrypoint-embed.sh /entrypoint-embed.sh
RUN chmod +x /entrypoint.sh /entrypoint-embed.sh
ENTRYPOINT ["/entrypoint.sh"]
