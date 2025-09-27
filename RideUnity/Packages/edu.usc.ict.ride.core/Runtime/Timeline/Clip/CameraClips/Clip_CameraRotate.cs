using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Rotate")]
    public class Clip_CameraRotate : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraRotate : RideTimelineBehaviour
        {
            [HideInInspector] public Camera m_internalCamera;
            public ExposedReference<Camera> m_camera;
            public Vector3 m_rotation;
            public float m_duration;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                if(m_duration <= 0)
                {
                    m_internalCamera.transform.rotation = Quaternion.Euler(m_rotation);
                    m_isFinished = true;
                    yield break;
                }

                float elapsedTime = 0;
                Vector3 startRotation = m_internalCamera.transform.eulerAngles;
                while (elapsedTime < m_duration)
                {
                    m_internalCamera.transform.eulerAngles = Vector3.Lerp(startRotation, m_rotation, Mathf.SmoothStep(0f, 1f, m_duration));
                    elapsedTime += Time.deltaTime;

                    yield return new WaitForEndOfFrame();
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraRotate"; }
        public Behaviour_CameraRotate m_behaviour = new Behaviour_CameraRotate();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraRotate>.Create(graph, m_behaviour);
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
