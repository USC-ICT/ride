# XTTS v2 Local TTS

Local Coqui XTTS v2 text-to-speech for RIDE and the VHToolkit. High-quality voices with
voice cloning support. Heavier than Kokoro or Piper -- GPU recommended.

**License note:** XTTS v2 weights are under the Coqui CPML (non-commercial). The container
sets `COQUI_TOS_AGREED=1` automatically, which signals you have read and agreed to those terms.
For commercial use, switch to Kokoro (Apache-2.0) or Piper (MIT).

- Port **9004**
- `GET  /voices`    -- list available voices
- `POST /synthesize` -- synthesize; returns JSON with `audio_base64`, `duration_seconds`


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

First run downloads the XTTS v2 model (~2 GB). Watch progress:

```
docker compose logs -f
```

Test once the server is ready (first synthesis takes ~15-30 s to load the model):

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

Unity client: `TextToSpeechSystemXTTS` in `edu.usc.ict.ride.cognition`. Sends
`POST /synthesize` (45 s timeout to accommodate model load on first call), decodes the
base64 WAV, and uses `duration_seconds` for lipsync timing. Available in the TTS debug
tab and the confirmation screen system selector.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Python 3.11-slim + espeak-ng + ffmpeg; installs TTS and FastAPI via requirements.txt |
| `compose.yaml` | Port mapping, model cache volume, `COQUI_TOS_AGREED=1` |
| `.env.example` | Copy to `.env`; set `API_TOKEN` to require auth |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | List voices and synthesize a sample |
| `unload.bat` / `unload.sh` | Stop and remove the container |
