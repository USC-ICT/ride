using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class SendVHMsgBehaviour : PlayableBehaviour
{
    public string m_Message;

    // Called when the owning graph starts playing
    public override void OnGraphStart(Playable playable)
    {
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
    }

    // Called when the state of the playable is set to Play
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (Application.isPlaying)
        {
#if false
            VHMsgBase.Get().SendVHMsg(m_Message);
#else
            Debug.LogError($"SendVHMsgBehaviour.OnBehaviourPlay() - TODO - Ride Refactor - {playable} - {info}");
#endif
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
