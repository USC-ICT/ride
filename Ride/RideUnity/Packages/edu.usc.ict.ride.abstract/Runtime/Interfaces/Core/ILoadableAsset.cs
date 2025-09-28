using System;

namespace Ride
{
    /// <summary>
    /// Abstract interface for anything that can load and reset an asset,
    /// initialise its own components, and broadcast when loading is done.
    /// </summary>
    public interface ILoadableAsset
    {
        void LoadAsset();
        void ResetAsset();
        void OnAssetLoaded(object loadedAsset);
        void InitializeAllComponents();
        event Action AssetLoaded; //for code-based subscriptions of the loadEvent
    }
}

