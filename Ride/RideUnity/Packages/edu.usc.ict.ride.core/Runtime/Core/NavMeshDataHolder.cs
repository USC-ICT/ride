using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lightweight container component for packaged <see cref="NavMeshData"/> assets and their related metadata.
/// </summary>
/// <remarks>
/// Ride terrain-loading code uses this as a wrapper when NavMesh data is delivered through prefabs or asset bundles.
/// The holder exposes the baked <see cref="NavMeshData"/> object that should be added to Unity's navigation system,
/// along with descriptive metadata that can help identify how and when that data was produced.
/// </remarks>
public class NavMeshDataHolder : MonoBehaviour
{
    [Header("NavMesh Data Reference")]
    [Tooltip("The baked Unity NavMeshData asset that should be added to the scene at load time.")]
    public NavMeshData navMeshData;

    [Header("Metadata")]
    [Tooltip("Human-readable terrain or source name associated with this baked NavMesh data.")]
    public string terrainName;
    [Tooltip("Unity version used when this NavMesh data was generated.")]
    public string unityVersion;
    [Tooltip("Version of the Unity AI Navigation package used when this NavMesh data was generated.")]
    public string navigationPackageVersion;
    [Tooltip("Optional custom tag for build, environment, or pipeline-specific identification.")]
    public string customTag;
}
