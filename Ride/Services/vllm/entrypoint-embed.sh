#!/bin/bash
# Embeddings variant of the vLLM entrypoint.
#
# vLLM serves ONE model per process, so semantic retrieval needs a second container alongside
# the chat one: this serves an embedding model on port 8001 under the alias the RIDE clients
# expect (vhtoolkit-embed), while entrypoint.sh serves the chat model on 8000 as vhtoolkit-llm.
#
# GPU budget: the chat service reserves most of the card, so this one is deliberately capped
# low. Embedding models are small (a few hundred MB) and only need room for short inputs.

vllm serve "$VLLM_EMBED_MODEL" \
    --served-model-name vhtoolkit-embed \
    --task embed \
    --host 0.0.0.0 \
    --port 8001 \
    --api-key "${VLLM_API_KEY:-local-dev-token}" \
    --gpu-memory-utilization "${VLLM_EMBED_GPU_FRACTION:-0.10}" \
    --max-model-len 512 \
    --enforce-eager &

VLLM_PID=$!

# Wait for ready, then send one warmup embedding so the first real request is not the one
# paying for kernel compilation.
until curl -sf http://localhost:8001/health > /dev/null 2>&1; do sleep 3; done
curl -sf http://localhost:8001/v1/embeddings \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer ${VLLM_API_KEY:-local-dev-token}" \
    -d '{"model":"vhtoolkit-embed","input":"warmup"}' \
    > /dev/null

echo "[vllm-embed] ready on :8001 (model: $VLLM_EMBED_MODEL, alias vhtoolkit-embed)"
wait $VLLM_PID
