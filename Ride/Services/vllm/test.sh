#!/bin/sh
curl http://127.0.0.1:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer local-dev-token" \
  -d '{"model":"vhtoolkit-llm","messages":[{"role":"system","content":"You are concise."},{"role":"user","content":"Give me a 2 sentence summary of what machine learning is."}],"max_tokens":80,"stream":false}'
