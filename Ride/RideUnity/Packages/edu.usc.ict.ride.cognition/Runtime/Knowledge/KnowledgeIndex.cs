using System;
using System.Collections.Generic;

namespace Ride.Conversation
{
    /// <summary>
    /// Retrieval index over chunked knowledge items. Always supports lexical scoring
    /// (idf-weighted term overlap); optionally also supports semantic and hybrid scoring
    /// once per-chunk embedding vectors are supplied via <see cref="SetChunkVectors"/>.
    /// The index never performs embedding itself: callers embed
    /// <see cref="GetEmbedText"/> for each chunk (and the query) with whatever
    /// embeddings provider the application uses, keeping this core free of transport
    /// and vendor dependencies.
    /// </summary>
    public class KnowledgeIndex
    {
        public struct Chunk
        {
            public string itemId;
            public string title;
            public int    index;  // position within the item
            public string text;
        }

        const int ChunkTargetChars = 600;

        // Max context (previous-turn) terms that participate in weighted scoring.
        const int MaxContextTerms = 8;

        static readonly HashSet<string> s_stopwords = new HashSet<string>(
            ("a an and are as at be but by for from has have i in is it its me my of on or so " +
             "that the this to was we what when where which who will with you your").Split(' '));

        // Reciprocal-rank-fusion constant (standard value) and how deep each ranking
        // participates in the fusion relative to the requested k. The lexical ranking
        // contributes at reduced weight: embeddings judge relevance by meaning and win
        // on paraphrased queries, while term overlap ranks confidently on shared
        // vocabulary alone; equal-weight fusion lets those term matches outvote correct
        // semantic results. Reduced lexical weight keeps exact-term wins as a tiebreak
        // without degrading semantic ranking quality.
        const int   RrfConstant    = 60;
        const int   FusionDepthMin = 12;
        const float LexicalFusionWeight = 0.5f;

        readonly List<Chunk>                    m_chunks      = new List<Chunk>();
        readonly List<Dictionary<string, int>>  m_chunkTf     = new List<Dictionary<string, int>>();
        readonly Dictionary<string, int>        m_df          = new Dictionary<string, int>();
        float[][]                               m_vectors;    // per-chunk embeddings; null until supplied

        public int ChunkCount => m_chunks.Count;

        /// <summary>
        /// Minimum tf-idf score a chunk must reach to be considered a lexical match. Chunks below
        /// it are dropped before ranking, so a query with no real topical signal yields no
        /// passages rather than the corpus's least-irrelevant one. 0 disables the floor.
        /// Applied to the lexical component, including the component that feeds hybrid fusion.
        /// </summary>
        public float MinLexicalScore { get; set; } = 0f;

        /// <summary>
        /// Minimum cosine similarity a chunk must reach to be considered a semantic match.
        /// Applied to the semantic component, including the component that feeds hybrid fusion.
        /// Cosine can be negative, so -1 disables the floor.
        /// </summary>
        public float MinSemanticScore { get; set; } = -1f;

        /// <summary>True once per-chunk embedding vectors have been supplied for the current build.</summary>
        public bool HasVectors => m_vectors != null;

        /// <summary>
        /// The text to embed for the chunk at the given index: title plus body, matching
        /// what lexical scoring indexes. Embed these (in index order) and pass the
        /// resulting vectors to <see cref="SetChunkVectors"/>.
        /// </summary>
        public string GetEmbedText(int chunkIndex)
        {
            var chunk = m_chunks[chunkIndex];
            return chunk.title + "\n" + chunk.text;
        }

        /// <summary>
        /// Supplies one embedding vector per chunk, in chunk order, enabling
        /// <see cref="ScoreSemantic"/> and <see cref="ScoreHybrid"/>. Must be called
        /// after <see cref="Build"/> with exactly <see cref="ChunkCount"/> vectors of a
        /// consistent dimension; rebuilding the index clears any previous vectors.
        /// </summary>
        public void SetChunkVectors(IReadOnlyList<float[]> vectors)
        {
            if (vectors == null || vectors.Count != m_chunks.Count)
                throw new ArgumentException(
                    $"Expected {m_chunks.Count} chunk vectors, got {vectors?.Count.ToString() ?? "null"}.");
            var copy = new float[vectors.Count][];
            for (int i = 0; i < vectors.Count; i++)
                copy[i] = vectors[i];
            m_vectors = copy;
        }

