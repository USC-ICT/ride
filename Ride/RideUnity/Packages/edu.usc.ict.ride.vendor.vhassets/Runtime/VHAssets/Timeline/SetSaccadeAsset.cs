using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class SetSaccadeAsset : PlayableAsset
{
    public string m_Character;
    public CharacterDefines.SaccadeType m_SaccadeType = CharacterDefines.SaccadeType.Default;
    public bool m_Finish = true;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<SetSaccadeBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_SaccadeType = m_SaccadeType;
        behaviour.GetBehaviour().m_Finish = m_Finish;
        return behaviour;
    }
}
}
