using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
/// <summary>
/// Various utlities for working with Mecanim. Note that there is also a companion file, VHMecanimEditorUtils.cs,
/// that serves a similar purpose but relies on classes using UnityEditor.
/// </summary>
public static class VHMecanimUtils
{
    /// <summary>
    /// Check that the specified layer exists on specified animator.
    /// </summary>
    public static bool LayerExists(string layerName, Animator animator)
    {
        bool m_layerExists = false;
        
        int m_index = animator.GetLayerIndex(layerName);
        if (m_index != -1)
        {
            m_layerExists = true;
        }

        return m_layerExists;
    }


    /// <summary>
    /// Set the weight of a layer on an animator controller.
    /// </summary>
    /// <param name="layerName">Name of layer.</param>
    /// <param name="animator">Name of animator layer exists in.</param>
    /// <param name="weight">Weight to be set.</param>
    public static void LayerSetWeight(string layerName, Animator animator, float weight)
    {
        // Check layer exists
        if (LayerExists(layerName, animator) != true)
        {
            Debug.LogErrorFormat("Specified layer: {0}, does not exist in animator: {1}", layerName, animator);
        }
        
        // Set weight
        int m_index = animator.GetLayerIndex(layerName);
        animator.SetLayerWeight(m_index, weight);
    }


    /// <summary>
    /// Check that the specified controller parameter exists on specified animator.
    /// </summary>
    public static bool ParameterExists(string parameterName, Animator animator)
    {
        bool m_paramExists = false;

        AnimatorControllerParameter[] m_params = animator.parameters;

        foreach (AnimatorControllerParameter i in m_params)
        {
            if (i.name == parameterName)
            {
                m_paramExists = true;
            }
        }

        return m_paramExists;
    }
    
    
    #region PlayState
    /// <summary>
    /// Wrapper around Animator.Play which gives better feedback.
    /// </summary>
    public static void PlayState(Animator animator, string targetState, int layerIndex, float normalizedTime)
    {
        if (DoesStateExist(animator, targetState, layerIndex))
        {
            animator.Play(targetState, layerIndex, normalizedTime);
        }
        else
        {
            Debug.LogError("No animator state exists: <color=white>" + targetState + "</color> in animator: <color=white>" + animator + "</color>");
        }
    }


    /// <summary>
    /// Wrapper around Animator.Play which gives better feedback.
    /// </summary>
    public static void PlayState(Animator animator, string targetState, int layerIndex)
    {
        PlayState(animator, targetState, layerIndex, float.NegativeInfinity);
    }


    /// <summary>
    /// Wrapper around Animator.Play which gives better feedback.
    /// </summary>
    public static void PlayState(Animator animator, string targetState)
    {
        if (DoesStateExist(animator, targetState))
        {
            animator.Play(targetState);
        }
        else
        {
            Debug.LogError("No animator state exists: <color=white>" + targetState + "</color> in animator: <color=white>" + animator + "</color>");
        }
    }
    #endregion

    #region DoesStateExist
    /// <summary>
    /// Checks every layer to see if the state exists in the given animator
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="stateName"></param>
    /// <returns></returns>
    public static bool DoesStateExist(Animator animator, string stateName)
    {
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (DoesStateExist(animator, stateName, i))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks the specified layer to see if the state exists in the given animator
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="stateName"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool DoesStateExist(Animator animator, string stateName, int layer)
    {
        int m_startingPostureHash = Animator.StringToHash(stateName);
        return animator.HasState(layer, m_startingPostureHash);
    }

    public static void AnimatorFloatTweenToZero(MonoBehaviour coroutineOwner, Animator animator, string paramName, float duration = 1f)
    {
        coroutineOwner.StartCoroutine(AnimatorFloatTweenToZeroCR(animator, paramName, duration));
    }

    public static IEnumerator AnimatorFloatTweenToZeroCR(Animator animator, string paramName, float duration = 1f)
    {
        if (animator == null)
        {
            yield break;
        }

        float timer = 0;
        while (timer < duration)
        {
            animator.SetFloat(paramName, Mathf.Lerp(animator.GetFloat(paramName), 0, Time.deltaTime / duration));
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        animator.SetFloat(paramName, 0f);
    }

    #endregion
}
}
