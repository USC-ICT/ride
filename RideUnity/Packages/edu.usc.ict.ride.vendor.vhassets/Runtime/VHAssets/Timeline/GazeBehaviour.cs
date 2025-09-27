using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class GazeBehaviour : MecanimPlayableBehaviour
{
    public string m_Character;
    public string m_GazeTarget = "";
    public float m_NeckSpeed = 400;
    public float m_EyeSpeed = 400;
    public CharacterDefines.GazeJointRange m_JointRange = CharacterDefines.GazeJointRange.EYES_NECK;

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
            m_CharacterController.SBGaze(m_Character, m_GazeTarget, m_NeckSpeed, m_EyeSpeed, m_JointRange);
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
