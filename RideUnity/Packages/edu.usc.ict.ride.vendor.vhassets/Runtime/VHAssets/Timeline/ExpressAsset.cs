using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class ExpressAsset : PlayableAsset
{
    public string m_Character;
    public string m_Expression;
    public string m_UttId;
    public string m_UttNum;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<ExpressBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Expression = m_Expression;
        behaviour.GetBehaviour().m_UttId = m_UttId;
        behaviour.GetBehaviour().m_UttNum = m_UttNum;
        return behaviour;
    }
}
}
