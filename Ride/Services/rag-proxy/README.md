# RIDE RAG Proxy

A transparent retrieval-augmentation layer in front of a local OpenAI-compatible LLM
endpoint - Ollama, vLLM, or anything else serving the same API.

By default the proxy owns Ollama's canonical port (11434) and forwards to the real
Ollama on an internal port (11436). Clients keep their normal configuration and
transparently gain retrieval augmentation - whether the local endpoint uses RAG is the
endpoint's concern, not the client's. For every `/v1/chat/completions` request, the proxy
retrieves the most relevant passages from a local document corpus and injects them into
the outgoing prompt as framed reference material; the response is returned unchanged. No
client code changes, no knowledge of the corpus or the retrieval mechanism.

One RAG layer per client: if a client already performs its own retrieval (e.g. a RIDE
app with a knowledge system), it prepends its own reference block. The proxy detects an
already-augmented request and passes it through untouched, so the two layers never
stack. Endpoint-side RAG (this proxy) is for clients WITHOUT native retrieval.

Retrieval is hybrid: semantic (embeddings from an OpenAI-compatible `/v1/embeddings`
endpoint, default model `nomic-embed-text`) with lexical tf-idf fallback while the
semantic index builds or when the embedding endpoint is unavailable. Both paths compile
the RIDE cognition package's retrieval sources directly, so the Unity runtime, the eval
harness, and this proxy share one retrieval implementation.

## Chat and embeddings are configured separately

Servers differ in a way that matters here. **Ollama** serves chat and embeddings from one
instance, so one URL is enough. **vLLM serves one model per process**, so embeddings need
a second process on its own port. `EMBED_URL` therefore defaults to `UPSTREAM_URL`:
single-instance servers need no extra configuration, and split deployments set both.

A wrong or unreachable embeddings endpoint does not fail the proxy - it silently degrades
retrieval to the weaker lexical tier. The startup banner names both resolved URLs, and
`GET /rag/status` reports `embedUrl` and `semanticReady`; check those first if answers
look vague.

## Run

One command brings the whole stack up, upstream first:

```
load.bat ollama                 # Ollama on 11436 + embedding model, proxy on 11434
load.bat vllm                   # vLLM chat 8000 + embeddings 8001, proxy on 8080
load.bat                        # proxy only - upstream must already be running
unload.bat ollama               # proxy, then Ollama, and restore its canonical port
unload.bat vllm                 # proxy, then both vLLM containers
unload.bat                      # proxy only
run.bat                         # foreground instead, log in view
```

The same entry points exist next to each service, for whoever looks there first:
`../ollama/load-with-rag.bat`, `../ollama/unload-with-rag.bat`, `../vllm/load-with-rag.bat`,
`../vllm/unload-with-rag.bat`. They just call this script, so the start ordering has one
definition. Each service's plain `load.bat` / `unload.bat` still gives a vanilla LLM with no
retrieval.

Both paths wait for the upstream's health endpoint before starting the proxy, rather than
sleeping a fixed interval - a first Ollama run pulls models and a first vLLM run compiles
kernels, either of which can take minutes.

**Order matters, and it is the same rule in both directions.** The proxy binds the port
clients use and forwards to the upstream on an internal port, so the upstream has to be
listening first:

| | Start | Stop |
|---|---|---|
| 1 | `../ollama/load.bat` (or `../vllm/load.bat`) | `unload.bat` (proxy) |
| 2 | `load.bat` (proxy) | `../ollama/unload.bat` |

Getting it wrong is not fatal but is confusing: a proxy started against a dead upstream
answers `/rag/*` normally while every chat request fails and the semantic index never
builds. `unload.bat` before rebuilding too - a running proxy holds `bin\RagProxy.exe`, and
the build then fails on the file copy (MSB3027) rather than on any compile error.

