using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Camera/Follow")]
    public class Clip_CameraFollow : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraFollow : RideTimelineBehaviour
        {
            [HideInInspector] public Camera m_internalCamera;
            [HideInInspector] public GameObject m_internalTarget;
            public ExposedReference<Camera> m_camera;
            public ExposedReference<GameObject> m_target;
            public string m_entityName;
            public Vector3 m_anchorPoint;
            [Range(1f,float.MaxValue)] public float m_duration;
            public bool m_getTargetByName;
            public bool m_lookAtTarget;

            public override IEnumerator ProcessContinuousBehaviour()
            {
                Vector3 startPosition;
                if (m_getTargetByName)
                    startPosition = (Vector3)Globals.api.agentSystem.GetAgentPosition(m_manager.GetEntityID(m_entityName)) + m_anchorPoint;
                else
                    startPosition = m_internalTarget.transform.position + m_anchorPoint;

                m_internalCamera.transform.position = startPosition;

                Vector3 startForward = m_internalCamera.transform.forward;

                float elapsedTime = 0;
                while (elapsedTime < m_duration)
                {
                    //Handle Position
                    Vector3 currPosition = m_internalCamera.transform.position;
                    Vector3 anchorPosition;
                    if (m_getTargetByName)
                        anchorPosition = (Vector3)Globals.api.agentSystem.GetAgentPosition(m_manager.GetEntityID(m_entityName)) + m_anchorPoint;
                    else
                        anchorPosition = m_internalTarget.transform.position + m_anchorPoint;

                    m_internalCamera.transform.position = anchorPosition;//Vector3.Lerp(currPosition, anchorPosition, elapsedTime / m_duration);

                    //Handle Rotation
                    if (m_lookAtTarget)
                    {
                        Vector3 targetPosition;
                        if (m_getTargetByName)
                        {
                            targetPosition = Globals.api.agentSystem.GetAgentPosition(m_manager.GetEntityID(m_entityName));
                        }
                        else
                            targetPosition = m_internalTarget.transform.position;
                        Vector3 lookAt = targetPosition - m_internalCamera.transform.localPosition;

                        m_internalCamera.transform.LookAt(targetPosition);// = /*Vector3.Lerp(startForward, lookAt, elapsedTime / m_duration);*/
                        // = Vector3.Lerp(forwardDirection, lookAt, elapsedTime/m_duration);
                        elapsedTime += Time.deltaTime;
                    }

                    elapsedTime += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }

                m_isFinished = true;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraFollow"; }
        public Behaviour_CameraFollow m_behaviour = new Behaviour_CameraFollow();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            m_behaviour.m_internalTarget = m_behaviour.m_target.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_CameraFollow>.Create(graph, m_behaviour);
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