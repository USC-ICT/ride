using UnityEngine;
using UnityEngine.AI;

public class NavMeshDataHolder : MonoBehaviour
{
    [Header("NavMesh Data Reference")]
    public NavMeshData navMeshData;

    [Header("Metadata")]
    public string terrainName;
    public string unityVersion;
    public string navigationPackageVersion;
    public string customTag;
}
