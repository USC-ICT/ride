using System.Collections.Generic;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>
    /// Feeds app-shipped reference documents into the registered <see cref="IKnowledgeSystem"/>.
    /// Loads every TextAsset under a Resources folder (default "VHKnowledge") and calls
    /// SetItems() once at Start - after the knowledge system has self-registered in Awake.
    ///
    /// Use this component to ship reference documents inside the build itself (no network
    /// dependency at runtime). Applications that receive their documents at runtime can
    /// instead call <see cref="IKnowledgeSystem.SetItems"/> directly.
    ///
    /// Safe by default: when the Resources folder, the TextAssets, or the knowledge system
    /// are absent, the component is a silent no-op, so it can be included in a shared
    /// systems prefab and only activates in projects that provide a corpus folder.
    /// </summary>
    public class KnowledgeSourceResources : RideMonoBehaviour
    {
        [Tooltip("Resources subfolder to load TextAssets from. Each asset becomes one knowledge " +
                 "item; the asset name is its title.")]
        [SerializeField] private string m_resourcesFolder = "VHKnowledge";

        [Tooltip("Log a one-line summary of what was loaded (item count, total characters).")]
        [SerializeField] private bool m_logSummary = true;

        protected override void Start()
        {
            base.Start();

            if (string.IsNullOrWhiteSpace(m_resourcesFolder))
                return;

            var assets = Resources.LoadAll<TextAsset>(m_resourcesFolder);
            if (assets == null || assets.Length == 0)
                return;  // project ships no corpus - normal for most apps

            var knowledge = Systems.Get<IKnowledgeSystem>();
            if (knowledge == null)
            {
                Debug.LogWarning($"[KnowledgeSourceResources] {assets.Length} TextAssets found under " +
                                 $"Resources/{m_resourcesFolder} but no IKnowledgeSystem is registered - " +
                                 "add a KnowledgeSystemUnity component (RideSystemsCognition.prefab has one).");
                return;
            }

            var items = new List<KnowledgeItem>(assets.Length);
            long totalChars = 0;
            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                    continue;
                items.Add(new KnowledgeItem
                {
                    id    = asset.name,
                    title = asset.name.Replace('-', ' ').Replace('_', ' '),
                    text  = asset.text,
                    type  = "file"
                });
                totalChars += asset.text.Length;
            }

            knowledge.SetItems(items);

            if (m_logSummary)
                Debug.Log($"[KnowledgeSourceResources] Loaded {items.Count} knowledge items " +
                          $"({totalChars / 1024} KB) from Resources/{m_resourcesFolder}.");
        }
    }
}
