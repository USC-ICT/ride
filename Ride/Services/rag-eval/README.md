# rag-eval - retrieval quality harness

Measures retrieval quality (hit@3) of the RIDE knowledge system by compiling the REAL
package sources (`KnowledgeIndex` and friends from `edu.usc.ict.ride.cognition` /
`.abstract`, referenced directly in `csharp/Eval.csproj`). What this measures is the
exact code the Unity runtime executes - not a reimplementation, and NOT the rag-proxy:
the harness builds the index in-process and never sends a chat completion (except in
`--proxy-smoke` mode, below).

## Pipeline vs data

The harness is pipeline only: it contains no corpus content and no questions. A
**dataset** supplies both - a folder holding:

- `pairs.json` - questions plus the page titles accepted as correct answers
- the corpus documents (`*.txt` / `*.md`), either in a `corpus/` subfolder or directly
  in the dataset folder

The same RAG pipeline currently serves three purposes, and each is its own dataset:

| Use | Data | Where |
|---|---|---|
| VHToolkit demo | public | `datasets/vhtoolkit/` (versioned here) |
| A corpus held elsewhere | whatever it is | any folder, by placing a `pairs.json` beside its documents |
| VHToolkit Studio | each researcher's own | any folder they point `--dataset` at |

## How to run

Lexical only - no services needed; defaults to the VHToolkit dataset:

```
dotnet run --project csharp/Eval.csproj
```

Any other dataset:

```
dotnet run --project csharp/Eval.csproj -- --dataset ..\rag-proxy\corpus
```

Lexical + semantic + hybrid - needs RAW Ollama on port 11436 with the
`nomic-embed-text` model:

```
..\ollama\load.bat
dotnet run --project csharp/Eval.csproj -- --ollama
```

The first `--ollama` run embeds every corpus chunk, so it takes a couple of minutes;
queries embed one call each.

### Other embedding backends

`--embed-model <name>` selects a different Ollama model. `--embed-prefix` turns on the
asymmetric task prefixes that some models are trained with, and `--query-prefix` /
`--doc-prefix` override the prefix strings (either one also implies `--embed-prefix`).
Prefixes are model-specific and using the wrong ones is worse than using none:

```
dotnet run --project csharp/Eval.csproj -- --ollama --embed-model embeddinggemma ^
  --query-prefix "task: search result | query: " --doc-prefix "title: none | text: "
```

`--openai` embeds through the OpenAI `/v1/embeddings` API with `text-embedding-3-small`,
matching what `EmbeddingsSystemOpenAI` uses in Unity. The key comes from the
`OPENAI_API_KEY` environment variable, or from a RIDE configuration file named with
`--config <ride.json>` (read from `openAIChatGPT.endpointKey`, the same field the Unity
system reads). No location is searched implicitly and the key is never printed.

### Calibrating the relevance floor

`--floor-sweep` (with `--ollama` or `--openai`) calibrates
`KnowledgeSettings.minSemanticScore`, the cosine below which a retrieved passage is
discarded instead of being added to the prompt. It reports three things:

1. Per pair, the cosine of the best chunk belonging to an accepted page - the score a
   floor must stay below or it drops a correct answer. The minimum across all pairs is the
   ceiling for any floor.
2. The top cosine produced by a set of queries the corpus does not cover. A floor is only
   useful if it sits above these, so the gap between this and (1) is the usable window.
3. Semantic and hybrid hit@3 at floors from 0.00 to 0.60.

A floor is meaningful only relative to one embedding model's score distribution, so
re-run this after changing model or provider. Measured 2026-08-06 on the vhtoolkit
dataset:

| Model | prefixes | semantic | hybrid | usable window |
|---|---|---|---|---|
| `text-embedding-3-small` (OpenAI) | n/a | 31/32 | 32/32 | 0.2596 .. 0.3436 |
| `embeddinggemma` | its own | 32/32 | 32/32 | 0.3239 .. 0.3362 |
| `embeddinggemma` | none | 31/32 | 32/32 | 0.3293 .. 0.3527 |
| `nomic-embed-text` | none | 31/32 | 32/32 | 0.5296 .. 0.5411 |
| `mxbai-embed-large` | none | 31/32 | 32/32 | none |
| `granite-embedding` | none | 31/32 | 31/32 | none |
| `bge-m3` | none | 30/32 | 31/32 | none |

Retrieval quality barely separates these; score calibration does. Only OpenAI produces a
window wide enough to threshold against with any margin. "None" means uncovered queries
score at least as high as the worst correct passage, so no threshold can separate them.

Note where the numbers live. `KnowledgeSettings` defaults both floors to off, because a
useful value depends on the corpus and the embedding model. The value an application ships
belongs on its own settings - for the VHToolkit that is `RideSystemsCognition.prefab`, which
carries `minSemanticScore: 0.3` from the OpenAI measurement above and is therefore inert on
the local models. Re-run this sweep for your own corpus rather than inheriting either.

### The lexical floor, measured

`--floor-sweep` also calibrates `minLexicalScore`, and needs no embedding backend for that
half - run it with no other flags. On the vhtoolkit dataset (2026-08-06) the answer is that
**no usable lexical floor exists**:

```
best accepted-page score   min 3.46 | median 11.56 | max 31.34
top uncovered-query score  7.38
```

