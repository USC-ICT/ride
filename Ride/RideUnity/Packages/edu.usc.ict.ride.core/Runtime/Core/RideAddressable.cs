using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ride;

/// <summary>Loads a labelled Addressables prefab and initializes the RIDE components attached to this object.</summary>
/// <remarks>
/// <para>
/// <see cref="AddressableSystem"/> performs the asynchronous load and calls <see cref="OnAssetLoaded"/> when
/// the object is available. The component is configured through the custom <c>RideAddressableEditor</c> inspector
/// and is demonstrated by <c>SamplesCoreAddressableSystem</c>.
/// </para>
/// <para>
/// Unity Addressables documentation:
/// <see href="https://docs.unity3d.com/Packages/com.unity.addressables@latest"/>.
/// </para>
/// </remarks>
public class RideAddressable : RideMonoBehaviour, ILoadableAsset
{
    /// <summary>The Addressables label requested when loading this component's asset.</summary>
    [SerializeField] private string m_assetLabelToLoad = string.Empty;

    /// <summary>Gets or sets the Addressables label requested by <see cref="LoadAsset"/>.</summary>
    public string LabelToLoad { get { return m_assetLabelToLoad; } set { m_assetLabelToLoad = value; } }

    /// <summary>The labels associated with the most recently loaded asset.</summary>
    [SerializeField] private List<string> m_assetLabelsOfLoadedObject = new(); 

    /// <summary>Gets the Addressables labels associated with the loaded object.</summary>
    public List<string> LabelsOfLoadedObject => m_assetLabelsOfLoadedObject;

    /// <summary>The instantiated object supplied by <see cref="AddressableSystem"/>.</summary>
    private GameObject m_loadedAssetInstance = null;

    /// <summary>Whether to request the configured asset automatically during <see cref="Start"/>.</summary>
    [SerializeField] private bool m_loadOnStart = true;

    /// <summary>Inspector-configurable callback invoked after an asset has loaded and its components are initialized.</summary>
    [Serializable] public class AssetLoadedEvent : UnityEvent { }

    /// <summary>Inspector-based subscriptions notified after the asset is initialized.</summary>
    [SerializeField] private AssetLoadedEvent m_onAssetLoaded; //for inspector-based subscriptions to the load event

    /// <summary>Whether the loaded asset's components have completed initialization.</summary>
    private bool m_assetInitialized = false;

    /// <summary>Gets or sets whether this component considers its loaded asset initialized.</summary>
    public bool AssetInitialized { get { return m_assetInitialized; } set { m_assetInitialized = value; } }

    /// <summary>The system responsible for loading and describing Addressables assets.</summary>
    private AddressableSystem m_rideAddressableSystem;

    /// <inheritdoc/>
    public event Action AssetLoaded;

    /// <inheritdoc/>
    protected override void Start()
    {
        m_rideAddressableSystem = Globals.api.GetSystem<AddressableSystem>();
        if (!string.IsNullOrEmpty(m_assetLabelToLoad) && m_loadOnStart && !m_assetInitialized)
            LoadAsset();
        base.Start();
    } 

    /// <inheritdoc/>
    public void LoadAsset()
    {
        if (!string.IsNullOrEmpty(m_assetLabelToLoad) && !m_assetInitialized)
            StartCoroutine(m_rideAddressableSystem.RequestAssetLoadCoroutine(m_assetLabelToLoad, this));
    }

    /// <summary>Records a completed load, initializes local components, and notifies inspector and code subscribers.</summary>
    /// <param name="loadedAsset">The instantiated asset returned by <see cref="AddressableSystem"/>.</param>
    public void OnAssetLoaded(object loadedAsset)
    {
        m_loadedAssetInstance = loadedAsset as GameObject;
        m_assetLabelsOfLoadedObject = m_rideAddressableSystem.GetAssetLabelsByAssetName(m_loadedAssetInstance.name);
        Debug.Log(m_assetLabelsOfLoadedObject.Count);
        InitializeAllComponents(); //To initialize all components on this GameObject
        m_onAssetLoaded?.Invoke(); //To initialize components on other GameObjects
        AssetLoaded?.Invoke(); //To invoke all code-based subscriptions to the Asset Load event
    }

    /// <summary>Invokes an <c>InitializeLoadedAsset</c> method on each attached component that defines one.</summary>
    /// <remarks>
    /// Reflection allows optional components to participate without sharing a common interface. Public and non-public
    /// instance methods are supported; methods must take no parameters.
    /// </remarks>
    public void InitializeAllComponents()
    {
        Component[] components = GetComponents<Component>();
        foreach (Component component in components)
        {
            Type type = component.GetType();
            MethodInfo method = type.GetMethod("InitializeLoadedAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method?.Invoke(component, null);
        }
        m_assetInitialized = true;
    }

    /// <inheritdoc/>
    public void ResetAsset()
    {
        if (m_loadedAssetInstance != null)
        {
            Destroy(m_loadedAssetInstance);
            m_loadedAssetInstance = null;
        }
        m_assetInitialized = false;
        m_assetLabelsOfLoadedObject = new();
    }
}
