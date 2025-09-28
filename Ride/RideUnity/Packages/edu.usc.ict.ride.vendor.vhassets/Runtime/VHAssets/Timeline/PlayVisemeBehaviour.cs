using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class PlayVisemeBehaviour : MecanimPlayableBehaviour
{
    public string m_Character = "";
    public string m_Viseme = "";
    public float m_Weight = 1;
    public float m_BlendTime = 1;

    // Called when the owning graph starts playing
    public override void OnGraphStart(Playable playable)
    {
        base.OnGraphStart(playable);
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
    }

    // Called when the state of the playable is set to Play
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (CanPlayBehaviour)
        {
            m_CharacterController.SBPlayViseme(m_Character, m_Viseme, m_Weight, (float)playable.GetDuration(), m_BlendTime);
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
