@echo off
echo Voices:
curl.exe http://127.0.0.1:8880/v1/audio/voices
echo.
echo Synthesizing to test_output.wav...
curl.exe -X POST http://127.0.0.1:8880/v1/audio/speech ^
  -H "Content-Type: application/json" ^
  -d "{\"model\":\"kokoro\",\"input\":\"Hello from the VHToolkit.\",\"voice\":\"af_heart\",\"response_format\":\"wav\"}" ^
  --output test_output.wav
echo Done. Open test_output.wav to verify.
