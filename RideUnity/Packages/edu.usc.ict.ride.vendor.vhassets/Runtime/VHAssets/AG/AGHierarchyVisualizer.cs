using System.Collections.Generic;
using UnityEngine;


namespace VHAssets
{
/// <summary>
/// Draw Unity gizmos to visualize a hierarchy of game objects.
/// </summary>
/// <remarks>
/// The primary use is to aid in character skeleton visualization for debugging of animation related
/// issues but it can be applied anywhere. To use simply add this component to a game object and by
/// default the hierarchy below it will be visualized. The hierarchy root can be manually changed in
/// the inspector if you want to only see part of the hierarchy.
/// </remarks>
[ExecuteInEditMode]
public class AGHierarchyVisualizer : MonoBehaviour
{

    public Transform hierarchyRoot;
    public float jointRadius = .005f;
    public float axisLength = .05f;
    public Color skeletonColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);


    /// <summary>
    /// Start!
    /// </summary>
    void Start()
    {
        hierarchyRoot = gameObject.transform;
    }


    /// <summary>
    /// OnDrawGizmos!
    /// </summary>
    void OnDrawGizmos()
    {
        if (hierarchyRoot != null)
        {
            DrawHierarchy(hierarchyRoot);
        }
    }


    /// <summary>
    /// Draw 'joint' hierarchy decendent from specified transform.
    /// </summary>
    /// <param name="Joint"></param>
    void DrawHierarchy(Transform rootTransform)
    {
        // Create stack and add initial joint
        Stack<Transform> transformStack = new Stack<Transform>();
        transformStack.Push(rootTransform);

        // Work through stack
        while (transformStack.Count > 0)
        {
            // Get current joint and then remove from stack
            Transform currentTransform = transformStack.Pop();

            // Visualize joint position
            Gizmos.color = skeletonColor;
            Gizmos.DrawWireSphere(currentTransform.position, jointRadius);

            // Draw line from join to children and push children on stack
            for (int i = 0; i < currentTransform.childCount; i++)
            {
                Transform childJointTransform = currentTransform.GetChild(i);

                Gizmos.DrawLine(currentTransform.position, childJointTransform.position);

                transformStack.Push(childJointTransform);
            }

            // Visualize joint orientation
            DrawAxisGizmo(currentTransform, axisLength);
        }
    }


    /// <summary>
    /// Draw a gizmo showing the three primary axis.
    /// </summary>
    /// <param name="target"></param>
    void DrawAxisGizmo(Transform target, float scaleMultiplier)
    {
        Gizmos.color = Color.red;
        Vector3 directionX = target.TransformDirection(Vector3.right) * scaleMultiplier;
        Gizmos.DrawRay(target.position, directionX);

        Gizmos.color = Color.blue;
        Vector3 directionZ = target.TransformDirection(Vector3.forward) * scaleMultiplier;
        Gizmos.DrawRay(target.position, directionZ);

        Gizmos.color = Color.green;
        Vector3 directionY = target.TransformDirection(Vector3.up) * scaleMultiplier;
        Gizmos.DrawRay(target.position, directionY);

    }
}
}
