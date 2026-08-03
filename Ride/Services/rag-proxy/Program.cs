// RIDE RAG proxy - a transparent retrieval-augmentation layer in front of a local
// OpenAI-compatible LLM endpoint (Ollama, vLLM, or anything speaking the same API).
//
// Clients call this proxy exactly as they would call the upstream endpoint. For every
// /v1/chat/completions request, the proxy retrieves the passages most relevant to the
// latest user message from a local document corpus and injects them into the outgoing
// prompt as framed reference material. The response is returned unchanged, so any
// client gains retrieval-augmented answers with a single base-URL change and no
// knowledge of the corpus or the retrieval mechanism.
//
// Retrieval is hybrid: semantic (embeddings from an OpenAI-compatible /v1/embeddings
// endpoint) with lexical tf-idf fallback while the semantic index is still building or
// when the embedding endpoint is unavailable. Both paths use the RIDE cognition
// package's retrieval sources compiled directly into this service.
//
// Chat and embeddings are configured separately because servers differ: Ollama serves
// both from one instance, while vLLM serves ONE model per process and therefore needs a
// second process for embeddings. EMBED_URL defaults to UPSTREAM_URL, so a single-instance
// server needs no extra configuration.
//
// Configuration (environment variables):
//   RAG_PORT      listen port                  (default 11434, Ollama's canonical port)
//   UPSTREAM_URL  chat completions base URL    (default http://127.0.0.1:11436)
//                 OLLAMA_URL is still honored for compatibility
//   EMBED_URL     embeddings base URL          (default: same as UPSTREAM_URL)
//   CORPUS_DIR    folder of .txt/.md documents (default ./corpus)
//   EMBED_MODEL   embedding model name         (default nomic-embed-text)
//   RAG_TOPK      passages per request         (default 4)
//   RAG_MAXCTX    max context characters       (default 3500)
//
// Example, Ollama (chat + embeddings on one instance):
//   UPSTREAM_URL=http://127.0.0.1:11436   EMBED_MODEL=nomic-embed-text
// Example, vLLM (one model per process):
//   UPSTREAM_URL=http://127.0.0.1:8000    EMBED_URL=http://127.0.0.1:8001
//   EMBED_MODEL=vhtoolkit-embed
//
// Debug/demo surface:
//   GET  /rag/status              index state, counts, mode
//   GET  /rag/retrieve?q=...      what would be retrieved (both modes)
//   POST /rag/reload              reload the corpus and rebuild both indexes

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ride.Conversation;

// By default the proxy owns Ollama's canonical port (11434): clients keep their default
// Ollama configuration and transparently gain retrieval augmentation - whether the local
// endpoint uses RAG is the endpoint's concern, never the client's. The raw Ollama
// container listens on the internal port 11436 (see WebServices/ollama/.env). Fronting a
// different server means owning its port and moving it aside the same way.
var port       = Env("RAG_PORT", "11434");
var upstreamUrl = Env("UPSTREAM_URL", Env("OLLAMA_URL", "http://127.0.0.1:11436")).TrimEnd('/');
var embedUrl   = Env("EMBED_URL", upstreamUrl).TrimEnd('/');
var corpusDir  = Env("CORPUS_DIR", Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "corpus"));
var embedModel = Env("EMBED_MODEL", "nomic-embed-text");
var topK       = int.Parse(Env("RAG_TOPK", "4"));
var maxCtx     = int.Parse(Env("RAG_MAXCTX", "3500"));

var settings = new KnowledgeSettings { topK = topK, maxContextChars = maxCtx };
var store = new RagStore(corpusDir, embedUrl, embedModel);
_ = store.ReloadAsync();  // build indexes in the background; lexical serves immediately

var builder = WebApplication.CreateBuilder();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();
var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

app.MapGet("/rag/status", () => Results.Json(store.Status()));

app.MapGet("/rag/retrieve", (string q, string context) =>
    Results.Json(store.Debug(q, context ?? "", topK)));

app.MapPost("/rag/reload", async () => { await store.ReloadAsync(); return Results.Json(store.Status()); });

