using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class SetPostureAsset : PlayableAsset
{
    public string m_Character;
    public string m_Posture;
    public float m_StartTime;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<SetPostureBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_Posture = m_Posture;
        behaviour.GetBehaviour().m_StartTime = m_StartTime;
        return behaviour;
    }
}
}
