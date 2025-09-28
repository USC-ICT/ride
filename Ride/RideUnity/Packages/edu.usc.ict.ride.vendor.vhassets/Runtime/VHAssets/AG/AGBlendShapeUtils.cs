using System.Collections;
using UnityEngine;

namespace VHAssets
{
/// <summary>
/// Collection of utilities to make working with blend shapes easier.
/// </summary>
/// <remarks>
/// Many of these are geared around being able to use the skinned mesh render as the primary object
/// that is being worked with rather than bouncing back and forth between that and the mesh.
/// </remarks>
public static class AGBlendShapeUtils
{
    // ---------------------------------------------------------------------------------------------
    /// <summary>
    /// Return number of blendshapes on specified skinned mesh renderer.
    /// </summary>
    /// <param name="skinnedMesh"></param>
    /// <returns></returns>
    public static int GetBlendShapeCount(SkinnedMeshRenderer skinnedMesh)
    {
        return skinnedMesh.sharedMesh.blendShapeCount;
    }


    // ---------------------------------------------------------------------------------------------
    /// <summary>
    /// Return value of a blend shape by name on the specified skinned mesh renderer.
    /// </summary>
    /// <param name="skinnedMesh"></param>
    /// <param name="bsName"></param>
    public static float GetBlendShapeWeight(SkinnedMeshRenderer skinnedMesh, string bsName)
    {
        Mesh mesh = skinnedMesh.sharedMesh;
        int index = mesh.GetBlendShapeIndex(bsName);
        float bsValue = skinnedMesh.GetBlendShapeWeight(index);
        return bsValue;
    }


    // ---------------------------------------------------------------------------------------------
    /// <summary>
    /// Set the value of a blend shape by name on specified skinned mesh renderer.
    /// </summary>
    /// <param name="skinnedMesh"></param>
    /// <param name="bsName"></param>
    /// <param name="weight"></param>
    public static void SetBlendShapeWeight(SkinnedMeshRenderer skinnedMesh, string bsName, float weight)
    {
        Mesh mesh = skinnedMesh.sharedMesh;
        int index = mesh.GetBlendShapeIndex(bsName);

        if (index == -1){
            Debug.LogError("No blendshape found on "+skinnedMesh.name+" named "+bsName);
        }

        skinnedMesh.SetBlendShapeWeight(index, weight);
    }
}
}