        /// <summary>Rebuilds the index from the given items (replaces any previous content).</summary>
        public void Build(IReadOnlyList<KnowledgeItem> items)
        {
            m_chunks.Clear();
            m_chunkTf.Clear();
            m_df.Clear();
            m_vectors = null; // vectors belong to the previous chunk set

            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.text))
                    continue;
                int idx = 0;
                foreach (var text in ChunkText(item.text))
                {
                    m_chunks.Add(new Chunk
                    {
                        itemId = item.id,
                        title  = item.title ?? "untitled",
                        index  = idx++,
                        text   = text
                    });
                }
            }

            // Term frequencies per chunk (body + title tokens), document frequencies across chunks.
            foreach (var chunk in m_chunks)
            {
                var tf = new Dictionary<string, int>();
                CountTokens(chunk.text, tf);
                CountTokens(chunk.title, tf);
                m_chunkTf.Add(tf);
                foreach (var term in tf.Keys)
                    m_df[term] = m_df.TryGetValue(term, out var n) ? n + 1 : 1;
            }
        }

        /// <summary>
        /// Scores every chunk against the query and returns the top-k with score > 0, best
        /// first. Score per chunk: sum over query terms of (1 + ln(tf)) * ln(1 + N/df).
        /// </summary>
        public List<ScoredPassage> Score(string query, int k) => Score(query, null, 0f, k);

        /// <summary>
        /// Weighted variant: terms from the query count fully; terms from the optional
        /// conversation context count at contextWeight. This lets anaphoric follow-ups
        /// ("like which ones?") inherit the topic of the previous turn without letting
        /// that turn's many generic terms outweigh a short, specific query.
        /// </summary>
        public List<ScoredPassage> Score(string query, string contextQuery, float contextWeight, int k)
        {
            if (m_chunks.Count == 0 || string.IsNullOrWhiteSpace(query) || k <= 0)
                return new List<ScoredPassage>();
            return Materialize(RankLexical(query, contextQuery, contextWeight), k);
        }

        /// <summary>
        /// Scores every chunk against a query embedding by cosine similarity and returns
        /// the top-k, best first. Requires <see cref="SetChunkVectors"/>. Callers that
        /// want conversation context to participate should blend the context embedding
        /// into the query vector before calling (the query staying dominant), mirroring
        /// the weighted-context behavior of the lexical scorer.
        /// </summary>
        public List<ScoredPassage> ScoreSemantic(float[] queryVector, int k)
        {
            if (m_vectors == null || m_chunks.Count == 0 || queryVector == null || k <= 0)
                return new List<ScoredPassage>();
            return Materialize(RankSemantic(queryVector), k);
        }

        /// <summary>
        /// Fuses the lexical and semantic rankings with reciprocal-rank fusion and
        /// returns the top-k, best first. Robust to the two scorers' incomparable score
        /// scales; a chunk ranked highly by either scorer surfaces. Requires
        /// <see cref="SetChunkVectors"/>; without vectors this equals lexical scoring.
        /// </summary>
        public List<ScoredPassage> ScoreHybrid(string query, string contextQuery, float contextWeight,
                                               float[] queryVector, int k)
        {
            if (m_chunks.Count == 0 || k <= 0)
                return new List<ScoredPassage>();
            if (m_vectors == null || queryVector == null)
                return Score(query, contextQuery, contextWeight, k);

            int depth = Math.Max(k * 3, FusionDepthMin);
            var lexical  = RankLexical(query, contextQuery, contextWeight);
            var semantic = RankSemantic(queryVector);

            var fused = new Dictionary<int, float>();
            for (int r = 0; r < lexical.Count && r < depth; r++)
                fused[lexical[r].i] = (fused.TryGetValue(lexical[r].i, out var s) ? s : 0f)
                                      + LexicalFusionWeight / (RrfConstant + r + 1);
            for (int r = 0; r < semantic.Count && r < depth; r++)
                fused[semantic[r].i] = (fused.TryGetValue(semantic[r].i, out var s) ? s : 0f)
                                       + 1f / (RrfConstant + r + 1);

            var ranked = new List<(float score, int i)>();
            foreach (var kv in fused)
                ranked.Add((kv.Value, kv.Key));
            ranked.Sort((a, b) => b.score.CompareTo(a.score));
            return Materialize(ranked, k);
        }

        // Full lexical ranking (all chunks with score > 0, best first).
        List<(float score, int i)> RankLexical(string query, string contextQuery, float contextWeight)
        {
            var scored = new List<(float score, int i)>();
            if (m_chunks.Count == 0 || string.IsNullOrWhiteSpace(query))
                return scored;

            var weightedTerms = new List<(string term, float weight)>();
            foreach (var term in Tokenize(query))
                weightedTerms.Add((term, 1f));
            if (!string.IsNullOrWhiteSpace(contextQuery) && contextWeight > 0f)
            {
                // Only the rarest few context terms participate. A full conversation turn
                // contributes many generic terms which, against a topically homogeneous
                // corpus, match every chunk and can collectively outweigh a short specific
                // query. The high-idf terms are the ones that actually carry an anaphoric
                // topic; terms absent from the corpus sort to idf 0 and drop away.
                var seen = new HashSet<string>();
                var contextTerms = new List<string>();
                foreach (var term in Tokenize(contextQuery))
                    if (seen.Add(term))
                        contextTerms.Add(term);
                contextTerms.Sort((a, b) => Idf(b).CompareTo(Idf(a)));
                for (int i = 0; i < contextTerms.Count && i < MaxContextTerms; i++)
                    if (Idf(contextTerms[i]) > 0f)
                        weightedTerms.Add((contextTerms[i], contextWeight));
            }

            int n = m_chunks.Count;

            for (int i = 0; i < m_chunks.Count; i++)
            {
                var tf = m_chunkTf[i];
                float score = 0f;
                foreach (var (term, weight) in weightedTerms)
                {
                    if (!tf.TryGetValue(term, out var f))
                        continue;
                    m_df.TryGetValue(term, out var df);
                    score += weight * (1f + (float)Math.Log(f)) * (float)Math.Log(1.0 + (double)n / (df > 0 ? df : 1));
                }
                if (score > 0f && score >= MinLexicalScore)
                    scored.Add((score, i));
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));
            return scored;
        }

        // Full semantic ranking by cosine similarity (all chunks, best first).
        List<(float score, int i)> RankSemantic(float[] queryVector)
        {
            var scored = new List<(float score, int i)>();
            for (int i = 0; i < m_vectors.Length; i++)
            {
                var vec = m_vectors[i];
                if (vec == null || vec.Length != queryVector.Length)
                    continue;
                float cos = Cosine(queryVector, vec);
                if (cos >= MinSemanticScore)
                    scored.Add((cos, i));
            }
            scored.Sort((a, b) => b.score.CompareTo(a.score));
            return scored;
        }

        // Converts the top-k of a ranking into passages.
        List<ScoredPassage> Materialize(List<(float score, int i)> ranked, int k)
        {
            var results = new List<ScoredPassage>();
            for (int r = 0; r < ranked.Count && r < k; r++)
            {
                var chunk = m_chunks[ranked[r].i];
                results.Add(new ScoredPassage
                {
                    itemId = chunk.itemId,
                    title  = chunk.title,
                    text   = chunk.text,
                    score  = ranked[r].score
                });
            }
            return results;
        }

        static float Cosine(float[] a, float[] b)
        {
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += (double)a[i] * b[i];
                na  += (double)a[i] * a[i];
                nb  += (double)b[i] * b[i];
            }
            return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-9));
        }

        // Splits on blank lines and packs paragraphs up to ~ChunkTargetChars per chunk
        // (paragraphs are never split unless a single paragraph alone exceeds the target).
        internal static List<string> ChunkText(string text)
        {
            var chunks = new List<string>();
            var buf = string.Empty;
            foreach (var raw in System.Text.RegularExpressions.Regex.Split(text ?? string.Empty, "\n\\s*\n"))
            {
                var para = raw.Trim();
                if (para.Length == 0)
                    continue;
                if (buf.Length > 0 && buf.Length + 2 + para.Length > ChunkTargetChars)
                {
                    chunks.Add(buf);
                    buf = para;
                }
                else
                {
                    buf = buf.Length > 0 ? buf + "\n\n" + para : para;
                }
            }
            if (buf.Length > 0)
                chunks.Add(buf);
            return chunks;
        }

        float Idf(string term)
            => m_df.TryGetValue(term, out var df) && df > 0
               ? (float)Math.Log(1.0 + (double)m_chunks.Count / df) : 0f;

        // Lowercase, strip non-alphanumerics, drop single chars and stopwords, and apply a
        // light plural stemmer so singular and plural forms match each other.
        internal static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            foreach (var c in (text ?? string.Empty).ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) && c < 128 ? c : ' ');
            foreach (var token in sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (token.Length > 1 && !s_stopwords.Contains(token))
                    tokens.Add(Stem(token));
            return tokens;
        }

        // Harman S-stemmer: cheap, conservative English plural normalization. Applied to
        // both corpus and query tokens, so matching stays consistent.
        static string Stem(string token)
        {
            if (token.Length > 4 && token.EndsWith("ies") && !token.EndsWith("eies") && !token.EndsWith("aies"))
                return token.Substring(0, token.Length - 3) + "y";
            if (token.Length > 3 && token.EndsWith("es") && !token.EndsWith("aes") && !token.EndsWith("ees") && !token.EndsWith("oes"))
                return token.Substring(0, token.Length - 1);
            if (token.Length > 3 && token.EndsWith("s") && !token.EndsWith("us") && !token.EndsWith("ss") && !token.EndsWith("is"))
                return token.Substring(0, token.Length - 1);
            return token;
        }

        static void CountTokens(string text, Dictionary<string, int> tf)
        {
            foreach (var token in Tokenize(text))
                tf[token] = tf.TryGetValue(token, out var n) ? n + 1 : 1;
        }
    }
}
