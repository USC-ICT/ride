using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Ride
{
    /// <summary>
    /// A MonoBehaviour that dynamically loads and instantiates an asset from a catalog at runtime,
    /// based on either its name or associated labels. Supports placeholder handling, loading progress tracking,
    /// and event callbacks upon asset load.
    /// </summary>
    public class RideCatalogAsset : RideMonoBehaviour, ILoadableAsset
    {
        /// <summary>
        /// Defines whether the asset should be loaded by name or by label(s).
        /// </summary>
        public enum LoadType
        {
            /// <summary>Load the asset using its unique name.</summary>
            Name,
            /// <summary>Load the first matching asset using one or more labels.</summary>
            Labels
        }

        [Tooltip("Choose whether to load by Asset Name or by Labels.")]
        public LoadType loadType = LoadType.Labels;
        [Tooltip("Asset Name to load (used if LoadType is Name).")]
        public string assetNameToLoad;
        [Tooltip("Labels to match (used if LoadType is Labels).")]
        public List<string> labelsToLoad = new();
        [SerializeField] private bool m_loadOnStart;
        private GameObject m_instantiatedAsset;
        private AssetLoadingSystemAssetBundles m_assetBundleLoader;
        private int m_currentLabelIndex = 0;
        private List<string> m_allAvailableLabels = new();

        /// <summary>
        /// UnityEvent triggered in the inspector when the asset finishes loading.
        /// </summary>
        [Serializable] public class AssetLoadedEvent : UnityEvent { }
        [SerializeField] private AssetLoadedEvent m_onAssetLoaded; //for inspector-based subscriptions to the load event
        private bool m_assetInitialized = false;
        public bool AssetInitialized => m_assetInitialized;

        /// <summary>
        /// Event triggered programmatically when the asset is fully loaded.
        /// </summary>
        public event Action AssetLoaded;
        private Coroutine m_progressLogger;
        [SerializeField] private GameObject m_placeholderObject;

        /// <summary>
        /// Automatically begins loading the asset if m_loadOnStart is true.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            m_assetBundleLoader = Globals.api.GetSystem<AssetLoadingSystemAssetBundles>();
            if (m_loadOnStart)
                LoadAsset();
        }

        /// <summary>
        /// Initiates loading of the asset using the selected method (Name or Labels).
        /// </summary>
        public void LoadAsset()
        {
            if (m_assetBundleLoader == null)
                m_assetBundleLoader = Globals.api.GetSystem<AssetLoadingSystemAssetBundles>();

            if (m_loadOnStart && m_placeholderObject != null)
                m_placeholderObject.SetActive(true);

            if (loadType == LoadType.Name)
            {
                if (string.IsNullOrEmpty(assetNameToLoad))
                {
                    Debug.LogError("RideCatalogAsset: No asset name specified.");
                    return;
                }

                var op = m_assetBundleLoader.LoadAssetByName(assetNameToLoad);
                op.Then(asset =>
                {
                    if (asset != null)
                        OnAssetLoaded(asset);
                    else
                        Debug.LogError("RideCatalogAsset: Failed to load asset by name.");
                })
                .Catch(error =>
                {
                    Debug.LogError($"RideCatalogAsset: LoadAssetByName failed: {error}");
                });
                m_progressLogger = StartCoroutine(LogProgressUntilLoaded(op, assetNameToLoad));
            }
            else
            {
                if (labelsToLoad == null || labelsToLoad.Count == 0)
                {
                    Debug.LogError("RideCatalogAsset: No labels specified.");
                    return;
                }

                string labelKey = string.Join(", ", labelsToLoad);
                var op = m_assetBundleLoader.LoadAnyAssetByLabels(labelsToLoad);
                op.Then(asset =>
                {
                    if (asset != null)
                        OnAssetLoaded(asset);
                    else
                        Debug.LogError("RideCatalogAsset: Failed to load asset by labels.");
                })
                .Catch(error =>
                {
                    Debug.LogError($"RideCatalogAsset: LoadAnyAssetByLabels2 failed: {error}");
                });
                m_progressLogger = StartCoroutine(LogProgressUntilLoaded(op, labelKey));
            }
        }

        /// <summary>
        /// Instantiates the loaded asset and initializes its components and event callbacks.
        /// </summary>
        /// <param name="loadedAsset">The loaded asset to instantiate.</param>
        public void OnAssetLoaded(object loadedAsset)
        {
            ResetAsset();
            if (m_placeholderObject != null)
                m_placeholderObject.SetActive(false);
            m_instantiatedAsset = Instantiate(loadedAsset as GameObject, transform);
            m_instantiatedAsset.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_assetInitialized = true;
            InitializeAllComponents(); //To initialize all components on this GameObject
            m_onAssetLoaded?.Invoke(); //To initialize components on other GameObjects
            AssetLoaded?.Invoke(); //To invoke all code-based subscriptions to the Asset Load event
        }


        /// <summary>
        /// Calls InitializeLoadedAsset() on all components attached to this GameObject.
        /// </summary>
        public void InitializeAllComponents()
        {
            Component[] components = GetComponents<Component>();
            foreach (Component component in components)
            {
                Type type = component.GetType();
                MethodInfo method = type.GetMethod("InitializeLoadedAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                method?.Invoke(component, null);
            }
        }

        /// <summary>
        /// Coroutine that logs the asset loading progress at 10% milestones.
        /// </summary>
        /// <param name="assetIdentifier">Identifier of the asset (name or label summary).</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator LogProgressUntilLoaded(LoadOperation<object> op, string assetIdentifier)
        {
            int lastMilestone = 0;
            while (!op.IsCompleted && !op.IsCancelled)
            {
                float progress = op.Progress;
                int currentPercent = Mathf.FloorToInt(progress * 100f);
                if (currentPercent >= lastMilestone + 10)
                {
                    lastMilestone = currentPercent / 10 * 10;
                    Debug.Log($"[RideCatalogAsset] Loading '{assetIdentifier}' - {lastMilestone}%");
                }

                yield return new WaitForSeconds(0.1f);
            }

            if (op.IsCompleted)
                Debug.Log($"[RideCatalogAsset] Asset '{assetIdentifier}' finished loading.");
        }

        /// <summary>
        /// Destroys the currently loaded asset instance and resets internal state.
        /// </summary>
        public void ResetAsset()
        {
            if (m_progressLogger != null)
                StopCoroutine(m_progressLogger);
            if (m_instantiatedAsset != null)
                Destroy(m_instantiatedAsset);
            m_instantiatedAsset = null;
            m_assetInitialized = false;
            if (m_placeholderObject != null)
                m_placeholderObject.SetActive(true);
        }

        private List<string> m_allAvailableAssetNames;
        private int m_currentAssetIndex = 0;

        /// <summary>
        /// Renders GUI controls (for DebugMenu) allowing developers to load/reset the asset in play mode.
        /// </summary>
        /// <param name="debugMenu">The debug menu instance to draw in.</param>
        /// <param name="assetBundleLoader">Reference to the asset loader system.</param>
        public void DrawAssetGUI(DebugMenu debugMenu, AssetLoadingSystemAssetBundles assetBundleLoader)
        {
            if (debugMenu == null || assetBundleLoader == null)
                return;

            using (debugMenu.Horizontal())
            {
                if (loadType == LoadType.Labels)
                {
                    if (m_allAvailableLabels == null || m_allAvailableLabels.Count == 0)
                        m_allAvailableLabels = new List<string>(assetBundleLoader.GetEntryInfoList().SelectMany(e => e.labels).Distinct());

                    if (m_allAvailableLabels.Count > 0)
                    {
                        if (m_currentLabelIndex < 0 || m_currentLabelIndex >= m_allAvailableLabels.Count)
                            m_currentLabelIndex = 0;

                        if (debugMenu.Button("<", 25))
                        {
                            m_currentLabelIndex--;
                            if (m_currentLabelIndex < 0)
                                m_currentLabelIndex = m_allAvailableLabels.Count - 1;
                        }

                        debugMenu.Label(m_allAvailableLabels[m_currentLabelIndex]);

                        if (debugMenu.Button(">", 25))
                        {
                            m_currentLabelIndex++;
                            if (m_currentLabelIndex >= m_allAvailableLabels.Count)
                                m_currentLabelIndex = 0;
                        }

                        labelsToLoad.Clear();
                        labelsToLoad.Add(m_allAvailableLabels[m_currentLabelIndex]);
                    }
                }
                else
                {
                    if (m_allAvailableAssetNames == null || m_allAvailableAssetNames.Count == 0)
                        m_allAvailableAssetNames = new List<string>(assetBundleLoader.GetEntryInfoList()
                            .Select(e => e.assetName)
                            .Distinct());

                    if (m_allAvailableAssetNames.Count > 0)
                    {
                        if (m_currentAssetIndex < 0 || m_currentAssetIndex >= m_allAvailableAssetNames.Count)
                            m_currentAssetIndex = 0;

                        if (debugMenu.Button("<", 25))
                        {
                            m_currentAssetIndex--;
                            if (m_currentAssetIndex < 0)
                                m_currentAssetIndex = m_allAvailableAssetNames.Count - 1;
                        }

                        debugMenu.Label(m_allAvailableAssetNames[m_currentAssetIndex]);

                        if (debugMenu.Button(">", 25))
                        {
                            m_currentAssetIndex++;
                            if (m_currentAssetIndex >= m_allAvailableAssetNames.Count)
                                m_currentAssetIndex = 0;
                        }

                        assetNameToLoad = m_allAvailableAssetNames[m_currentAssetIndex];
                    }
                    else
                    {
                        debugMenu.Label("No assets in catalog");
                    }        
                }

                if (debugMenu.Button("Load", 60))
                    LoadAsset();

                if (debugMenu.Button("Reset", 60))
                    ResetAsset();
            }
        }
    }
}
