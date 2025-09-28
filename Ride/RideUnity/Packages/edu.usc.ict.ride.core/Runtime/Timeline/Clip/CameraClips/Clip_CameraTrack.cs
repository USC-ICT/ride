using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VHAssets;

namespace Ride.Timeline
{
    [DisplayName("Camera/Move Along Track")]
    public class Clip_CameraTrack : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_CameraTrack : RideTimelineBehaviour
        {
            public ExposedReference<Camera> m_camera;
            public ExposedReference<Curve> m_curve;
            public ExposedReference<vhAssetsBezierCurveSystem> m_curveSystem;
            public ExposedReference<GameObject> m_lookAtTarget;
            [HideInInspector] public Camera m_internalCamera;
            [HideInInspector] public Curve m_internalCurve;
            [HideInInspector] public vhAssetsBezierCurveSystem m_internalCurveSystem;
            [HideInInspector] public GameObject m_internalLookAtTarget;
            [Range(1f, float.MaxValue)] public float m_duration;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                var goSystem = (GameObjectSystemUnity)Systems.GameObject;
                RideID cameraID = goSystem.GetRideID(m_internalCamera.gameObject);
                RideID curveID = m_internalCurveSystem.GetCurveID(m_internalCurve);

                float currTime = 0;
                while (currTime < m_duration)
                {
                    m_internalCurveSystem.MoveAlong(curveID, cameraID, m_duration);
                    if (m_internalLookAtTarget != null)
                    {
                        m_internalCurveSystem.MoveAlong(curveID, cameraID, m_duration, m_internalLookAtTarget.transform.position);
                        m_internalCamera.transform.LookAt(m_internalLookAtTarget.transform.position);
                    }

                    currTime += Time.deltaTime;
                }

                m_isFinished = true;
                yield return null;
            }
        }

        [HideInInspector] public override string m_commandType { get => "CameraOnTrack"; }
        public Behaviour_CameraTrack m_behaviour = new Behaviour_CameraTrack();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_internalCamera = m_behaviour.m_camera.Resolve(graph.GetResolver());
            m_behaviour.m_internalCurve = m_behaviour.m_curve.Resolve(graph.GetResolver());
            m_behaviour.m_internalCurveSystem = m_behaviour.m_curveSystem.Resolve(graph.GetResolver());
            m_behaviour.m_internalLookAtTarget = m_behaviour.m_lookAtTarget.Resolve(graph.GetResolver());
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_CameraTrack>.Create(graph, m_behaviour);
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
