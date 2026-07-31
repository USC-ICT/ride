using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.Conversation
{
    /// <summary>
    /// Shared <see cref="IEmbeddingsSystem"/> implementation for services exposing the
    /// OpenAI-compatible <c>/v1/embeddings</c> contract. Derived systems supply the
    /// endpoint, the model id, and (when the service requires one) an authorization key.
    /// Coroutine-based (no async/await) so it runs on every RIDE platform including WebGL.
    /// </summary>
    public abstract class EmbeddingsSystemOpenAICompatible : RideSystemMonoBehaviour, IEmbeddingsSystem
    {
        protected string m_endpoint;
        protected string m_model;

        [SerializeField, Min(1), Tooltip(
            "Seconds allowed for one embeddings request.\n" +
            "A corpus is embedded in batches, so this covers a batch rather than a single passage; " +
            "raise it for large corpora on a slow or cold local service.")]
        protected int m_requestTimeoutSeconds = 20;

        /// <inheritdoc/>
        public string ModelId => m_model;

        /// <summary>Authorization bearer token for the endpoint, or empty when none is required.</summary>
        protected virtual string GetAuthorizationKey() => string.Empty;

        /// <inheritdoc/>
        public void EmbedBatch(IReadOnlyList<string> texts, Action<float[][]> onComplete, Action<string> onError)
        {
            if (texts == null || texts.Count == 0)
            {
                onComplete?.Invoke(Array.Empty<float[]>());
                return;
            }
            StartCoroutine(EmbedCoroutine(texts, onComplete, onError));
        }

        private IEnumerator EmbedCoroutine(IReadOnlyList<string> texts, Action<float[][]> onComplete, Action<string> onError)
        {
            var input = new string[texts.Count];
            for (int i = 0; i < texts.Count; i++)
                input[i] = texts[i] ?? string.Empty;

            string payload = RideIO.JsonSerializeNoObjRef(new EmbeddingsQuestion
            {
                model = m_model,
                input = input
            });

            using var webRequest = new UnityWebRequest(m_endpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = m_requestTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            string key = GetAuthorizationKey();
            if (!string.IsNullOrEmpty(key))
                webRequest.SetRequestHeader("Authorization", "Bearer " + key);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"{webRequest.result}: {webRequest.error}");
                yield break;
            }

            EmbeddingsAnswer answer = RideIO.JsonDeserialize<EmbeddingsAnswer>(webRequest.downloadHandler.text);
            if (answer.data == null || answer.data.Length != texts.Count)
            {
                onError?.Invoke($"expected {texts.Count} embeddings, received {answer.data?.Length ?? 0}");
                yield break;
            }

            // Order by the reported index rather than assuming array order.
            var vectors = new float[texts.Count][];
            foreach (var entry in answer.data)
            {
                if (entry.index < 0 || entry.index >= vectors.Length || entry.embedding == null)
                {
                    onError?.Invoke("malformed embeddings response");
                    yield break;
                }
                vectors[entry.index] = entry.embedding;
            }
            onComplete?.Invoke(vectors);
        }

        [Serializable]
        private struct EmbeddingsQuestion
        {
            public string   model;
            public string[] input;
        }

        [Serializable]
        private struct EmbeddingsAnswer
        {
            public EmbeddingsData[] data;
        }

        [Serializable]
        private struct EmbeddingsData
        {
            public int     index;
            public float[] embedding;
        }
    }
}
