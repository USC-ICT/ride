using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class FacialAnimationPlayer_BlendShape : FacialAnimationPlayer
{
    #region Variables
    [SerializeField] SkinnedMeshRenderer m_SkinnedMesh;
    Dictionary<string, int> m_BlendShapes = new Dictionary<string, int>();
    #endregion

    #region Functions
    void Start()
    {
        if (m_SkinnedMesh == null)
        {
            m_SkinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();

            if (m_SkinnedMesh == null)
            {
                Debug.LogError("Gameobject " + name + " doesn't have a SkinnedMeshRendere. Facial animations won't work");
            }
        }

        for (int i = 0; i < m_SkinnedMesh.sharedMesh.blendShapeCount; i++)
        {
            string blendShapeName = m_SkinnedMesh.sharedMesh.GetBlendShapeName(i);
            m_BlendShapes.Add(blendShapeName, i);
        }
    }

    protected override void SetViseme(string viseme, float weight)
    {
        if (DoesBlendShapeExist(viseme))
        {
            m_SkinnedMesh.SetBlendShapeWeight(m_BlendShapes[viseme], weight * m_FacialVisemeMultiplier * GetVisemeModifierWeightMultiplier(viseme));
        }
    }

    protected override float GetViseme(string viseme)
    {
        return DoesBlendShapeExist(viseme) ? m_SkinnedMesh.GetBlendShapeWeight(m_BlendShapes[viseme]) : 0;
    }

    bool DoesBlendShapeExist(string blendShapeName)
    {
        bool exists = false;
        if (m_BlendShapes.ContainsKey(blendShapeName))
        {
            exists = true;
        }
        else
        {
            Debug.LogError("No blend shape named " + blendShapeName + " on character " + m_SkinnedMesh.name);
        }
        return exists;
    }
    #endregion
}
}
