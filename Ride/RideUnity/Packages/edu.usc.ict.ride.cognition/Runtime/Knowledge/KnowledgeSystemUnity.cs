using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>
    /// Default IKnowledgeSystem implementation. Lexical retrieval (tf-idf over paragraph
    /// chunks via <see cref="KnowledgeIndex"/>) always works: no network, no cost,
    /// WebGL-safe. When an <see cref="IEmbeddingsSystem"/> is also registered, the corpus
    /// is embedded in the background after <see cref="SetItems"/> and retrieval upgrades
    /// to semantic/hybrid scoring per <see cref="KnowledgeSettings.retrievalMode"/>
    /// (Auto = hybrid once vectors are ready, lexical until then and whenever the
    /// embeddings service is unavailable - degradation is automatic, never an error).
    ///
    /// Add this component to any GameObject in the scene to activate retrieval
    /// (RideSystemMonoBehaviour.Awake() auto-registers it; NlpSystemUnity then augments every
    /// user conversation turn automatically via KnowledgeAugmentation). Removing the component
    /// = no-op (Systems.Get returns null and requests pass through untouched). With the
    /// component present but no items set, requests also pass through untouched.
    /// </summary>
    public class KnowledgeSystemUnity : RideSystemMonoBehaviour, IKnowledgeSystem
    {
        [SerializeField] private KnowledgeSettings m_settings = new KnowledgeSettings();
        public KnowledgeSettings Settings { get => m_settings; set => m_settings = value; }

        // Chunks per embeddings request while indexing the corpus.
        const int EmbedBatchSize = 64;

        readonly KnowledgeIndex m_index = new KnowledgeIndex();
        int  m_itemCount;
        int  m_embedGeneration;   // invalidates in-flight embedding when items change
        bool m_vectorsReady;

        public int ItemCount => m_itemCount;

        /// <summary>True once the current corpus has embedding vectors (semantic/hybrid active).</summary>
        public bool VectorsReady => m_vectorsReady;

        /// <inheritdoc/>
        /// <remarks>
        /// Builds the lexical index synchronously - fine up to corpora of a few MB (roughly:
        /// 1 MB of text is ~1-2k chunks, subsecond). If an embeddings system is registered
        /// and the retrieval mode wants vectors, corpus embedding then proceeds in the
        /// background; retrieval serves lexical results until it completes.
        /// </remarks>
        public void SetItems(IReadOnlyList<KnowledgeItem> items)
        {
            m_itemCount = items?.Count ?? 0;
            m_index.Build(items);
            m_vectorsReady = false;
            m_embedGeneration++;
            StartCorpusEmbedding();
        }

        public void Clear()
        {
            m_itemCount = 0;
            m_index.Build(null);
            m_vectorsReady = false;
            m_embedGeneration++;
        }

        /// <inheritdoc/>
        public void Retrieve(string query, int k, Action<IReadOnlyList<ScoredPassage>> onComplete)
            => Retrieve(query, null, k, onComplete);

        /// <inheritdoc/>
        public void Retrieve(string query, string conversationContext, int k,
                             Action<IReadOnlyList<ScoredPassage>> onComplete)
        {
            // Relevance floors live in settings but are applied inside the index, so push the
            // current values in before every retrieval - they are tuned live from the debug menu.
            m_index.MinLexicalScore  = m_settings.minLexicalScore;
            m_index.MinSemanticScore = m_settings.minSemanticScore;

            var mode = m_settings.retrievalMode;
            var embeddings = mode != KnowledgeRetrievalMode.Lexical ? Systems.Get<IEmbeddingsSystem>() : null;
            if (embeddings == null || !m_vectorsReady)
            {
                onComplete?.Invoke(ScoreLexical(query, conversationContext, k));
                return;
            }

            // Embed the query (and, in the same call, the conversation context when present),
            // then blend the context into the query vector at the configured weight - the
            // semantic counterpart of the lexical scorer's weighted context terms.
            bool useContext = !string.IsNullOrWhiteSpace(conversationContext)
                              && m_settings.contextQueryWeight > 0f;
            var inputs = useContext
                ? new List<string> { query, conversationContext }
                : new List<string> { query };

            embeddings.EmbedBatch(inputs,
                vectors =>
                {
                    var queryVector = vectors[0];
                    if (useContext)
                    {
                        var contextVector = vectors[1];
                        for (int i = 0; i < queryVector.Length && i < contextVector.Length; i++)
                            queryVector[i] += m_settings.contextQueryWeight * contextVector[i];
                    }
                    var results = mode == KnowledgeRetrievalMode.Semantic
                        ? m_index.ScoreSemantic(queryVector, k)
                        : m_index.ScoreHybrid(query, conversationContext,
                                              m_settings.contextQueryWeight, queryVector, k);
                    onComplete?.Invoke(results);
                },
                error =>
                {
                    if (m_settings.logRetrievals)
                        Debug.LogWarning($"[Knowledge] query embedding failed ({error}) - serving lexical results");
                    onComplete?.Invoke(ScoreLexical(query, conversationContext, k));
                });
        }

        IReadOnlyList<ScoredPassage> ScoreLexical(string query, string conversationContext, int k)
            => m_index.Score(query, conversationContext, m_settings.contextQueryWeight, k);

        // ---- background corpus embedding ----

        void StartCorpusEmbedding()
        {
            if (m_settings.retrievalMode == KnowledgeRetrievalMode.Lexical || m_index.ChunkCount == 0)
                return;
            var embeddings = Systems.Get<IEmbeddingsSystem>();
            if (embeddings == null)
                return;

            var texts = new List<string>(m_index.ChunkCount);
            for (int i = 0; i < m_index.ChunkCount; i++)
                texts.Add(m_index.GetEmbedText(i));
            EmbedFrom(embeddings, texts, 0, new List<float[]>(texts.Count), m_embedGeneration);
        }

        // Embeds the corpus batch by batch via chained callbacks; a generation mismatch
        // means the items changed mid-flight and this run's results are obsolete.
        void EmbedFrom(IEmbeddingsSystem embeddings, List<string> texts, int offset,
                       List<float[]> collected, int generation)
        {
            if (generation != m_embedGeneration)
                return;

            if (offset >= texts.Count)
            {
                m_index.SetChunkVectors(collected);
                m_vectorsReady = true;
                if (m_settings.logRetrievals)
                    Debug.Log($"[Knowledge] {collected.Count} chunks embedded via {embeddings.ModelId} - " +
                              $"{m_settings.retrievalMode} retrieval active");
                return;
            }

            int count = Math.Min(EmbedBatchSize, texts.Count - offset);
            embeddings.EmbedBatch(texts.GetRange(offset, count),
                vectors =>
                {
                    if (generation != m_embedGeneration)
                        return;
                    collected.AddRange(vectors);
                    EmbedFrom(embeddings, texts, offset + count, collected, generation);
                },
                error =>
                {
                    if (generation == m_embedGeneration)
                        Debug.LogWarning($"[Knowledge] corpus embedding unavailable ({error}) - lexical retrieval active");
                });
        }
    }
}
