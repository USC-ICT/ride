using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Ride
{
    /// <summary>
    /// Manages the asynchronous loading, instantiation, and lifecycle of a single
    /// catalog asset using the RIDE AssetLoadingSystemAssetBundles infrastructure.
    ///
    /// This component supports loading an asset either by explicit catalog name
    /// or by label set, optionally displaying a placeholder object while the asset
    /// is loading. Once loaded, the asset is instantiated as a child of this
    /// GameObject and positioned at the local origin.
    ///
    /// The class provides multiple extensibility points without requiring direct
    /// compile-time dependencies:
    /// - Progress updates are reported via SendMessage using
    ///   <c>UpdateLoadedAssetProgress(float)</c>.
    /// - Post-load initialization hooks are invoked via SendMessage using
    ///   <c>InitializeLoadedAsset()</c>.
    /// - Pre-unload/reset hooks are invoked via SendMessage using
    ///   <c>ResetLoadedAsset()</c>.
    /// - Completion events are exposed via UnityEvent and C# event callbacks.
    ///
    /// Load requests are guarded to prevent re-entrancy. If a load is already in
    /// progress or an asset is already instantiated, subsequent calls to
    /// <see cref="LoadAsset"/> will early-out safely.
    ///
    /// Calling <see cref="ResetAsset"/> will destroy the currently instantiated
    /// asset (if any), restore the placeholder object, and invalidate any in-flight
    /// load operation so that late completion callbacks cannot instantiate stale
    /// results. Before destruction occurs, components are notified so they may
    /// detach from references to the loaded asset.
    ///
    /// This component does not directly cancel underlying I/O operations; instead,
    /// it uses a request-id fencing mechanism to ensure that only the most recent
    /// load request is allowed to affect scene state.
    ///
    /// Intended usage:
    /// - Attach to a GameObject that represents a logical "asset slot".
    /// - Optionally assign a placeholder GameObject for visual feedback.
    /// - Configure loading mode (by name or labels) in the inspector.
    /// - Call <see cref="LoadAsset"/> to begin loading.
    /// - Call <see cref="ResetAsset"/> to unload and return to placeholder state.
    ///
    /// Components on the same GameObject may optionally implement the following
    /// message handlers:
    /// - <c>void InitializeLoadedAsset()</c>
    ///   Called once after the asset has been successfully instantiated.
    /// - <c>void ResetLoadedAsset()</c>
    ///   Called immediately before the instantiated asset is destroyed, allowing
    ///   components to release references and detach from runtime-created objects.
    /// - <c>void UpdateLoadedAssetProgress(float progress01)</c>
    ///   Called periodically during loading with normalized progress [0..1].
    ///
    /// This class is designed to favor composition and loose coupling over
    /// inheritance. Custom behavior should generally be implemented via the
    /// messaging hooks or completion events rather than by subclassing.
    /// </summary>
    public class RideCatalogAsset : RideMonoBehaviour, ILoadableAsset
    {
        /// <summary>Defines whether the asset should be loaded by name or by label(s).</summary>
        public enum LoadType
        {
            /// <summary>Load the asset using its unique name.</summary>
            Name,
            /// <summary>Load the first matching asset using one or more labels.</summary>
            Labels
        }


        [Header("Catalog Load Source")]
        [Tooltip("How this component chooses which catalog asset to load. " +
                 "If set to Name, only Asset Name is used. If set to Labels, only Labels is used.")]
        [SerializeField]
        private LoadType loadType = LoadType.Labels;

        [Tooltip("Catalog asset name to load when Load Type is Name. " +
                 "This should match the key/name used by the catalog system.")]
        [SerializeField]
        private string assetNameToLoad;

        [Tooltip("One or more labels to load by when Load Type is Labels. " +
                 "The loader will attempt labels in order until an asset is found.")]
        [SerializeField]
        private List<string> labelsToLoad = new();

        [Tooltip("If true, the asset load begins automatically in Start(). " +
                 "If false, call LoadAsset() from your own code.")]
        [SerializeField]
        private bool m_loadOnStart;

        [SerializeField] private GameObject m_placeholderObject;
        [SerializeField] private UnityEvent m_onAssetLoaded; //for inspector-based subscriptions to the load event

        [Header("Debug")]
        [Tooltip("If true, logs loading progress at 10% milestones.")]
        [SerializeField] private bool m_logProgress = true;


        /// <summary>Event triggered programmatically when the asset is fully loaded.</summary>
        public event Action AssetLoaded;

        /// <summary>
        /// Unity-only event fired after the asset instance has been instantiated and initialized.
        /// This is separate from the interface AssetLoaded event (ride.abstract) so it can include Unity types.
        /// </summary>
        public event Action<RideCatalogAsset, GameObject> AssetInstanceLoaded;

        /// <summary>
        /// Unity-only event fired when the current instantiated asset is being reset/unloaded.
        /// The instance may be null if nothing is currently instantiated.
        /// </summary>
        public event Action<RideCatalogAsset, GameObject> AssetInstanceReset;
        public event Action<RideCatalogAsset> AssetLoadStarted;
        public event Action<RideCatalogAsset, float> AssetLoadProgressChanged;
        public event Action<RideCatalogAsset, string> AssetLoadFailed;
        public event Action<RideCatalogAsset> AssetLoadCancelled;


        private GameObject m_instantiatedAsset;
        private bool m_assetInitialized = false;

        private LoadOperation<object> m_loadOperation;
        private bool m_isLoading;
        private string m_lastLoadError;
        private string m_lastLoadIdentifier;
        private float m_currentLoadProgress;
        private float m_lastProgressReported = -1f;
        private float m_nextProgressReportTime;
        private int m_loadRequestId;
        private int m_lastLoggedMilestone = -1;


        public bool AssetInitialized => m_assetInitialized;
        public bool IsLoading => m_isLoading;
        public float CurrentLoadProgress => m_currentLoadProgress;
        public string LastLoadError => m_lastLoadError;
        public string LastLoadIdentifier => m_lastLoadIdentifier;


        /// <summary>
        /// Automatically begins loading the asset if m_loadOnStart is true.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            if (m_loadOnStart)
                LoadAsset();
        }

        protected override void Update()
        {
            base.Update();

            CheckLoadProgress();
        }

        /// <summary>
        /// Initiates loading of the asset using the selected method (Name or Labels).
        /// </summary>
        public void LoadAsset()
        {
            if (m_isLoading)
            {
                Debug.LogWarning($"RideCatalogAsset: LoadAsset() {gameObject.name} called while already loading. Ignoring.");
                return;
            }

            if (m_assetInitialized && m_instantiatedAsset != null)
            {
                Debug.LogWarning($"RideCatalogAsset: LoadAsset() {gameObject.name} called but asset is already loaded. Ignoring.");
                return;
            }

            var assetBundleLoader = Systems.Get<AssetLoadingSystemAssetBundles>();

            if (m_placeholderObject != null)
                m_placeholderObject.SetActive(true);

            string identifierForLogs;
            LoadOperation<object> op;

            if (loadType == LoadType.Name)
            {
                if (string.IsNullOrEmpty(assetNameToLoad))
                {
                    Debug.LogError("RideCatalogAsset: No asset name specified.");
                    return;
                }

                identifierForLogs = assetNameToLoad;
                op = assetBundleLoader.LoadAssetByName(assetNameToLoad);
            }
            else
            {
                if (labelsToLoad == null || labelsToLoad.Count == 0)
                {
                    Debug.LogError("RideCatalogAsset: No labels specified.");
                    return;
                }

                identifierForLogs = string.Join(", ", labelsToLoad);
                op = assetBundleLoader.LoadAnyAssetByLabels(labelsToLoad);
            }

            if (op == null)
            {
                Debug.LogError($"RideCatalogAsset: {gameObject.name} - {identifierForLogs} - Asset load operation was null.");
                return;
            }

            m_isLoading = true;
            m_loadOperation = op;
            m_lastLoadError = null;
            m_lastLoadIdentifier = identifierForLogs;
            m_currentLoadProgress = 0f;
            m_lastProgressReported = -1f;
            m_nextProgressReportTime = 0f;
            m_lastLoggedMilestone = -1;
            AssetLoadStarted?.Invoke(this);

            int requestId = ++m_loadRequestId;

            op.Then(asset =>
            {
                if (requestId != m_loadRequestId)
                    return;

                m_isLoading = false;
                m_loadOperation = null;
                m_currentLoadProgress = 1f;

                if (asset != null)
                {
                    // Force a final progress update of 1.0 for listeners.
                    gameObject.SendMessage("UpdateLoadedAssetProgress", 1.0f, SendMessageOptions.DontRequireReceiver);

                    OnAssetLoaded(asset);
                }
                else
                {
                    Debug.LogError($"RideCatalogAsset: {gameObject.name} - Failed to load asset '{identifierForLogs}'.");
                }
            })
            .Catch(error =>
            {
                if (requestId != m_loadRequestId)
                    return;

                m_isLoading = false;
                m_loadOperation = null;
                m_lastLoadError = error;

                Debug.LogError($"RideCatalogAsset: {gameObject.name} - Load failed for '{identifierForLogs}': {error}");
                AssetLoadFailed?.Invoke(this, error);
            });
        }

        /// <summary>
        /// Destroys the currently loaded asset instance and resets internal state.
        /// </summary>
        public void ResetAsset()
        {
            // If we are loading, invalidate the current request so stale callbacks don't instantiate later.
            bool wasLoading = m_isLoading || m_loadOperation != null;
            if (m_isLoading || m_loadOperation != null)
                m_loadRequestId++;

            m_isLoading = false;
            m_loadOperation = null;
            m_currentLoadProgress = 0f;
            m_lastProgressReported = -1f;
            m_nextProgressReportTime = 0f;
            m_lastLoggedMilestone = -1;

            if (wasLoading)
            {
                m_lastLoadError = "Cancelled";
                AssetLoadCancelled?.Invoke(this);
            }

            // Notify external listeners first.
            AssetInstanceReset?.Invoke(this, m_instantiatedAsset);

            // Notify listeners that the currently loaded asset is being reset/unloaded.
            // This is symmetrical with InitializeLoadedAsset and allows controllers to detach
            // from components that are about to be destroyed.
            gameObject.SendMessage("ResetLoadedAsset", SendMessageOptions.DontRequireReceiver);

            if (m_instantiatedAsset != null)
                Destroy(m_instantiatedAsset);

            m_instantiatedAsset = null;
            m_assetInitialized = false;

            if (m_placeholderObject != null)
                m_placeholderObject.SetActive(true);
        }

        private void CheckLoadProgress()
        {
            if (!m_isLoading || m_loadOperation == null)
                return;

            if (m_loadOperation.IsCancelled)
            {
                // Treat as finished from our perspective.
                m_isLoading = false;
                m_loadOperation = null;
                m_lastLoadError = "Cancelled";
                m_currentLoadProgress = 0f;
                AssetLoadCancelled?.Invoke(this);
                return;
            }

            // Throttle progress reporting to avoid excessive message dispatch.
            // (Update runs every frame; this keeps it light.)
            float now = Time.unscaledTime;
            if (now < m_nextProgressReportTime)
                return;

            float progress = Mathf.Clamp01(m_loadOperation.Progress);

            // Only report if the value meaningfully changed.
            // 0.01 = 1% steps. Tune as desired.
            const float MinDelta = 0.01f;
            if (m_lastProgressReported >= 0f && Mathf.Abs(progress - m_lastProgressReported) < MinDelta)
                return;

            m_lastProgressReported = progress;
            m_currentLoadProgress = progress;

            // Report to components on this GameObject.
            gameObject.SendMessage("UpdateLoadedAssetProgress", progress, SendMessageOptions.DontRequireReceiver);
            AssetLoadProgressChanged?.Invoke(this, progress);

            // 10Hz reporting is usually plenty for UI.
            m_nextProgressReportTime = now + 0.1f;
        }

        // Called via SendMessage from Update() while loading.
        public void UpdateLoadedAssetProgress(float progress01)
        {
            if (!m_logProgress)
                return;

            // Recreate the old coroutine behavior: log at 10% milestones.
            int percent = Mathf.Clamp(Mathf.FloorToInt(progress01 * 100f), 0, 100);
            int milestone = (percent / 10) * 10;

            if (milestone <= m_lastLoggedMilestone)
                return;

            m_lastLoggedMilestone = milestone;

            // Match your old style; tweak the string if you want name/labels included.
            Debug.Log($"[RideCatalogAsset] {gameObject.name} loading - {milestone}%");
        }

        /// <summary>
        /// Instantiates the loaded asset and initializes its components and event callbacks.
        /// </summary>
        /// <param name="loadedAsset">The loaded asset to instantiate.</param>
        private void OnAssetLoaded(object loadedAsset)
        {
            if (m_placeholderObject != null)
                m_placeholderObject.SetActive(false);

            if (loadedAsset is not GameObject prefab)
            {
                Debug.LogError($"RideCatalogAsset: Loaded asset is not a GameObject. Type={loadedAsset?.GetType().Name ?? "null"}");
                return;
            }

            if (m_instantiatedAsset != null)
                Destroy(m_instantiatedAsset);

            m_instantiatedAsset = Instantiate(prefab, transform);
            m_instantiatedAsset.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            m_assetInitialized = true;

            // Use Unity's built-in message dispatch
            // This is name-based, but avoids manual reflection.
            gameObject.SendMessage("InitializeLoadedAsset", SendMessageOptions.DontRequireReceiver);

            AssetLoaded?.Invoke();
            AssetInstanceLoaded?.Invoke(this, m_instantiatedAsset);

            m_onAssetLoaded?.Invoke();
        }


        ///////////////////////////////////////////////////////////////
        // Debug Menu Support
        ///////////////////////////////////////////////////////////////

        private List<string> m_allAvailableLabels = new();
        private int m_currentLabelIndex = 0;
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
                            m_currentLabelIndex = RideMath.DecrementWrap(m_currentLabelIndex, m_allAvailableLabels.Count);

                        debugMenu.Label(m_allAvailableLabels[m_currentLabelIndex]);

                        if (debugMenu.Button(">", 25))
                            m_currentLabelIndex = RideMath.IncrementWrap(m_currentLabelIndex, m_allAvailableLabels.Count);

                        labelsToLoad.Clear();
                        labelsToLoad.Add(m_allAvailableLabels[m_currentLabelIndex]);
                    }
                }
                else
                {
                    if (m_allAvailableAssetNames == null || m_allAvailableAssetNames.Count == 0)
                        m_allAvailableAssetNames = new List<string>(assetBundleLoader.GetEntryInfoList().Select(e => e.assetName).Distinct());

                    if (m_allAvailableAssetNames.Count > 0)
                    {
                        if (m_currentAssetIndex < 0 || m_currentAssetIndex >= m_allAvailableAssetNames.Count)
                            m_currentAssetIndex = 0;

                        if (debugMenu.Button("<", 25))
                            m_currentAssetIndex = RideMath.DecrementWrap(m_currentAssetIndex, m_allAvailableAssetNames.Count);

                        debugMenu.Label(m_allAvailableAssetNames[m_currentAssetIndex]);

                        if (debugMenu.Button(">", 25))
                            m_currentAssetIndex = RideMath.IncrementWrap(m_currentAssetIndex, m_allAvailableAssetNames.Count);

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
