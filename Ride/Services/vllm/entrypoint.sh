#!/bin/bash

vllm serve "$VLLM_MODEL" \
    --served-model-name vhtoolkit-llm \
    --host 0.0.0.0 \
    --port 8000 \
    --api-key "${VLLM_API_KEY:-local-dev-token}" \
    --gpu-memory-utilization 0.90 \
    --max-model-len 2048 \
    --enforce-eager &

VLLM_PID=$!

# Wait for server ready, then send one warmup request to pre-compile the Triton
# _compute_slot_mapping_kernel -- avoids a multi-minute JIT spike on the first real request.
until curl -sf http://localhost:8000/health > /dev/null 2>&1; do sleep 3; done
curl -sf http://localhost:8000/v1/chat/completions \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer ${VLLM_API_KEY:-local-dev-token}" \
    -d '{"model":"vhtoolkit-llm","messages":[{"role":"user","content":"hi"}],"max_tokens":5,"stream":false}' \
    > /dev/null

wait $VLLM_PID
