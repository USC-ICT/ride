using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Move")]
    public class Clip_CameraMove : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraMove : RideTimelineBehaviour
        {
            [HideInInspector] public Camera m_internalCamera;
            public ExposedReference<Camera> m_camera;
            public Vector3 m_position;
            public float m_duration;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                if (m_duration <= 0)
                {
                    m_internalCamera.transform.position = m_position;
                    m_isFinished = true;
                    yield break;
                }

                float elapsedTime = 0;
                Vector3 startPosition = m_internalCamera.transform.position;
                while (elapsedTime < m_duration)
                {
                    m_internalCamera.transform.position = Vector3.Lerp(startPosition, m_position, elapsedTime/m_duration);
                    elapsedTime += Time.deltaTime;

                    yield return new WaitForEndOfFrame();
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraMove"; }
        public Behaviour_CameraMove m_behaviour = new Behaviour_CameraMove();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraMove>.Create(graph, m_behaviour);
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