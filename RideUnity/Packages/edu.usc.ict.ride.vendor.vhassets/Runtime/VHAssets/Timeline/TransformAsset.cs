using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class TransformAsset : PlayableAsset
{
    public ExposedReference<Transform> m_Source;
    public ExposedReference<Transform> m_Target;
    public bool m_IncludeRotation;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<TransformBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Source = m_Source.Resolve(graph.GetResolver());
        behaviour.GetBehaviour().m_Target = m_Target.Resolve(graph.GetResolver());
        behaviour.GetBehaviour().m_IncludeRotation = m_IncludeRotation;
        return behaviour;
    }
}
}
