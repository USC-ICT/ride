using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class GazeAsset : PlayableAsset
{
    public string m_Character;
    public string m_GazeTarget = "";
    public float m_NeckSpeed = 400;
    public float m_EyeSpeed = 400;
    public CharacterDefines.GazeJointRange m_JointRange = CharacterDefines.GazeJointRange.EYES_NECK;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<GazeBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_GazeTarget = m_GazeTarget;
        behaviour.GetBehaviour().m_NeckSpeed = m_NeckSpeed;
        behaviour.GetBehaviour().m_EyeSpeed = m_EyeSpeed;
        behaviour.GetBehaviour().m_JointRange = m_JointRange;
        return behaviour;
    }
}
}
