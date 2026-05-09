using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace VHAssets
{
// A behaviour that is attached to a playable
public class SetAnimatorParamBehaviour : PlayableBehaviour
{
    public string m_Character;
    public string m_ParamName;
    public bool m_ParamBoolValue;
    public int m_ParamIntValue;
    public float m_ParamFloatValue;
    public AnimatorControllerParameterType m_Type;
    Animator m_Animtor;
    
    // Called when the owning graph starts playing
    public override void OnGraphStart(Playable playable)
    {
        var go = GameObject.Find(m_Character);
        if (go != null)
        {
            m_Animtor = go.GetComponent<Animator>();
            if (m_Animtor == null)
                Debug.LogError($"{GetType()} failed to find component {typeof(Animator)} on gameobject {m_Character}");
        }
        else
        {
            Debug.LogError($"{GetType()} failed to find gameobject with name {m_Character}");
        }
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
    }

    // Called when the state of the playable is set to Play
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (Application.isPlaying && m_Animtor != null)
        {
            switch (m_Type)
            {
                case AnimatorControllerParameterType.Bool:
                    m_Animtor.SetBool(m_ParamName, m_ParamBoolValue);
                    break;

                case AnimatorControllerParameterType.Float:
                    m_Animtor.SetFloat(m_ParamName, m_ParamFloatValue);
                    break;

                case AnimatorControllerParameterType.Int:
                    m_Animtor.SetInteger(m_ParamName, m_ParamIntValue);
                    break;

                case AnimatorControllerParameterType.Trigger:
                    m_Animtor.SetTrigger(m_ParamName);
                    break;
            }
        }
    }

    // Called when the state of the playable is set to Paused
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
    }

    // Called each frame while the state is set to Play
    public override void PrepareFrame(Playable playable, FrameData info)
    {
    }
}
}
