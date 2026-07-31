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
    [DisplayName("Virtual Human/Body Movement")]
    public class Clip_vhBodyMovement : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhBodyMovement : RideTimelineBehaviour
        {            
            public string m_characterName;
            public string m_animationName;
            public string m_timestampID;
            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override IEnumerator ProcessContinuousBehaviour()
            {
                RideTimelineManager manager = FindAnyObjectByType<RideTimelineManager>();
                var character = manager.GetCharacter(m_characterName);
                
                var mecanimChar = character.GetComponent<MecanimCharacter>();
                mecanimChar.PlayAnim(m_animationName);                

                yield return new WaitForSeconds(1f);
                
                var animator = character.GetComponent<Animator>();
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    string clipName = clipInfo[0].clip.name;
                    float duration = clipInfo[0].clip.length;

                    yield return new WaitForSeconds(duration - 1f);
                }
                
                m_isFinished = true;
            }
        }

        public Behaviour_vhBodyMovement m_behaviour = new Behaviour_vhBodyMovement();
        [HideInInspector] public override string m_commandType { get => "VhBodyMovement"; }
        [HideInInspector] public Clip_vhCutscene m_parentClip;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;            
            return ScriptPlayable<Behaviour_vhBodyMovement>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            if (m_behaviour.m_animationName == string.Empty)
                return;

            string uniqueLabel = GetUniqueLabel(m_behaviour.m_animationName, ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
