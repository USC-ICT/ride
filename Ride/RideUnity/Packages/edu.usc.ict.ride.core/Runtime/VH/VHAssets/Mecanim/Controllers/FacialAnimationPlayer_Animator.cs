using System.Collections.Generic;
using Ride;
using UnityEngine;

namespace VHAssets
{
public class FacialAnimationPlayer_Animator : FacialAnimationPlayer
{
    #region Variables
    [SerializeField] Animator m_Animator;
    HashSet<string> m_AnimatorParams = new HashSet<string>();
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
                Debug.LogError("Gameobject " + name + " doesn't have an animator. Facial animations won't work");
        }

        if (m_Animator != null)
        {
            foreach (AnimatorControllerParameter p in m_Animator.parameters)
                m_AnimatorParams.Add(p.name);
        }
    }

    protected override void SetViseme(string viseme, float weight)
    {
        if (viseme.Contains("Pitch") || viseme.Contains("Yaw") || viseme.Contains("Roll"))
            return;

        if (m_AnimatorParams.Contains(viseme) == false) { return; }

        if (viseme == "face_neutral")
        {
            foreach (var map in MecanimManager.VisemeMap)
                foreach (var anim in map.Value)
                    m_Animator.SetFloat(anim, 0);
        }
        else
            m_Animator.SetFloat(viseme, weight * m_FacialVisemeMultiplier * GetVisemeModifierWeightMultiplier(viseme));
    }

    protected override float GetViseme(string viseme)
    {
        float articulation = 0;
        if (m_AnimatorParams.Contains(viseme))
            articulation = m_Animator.GetFloat(viseme);
        else
        {
            string[] effectedVisemes = MecanimManager.GetEffectedFaceParameterNames(viseme);
            if (effectedVisemes != null && effectedVisemes.Length >= 1)
                articulation =  m_Animator.GetFloat(effectedVisemes[0]);
            else
                Debug.LogError("Failed to find parameter " + viseme);
        }

        return articulation;
    }
    #endregion
}
}
