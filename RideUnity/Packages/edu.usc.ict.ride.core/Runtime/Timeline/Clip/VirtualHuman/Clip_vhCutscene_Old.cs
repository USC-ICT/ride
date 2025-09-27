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
    [DisplayName("Virtual Human/Play Cutscene")]
    public class Clip_vhCutscene_Old : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhCutscene_Old : RideTimelineBehaviour
        {
            public ExposedReference<Cutscene> m_cutscene;
            public Cutscene m_internalCutscene;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                m_internalCutscene.Play();

                if (m_waitUntilFinished)
                    yield return new WaitForSeconds(m_internalCutscene.Length);

                m_isFinished = true;
                yield return null;
            }
        }

        [HideInInspector] public override string m_commandType { get => "VhCutsceneOld"; }
        public Behaviour_vhCutscene_Old m_behaviour = new Behaviour_vhCutscene_Old();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            m_behaviour.m_internalCutscene = m_behaviour.m_cutscene.Resolve(graph.GetResolver());
            return ScriptPlayable<Behaviour_vhCutscene_Old>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            if (m_behaviour.m_internalCutscene == null)
                return;

            string uniqueLabel = GetUniqueLabel(m_behaviour.m_internalCutscene.name, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