app.MapPost("/v1/chat/completions", async (HttpContext ctx) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var root = JsonNode.Parse(body);
    var messages = root?["messages"]?.AsArray();

    if (messages != null && messages.Count > 0)
    {
        // Latest user message is the query; the turn before it is retrieval context.
        int lastUser = -1;
        for (int i = messages.Count - 1; i >= 0; i--)
            if ((string)messages[i]?["role"] == "user") { lastUser = i; break; }

        if (lastUser >= 0)
        {
            var query = (string)messages[lastUser]["content"] ?? "";

            // Don't augment an already-augmented request. A RIDE client with its own
            // knowledge system prepends this same reference block before sending; a second
            // layer here would stack corpora and retrieve against the preamble text. RAG
            // belongs in exactly one place per client - if the client does it, the endpoint
            // stays out of the way.
            if (query.Contains(RagStore.PreambleMarker))
            {
                Console.WriteLine("[rag] passthrough: request already carries a reference block (client-side RAG)");
            }
            else
            {
                var context = new StringBuilder();
                for (int i = Math.Max(0, lastUser - 2); i < lastUser; i++)
                    context.AppendLine((string)messages[i]?["content"] ?? "");

                var block = store.BuildContextBlock(settings, query, context.ToString(), out var summary);
                if (block != null)
                    messages[lastUser]["content"] = block + "\n\n" + query;
                Console.WriteLine($"[rag] q=\"{Trunc(query, 60)}\" {summary}");
            }
        }
    }

    var upstream = new HttpRequestMessage(HttpMethod.Post, upstreamUrl + "/v1/chat/completions")
    { Content = new StringContent(root?.ToJsonString() ?? body, Encoding.UTF8, "application/json") };
    var resp = await http.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead);
    ctx.Response.StatusCode = (int)resp.StatusCode;
    ctx.Response.ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    await resp.Content.CopyToAsync(ctx.Response.Body);   // works for streamed responses too
});

// Generic passthrough for everything else (model lists, embeddings, native API).
app.MapFallback(async (HttpContext ctx) =>
{
    var target = upstreamUrl + ctx.Request.Path + ctx.Request.QueryString;
    var upstream = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);
    if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        upstream.Content = new StringContent(body, Encoding.UTF8,
            ctx.Request.ContentType ?? "application/json");
    }
    var resp = await http.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead);
    ctx.Response.StatusCode = (int)resp.StatusCode;
    ctx.Response.ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
    await resp.Content.CopyToAsync(ctx.Response.Body);
});

// Name both URLs at startup: a wrong or unreachable embeddings endpoint does not fail the
// proxy, it silently degrades retrieval to the weaker lexical tier, which is easy to miss.
Console.WriteLine($"[rag] proxy on http://127.0.0.1:{port} -> chat {upstreamUrl}");
Console.WriteLine($"[rag] embeddings {embedUrl} model={embedModel}" +
    (embedUrl == upstreamUrl ? " (same instance as chat)" : " (separate instance)"));
Console.WriteLine($"[rag] corpus={Path.GetFullPath(corpusDir)}");
app.Run($"http://127.0.0.1:{port}");

static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) ?? fallback;
static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "...";

// ---------------------------------------------------------------------------------

/// <summary>
/// The corpus store: documents, a lexical index (always available), and a semantic index
/// (embeddings from an OpenAI-compatible endpoint, built in the background). Retrieval
/// prefers the semantic index and falls back to lexical.
/// </summary>
sealed class RagStore
{
    // Leading fragment of the default reference-block preamble (shared by the RIDE
    // KnowledgeSettings default and this proxy). Used to detect a request a client
    // already augmented, so the proxy does not stack a second RAG layer on top.
    public const string PreambleMarker = "Reference material related to the conversation";

    readonly string m_dir, m_embedUrl, m_embedModel;
    readonly HttpClient m_http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    readonly object m_lock = new object();

    KnowledgeIndex m_lexical = new KnowledgeIndex();
    List<(string title, string text, float[] vec)> m_vectors = new();
    int m_docCount;
    volatile bool m_semanticReady;
    string m_lastError = "";

    public RagStore(string dir, string embedUrl, string embedModel)
    { m_dir = dir; m_embedUrl = embedUrl; m_embedModel = embedModel; }

    public object Status() => new
    {
        corpusDir = Path.GetFullPath(m_dir),
        documents = m_docCount,
        lexicalChunks = m_lexical.ChunkCount,
        semanticChunks = m_vectors.Count,
        semanticReady = m_semanticReady,
        embedUrl = m_embedUrl,
        embedModel = m_embedModel,
        lastError = m_lastError
    };

