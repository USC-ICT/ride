// RAG eval harness. Compiles the REAL RIDE package retrieval sources (see Eval.csproj).
//
// The harness is pipeline only - it contains no corpus content and no questions. A
// DATASET supplies both: a folder holding pairs.json (questions + accepted page titles)
// and the corpus documents (*.txt / *.md, either in a corpus/ subfolder or directly in
// the dataset folder). Datasets are separate from the tool by design: the VHToolkit dataset
// ships in datasets/vhtoolkit, and any other corpus becomes a dataset by placing a pairs.json
// beside its documents - including a corpus that lives outside this folder entirely.
//
// Modes:
//   dotnet run                          -> lexical hit@3 on the default dataset
//                                          (datasets/vhtoolkit)
//   dotnet run -- --dataset <folder>    -> same, on any dataset folder
//   dotnet run -- --ollama              -> ALSO semantic + hybrid, exercising the
//                                          package's ScoreSemantic/ScoreHybrid with
//                                          vectors from local Ollama /v1/embeddings
//                                          (raw Ollama on 11436; 11434 is the RAG proxy
//                                          when it is running)
//   dotnet run -- --openai [--config <ride.json>] -> same as --ollama but embeds via the
//                                          OpenAI /v1/embeddings API with text-embedding-3-small,
//                                          matching EmbeddingsSystemOpenAI; key read from
//                                          RideConfig.openAIChatGPT.endpointKey or OPENAI_API_KEY
//   dotnet run -- --ollama --floor-sweep -> ALSO calibrates MinSemanticScore: per-pair
//                                          cosine of the best accepted-page chunk, plus
//                                          semantic/hybrid hit@3 at floors 0.00-0.60
//   dotnet run -- --proxy-smoke         -> sends the dataset's first questions through
//                                          a RUNNING rag-proxy (11434) and raw Ollama
//                                          side by side, to eyeball augmentation
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Ride.Conversation;

// Minimal stand-in for the RIDE core interface IKnowledgeSystem derives from; the harness
// only exercises KnowledgeIndex, which does not touch IRideSystem members.
namespace Ride { public interface IRideSystem { } }

static class Program
{
    const string OllamaUrl = "http://127.0.0.1:11436";

    // Local embedding model, overridable with --embed-model <name>. The model choice dominates
    // how well a relevance floor can separate covered from uncovered queries, so it is a
    // measured parameter rather than a constant.
    static string s_ollamaEmbedModel = "nomic-embed-text";

    // Some embedding models are trained with asymmetric task prefixes and score poorly without
    // them (embeddinggemma, snowflake-arctic-embed, and the e5/gte families). --embed-prefix
    // turns them on so a model can be measured the way it was trained rather than at a handicap.
    static bool s_useEmbedPrefix;
    static string s_queryPrefix = "search_query: ";
    static string s_documentPrefix = "search_document: ";
    // Documents are embedded in bulk by AttachVectors, queries one at a time everywhere else,
    // so a flag distinguishes which prefix a given Embed call should apply.
    static bool s_embeddingDocuments;

    // OpenAI embeddings, matching EmbeddingsSystemOpenAI: same endpoint, same default model,
    // and the key read from the same RideConfig field the Unity system uses. No dimensions
    // parameter, so vectors are the model's native width - as in the package.
    const string OpenAiEmbedUrl = "https://api.openai.com/v1/embeddings";
    const string OpenAiEmbedModel = "text-embedding-3-small";

    // Selected by --openai; the key is read from a ride.json and never echoed.
    static bool s_useOpenAi;
    static string s_openAiKey = "";

    static string EmbedBackendName => s_useOpenAi ? OpenAiEmbedModel : s_ollamaEmbedModel;

