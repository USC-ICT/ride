using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ride;

public class RideAddressable : RideMonoBehaviour, ILoadableAsset
{
    [SerializeField] private string m_assetLabelToLoad = string.Empty;
    public string LabelToLoad { get { return m_assetLabelToLoad; } set { m_assetLabelToLoad = value; } }
    [SerializeField] private List<string> m_assetLabelsOfLoadedObject = new(); 
    public List<string> LabelsOfLoadedObject => m_assetLabelsOfLoadedObject;
    private GameObject m_loadedAssetInstance = null;
    [SerializeField] private bool m_loadOnStart = true;
    [Serializable] public class AssetLoadedEvent : UnityEvent { }
    [SerializeField] private AssetLoadedEvent m_onAssetLoaded; //for inspector-based subscriptions to the load event

    private bool m_assetInitialized = false;
    public bool AssetInitialized { get { return m_assetInitialized; } set { m_assetInitialized = value; } }
    private AddressableSystem m_rideAddressableSystem;

    public event Action AssetLoaded;

    protected override void Start()
    {
        m_rideAddressableSystem = Globals.api.GetSystem<AddressableSystem>();
        if (!string.IsNullOrEmpty(m_assetLabelToLoad) && m_loadOnStart && !m_assetInitialized)
            LoadAsset();
        base.Start();
    } 
    public void LoadAsset()
    {
        if (!string.IsNullOrEmpty(m_assetLabelToLoad) && !m_assetInitialized)
            StartCoroutine(m_rideAddressableSystem.RequestAssetLoadCoroutine(m_assetLabelToLoad, this));
    }

    public void OnAssetLoaded(object loadedAsset)
    {
        m_loadedAssetInstance = loadedAsset as GameObject;
        m_assetLabelsOfLoadedObject = m_rideAddressableSystem.GetAssetLabelsByAssetName(m_loadedAssetInstance.name);
        Debug.Log(m_assetLabelsOfLoadedObject.Count);
        InitializeAllComponents(); //To initialize all components on this GameObject
        m_onAssetLoaded?.Invoke(); //To initialize components on other GameObjects
        AssetLoaded?.Invoke(); //To invoke all code-based subscriptions to the Asset Load event
    }

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
