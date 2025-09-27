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
    [DisplayName("Virtual Human/Play Audio")]
    public class Clip_vhPlayAudio : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhPlayAudio : RideTimelineBehaviour
        {           
            public string m_characterName;
            public AudioClip m_audioClip;
            public TextAsset m_lipSyncInfo;
            public string m_timestampID;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override System.Collections.IEnumerator ProcessContinuousBehaviour()
            {
                string facefxCurveInfo = m_lipSyncInfo.text;
                AudioSpeechFile utterance = AudioSpeechFile.CreateAudioSpeechFile(facefxCurveInfo, string.Empty, m_audioClip);

                RideTimelineManager manager = FindFirstObjectByType<RideTimelineManager>();
                var character = manager.GetCharacter(m_characterName);

                character.PlayAudio(utterance);

                Destroy(utterance.gameObject);  // Clean up utterance scene-object.
                yield return null;
            }
        }

        public Behaviour_vhPlayAudio m_behaviour = new Behaviour_vhPlayAudio();
        [HideInInspector] public override string m_commandType { get => "VhPlayAudio"; }
        [HideInInspector] public Clip_vhCutscene m_parentClip;


        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_vhPlayAudio>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            string uniqueLabel = GetUniqueLabel(m_behaviour.m_audioClip.name, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
