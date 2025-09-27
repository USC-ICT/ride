using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class SetAnimatorParamAsset : PlayableAsset
{ 
    public string m_Character;
    public string m_ParamName;
    public bool m_ParamBoolValue;
    public int m_ParamIntValue;
    public float m_ParamFloatValue;
    public AnimatorControllerParameterType m_Type;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<SetAnimatorParamBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_ParamName = m_ParamName;
        behaviour.GetBehaviour().m_ParamBoolValue = m_ParamBoolValue;
        behaviour.GetBehaviour().m_ParamIntValue = m_ParamIntValue;
        behaviour.GetBehaviour().m_ParamFloatValue = m_ParamFloatValue;
        behaviour.GetBehaviour().m_Type = m_Type;
        return behaviour;
    }
}
}
