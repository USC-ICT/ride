using System.Collections.Generic;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>Selectable OpenAI embedding models. Model ids live in code (not RideConfig) - see the
    /// dictionary below; mirrors the NLP provider pattern.</summary>
    public enum OpenAIEmbeddingsModel
    {
        Small3 = 10,
        Large3 = 20,
    }

    /// <summary>
    /// Cloud embeddings via the OpenAI <c>/v1/embeddings</c> API. Uses the OpenAI key from
    /// <see cref="RideConfig"/> (the same key the ChatGPT NLP system uses). Each call is
    /// billed to that account; embedding is priced per input token, with a whole corpus
    /// embedded once per session (or per item change) and one small call per user turn.
    /// </summary>
    public class EmbeddingsSystemOpenAI : EmbeddingsSystemOpenAICompatible
    {
        [SerializeField, Tooltip(
            "Which OpenAI embedding model to use.\n" +
            "The small model is the cost/quality default; the large one produces higher-dimensional " +
            "vectors at several times the price. Changing this invalidates any vectors already " +
            "computed, so the corpus is re-embedded.")]
        private OpenAIEmbeddingsModel m_openAIModel = OpenAIEmbeddingsModel.Small3;

        private readonly Dictionary<OpenAIEmbeddingsModel, string> m_modelDictionary = new()
        {
            { OpenAIEmbeddingsModel.Small3, "text-embedding-3-small" },
            { OpenAIEmbeddingsModel.Large3, "text-embedding-3-large" },
        };

        /// <inheritdoc/>
        public override void SystemInit()
        {
            m_endpoint = "https://api.openai.com/v1/embeddings";
            m_model = m_modelDictionary[m_openAIModel];
            base.SystemInit();
        }

        /// <inheritdoc/>
        protected override string GetAuthorizationKey()
        {
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            return configSystem != null ? configSystem.Config.openAIChatGPT.endpointKey : string.Empty;
        }
    }
}
