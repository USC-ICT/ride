// RAG eval harness. Compiles the REAL RIDE package retrieval sources (see Eval.csproj).
//
// The harness is pipeline only - it contains no corpus content and no questions. A
// DATASET supplies both: a folder holding pairs.json (questions + accepted page titles)
// and the corpus documents (*.txt / *.md, either in a corpus/ subfolder or directly in
// the dataset folder). Datasets are separate from the tool by design: the VHToolkit
// dataset ships in datasets/vhtoolkit (public content), the internal drone-demo corpus
// stays unversioned next to the rag-proxy, and a researcher's own material is just
// another folder.
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
    const string EmbedModel = "nomic-embed-text";
    static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

    record Pair(string id, string question, string[] accept, bool hard);

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
        bool useOllama = args.Contains("--ollama");

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
        }
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

    // The demo in miniature: the dataset's first questions asked of raw Ollama and of
    // the RAG proxy side by side. With a corpus the model cannot know (past its training
    // cutoff, or internal material), the raw side visibly confabulates where the proxy
    // answers from the documents. Assumes the proxy layout: proxy on 11434, raw Ollama
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
        for (int i = 0; i < texts.Count; i += 64)
            vectors.AddRange(Embed(texts.Skip(i).Take(64).ToList()));
        index.SetChunkVectors(vectors);
        Console.WriteLine($"vectors attached: {vectors.Count} chunks via {EmbedModel}");
    }

    static List<float[]> Embed(List<string> texts)
    {
        var payload = JsonSerializer.Serialize(new { model = EmbedModel, input = texts });
        var resp = Http.PostAsync(OllamaUrl + "/v1/embeddings",
            new StringContent(payload, Encoding.UTF8, "application/json")).Result;
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(resp.Content.ReadAsStringAsync().Result);
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(d => d.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray())
            .ToList();
    }
}
