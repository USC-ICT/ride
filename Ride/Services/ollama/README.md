# Ollama Local LLM

Local Ollama server for RIDE and the VHToolkit. Holds multiple models resident and switches
between them per request by name -- no restart, no reload. Apache-2.0 engine.

- Port **11434**
- OpenAI-compatible endpoint: `POST /v1/chat/completions`
- Default models: `phi4-mini` (MIT, 3.8B) and `qwen3:1.7b` (Apache-2.0, 1.7B)
- Both models are kept resident for instant hot-swap (`OLLAMA_KEEP_ALIVE=-1`)


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

First run pulls both models (~3.5 GB total). Watch progress:

```
docker compose logs -f
```

The first couple of interactions may be slow. Test once the server is ready:

```
test.bat        (Windows)
./test.sh       (macOS / Linux)
```

Unload:

```
unload.bat      (Windows)
./unload.sh     (macOS / Linux)
```


## Switching models

Edit `.env` (`OLLAMA_MODEL_A` / `OLLAMA_MODEL_B`) and re-run `load.bat` / `./load.sh`. The
names must match what `NlpSystemOllama` requests in its model dictionary.


## WSL2 vs vLLM

Ollama uses llama.cpp kernels with no Triton JIT issue, making it the preferred local LLM on
WSL2 12 GB. vLLM is better on native Linux 16+ GB where CUDA graphs fit and throughput matters.

| | vLLM | Ollama |
|---|---|---|
| Models per process | one (set at launch) | many resident, switch per request |
| Hot-swap | no (restart required) | yes |
| Throughput | higher (batching) | good for single-user / dev |
| WSL2 12 GB | unusably slow (Triton JIT per shape) | works well |


## VHToolkit integration

Unity client: `NlpSystemOllama` in `edu.usc.ict.ride.cognition`. Exposes two selectable models
with `ToggleModel()` / `SetActiveModel()` hot-swap. Available in the NLP debug tab and the
confirmation screen system selector.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Builds on `ollama/ollama:0.30.10`; adds entrypoint with model pre-pull |
| `entrypoint.sh` | Starts Ollama, waits for readiness, pre-pulls both configured models |
| `compose.yaml` | GPU passthrough (NVIDIA), port mapping, model volume, keep-alive settings |
| `.env.example` | Copy to `.env`; set `OLLAMA_MODEL_A` / `OLLAMA_MODEL_B` to taste |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | Send a quick chat request to verify the server is responding |
| `unload.bat` / `unload.sh` | Stop and remove the container |
