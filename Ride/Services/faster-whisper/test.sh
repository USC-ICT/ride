#!/bin/sh
echo "Health check:"
curl http://127.0.0.1:9005/health
echo ""
echo "To test transcription, run:"
echo "  curl -X POST 'http://127.0.0.1:9005/transcribe?language=en&vad_filter=true' -H 'Content-Type: audio/wav' --data-binary '@your_file.wav'"
