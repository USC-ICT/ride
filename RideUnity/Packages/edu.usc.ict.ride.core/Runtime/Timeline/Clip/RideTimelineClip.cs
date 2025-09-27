using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
using System.Collections.Generic;

namespace Ride.Timeline
{
    public abstract class RideTimelineClip : PlayableAsset, ITimelineClipAsset/*, IPropertyPreview*/
    {
        //[HideInInspector] public abstract RideTimelineBehaviour m_template { get; }
        [HideInInspector] public abstract string m_commandType { get; }
        [HideInInspector] public string m_label = "";
        public bool m_waitUntilFinished { get; }   //-IMPORTANT: This value must be passed onto RideTimelineBehaviour::m_waitUntilFinished

        public ClipCaps clipCaps { get { return ClipCaps.None; } }
        public override abstract Playable CreatePlayable(PlayableGraph graph, GameObject owner);

        /// <summary>
        /// Configure clip asset's label, time, or any other specif
        /// Called from RideTimelineTrack.cs::CreateTrackMixer().
        /// </summary>
        /// <param name="clip">clip asset</param>
        /// <param name="clipTimeData">Dictionary containing all of the clips within the track</param>
        public abstract void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData);


        /// <summary>
        /// Add suffix to create unique label.
        /// Having unique label is essential for handling 'WaitUntilFinished' from RideTimelineMixer.cs::ProcessPlayModeFrame().
        /// </summary>
        /// <param name="label"></param>
        /// <param name="clipTimeData"></param>
        /// <returns></returns>
        public string GetUniqueLabel(string label, ref Dictionary<string, double> clipTimeData)
        {
            if (clipTimeData.ContainsKey(label) == false)
                return label;

            int counter = 1;
            string uniqueLabel = label + "_" + counter;

            while (true)
            {
                if (clipTimeData.ContainsKey(uniqueLabel) == false)
                {
                    break;
                }
                counter++;
                uniqueLabel = label + "_" + counter;
            }
            return uniqueLabel;
        }
    }


    public class RideTimelineBehaviour : PlayableBehaviour
    {
        protected RideTimelineManager m_manager { get; set; }
        //private bool m_InClip = false;

        [HideInInspector] public string m_label     { get; set; }
        [HideInInspector] public bool m_isStarted   { get; set; }
        [HideInInspector] public bool m_isFinished { get; set; }
        public virtual bool waitUntilFinished { get; }

        //public virtual void OnClipEnter() { }
        public virtual void OnClipExit() { }
        public virtual void ProcessBehaviour() { }
        public virtual IEnumerator ProcessContinuousBehaviour() { yield break; }


        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            m_manager = playerData as RideTimelineManager;
            if (m_manager == null) { return; }

            //if (!m_isStarted)
            //    m_isStarted = true;
            //{
            //    OnClipEnter();
            //    ProcessClip();
            //}
        }


        public sealed override void OnPlayableDestroy(Playable playable)
        {
            if (m_isStarted && m_manager != null)
            {
                OnClipExit();
                m_isFinished = true;
            }
        }

        public sealed override void OnBehaviourPause(Playable playable, FrameData info) { }
        public sealed override void OnBehaviourPlay(Playable playable, FrameData info) { }
    }
}

