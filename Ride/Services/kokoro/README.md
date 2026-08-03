# Kokoro Local TTS

Local Kokoro text-to-speech for RIDE and the VHToolkit. Exposes an OpenAI-compatible
audio API backed by the `kokoro` library (Apache-2.0). High-quality voices, lightweight (~300 MB).

- Port **8880**
- `GET  /v1/audio/voices` -- list available voices
- `POST /v1/audio/speech` -- synthesize; returns raw WAV bytes


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

Test once the server is ready (first request loads the model, ~5-10 s):

```
test.bat        (Windows)
./test.sh       (macOS / Linux)
```

The test saves synthesized audio to `test_output.wav` in the service folder.

Unload:

```
unload.bat      (Windows)
./unload.sh     (macOS / Linux)
```


## Voices

Default voices (configure via `SUPPORTED_VOICES` in `.env`):

| Voice | Style |
|---|---|
| `af_heart` | American female, warm (default) |
| `af_bella` | American female, clear |
| `am_adam` | American male |
| `am_michael` | American male |
| `bf_emma` | British female |
| `bm_george` | British male |

Voice prefix determines the language (e.g. `a` = American English, `b` = British English,
`j` = Japanese). Any Kokoro-supported voice can be added to `SUPPORTED_VOICES`.


## VHToolkit integration

Unity client: `TextToSpeechSystemKokoro` in `edu.usc.ict.ride.cognition`. Fetches the voice
list on startup, sends `POST /v1/audio/speech`, and writes the WAV to
`Application.persistentDataPath` for playback. Available in the TTS debug tab and the
confirmation screen system selector.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Python 3.11-slim; installs kokoro and FastAPI via requirements.txt |
| `app/main.py` | FastAPI server; OpenAI-compatible `/v1/audio/speech` and `/v1/audio/voices` |
| `compose.yaml` | Port mapping, model cache volume, voice environment variables |
| `.env.example` | Copy to `.env`; set `API_TOKEN`, `DEFAULT_VOICE`, `SUPPORTED_VOICES` |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | List voices and synthesize a sample to `test_output.wav` |
| `unload.bat` / `unload.sh` | Stop and remove the container |
