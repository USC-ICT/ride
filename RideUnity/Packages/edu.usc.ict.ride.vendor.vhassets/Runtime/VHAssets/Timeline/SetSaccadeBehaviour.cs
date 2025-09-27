using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class SetSaccadeBehaviour : MecanimPlayableBehaviour
{
    public string m_Character;
    public CharacterDefines.SaccadeType m_SaccadeType = CharacterDefines.SaccadeType.Default;
    public bool m_Finish = true;

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
            if (m_SaccadeType == CharacterDefines.SaccadeType.End)
            {
                m_CharacterController.SBStopSaccade(m_Character);
            }
            else
            {
                m_CharacterController.SBSaccade(m_Character, m_SaccadeType, m_Finish, (float)playable.GetDuration());
            }
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
