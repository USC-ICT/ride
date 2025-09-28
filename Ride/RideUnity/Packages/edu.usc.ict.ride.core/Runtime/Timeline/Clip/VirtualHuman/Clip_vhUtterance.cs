using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Virtual Human/Utterance")]
    public class Clip_vhUtterance : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhUtterance : RideTimelineBehaviour
        {
            public TextAsset m_utteranceTextFile = null;
            public float m_time;
            public string m_timestampID;
            public string m_utterance;

            public override void ProcessBehaviour()
            {
            }
        }

        public Behaviour_vhUtterance m_behaviour = new Behaviour_vhUtterance();
        [HideInInspector] public override string m_commandType { get => "VhUtterance"; }
        [HideInInspector] public Clip_vhCutscene m_parentClip;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_vhUtterance>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            if (m_behaviour.m_utteranceTextFile == null)
                return;

            string uniqueLabel = GetUniqueLabel(m_behaviour.m_utteranceTextFile.name, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = m_behaviour.m_utterance;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
