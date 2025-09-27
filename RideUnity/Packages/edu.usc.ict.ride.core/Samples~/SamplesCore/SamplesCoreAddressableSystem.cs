using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Samples
{
    public class SamplesCoreAddressableSystem : RideMonoBehaviour
    {
        [Tooltip("The name of the S3 bucket if loading remotely. Leave empty if loading locally.")]
        [SerializeField] private string m_bucketName;

        [Tooltip("The filepath to the folder containing the asset and catalog files.")]
        [SerializeField] private string m_filepath;

        [SerializeField] private string m_catalogFilename = "catalog_0.1.json";

        [Tooltip("The parent object of the loaded asset. Contains the specific label used for loading from the RideAddressableSystem.")]
        [SerializeField] private List<RideAddressable> m_assetsToLoad;

        private DebugMenu m_debugMenu;
        private AddressableSystem m_rideAddressableSystem;
        private AddressableCatalogInfo m_addressableInfo;

        void Awake()
        {
            m_addressableInfo = new(m_bucketName, m_filepath, m_catalogFilename, string.IsNullOrEmpty(m_bucketName));
        }

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_rideAddressableSystem = Globals.api.GetSystem<AddressableSystem>();
        }

        public void OnGUIRideAddressable()
        {
            m_debugMenu.Label("Catalog Location:");
            string catalogLocation = m_debugMenu.TextField(m_addressableInfo.GetCatalogAssetKey());

            if (m_debugMenu.Button("Load Catalog"))
                m_rideAddressableSystem.AddAndLoadAddressableInfo(m_addressableInfo);

            m_debugMenu.Label("Assets:");
            foreach (var asset in m_assetsToLoad)
            {
                using (m_debugMenu.Horizontal())
                {
                    if (m_rideAddressableSystem.AvailableLabels.Count > 0)
                    {
                        int currentIndex = m_rideAddressableSystem.AvailableLabels.IndexOf(asset.LabelToLoad);
                        if (currentIndex == -1)
                            currentIndex = 0;

                        if (m_debugMenu.Button("<", 25)) 
                        { 
                            currentIndex--;
                            if (currentIndex < 0)
                                currentIndex = m_rideAddressableSystem.AvailableLabels.Count - 1;

                            asset.LabelToLoad = m_rideAddressableSystem.AvailableLabels[currentIndex]; 
                        }

                        if (m_debugMenu.Button(asset.LabelToLoad) || m_debugMenu.Button(">", 25))
                        { 
                            currentIndex++;
                            if (currentIndex >= m_rideAddressableSystem.AvailableLabels.Count)
                                currentIndex = 0;

                            asset.LabelToLoad = m_rideAddressableSystem.AvailableLabels[currentIndex]; 
                        }

                        if (m_debugMenu.Button("Load", 60))
                            asset.LoadAsset();

                        if (m_debugMenu.Button("Reset", 60))
                            asset.ResetAsset();
                    }
                }
            }

            if (m_rideAddressableSystem.AllAvailableAssets.Count > 0)
                m_debugMenu.Label("Assets In Catalogs:");

            foreach (var asset in m_rideAddressableSystem.AllAvailableAssets)
               m_debugMenu.Label(m_rideAddressableSystem.GetAssetLabelString(asset.Key));
        }
    }
}
