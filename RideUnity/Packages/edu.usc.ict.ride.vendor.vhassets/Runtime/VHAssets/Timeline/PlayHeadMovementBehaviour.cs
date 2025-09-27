using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class PlayHeadMovementBehaviour : MecanimPlayableBehaviour
{
    public string m_Character = "";
    public float m_Amplitude = 1;
    public float m_Repeats = 1;
    public CharacterDefines.HeadMovementType m_MovementType = CharacterDefines.HeadMovementType.Nod;

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
            switch (m_MovementType)
            {
                case CharacterDefines.HeadMovementType.Shake:
                    m_CharacterController.SBShake(m_Character, m_Amplitude, m_Repeats, (float)playable.GetDuration());
                    break;

                case CharacterDefines.HeadMovementType.Tilt:
                    m_CharacterController.SBTilt(m_Character, m_Amplitude, m_Repeats, (float)playable.GetDuration());
                    break;

                case CharacterDefines.HeadMovementType.Nod:
                    m_CharacterController.SBNod(m_Character, m_Amplitude, m_Repeats, (float)playable.GetDuration());
                    break;
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
