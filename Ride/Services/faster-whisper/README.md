# Faster-Whisper Local ASR

Local automatic speech recognition for RIDE and the VHToolkit, powered by
`faster-whisper` (CTranslate2-based Whisper). Accepts WAV audio and returns a transcript.

- Port **9005**
- Endpoint: `POST /transcribe` (WAV bytes in body, query params: `language`, `vad_filter`)
- Default model: `base.en` (fast, English-only)
- GPU recommended; falls back to CPU


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

First run downloads the Whisper model weights. Watch progress:

```
docker compose logs -f
```

Test once the server is ready:

```
test.bat        (Windows)
./test.sh       (macOS / Linux)
```

Transcription test (provide your own WAV file, 16 kHz mono recommended):

```
curl -X POST "http://127.0.0.1:9005/transcribe?language=en&vad_filter=true" \
  -H "Content-Type: audio/wav" \
  --data-binary "@your_file.wav"
```

Unload:

```
unload.bat      (Windows)
./unload.sh     (macOS / Linux)
```


## VHToolkit integration

Unity client: `SpeechRecognitionSystemFasterWhisper` in `edu.usc.ict.ride.cognition`.
Sends 16 kHz mono WAV, VAD filter enabled, 15 s max recording. Available in the ASR
debug tab and the confirmation screen system selector.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | CUDA base image; installs faster-whisper and FastAPI via requirements.txt |
| `compose.yaml` | GPU passthrough, port mapping, HF cache volume |
| `.env.example` | Copy to `.env`; set `API_TOKEN` to require auth |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | Health check and transcription command hint |
| `unload.bat` / `unload.sh` | Stop and remove the container |
