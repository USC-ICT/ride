using System.Collections.Generic;
using UnityEngine;

namespace Ride.Samples
{
    public class SamplesCoreRideAssetBundle : RideMonoBehaviour
    {
        [Tooltip("Catalogs to load, specifying path and whether remote.")]
        [SerializeField] private List<CatalogLoadInfoUnity> m_catalogsToLoad = new();

        [Tooltip("Assets to load, using RideCatalogAsset components.")]
        [SerializeField] private List<RideCatalogAsset> m_assetsToLoad = new();

        private DebugMenu m_debugMenu;
        private AssetLoadingSystemAssetBundles m_assetBundleLoader;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_assetBundleLoader = Globals.api.GetSystem<AssetLoadingSystemAssetBundles>();

            if (m_assetBundleLoader == null)
                Debug.LogError("SamplesCoreRideAssetBundle: AssetLoadingSystemAssetBundles not found!");
        }

        public void OnGUIRideAssetBundle()
        {
            if (m_debugMenu == null || m_assetBundleLoader == null)
                return;

            m_debugMenu.Label("Catalogs:");
            foreach (var catalogInfo in m_catalogsToLoad)
            {
                using (m_debugMenu.Horizontal())
                {
                    string location = "";
                    if (catalogInfo.catalogJsonFile != null)
                        location = $"TextAsset: {catalogInfo.catalogJsonFile.name}";
                    else
                        location = catalogInfo.isRemote ? $"Remote: {catalogInfo.catalogPath}" : $"Local: {catalogInfo.catalogPath}";

                    m_debugMenu.Label(location);

                    if (m_debugMenu.Button("Load", 60))
                    {
                        Debug.Log($"Loading catalog: {location}");
                        _ = m_assetBundleLoader.LoadCatalog(catalogInfo.ToCoreInfo());
                    }
                }
            }
            if (m_assetBundleLoader.NumCatalogsLoaded > 0 && m_assetBundleLoader.GetEntryCount() > 0)
            {
                m_debugMenu.Label("Assets:");
                foreach (var asset in m_assetsToLoad)
                    asset.DrawAssetGUI(m_debugMenu, m_assetBundleLoader);

                m_debugMenu.Label("Assets In Catalogs:");
                foreach (var (assetName, labels) in m_assetBundleLoader.GetEntryInfoList())
                    m_debugMenu.Label($"{assetName} | {string.Join(", ", labels)}");
            }
        }
    }
}
