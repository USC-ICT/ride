using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class PlayAnimAsset : PlayableAsset
{
    public string m_Character;
    public string m_Anim;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<PlayAnimBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Anim = m_Anim;
        return behaviour;
    }
}
}