Configuration via environment variables (defaults in parentheses):
`RAG_PORT` (11434, the canonical Ollama port), `UPSTREAM_URL`
(http://127.0.0.1:11436, the chat endpoint; `OLLAMA_URL` is still honored),
`EMBED_URL` (same as `UPSTREAM_URL`), `CORPUS_DIR` (./corpus),
`EMBED_MODEL` (nomic-embed-text), `RAG_TOPK` (4), `RAG_MAXCTX` (3500).

Startup order (same machine): the upstream endpoint must be up before the proxy binds
its port.

### With Ollama

`load.bat ollama` does all of it: writes `OLLAMA_PORT=11436` and
`OLLAMA_MODEL_EMBED=nomic-embed-text` into `../ollama/.env` (leaving any custom model choices
alone), starts the container - the entrypoint pulls the embedding model - waits for it, then
starts the proxy on 11434. Chat and embeddings both resolve to 11436, so no `EMBED_URL` is
needed. Clients keep pointing at `http://127.0.0.1:11434/v1` and gain retrieval.

### With vLLM

`load.bat vllm` starts two containers, because vLLM serves one model per process: the chat
model on 8000 (`vhtoolkit-llm`) and an embedding model on 8001 (`vhtoolkit-embed`, from the
`rag` compose profile). The proxy then listens on **8080** rather than taking 8000, so both a
plain endpoint and a retrieval-augmented one are available at once:

```
http://127.0.0.1:8000/v1    plain vLLM, no retrieval
http://127.0.0.1:8080/v1    same model, with retrieval
```

Two caveats. The chat service reserves `--gpu-memory-utilization 0.90`, so on a single card it
will not fit alongside the embeddings container - lower it in `../vllm/entrypoint.sh` to roughly
0.75 first. And this path is newer than the Ollama one: the `vhtoolkit-embed` conventions were
chosen before any live vLLM embeddings instance existed, so treat the first run as a test and
check `GET /rag/status` for `semanticReady`.

## Point a client at it

Nothing to change: a client configured for the endpoint's normal address
(`http://127.0.0.1:11434/v1` for Ollama) now reaches the proxy and gains RAG
transparently. The raw un-augmented endpoint stays available on its internal port if a
client explicitly wants it. Requests other than chat completions pass through untouched.

## Corpus

`corpus/` holds plain `.txt`/`.md` documents; each becomes retrievable knowledge.
Prose beats structured blobs: state facts the model should answer crisply in plain
sentences. To update knowledge, drop in or edit documents and either restart or
`POST /rag/reload` - the corpus is the single source of truth, re-indexed in seconds.

**`corpus/` is deliberately not versioned** (`svn:ignore`), because a corpus is whatever the
person running the proxy needs it to be, and is often material that should not be distributed.
A fresh checkout therefore has no documents: the proxy starts, serves as a plain pass-through,
and `GET /rag/status` reports `documents: 0` until you create `corpus/` and put files in it.
Override the location with `CORPUS_DIR` if you keep your documents elsewhere.

Good candidates are facts a model cannot know - recent developments past its training cutoff,
internal or project-specific documentation, product details. One topic per file, named after
the topic, since the filename becomes the passage title the model sees.

## Debug / demo surface

- `GET  /rag/status` - index state (documents, chunks, semantic readiness, embeddings URL and model)
- `GET  /rag/retrieve?q=...` - what would be retrieved for a query, both modes
- `POST /rag/reload` - reload the corpus and rebuild both indexes

The proxy logs one line per augmented request: query, retrieval mode, passage titles.

## Demo guidance

- Ask one fact per question. Multi-part questions spanning documents can exceed the
  passage budget, and small local models fill unsupported gaps confidently.
- Build the demo corpus from material that postdates the model's training cutoff. Then an
  un-augmented model (cloud or local) visibly confabulates where the proxy answers precisely,
  which is the whole point of showing them side by side: the same question against 11436
  (raw Ollama) and 11434 (RAG proxy). Recent-events or internal-documentation topics work well;
  anything the model already knows makes a weak demo because both answers look the same.
- Retrieval quality is measured by the harness in `rag-eval/` (workspace root):
  semantic hit@3 = 0.95 overall / 0.89 on paraphrase-hard questions, vs 0.75 / 0.44
  lexical, on the reference eval set.
