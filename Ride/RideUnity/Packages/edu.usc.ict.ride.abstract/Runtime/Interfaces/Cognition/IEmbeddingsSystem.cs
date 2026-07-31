using System;
using System.Collections.Generic;
using Ride;

namespace Ride.Conversation
{
    /// <summary>
    /// Produces embedding vectors for text, enabling semantic (meaning-based) retrieval in
    /// implementations of <see cref="IKnowledgeSystem"/>. Vectors are only comparable when
    /// produced by the same model, so a knowledge index must be embedded and queried through
    /// the same registered embeddings system.
    ///
    /// Callback-shaped: implementations answer over the network (a local inference service
    /// or a cloud embeddings API). No component registered = semantic retrieval unavailable,
    /// and knowledge systems fall back to lexical scoring.
    /// </summary>
    public interface IEmbeddingsSystem : IRideSystem
    {
        /// <summary>Identifier of the embedding model in use, for logging and diagnostics.</summary>
        string ModelId { get; }

        /// <summary>
        /// Embeds each text and delivers one vector per input, in input order. Exactly one
        /// of the callbacks is invoked: onComplete with the vectors on success, onError with
        /// a short description when the service is unreachable or refuses the request.
        /// </summary>
        void EmbedBatch(IReadOnlyList<string> texts, Action<float[][]> onComplete, Action<string> onError);
    }
}
