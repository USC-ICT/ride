using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class MoveToAsset : PlayableAsset
{
    public string m_Character = "";
    public ExposedReference<Transform> m_Destination;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<MoveToBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Destination = m_Destination.Resolve(graph.GetResolver());

        return behaviour;
    }
}
}
