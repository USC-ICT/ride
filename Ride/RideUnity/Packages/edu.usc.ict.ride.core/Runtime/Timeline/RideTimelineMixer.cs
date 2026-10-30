using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ride.Timeline
{
    /// <summary>Evaluates RIDE Timeline clips and coordinates clips that must pause their director until completion.</summary>
    /// <remarks>Created by <see cref="RideTimelineTrack.CreateTrackMixer"/> and bound to a <see cref="RideTimelineManager"/>.</remarks>
    public class RideTimelineTrackMixer : PlayableBehaviour
    {
        /// <summary>The manager bound to the track, or the first manager found in the scene as a fallback.</summary>
        private RideTimelineManager m_trackBinding = null;

        /// <summary>Maps unique clip labels to their configured end times.</summary>
        public Dictionary<string, double> ClipTimeData;   //-Key: Clip label, Value: End time of the clip

        /// <summary>The active behavior currently preventing Timeline progression.</summary>
        private RideTimelineBehaviour m_unfinishedBehaviour = null;

        /// <inheritdoc/>
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

        /// <summary>Starts newly active clip behaviors and tracks any behavior that requests completion blocking.</summary>
        /// <param name="playable">The track mixer playable containing the clip inputs.</param>
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

        /// <summary>Pauses affected directors until the blocking behavior reports completion.</summary>
        /// <param name="director">The director resolving the current playable graph.</param>
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

    /// <summary>Provides the layer-mixer hook required for nested <see cref="RideTimelineTrack"/> instances.</summary>
    /// <remarks>
    /// Layer inputs are currently inspected without applying additional blending behavior. For the Unity layering API,
    /// see <see href="https://docs.unity3d.com/Packages/com.unity.timeline@1.8/api/UnityEngine.Timeline.ILayerable.html"/>.
    /// </remarks>
    public class TimelineCustomLayerMixer : PlayableBehaviour
    {
        /// <inheritdoc/>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                ProcessPlayModeFrame(playable);
            }
        }

        /// <summary>Enumerates the child track mixers participating in the current layer.</summary>
        /// <param name="playable">The layer mixer playable containing the track inputs.</param>
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
