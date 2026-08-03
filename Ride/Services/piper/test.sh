#!/bin/sh
echo "Voices:"
curl http://127.0.0.1:9002/voices
echo ""
echo "Synthesizing:"
curl -X POST http://127.0.0.1:9002/synthesize \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello from the VHToolkit.","voice":"en_US-lessac-medium"}'
