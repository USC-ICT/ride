using System.Collections.Generic;
using Ride;
using UnityEngine;

namespace VHAssets
{
public class FacialAnimationPlayer_Animator : FacialAnimationPlayer
{
    #region Fields
    [SerializeField] private Animator m_Animator;

    private HashSet<string> m_animatorParams = new();
    #endregion

    #region Functions
    void Start()
    {
        if (!TryGetComponent(out ILoadableAsset loadedAsset))
            InitializeLoadedAsset();
    }

    public void InitializeLoadedAsset()
    {
        if (m_Animator == null)
        {
            m_Animator = GetComponentInChildren<Animator>();
            if (m_Animator == null)
                Debug.LogError($"Gameobject {name} doesn't have an animator. Facial animations won't work");
        }

        if (m_Animator != null)
        {
            foreach (var p in m_Animator.parameters)
                m_animatorParams.Add(p.name);
        }
    }

    protected override void SetViseme(string viseme, float weight)
    {
        if (viseme.Contains("Pitch") || viseme.Contains("Yaw") || viseme.Contains("Roll"))
            return;

        if (!m_animatorParams.Contains(viseme))
            return;

        m_Animator.SetFloat(viseme, weight * m_FacialVisemeMultiplier * GetVisemeModifierWeightMultiplier(viseme));
    }

    protected override float GetViseme(string viseme)
    {
        float articulation = 0;
        if (m_animatorParams.Contains(viseme))
        {
            articulation = m_Animator.GetFloat(viseme);
        }
        else
        {
            var parameters = MecanimManager.GetAnimatorParametersForViseme(viseme);
            if (parameters != null && parameters.Length > 0)
                articulation = m_Animator.GetFloat(parameters[0]);
            else
                Debug.LogError($"Failed to find parameter {viseme}");
        }

        return articulation;
    }
    #endregion
}
}
