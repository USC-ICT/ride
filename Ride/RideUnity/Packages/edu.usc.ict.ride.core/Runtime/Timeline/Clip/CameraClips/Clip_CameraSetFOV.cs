using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Set FOV")]
    public class Clip_CameraSetFOV : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraSetFOV : RideTimelineBehaviour
        {
            public ExposedReference<Camera> m_camera;
            [HideInInspector] public Camera m_internalCamera; 
            public float m_FOV = 60f;
            public float m_duration;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                if(m_duration <= 0)
                {
                    m_internalCamera.fieldOfView = m_FOV;
                    m_isFinished = true;
                    yield return null;
                }

                float elapsedTime = 0;
                float startFov = m_internalCamera.fieldOfView;

                while (elapsedTime < m_duration)
                {
                    m_internalCamera.fieldOfView = Mathf.SmoothStep(startFov, m_FOV, elapsedTime / m_duration);
                    elapsedTime += Time.deltaTime;

                    yield return new WaitForEndOfFrame();
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraSetFOV"; }
        public Behaviour_CameraSetFOV m_behaviour = new Behaviour_CameraSetFOV();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraSetFOV>.Create(graph, m_behaviour);
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
