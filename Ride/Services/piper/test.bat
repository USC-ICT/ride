@echo off
echo Voices:
curl.exe http://127.0.0.1:9002/voices
echo.
echo Synthesizing:
curl.exe -X POST http://127.0.0.1:9002/synthesize ^
  -H "Content-Type: application/json" ^
  -d "{\"text\":\"Hello from the VHToolkit.\",\"voice\":\"en_US-lessac-medium\"}"
