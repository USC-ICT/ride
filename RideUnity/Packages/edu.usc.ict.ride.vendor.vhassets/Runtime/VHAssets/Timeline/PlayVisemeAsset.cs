using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class PlayVisemeAsset : PlayableAsset
{
    public string m_Character;
    public string m_Viseme;
    public float m_Weight = 1;
    public float m_BlendTime = 1;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<PlayVisemeBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Viseme = m_Viseme;
        behaviour.GetBehaviour().m_Weight = m_Weight;
        behaviour.GetBehaviour().m_BlendTime = m_BlendTime;
        return behaviour;
    }
}
}
