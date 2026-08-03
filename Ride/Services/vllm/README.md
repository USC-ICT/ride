# vLLM Local LLM

Local vLLM server for RIDE and the VHToolkit. Serves a single model via an OpenAI-compatible
`/v1/chat/completions` endpoint. Pinned to `vllm/vllm-openai:v0.23.0` (v0.24.0 introduced a
V2 model runner that requires UVA, unavailable on WSL2 -- do not upgrade until running on
native Linux or vLLM adds a fallback flag).

- Port **8000**
- Default model: `microsoft/Phi-4-mini-instruct` (MIT license)
- Served as alias `vhtoolkit-llm` (the Unity client always requests this alias)
- Auth: `Bearer local-dev-token`


## Quick start

```
load.bat        (Windows)
./load.sh       (macOS / Linux)
```

First run downloads the model (~2-8 GB depending on model). Watch progress:

```
docker compose logs -f
```

Test once the server is ready (startup takes 2-3 minutes):

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

Edit `.env` and change `VLLM_MODEL`, then re-run `load.bat` / `./load.sh`.

**WSL2 constraint:** only models using the V1 runner work on WSL2 (Qwen3 triggers the V2 runner,
which requires UVA -- use Ollama for Qwen3 on WSL2). On native Linux any model works.

```
# Phi-4-mini (MIT, commercial use OK; 3.8B; default)
VLLM_MODEL=microsoft/Phi-4-mini-instruct

# Qwen2.5 (non-commercial only; works on WSL2)
#VLLM_MODEL=Qwen/Qwen2.5-3B-Instruct

# Qwen3 (Apache-2.0, commercial use OK; native Linux only -- use Ollama on WSL2)
#VLLM_MODEL=Qwen/Qwen3-4B-Instruct-2507

# Gemma 4 E2B (gated -- grant access at huggingface.co/google/gemma-4-E2B-it;
#              OOMs on 12 GB WSL2 due to display driver overhead; may fit on native Linux)
#VLLM_MODEL=google/gemma-4-E2B-it
```


## WSL2 12 GB performance note

vLLM requires `--enforce-eager` on WSL2 12 GB (CUDA graphs OOM). In enforce-eager mode, Triton
JIT-compiles a kernel for each new sequence-length shape -- fast after warmup for simple prompts,
but the full VH system prompt hits new shapes and is unusably slow (~0.1 tokens/s).

**For WSL2, use Ollama instead** (`ollama-local/`). vLLM is the right choice on native Linux with
16+ GB VRAM where CUDA graphs fit in memory.


## VHToolkit integration

Unity client: `NlpSystemVLLM` in `edu.usc.ict.ride.cognition`. It requests the `vhtoolkit-llm`
alias, so swapping models in `.env` requires no Unity changes.


## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Builds on `vllm/vllm-openai:v0.23.0`; sets model alias, API key, enforce-eager, and max context |
| `entrypoint.sh` | Starts vLLM, waits for health, fires a warmup request to pre-compile Triton kernels |
| `compose.yaml` | GPU passthrough, port mapping, HF cache volume |
| `.env.example` | Copy to `.env`; set `HF_TOKEN` and optionally `VLLM_MODEL` / `HF_CACHE_PATH` |
| `load.bat` / `load.sh` | Build and start the container |
| `test.bat` / `test.sh` | Send a quick chat request to verify the server is responding |
| `unload.bat` / `unload.sh` | Stop and remove the container |
