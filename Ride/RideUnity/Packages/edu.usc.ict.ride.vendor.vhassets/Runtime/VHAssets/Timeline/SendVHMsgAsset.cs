using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class SendVHMsgAsset : PlayableAsset
{
    public string m_Message;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<SendVHMsgBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Message = m_Message;
        return behaviour;
    }
}
}
