using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class ExpressBehaviour : MecanimPlayableBehaviour
{
    public string m_Character;
    public string m_Expression;
    public string m_UttId;
    public string m_UttNum;

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
            m_CharacterController.SBExpress(m_Character, m_UttId, m_UttNum, m_Expression);
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
