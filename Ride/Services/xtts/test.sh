#!/bin/sh
echo "Voices:"
curl http://127.0.0.1:9004/voices
echo ""
echo "Synthesizing:"
curl -X POST http://127.0.0.1:9004/synthesize \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello from the VHToolkit.","voice":"Ana Florence"}'
