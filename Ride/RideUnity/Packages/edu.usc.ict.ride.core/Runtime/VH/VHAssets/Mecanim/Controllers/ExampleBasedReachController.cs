using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class ExampleBasedReachController : FunctionalityController, IReach
{
    /// <summary>
    /// Animator Controller paramter name that controls the horizontal axis of the reach blend tree
    /// </summary>
    public string horizontalParam = "horizontalReach";

    /// <summary>
    /// Animator Controller paramter name that controls the vertical axis of the reach blend tree. Vertical can also mean depth
    /// </summary>
    public string verticalParam = "verticalReach";

    /// <summary>
    /// Animator Controller parameter name that controls the triggering of the left reach blend tree
    /// </summary>
    public string triggerReachLeftParam = "reachLeft";

    /// <summary>
    /// Animator Controller parameter name that controls the triggering of the right reach blend tree
    /// </summary>
    public string triggerReachRightParam = "reachRight";

    /// <summary>
    /// False uses the z axis for calulating the vertical blend
    /// </summary>
    public bool useYForVertical = true;

    /// <summary>
    /// Debug object used as the start marker to draw the reach area gizmo
    /// </summary>
    public Transform startBound;

    /// <summary>
    /// Debug object used as the end marker to draw the reach area gizmo
    /// </summary>
    public Transform endBound;

    /// <summary>
    /// Reach area cube color
    /// </summary>
    public Color debugGizmoColor = Color.green;

    /// <summary>
    /// The transform that you want to reach towards
    /// </summary>
    /// <param name="target"></param>
    public void Reach(Transform target)
    {
        Reach(target.position);
    }

    /// <summary>
    /// The world position that you want to reach towards
    /// </summary>
    /// <param name="target"></param>
    public void Reach(Vector3 target)
    {
        CalculatedNDC(target);
        if (IsRightReach(target))
        {
            m_animator.SetTrigger(triggerReachRightParam);
        }
        else
        {
            m_animator.SetTrigger(triggerReachLeftParam);
        }
        
    }

    /// <summary>
    /// Returns true if the animator should trigger the triggerReachRightParam parameter, false if the trigger is triggerReachLeftParam
    /// </summary>
    /// <returns></returns>
    bool IsRightReach(Vector3 target)
    {
        bool isRight = true;
        Vector3 toTarget = (target - transform.position).normalized;

        // calculate if it is on the right side
        float dot = Vector3.Dot(toTarget, transform.right);

        float centDot = Vector3.Dot(toTarget, transform.forward);
        float angleBetween = Mathf.Acos(centDot) * Mathf.Rad2Deg;
        Debug.Log("angleBetween: " + angleBetween);
        if (angleBetween < 30)
        {
            // the target is close to center, randomize between left and right
            isRight = Random.Range(0, 2) == 0;
        }
        else
        {
            isRight = dot > 0;
        }
        return isRight;
    }

    /// <summary>
    /// Puts blend parameters into the range [-1,1]
    /// </summary>
    /// <param name="target"></param>
    void CalculatedNDC(Vector3 target)
    {
        float hor = m_animator.GetFloat(horizontalParam);
        float vert = m_animator.GetFloat(verticalParam);

        hor = 2 * ((target.x - startBound.position.x) / (endBound.position.x - startBound.position.x)) - 1;
        if (useYForVertical)
        {
            vert = 2 * (target.y - startBound.position.y) / (endBound.position.y - startBound.position.y) - 1;
        }
        else
        {
            vert = 2 * (target.z - startBound.position.z) / (endBound.position.z - startBound.position.z) - 1;
        }

        m_animator.SetFloat(horizontalParam, hor);
        m_animator.SetFloat(verticalParam, vert);
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (startBound != null && endBound != null)
        {
            Vector3 center = Vector3.Lerp(startBound.position, endBound.position, 0.5f);
            Vector3 size = new Vector3(
                Mathf.Abs(endBound.position.x - startBound.position.x),
                Mathf.Abs(endBound.position.y - startBound.position.y),
                Mathf.Abs(endBound.position.z - startBound.position.z)
                );

            Gizmos.color = debugGizmoColor;
            Gizmos.DrawCube(center, size);
        }
#endif
    }
}
}
