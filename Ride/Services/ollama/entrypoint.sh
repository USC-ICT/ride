#!/bin/sh
set -e

# Start the Ollama server in the background.
ollama serve &
SERVE_PID=$!

# Wait until the server is responding.
echo "[ollama-local] waiting for server to start..."
until ollama list >/dev/null 2>&1; do
  sleep 1
done

# Pre-pull the two hot-swappable models, plus the embedding model when one is configured
# (idempotent; cached in the volume after first run). The embedding model is what makes
# semantic retrieval possible - without it, retrieval falls back to lexical scoring.
for MODEL in "$OLLAMA_MODEL_A" "$OLLAMA_MODEL_B" "$OLLAMA_MODEL_EMBED"; do
  if [ -n "$MODEL" ]; then
    echo "[ollama-local] pulling $MODEL ..."
    ollama pull "$MODEL" || echo "[ollama-local] WARNING: failed to pull $MODEL"
  fi
done

echo "[ollama-local] ready on :11434 (models: $OLLAMA_MODEL_A, $OLLAMA_MODEL_B${OLLAMA_MODEL_EMBED:+, embed: $OLLAMA_MODEL_EMBED})"
wait "$SERVE_PID"
