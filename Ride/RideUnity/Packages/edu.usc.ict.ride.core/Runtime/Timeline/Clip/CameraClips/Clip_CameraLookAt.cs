using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Look At")]
    public class Clip_CameraLookAt : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraLookAt : RideTimelineBehaviour
        {
            [HideInInspector] public Camera m_internalCamera;
            [HideInInspector] public GameObject m_internalTarget;
            public ExposedReference<Camera> m_camera;
            public ExposedReference<GameObject> m_target;
            public string m_entityName;
            public Vector3 m_position;
            public float m_duration;
            public bool m_getTargetByName;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                if (m_duration <= 0)
                {
                    Vector3 lookAt;// = (m_internalTarget == null) ? m_position : m_internalTarget.transform.position;
                    if (m_getTargetByName)
                        lookAt = Globals.api.agentSystem.GetAgentPosition(m_manager.GetEntityID(m_entityName));
                    else
                        lookAt = (m_internalTarget == null) ? m_position : m_internalTarget.transform.position;

                    m_internalCamera.transform.LookAt(lookAt);
                    m_isFinished = true;
                    Debug.Log("Foo");
                    yield break;
                }

                float elapsedTime = 0;
                Vector3 forwardDirection = m_internalCamera.transform.forward;
                while (elapsedTime < m_duration)
                {
                    Vector3 targetPosition;
                    if (m_getTargetByName)
                    {
                        targetPosition = Globals.api.agentSystem.GetAgentPosition(m_manager.GetEntityID(m_entityName));
                    }
                    else
                        targetPosition = (m_internalTarget == null) ? m_position : m_internalTarget.transform.position;

                    Vector3 lookAt = targetPosition - m_internalCamera.transform.localPosition;

                    m_internalCamera.transform.forward = Vector3.Lerp(forwardDirection, lookAt, elapsedTime / m_duration);
                        // = Vector3.Lerp(forwardDirection, lookAt, elapsedTime/m_duration);
                    elapsedTime += Time.deltaTime;

                    yield return null;
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraLookAt"; }
        public Behaviour_CameraLookAt m_behaviour = new Behaviour_CameraLookAt();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            m_behaviour.m_internalTarget = m_behaviour.m_target.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraLookAt>.Create(graph, m_behaviour);
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