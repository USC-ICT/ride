using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Swap")]
    public class Clip_CameraSwap : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraSwap : RideTimelineBehaviour
        {
            public ExposedReference<Camera> m_from;
            public ExposedReference<Camera> m_to;
            [HideInInspector] public Camera m_internalFrom;
            [HideInInspector] public Camera m_internalTo;
            
            public override void ProcessBehaviour()
            {
                m_internalFrom.gameObject.SetActive(false);
                m_internalTo.gameObject.SetActive(true);
                //var audioListener = m_internalFrom.GetComponent<AudioListener>();
                //if(audioListener != null)
                //    audioListener.enabled = false;
                //m_internalFrom.enabled = false;

                //m_internalTo.enabled = true;
                //audioListener = m_internalTo.GetComponent<AudioListener>();
                //if (audioListener != null)
                //    audioListener.enabled = true;
                //var camera = m_internalTo.GetComponent<Camera>();
                //if (camera != null)
                //    camera.enabled = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraSwap"; }
        public Behaviour_CameraSwap m_behaviour = new Behaviour_CameraSwap();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalFrom = m_behaviour.m_from.Resolve(graph.GetResolver());
            m_behaviour.m_internalTo = m_behaviour.m_to.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraSwap>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            string uniqueLabel = GetUniqueLabel(m_commandType, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