The window is inverted by 3.9, worse than any embedding model. The cause is visible in the
per-pair output, which prints query word counts alongside scores: a tf-idf sum grows with the
number of query terms, so score confounds relevance with question length. "What is RIDE?" is
three words and on topic, scoring 5.70; "How much does a used car cost these days?" is eight
words and off topic, scoring 7.38. No threshold separates those, because they do not measure
the same thing.

Worse, a floor here is not merely useless, it is costly:

| floor | lexical hit@3 |
|---|---|
| 0 | 30/32 |
| 4 | 30/32 |
| 6 | 28/32 |
| 8 | 26/32 |
| 10 | 20/32 |

`minLexicalScore` shipped at `8` until 2026-08-06, which dropped four questions the retriever
otherwise answered - including "What is RIDE?" - while rejecting nothing, since every
uncovered query already scored below it. It is now `0`. Off-topic rejection on the lexical
path comes from hybrid retrieval and from `contextPreamble` framing the material as
reference rather than instruction, not from a threshold.

### Ollama port notes (read before debugging a failed semantic run)

The harness hardcodes `http://127.0.0.1:11436` - raw Ollama's INTERNAL port in the
rag-proxy layout (the proxy owns the canonical 11434). Which script you start Ollama
with matters:

- `..\ollama\load.bat` uses whatever `OLLAMA_PORT` is in `ollama\.env`. It must be 11436
  for the harness. The embed model is only pulled if `.env` sets
  `OLLAMA_MODEL_EMBED=nomic-embed-text` (or it survives in the Docker volume from an
  earlier pull).
- `..\ollama\load-with-rag.bat` ALSO works for the eval - it forces 11436 and guarantees
  the embed model - at the cost of an idle rag-proxy on 11434, which the harness
  ignores except in `--proxy-smoke` mode.
- Footgun: `unload-with-rag.bat` restores `.env` to `OLLAMA_PORT=11434` for plain-Ollama
  users. A later plain `load.bat` then puts Ollama where the harness cannot find it, and
  the semantic run fails or silently degrades. Check `.env` first when embeddings
  cannot connect.

## The vhtoolkit dataset is a snapshot

`datasets/vhtoolkit/corpus/` is a COPY of the knowledge base the Unity projects ship in
`Assets/VHShared/Resources/VHKnowledge` (rewritten to spoken prose 2026-07-29), not a live
reference. After editing the knowledge base, re-sync before re-running - for example, from
this folder, with the Unity project checked out alongside:

```
Copy-Item "<unity-project>\Assets\VHShared\Resources\VHKnowledge\*.txt" datasets\vhtoolkit\corpus\
```

Renamed or new knowledge files also
need `pairs.json` accept lists updated - accepted pages match by EXACT title, and a
page's title is its filename without extension.

## Baselines (vhtoolkit dataset)

| Date | Corpus | Pairs | Lexical (overall/hard) | Semantic | Hybrid |
|---|---|---|---|---|---|
| 2026-07 | June wiki scrape | 20 | 0.75 / 0.44 | 0.95 / 0.89 | 0.95 / 0.89 |
| 2026-07-29 | VHKnowledge spoken prose | 32 | 0.94 / 0.87 | 0.97 / 0.93 | 1.00 / 1.00 |

Hybrid is what production runs (retrievalMode Auto = hybrid once vectors exist), so the
hybrid column is the headline. The 2026-07-29 hybrid beats both of its inputs - the one
question semantic misses (q22) is carried by its lexical ranking, and the two questions
lexical misses (q12, q14) are carried semantically, which is the point of fusion. An
earlier snapshot the same day had hybrid at 0.97 with q13 ("can participants use their
phone to talk to the character") as the sole miss: the mobile pages only discussed
installation, not talking, so the embedding pulled the query to the privacy page's
"talk and interact... microphone" instead. Adding one sentence of user-vocabulary
content to each mobile page ("participants can hold spoken conversations with a
character on their phone...") cleared it - the same content-gap class as tuning edge #7
in ride-rag-plan.md. The two rows are NOT comparable - corpus
text and question set both changed (and the pre-2026-07-29 corpus and pairs no longer
exist; the row is historical record only); within a row, lexical vs semantic vs hybrid
is the meaningful comparison. q31-q32 ("which lab develops the VHToolkit / RIDE") were
added after the live "which labs" incident, together with the corpus fix that makes them
answerable (tuning edge #7 in ride-rag-plan.md) - they hit in all three tiers.

## The JS stub scorer (Studio)

`run-eval.js` scores the VHToolkit Studio wizard's STUB retriever (`study-config.js`
from the vh-study-wizard tree) against the same datasets, with the same hit@3 method:

```
node run-eval.js
node run-eval.js --dataset <folder>
```

On the 2026-07-29 vhtoolkit dataset the stub scores 0.83 overall / 0.80 hard - below
even the package's lexical tier, which is the measured argument for retiring the stub
in favor of package retrieval when Studio's knowledge wire-up lands (plan Phase 5).

## Proxy smoke test

`--proxy-smoke` asks the dataset's first three questions of raw Ollama (11436) and the
RUNNING rag-proxy (11434) side by side. With a corpus the model cannot already know -
material past its training cutoff, or private to the deployment - the raw side visibly
confabulates where the proxy answers from the documents. Run it with the same dataset the
proxy is serving, pointing `--dataset` at that corpus folder.
