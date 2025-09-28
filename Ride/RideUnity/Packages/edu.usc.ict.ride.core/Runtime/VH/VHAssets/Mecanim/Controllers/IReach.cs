using UnityEngine;

namespace VHAssets
{
public interface IReach 
{
    /// <summary>
    /// The transform that you want to reach towards
    /// </summary>
    /// <param name="target"></param>
    void Reach(Transform target);

    /// <summary>
    /// The world position that you want to reach towards
    /// </summary>
    /// <param name="target"></param>
    void Reach(Vector3 target);
    //void Reach(Transform target, float duration = 1);
}
}
