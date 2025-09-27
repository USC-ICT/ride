using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Virtual Human/Viseme")]
    public class Clip_vhViseme : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhViseme : RideTimelineBehaviour
        {
            public TextAsset m_visemeTextFile = null;
            public float m_time;            
            public string m_viseme;
            public float m_articulation;
            public float m_startTime;
            public float m_readyTime;
            public float m_relaxTime;
            public float m_endTime;
        }

        public Behaviour_vhViseme m_behaviour = new Behaviour_vhViseme();
        [HideInInspector] public override string m_commandType { get => "vhViseme"; }
        [HideInInspector] public Clip_vhCutscene m_parentClip;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_vhViseme>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            if (m_behaviour.m_visemeTextFile == null)
                return;

            string uniqueLabel = GetUniqueLabel(m_behaviour.m_visemeTextFile.name, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = m_behaviour.m_viseme;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
