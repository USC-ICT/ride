#!/bin/sh
curl http://127.0.0.1:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"phi4-mini","messages":[{"role":"user","content":"One sentence: what is a virtual human?"}],"stream":false}'