    public async Task ReloadAsync()
    {
        var items = Directory.Exists(m_dir)
            ? Directory.GetFiles(m_dir, "*.txt").Concat(Directory.GetFiles(m_dir, "*.md"))
                .Select(f => new KnowledgeItem { id = Path.GetFileNameWithoutExtension(f),
                    title = Path.GetFileNameWithoutExtension(f).Replace('-', ' '),
                    text = File.ReadAllText(f), type = "file" }).ToList()
            : new List<KnowledgeItem>();

        var lexical = new KnowledgeIndex();
        lexical.Build(items);
        lock (m_lock) { m_lexical = lexical; m_docCount = items.Count; m_semanticReady = false; m_vectors = new(); }
        Console.WriteLine($"[rag] corpus loaded: {items.Count} documents, {lexical.ChunkCount} chunks (lexical ready)");

        try
        {
            var vectors = new List<(string, string, float[])>();
            foreach (var item in items)
            {
                var chunks = KnowledgeIndex.ChunkText(item.text);
                for (int i = 0; i < chunks.Count; i += 64)
                {
                    var batch = chunks.Skip(i).Take(64).Select(c => item.title + "\n" + c).ToList();
                    var vecs = await EmbedAsync(batch);
                    for (int j = 0; j < batch.Count; j++)
                        vectors.Add((item.title, batch[j], vecs[j]));
                }
            }
            lock (m_lock) { m_vectors = vectors; m_semanticReady = true; m_lastError = ""; }
            Console.WriteLine($"[rag] semantic index ready: {vectors.Count} chunks via {m_embedModel}");
        }
        catch (Exception e)
        {
            m_lastError = e.Message;
            Console.WriteLine($"[rag] semantic index unavailable ({e.Message}) - lexical fallback active");
        }
    }

    /// <summary>Builds the framed reference block for a request, or null when nothing applies.</summary>
    public string BuildContextBlock(KnowledgeSettings settings, string query, string context, out string summary)
    {
        // Acknowledgment turns carry no query signal; retrieve on the previous turn instead.
        var contentTerms = KnowledgeIndex.Tokenize(query).Count;
        if (contentTerms <= 1)
        {
            if (string.IsNullOrWhiteSpace(context)) { summary = "skipped (low signal, no context)"; return null; }
            query = context; context = "";
        }

        List<(string title, string text)> passages;
        string mode;
        if (m_semanticReady)
        {
            passages = SemanticTop(query, context, settings.contextQueryWeight, settings.topK);
            mode = "semantic";
        }
        else
        {
            passages = m_lexical.Score(query, context, settings.contextQueryWeight, settings.topK)
                                .Select(p => (p.title, p.text)).ToList();
            mode = "lexical";
        }

        if (passages.Count == 0) { summary = $"{mode}: no matches"; return null; }
        summary = $"{mode}: {string.Join(" | ", passages.Select(p => p.title))}";

        var sb = new StringBuilder(settings.contextPreamble);
        int budget = settings.maxContextChars;
        foreach (var (title, text) in passages)
        {
            var entry = $"\n\n[{title}]\n{text}";
            if (entry.Length > budget) { if (sb.Length == settings.contextPreamble.Length) sb.Append(entry, 0, budget); break; }
            sb.Append(entry);
            budget -= entry.Length;
        }
        return sb.ToString();
    }

    public object Debug(string query, string context, int k) => new
    {
        semanticReady = m_semanticReady,
        semantic = m_semanticReady
            ? SemanticTop(query, context, 0.3f, k).Select(p => p.title).ToList()
            : new List<string>(),
        lexical = m_lexical.Score(query, context, 0.3f, k).Select(p => $"{p.title} ({p.score:F1})").ToList()
    };

    List<(string title, string text)> SemanticTop(string query, string context, float contextWeight, int k)
    {
        var q = EmbedAsync(new List<string> { query }).Result[0];
        if (!string.IsNullOrWhiteSpace(context) && contextWeight > 0f)
        {
            // Blend the query vector toward the conversation context, keeping the query dominant.
            var c = EmbedAsync(new List<string> { context }).Result[0];
            for (int i = 0; i < q.Length; i++) q[i] += contextWeight * c[i];
        }
        List<(string title, string text, float[] vec)> snapshot;
        lock (m_lock) snapshot = m_vectors;
        return snapshot.Select(v => (v.title, v.text, score: Cosine(q, v.vec)))
                       .OrderByDescending(v => v.score).Take(k)
                       .Select(v => (v.title, v.text)).ToList();
    }

    static float Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-9));
    }

    async Task<List<float[]>> EmbedAsync(List<string> texts)
    {
        var payload = JsonSerializer.Serialize(new { model = m_embedModel, input = texts });
        var resp = await m_http.PostAsync(m_embedUrl + "/v1/embeddings",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(d => d.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray())
            .ToList();
    }
}
