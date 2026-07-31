using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ride.Timeline
{
    public class RideTimelineTrackMixer : PlayableBehaviour
    {
        private RideTimelineManager m_trackBinding = null;
        public Dictionary<string, double> ClipTimeData;   //-Key: Clip label, Value: End time of the clip
        private RideTimelineBehaviour m_unfinishedBehaviour = null;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                m_trackBinding = playerData as RideTimelineManager;
                if (m_trackBinding == null)
                    m_trackBinding = Object.FindAnyObjectByType<RideTimelineManager>();
                if (m_trackBinding == null)
                {
                    Debug.LogWarning("RideTimelineMixer.cs: Track binding is null."); return;
                }

                ProcessPlayModeFrame(playable);
            }
        }

        public void ProcessPlayModeFrame(Playable playable)
        {
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; ++i)
            {
                Playable inputPlayable = playable.GetInput(i);
                RideTimelineBehaviour behaviour = ((ScriptPlayable<RideTimelineBehaviour>)inputPlayable).GetBehaviour();

                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f) { continue; }   // input weight is > 0 when the needle hits any clip.

                if (behaviour.m_isStarted == false)
                {
                    m_trackBinding.ProcessClip(behaviour);
                    behaviour.m_isStarted = true;
                }
                if (behaviour.waitUntilFinished && behaviour.m_isFinished == false)   // If waitUntilFinished flag is set as true, 
                {
                    m_unfinishedBehaviour = behaviour;
                }
            }
            if (m_unfinishedBehaviour != null)
                HandleUnfinishedBehaviour(playable.GetGraph().GetResolver() as PlayableDirector);
        }

        private void HandleUnfinishedBehaviour(PlayableDirector director)
        {
            if (m_unfinishedBehaviour.m_isFinished)
            {
                m_unfinishedBehaviour = null;
                m_trackBinding.ResumeTimelines();
                return;
            }
            else
            {
                m_trackBinding.PauseTimelines(director);
            }
        }
    }

    /// <summary>
    /// This mixer handles the track layering. Reference: https://forum.unity.com/threads/any-code-samples-for-how-to-use-ilayerable.745751/
    /// </summary>
    public class TimelineCustomLayerMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                ProcessPlayModeFrame(playable);
            }
        }

        private void ProcessPlayModeFrame(Playable playable)
        {
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; ++i)
            {
                var input = playable.GetInput(i);

                RideTimelineTrackMixer trackMixer = ((ScriptPlayable<RideTimelineTrackMixer>)input).GetBehaviour();

                if (Application.isPlaying)
                {

                }
            }
        }
    }
}
