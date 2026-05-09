using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
[Serializable]
public struct BlendshapeID
{
    public string name;
    public int index;
}

public class BlendshapeSaccadeController : SaccadeController
{
    public SkinnedMeshRenderer targetSkinnedMesh;
    [SerializeField] Vector2 maxEyeAngle = new Vector2(30, 30);
    [SerializeField] BlendshapeID eyesUpBlendShape;
    [SerializeField] BlendshapeID eyesDownBlendShape;
    [SerializeField] BlendshapeID eyesLeftBlendShape;
    [SerializeField] BlendshapeID eyesRightBlendShape;

    //float m_yWeight = 0;
    //float m_xWeight = 0;

    protected override void Awake()
    {
        base.Awake();

        Mesh meshTarget = targetSkinnedMesh.sharedMesh;

        eyesUpBlendShape.index = meshTarget.GetBlendShapeIndex(eyesUpBlendShape.name);
        eyesDownBlendShape.index = meshTarget.GetBlendShapeIndex(eyesDownBlendShape.name);
        eyesLeftBlendShape.index = meshTarget.GetBlendShapeIndex(eyesLeftBlendShape.name);
        eyesRightBlendShape.index = meshTarget.GetBlendShapeIndex(eyesRightBlendShape.name);
    }



    protected override void ApplyProcessedSaccade()
    {
        // This function needs revisiting after SaccadeController refactoring.

        throw new NotImplementedException("This function needs revisiting after SaccadeController refactoring.");

#if false
        const float lerpValue = 0.5f;

        Quaternion rot = default;
        if (m_SaccadeState == SaccadeState.FadeIn)
        {

            foreach (EyeData eye in m_Eyes)
            {
                rot = Quaternion.Slerp(eye.m_LastRotation, eye.m_TargetRotation, lerpValue);
                eye.m_Eye.localRotation = rot;
            }

        }
        else if (m_SaccadeState == SaccadeState.FadeOut)
        {

            foreach (EyeData eye in m_Eyes)
            {
                rot = Quaternion.Slerp(eye.m_TargetRotation, eye.m_InitialRotation, lerpValue);
                eye.m_Eye.localRotation = rot;
            }
        }

        Vector3 angle = rot.eulerAngles;
        m_yWeight = 0;
        m_xWeight = 0;

        if (angle.x > 180)
        {
            angle.x = 360 - angle.x;
            m_xWeight = (100 * angle.x / maxEyeAngle.x);
            targetSkinnedMesh.SetBlendShapeWeight(eyesUpBlendShape.index, m_xWeight);
            targetSkinnedMesh.SetBlendShapeWeight(eyesDownBlendShape.index, 0);
        }
        else if (angle.x > 0)
        {
            m_xWeight = (100 * angle.x / maxEyeAngle.x);
            targetSkinnedMesh.SetBlendShapeWeight(eyesDownBlendShape.index, m_xWeight);
            targetSkinnedMesh.SetBlendShapeWeight(eyesUpBlendShape.index, 0);
        }
        else
        {
            targetSkinnedMesh.SetBlendShapeWeight(eyesUpBlendShape.index, 0);
            targetSkinnedMesh.SetBlendShapeWeight(eyesDownBlendShape.index, 0);
        }

        if (angle.y > 180)
        {
            angle.y = 360 - angle.y;
            m_yWeight = (100 * angle.y / maxEyeAngle.y);
            targetSkinnedMesh.SetBlendShapeWeight(eyesLeftBlendShape.index, m_yWeight);
            targetSkinnedMesh.SetBlendShapeWeight(eyesRightBlendShape.index, 0);
        }
        else if (angle.y > 0)
        {
            m_yWeight = (100 * angle.y / maxEyeAngle.y);
            targetSkinnedMesh.SetBlendShapeWeight(eyesRightBlendShape.index, m_yWeight);
            targetSkinnedMesh.SetBlendShapeWeight(eyesLeftBlendShape.index, 0);
        }

        else
        {
            targetSkinnedMesh.SetBlendShapeWeight(eyesLeftBlendShape.index, 0);
            targetSkinnedMesh.SetBlendShapeWeight(eyesRightBlendShape.index, 0);
        }
#endif
    }
}
}
