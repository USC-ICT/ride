using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class TransformBehaviour : PlayableBehaviour
{
    public Transform m_Source;
    public Transform m_Target;
    public bool m_IncludeRotation;
    //Vector3 m_OriginalPos;
    //Quaternion m_OriginalRot;

    // Called when the owning graph starts playing
    public override void OnGraphStart(Playable playable)
    {
        //m_OriginalPos = m_Source.position;
        //m_OriginalRot = m_Source.rotation;
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
    }

    // Called when the state of the playable is set to Play
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        m_Source.position = m_Target.position;
        if (m_IncludeRotation)
        {
            m_Source.rotation = m_Target.rotation;
        }
    }

    // Called when the state of the playable is set to Paused
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
    }

    // Called each frame while the state is set to Play
    public override void PrepareFrame(Playable playable, FrameData info)
    {
    }
}
}
