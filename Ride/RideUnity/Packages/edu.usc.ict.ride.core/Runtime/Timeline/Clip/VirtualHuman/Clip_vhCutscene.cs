using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ride.Timeline
{
    [DisplayName("Virtual Human/Play Cutscene")]
    public class Clip_vhCutscene : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhCutscene : RideTimelineBehaviour
        {
            public string m_characterName;
            public AudioClip m_audio;
            public TextAsset m_utteranceText;
            public TextAsset m_lipSyncInfo;
            public TextAsset m_xml;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                m_isFinished = true;
                yield return null;
            }
        }

        [HideInInspector] public override string m_commandType { get => "VhCutscene"; }
        public Behaviour_vhCutscene m_behaviour = new Behaviour_vhCutscene();
        [HideInInspector] public TrackAsset m_audioTrack;
        [HideInInspector] public TrackAsset m_utteranceTrack;
        [HideInInspector] public TrackAsset m_lipsyncTrack;
        [HideInInspector] public TrackAsset m_animationTrack;
        [HideInInspector] public TrackAsset m_visemeTrack;
        [HideInInspector] public TrackAsset m_faceAnimationTrack;
        [HideInInspector] public bool m_createLayer = false;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_vhCutscene>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            if (m_behaviour.m_audio == null)
                return;

            string uniqueLabel = GetUniqueLabel(m_behaviour.m_audio.name, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }

        /// <summary>
        /// Setting m_createLayer as true will trigger CreateLayer method in CutsceneExperimental Editor script.
        /// Layer is created from the editor script due to references that are hard to reach from a clip script.
        /// </summary>
        public void CreateLayer()
        {
            m_createLayer = true;
        }
    }
}