    // Resolves the OpenAI key from the OPENAI_API_KEY environment variable, or from a RIDE
    // configuration file named explicitly with --config <ride.json>, where it is read from the
    // same field the Unity embeddings system uses (openAIChatGPT.endpointKey). No location is
    // searched implicitly, and the key itself is never written to the console.
    static void LoadOpenAiKey(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
        {
            s_openAiKey = env.Trim();
            Console.WriteLine("openai key: from OPENAI_API_KEY");
            return;
        }

        int at = Array.IndexOf(args, "--config");
        if (at >= 0 && at + 1 < args.Length)
        {
            var path = args[at + 1];
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("openAIChatGPT", out var section) &&
                    section.TryGetProperty("endpointKey", out var key))
                {
                    var value = key.GetString() ?? "";
                    if (value.Length > 8 && !value.StartsWith("XXXX"))
                    {
                        s_openAiKey = value;
                        Console.WriteLine($"openai key: from {path} (openAIChatGPT.endpointKey, {value.Length} chars)");
                        return;
                    }
                    Console.WriteLine($"openai key: {path} has no usable openAIChatGPT.endpointKey");
                }
            }
            catch (Exception e) { Console.WriteLine($"openai key: could not read {path} - {e.Message}"); }
        }

        Console.WriteLine("openai key: NOT FOUND - set OPENAI_API_KEY or pass --config <ride.json>");
    }
    static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

    record Pair(string id, string question, string[] accept, bool hard);

    // Questions no VHToolkit-style corpus covers. A relevance floor is only useful if it sits
    // above whatever these score, so they set the lower edge of the usable window. Shared by
    // both floor sweeps so the two floors are calibrated against the same probe set.
    static readonly string[] OffTopicQueries =
    {
        "What is the weather going to be like tomorrow?",
        "How do I make a decent risotto?",
        "Who won the game last night?",
        "My back has been hurting for a week, what should I do?",
        "Can you keep a secret between us?",
        "What do you think about the election?",
        "How much does a used car cost these days?",
        "Tell me a joke about penguins.",
    };

    static void Main(string[] args)
    {
        var dataset = ResolveDataset(args);
        var pairs = LoadPairs(Path.Combine(dataset, "pairs.json"));
        var corpusDir = Directory.Exists(Path.Combine(dataset, "corpus"))
            ? Path.Combine(dataset, "corpus") : dataset;
        var corpus = LoadDir(corpusDir);
        Console.WriteLine($"dataset: {dataset}");
        Console.WriteLine($"corpus: {corpus.Count} pages; {pairs.Count} pairs");

        if (args.Contains("--proxy-smoke")) { ProxySmoke(pairs); return; }
        s_useOpenAi = args.Contains("--openai");
        if (s_useOpenAi) LoadOpenAiKey(args);
        int modelAt = Array.IndexOf(args, "--embed-model");
        if (modelAt >= 0 && modelAt + 1 < args.Length) s_ollamaEmbedModel = args[modelAt + 1];
        s_useEmbedPrefix = args.Contains("--embed-prefix");
        int qAt = Array.IndexOf(args, "--query-prefix");
        if (qAt >= 0 && qAt + 1 < args.Length) { s_queryPrefix = args[qAt + 1]; s_useEmbedPrefix = true; }
        int dAt = Array.IndexOf(args, "--doc-prefix");
        if (dAt >= 0 && dAt + 1 < args.Length) { s_documentPrefix = args[dAt + 1]; s_useEmbedPrefix = true; }
        bool useOllama = args.Contains("--ollama") || s_useOpenAi;

        // One index, three modes - all through the package code.
        var lex = new KnowledgeIndex();
        lex.Build(corpus);
        Score("lexical", pairs, q => lex.Score(q, 3).Select(p => p.title).ToList());

        if (useOllama)
        {
            AttachVectors(lex);
            Score("semantic", pairs, q => lex.ScoreSemantic(Embed(new List<string> { q })[0], 3)
                                             .Select(p => p.title).ToList());
            Score("hybrid", pairs, q => lex.ScoreHybrid(q, null, 0f, Embed(new List<string> { q })[0], 3)
                                           .Select(p => p.title).ToList());

            if (args.Contains("--floor-sweep"))
                FloorSweep(pairs, lex);
        }
        else if (args.Contains("--floor-sweep"))
        {
            // The lexical floor needs no embeddings, so it is measurable on its own.
            FloorSweepLexical(pairs, lex);
        }
    }


    // Calibrates KnowledgeSettings.minLexicalScore the same way FloorSweep calibrates the
    // semantic floor: the tf-idf score of the best accepted-page chunk per pair sets the
    // ceiling, the top score for uncovered queries sets the lower edge, and the sweep shows
    // what each candidate floor costs. Needs no embedding backend.
    //
    // One difference from cosine matters when reading the output: a tf-idf sum is unbounded
    // and grows with the number of query terms, so a long question scores higher than a short
    // one on the same page. Query word counts are printed alongside for that reason.
    static void FloorSweepLexical(List<Pair> pairs, KnowledgeIndex index)
    {
        float savedFloor = index.MinLexicalScore;
        index.MinLexicalScore = 0f;

        Console.WriteLine("\n=== lexical score of the best accepted-page chunk, per pair ===");
        var expected = new List<float>();
        foreach (var p in pairs)
        {
            var ranked = index.Score(p.question, index.ChunkCount);
            float best = -1f;
            foreach (var passage in ranked)
                if (p.accept.Contains(passage.title)) { best = passage.score; break; }
            if (best >= 0f) expected.Add(best);
            int words = p.question.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            string score = best < 0f ? "NOT FOUND" : best.ToString("F2");
            Console.WriteLine($"  {p.id,-5} {score,9}  {words,2}w  {(p.hard ? "hard" : "easy")}  {p.question}");
        }

        if (expected.Count == 0) { Console.WriteLine("  no accepted pages found - check the dataset"); return; }
        expected.Sort();
        Console.WriteLine($"\n  min {expected[0]:F2} | p5 {expected[Math.Max(0, expected.Count / 20)]:F2}" +
                          $" | median {expected[expected.Count / 2]:F2} | max {expected[expected.Count - 1]:F2}" +
                          $" | pairs measured {expected.Count}/{pairs.Count}");
        Console.WriteLine($"  a floor at or above {expected[0]:F2} starts dropping correct passages");

        Console.WriteLine("\n=== top lexical score for queries the corpus does not cover ===");
        float offTopicMax = -1f;
        foreach (var q in OffTopicQueries)
        {
            var top = index.Score(q, 1);
            float s = top.Count > 0 ? top[0].score : 0f;
            if (s > offTopicMax) offTopicMax = s;
            Console.WriteLine($"  {s,8:F2}  {(top.Count > 0 ? top[0].title : "-"),-42}  {q}");
        }
        Console.WriteLine($"\n  highest off-topic score {offTopicMax:F2} | lowest correct-passage score {expected[0]:F2}");
        Console.WriteLine(offTopicMax < expected[0]
            ? $"  usable floor window: {offTopicMax:F2} .. {expected[0]:F2} - midpoint {(offTopicMax + expected[0]) / 2f:F2}"
            : "  NO usable floor: off-topic queries score as high as correct passages");

        Console.WriteLine("\n=== lexical hit@3 by floor ===");
        Console.WriteLine("  floor   lexical");
        for (float floor = 0f; floor <= 30.01f; floor += 2f)
        {
            index.MinLexicalScore = floor;
            int hits = 0;
            foreach (var p in pairs)
                if (index.Score(p.question, 3).Any(x => p.accept.Contains(x.title))) hits++;
            Console.WriteLine($"  {floor,5:F1}    {hits,2}/{pairs.Count}");
        }

        index.MinLexicalScore = savedFloor;
    }

    // Dataset = --dataset <folder>, or datasets/vhtoolkit under the harness root.
    // A dataset folder holds pairs.json plus the corpus (corpus/ subfolder, or the
    // documents directly in the folder - which lets a corpus that lives elsewhere, like
    // the rag-proxy demo corpus, act as a dataset by dropping a pairs.json next to its
    // documents).
    static string ResolveDataset(string[] args)
    {
        int at = Array.IndexOf(args, "--dataset");
        if (at >= 0 && at + 1 < args.Length)
        {
            var given = Path.GetFullPath(args[at + 1]);
            if (!File.Exists(Path.Combine(given, "pairs.json")))
            { Console.Error.WriteLine($"no pairs.json in {given}"); Environment.Exit(1); }
            return given;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "datasets")))
            dir = dir.Parent;
        if (dir == null)
        { Console.Error.WriteLine("datasets folder not found; pass --dataset <folder>"); Environment.Exit(1); }
        return Path.Combine(dir.FullName, "datasets", "vhtoolkit");
    }

    // Retrieval augmentation in miniature: the dataset's first questions asked of raw Ollama and
    // of the RAG proxy side by side. With a corpus the model cannot already know - material past
    // its training cutoff, or private to the deployment - the raw side visibly confabulates where
    // the proxy answers from the documents. Assumes the proxy layout: proxy on 11434, raw Ollama
    // on 11436, and the proxy serving the SAME corpus as the dataset.
    static void ProxySmoke(List<Pair> pairs)
    {
        foreach (var p in pairs.Take(3))
        {
            var q = p.question + " Answer in two sentences.";
            Console.WriteLine($"\nQ: {q}");
            Console.WriteLine($"  RAW   : {Chat("http://127.0.0.1:11436", q)}");
            Console.WriteLine($"  PROXY : {Chat("http://127.0.0.1:11434", q)}");
        }
    }

    static string Chat(string baseUrl, string question)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                model = "phi4-mini",
                messages = new[] { new { role = "user", content = question } },
                stream = false,
                temperature = 0.2
            });
            var resp = Http.PostAsync(baseUrl + "/v1/chat/completions",
                new StringContent(payload, Encoding.UTF8, "application/json")).Result;
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(resp.Content.ReadAsStringAsync().Result);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                          .GetProperty("content").GetString() ?? "";
            return text.Replace("\n", " ").Trim();
        }
        catch (Exception e) { return "ERROR: " + e.Message; }
    }

    // Calibrates KnowledgeIndex.MinSemanticScore, the relevance floor below which a passage
    // is discarded rather than added to the prompt. Reports, per pair, the cosine of the
    // best-scoring chunk that belongs to an accepted page - the score a floor must stay
    // under to avoid dropping a correct passage - then sweeps candidate floors and reports
    // semantic and hybrid hit@3 at each. Query vectors are embedded once and reused across
    // the sweep, so the cost is one embedding pass regardless of how many floors are tried.
    static void FloorSweep(List<Pair> pairs, KnowledgeIndex index)
    {
        float savedFloor = index.MinSemanticScore;
        index.MinSemanticScore = -1f;  // floor off while measuring the raw distribution

        var queryVectors = new Dictionary<string, float[]>();
        foreach (var p in pairs)
            queryVectors[p.id] = Embed(new List<string> { p.question })[0];

        Console.WriteLine("\n=== cosine of the best accepted-page chunk, per pair ===");
        var expectedScores = new List<(string id, float cos)>();
        foreach (var p in pairs)
        {
            // Deep k so the accepted page is found even when it ranks poorly.
            var ranked = index.ScoreSemantic(queryVectors[p.id], index.ChunkCount);
            float best = -1f;
            foreach (var passage in ranked)
                if (p.accept.Contains(passage.title)) { best = passage.score; break; }
            expectedScores.Add((p.id, best));
            Console.WriteLine($"  {p.id,-5} {(best < 0f ? "NOT FOUND" : best.ToString("F4"))}" +
                              $"  {(p.hard ? "hard" : "easy")}  {p.question}");
        }

        var found = expectedScores.Where(e => e.cos >= 0f).Select(e => e.cos).OrderBy(c => c).ToList();
        if (found.Count == 0) { Console.WriteLine("  no accepted pages found at all - check the dataset"); return; }
        Console.WriteLine($"\n  min {found[0]:F4} | p5 {found[Math.Max(0, found.Count / 20)]:F4}" +
                          $" | median {found[found.Count / 2]:F4} | max {found[found.Count - 1]:F4}" +
                          $" | pairs measured {found.Count}/{pairs.Count}");
        Console.WriteLine($"  a floor at or above {found[0]:F4} starts dropping correct passages");

        // The floor exists to reject queries the corpus does not cover, so a useful floor must
        // sit ABOVE the top cosine an unrelated query produces and BELOW the lowest correct-passage
        // cosine above. If those two numbers overlap, no floor value can do the job and the
        // rejection has to come from somewhere else.
        Console.WriteLine("\n=== top cosine for queries the corpus does not cover ===");
        float offTopicMax = -1f;
        foreach (var q in OffTopicQueries)
        {
            var top = index.ScoreSemantic(Embed(new List<string> { q })[0], 1);
            float cos = top.Count > 0 ? top[0].score : -1f;
            if (cos > offTopicMax) offTopicMax = cos;
            Console.WriteLine($"  {cos:F4}  {(top.Count > 0 ? top[0].title : "-"),-42}  {q}");
        }
        Console.WriteLine($"\n  highest off-topic cosine {offTopicMax:F4} | lowest correct-passage cosine {found[0]:F4}");
        Console.WriteLine(offTopicMax < found[0]
            ? $"  usable floor window: {offTopicMax:F4} .. {found[0]:F4} - midpoint {(offTopicMax + found[0]) / 2f:F4}"
            : "  NO usable floor: off-topic queries score as high as correct passages");

        Console.WriteLine("\n=== hit@3 by floor ===");
        Console.WriteLine("  floor   semantic   hybrid");
        for (float floor = 0f; floor <= 0.601f; floor += 0.05f)
        {
            index.MinSemanticScore = floor;
            int semHits = 0, hybHits = 0;
            foreach (var p in pairs)
            {
                var sem = index.ScoreSemantic(queryVectors[p.id], 3).Select(x => x.title);
                var hyb = index.ScoreHybrid(p.question, null, 0f, queryVectors[p.id], 3).Select(x => x.title);
                if (sem.Any(t => p.accept.Contains(t))) semHits++;
                if (hyb.Any(t => p.accept.Contains(t))) hybHits++;
            }
            Console.WriteLine($"  {floor:F2}    {semHits,2}/{pairs.Count}      {hybHits,2}/{pairs.Count}");
        }

        index.MinSemanticScore = savedFloor;
    }

    static void Score(string label, List<Pair> pairs, Func<string, List<string>> top3)
    {
        int hits = 0, hardHits = 0, hardTotal = pairs.Count(p => p.hard);
        var misses = new List<string>();
        foreach (var p in pairs)
        {
            var got = top3(p.question);
            bool hit = got.Any(g => p.accept.Contains(g));
            if (hit) { hits++; if (p.hard) hardHits++; }
            else misses.Add($"{p.id} \"{p.question}\" -> [{string.Join(", ", got)}]");
        }
        int easyTotal = pairs.Count - hardTotal, easyHits = hits - hardHits;
        Console.WriteLine($"\n=== {label} hit@3 ===  overall {hits}/{pairs.Count} = {(double)hits / pairs.Count:F2}" +
                          $" | easy {easyHits}/{easyTotal} = {(double)easyHits / easyTotal:F2}" +
                          $" | hard {hardHits}/{hardTotal} = {(double)hardHits / hardTotal:F2}");
        foreach (var m in misses) Console.WriteLine("  miss: " + m);
    }

    static List<Pair> LoadPairs(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("pairs").EnumerateArray().Select(e => new Pair(
            e.GetProperty("id").GetString(),
            e.GetProperty("question").GetString(),
            e.GetProperty("accept").EnumerateArray().Select(a => a.GetString()).ToArray(),
            e.GetProperty("hard").GetBoolean())).ToList();
    }

    static List<KnowledgeItem> LoadDir(string dir)
        => Directory.GetFiles(dir, "*.md").Concat(Directory.GetFiles(dir, "*.txt"))
            .Select(f => new KnowledgeItem { id = Path.GetFileNameWithoutExtension(f),
                title = Path.GetFileNameWithoutExtension(f), text = File.ReadAllText(f), type = "file" })
            .ToList();

    // Embeds every chunk of the index (in index order, batched) via Ollama and attaches
    // the vectors - the same flow KnowledgeSystemUnity performs with its embeddings
    // provider, so the eval exercises the exact package retrieval paths.
    static void AttachVectors(KnowledgeIndex index)
    {
        var texts = new List<string>();
        for (int i = 0; i < index.ChunkCount; i++)
            texts.Add(index.GetEmbedText(i));
        var vectors = new List<float[]>();
        s_embeddingDocuments = true;
        for (int i = 0; i < texts.Count; i += 64)
            vectors.AddRange(Embed(texts.Skip(i).Take(64).ToList()));
        s_embeddingDocuments = false;
        index.SetChunkVectors(vectors);
        Console.WriteLine($"vectors attached: {vectors.Count} chunks via {EmbedBackendName}" +
                          (s_useEmbedPrefix ? " (task prefixes on)" : ""));
    }

    static List<float[]> Embed(List<string> texts)
    {
        string url = s_useOpenAi ? OpenAiEmbedUrl : OllamaUrl + "/v1/embeddings";
        var input = texts;
        if (s_useEmbedPrefix)
        {
            var prefix = s_embeddingDocuments ? s_documentPrefix : s_queryPrefix;
            input = texts.Select(t => prefix + t).ToList();
        }
        var payload = JsonSerializer.Serialize(new { model = EmbedBackendName, input });
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        if (s_useOpenAi)
            request.Headers.Add("Authorization", "Bearer " + s_openAiKey);
        var resp = Http.SendAsync(request).Result;
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(resp.Content.ReadAsStringAsync().Result);
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(d => d.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray())
            .ToList();
    }
}
