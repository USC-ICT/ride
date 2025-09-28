using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class PlayHeadMovementAsset : PlayableAsset
{
    public string m_Character = "";
    public float m_Amplitude = 1;
    public float m_Repeats = 1;
    public CharacterDefines.HeadMovementType m_MovementType = CharacterDefines.HeadMovementType.Nod;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<PlayHeadMovementBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Amplitude = m_Amplitude;
        behaviour.GetBehaviour().m_Repeats = m_Repeats;
        behaviour.GetBehaviour().m_MovementType = m_MovementType;
        return behaviour;
    }
}
}
