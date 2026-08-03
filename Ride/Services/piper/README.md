# Piper Local TTS

Local Piper text-to-speech for RIDE and the VHToolkit. Lightweight, CPU-friendly, no GPU
required. Returns base64-encoded WAV with duration metadata.

- Port **9002**
- `GET  /voices`    -- list available voices
- `POST /synthesize` -- synthesize; returns JSON with `audio_base64`, `duration_seconds`


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

First run downloads the voice model. Watch progress:

```
docker compose logs -f
```

Test once the server is ready:

```
test.bat        (Windows)
./test.sh       (macOS / Linux)
```

Unload:

```
unload.bat      (Windows)
./unload.sh     (macOS / Linux)
```


## VHToolkit integration

Unity client: `TextToSpeechSystemPiper` in `edu.usc.ict.ride.cognition`. Sends
`POST /synthesize`, decodes the base64 WAV, and uses `duration_seconds` for lipsync timing.
Available in the TTS debug tab and the confirmation screen system selector.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Python 3.11-slim + espeak-ng; installs Piper and FastAPI via requirements.txt |
| `compose.yaml` | Port mapping, model cache volume |
| `.env.example` | Copy to `.env`; set `API_TOKEN` to require auth |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | List voices and synthesize a sample |
| `unload.bat` / `unload.sh` | Stop and remove the container |
