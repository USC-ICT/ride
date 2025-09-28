using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class MecanimPlayableBehaviour : PlayableBehaviour
{
    protected ICharacterController m_CharacterController;

    protected bool CanPlayBehaviour {  get { return Application.isPlaying && m_CharacterController != null;  } }

    // Called when the owning graph starts playing
    public override void OnGraphStart(Playable playable)
    {
        m_CharacterController = VHUtils.FindObjectOfType<ICharacterController>(GetType().ToString());
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
    }

    // Called when the state of the playable is set to Play
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
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
