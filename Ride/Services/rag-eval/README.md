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
| Drone demo (internal) | internal | `../rag-proxy/corpus/` (unversioned, pairs.json next to the documents) |
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

`datasets/vhtoolkit/corpus/` is a COPY of the knowledge base shipped in
`svn_vh_branch/VHUnityURP-Internal/Assets/Resources/VHKnowledge` (rewritten to spoken
prose 2026-07-29), not a live reference. After editing the knowledge base, re-sync
before re-running:

```
Copy-Item "..\..\..\svn_vh_branch\VHUnityURP-Internal\Assets\Resources\VHKnowledge\*.txt" datasets\vhtoolkit\corpus\
```

(Assumes the standard workspace layout with `svn_ride_trunk` and `svn_vh_branch`
checked out side by side; adjust if yours differs.) Renamed or new knowledge files also
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
RUNNING rag-proxy (11434) side by side. With a corpus the model cannot know - past its
training cutoff, or internal material - the raw side visibly confabulates where the
proxy answers from the documents. Run it with the same dataset the proxy is serving,
e.g. `--dataset ..\rag-proxy\corpus` for the drone demo.
