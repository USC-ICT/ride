using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
[Serializable]
public class PlayUtteranceAsset : PlayableAsset
{
    public string m_Character;
    public string m_UtteranceName;
    public AudioClip m_Utterance;
    //public ExposedReference<AudioSpeechFile> m_Utterance;


    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<PlayUtteranceBehaviour>.Create(graph);
        behaviour.GetBehaviour().m_Character = m_Character;
        behaviour.GetBehaviour().m_UtteranceName = m_UtteranceName;
        behaviour.GetBehaviour().m_Utterance = m_Utterance;
        //behaviour.GetBehaviour().m_Utterance = m_Utterance.Resolve(graph.GetResolver());
        
        if (behaviour.GetBehaviour().m_Utterance != null)
        {
            behaviour.SetDuration((double)behaviour.GetBehaviour().m_Utterance.length);
        }

        return behaviour;
    }
}
}
