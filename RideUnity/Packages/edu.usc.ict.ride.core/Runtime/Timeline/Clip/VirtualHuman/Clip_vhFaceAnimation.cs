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
    [DisplayName("Virtual Human/Face Animation")]
    public class Clip_vhFaceAnimation : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhFaceAnimation : RideTimelineBehaviour
        {
            public string m_characterName;
            public vhFaceAnimationType m_type;
            public CharacterDefines.FaceSide m_faceSide;
            public float m_duration;
            [Range(0.0f, 1.0f)]
            public float m_weight = 1.0f;

            public override void ProcessBehaviour()
            {
                MecanimManager mecanimManager = null;

                var managers = FindObjectsByType<MecanimManager>(FindObjectsSortMode.None);
                foreach (var m in managers)
                {
                    if (m.gameObject.activeSelf == true)
                        mecanimManager = m;
                }
                
                if (mecanimManager == null) { Debug.LogWarning("Clip_vhFaceAnimation.cs::Missing MecanimManager in the scene."); return; }

                mecanimManager.SBPlayFAC(m_characterName, (int)m_type, m_faceSide, m_weight, m_duration);
            }
        }

        public Behaviour_vhFaceAnimation m_behaviour = new Behaviour_vhFaceAnimation();
        [HideInInspector] public override string m_commandType { get => "vhFaceAnimation"; }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;
            return ScriptPlayable<Behaviour_vhFaceAnimation>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {
            string uniqueLabel = GetUniqueLabel(m_behaviour.m_type.ToString(), ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }

        public enum vhFaceAnimationType
        {
            InnerBrowRaiser     = 1,
            OuterBrowRaiser     = 2,
            BrowLowerer         = 4,
            UpperLidRaiser      = 5,
            CheekRaiser         = 6,
            LidTightener        = 7,
            UpperLipRaiser      = 10,
            LipCornerPuller     = 12,
            Smile               = 14,
            lips_part           = 25,
            JawDrop             = 26,
            Blink               = 45,
            SmallSmile          = 100,
            Happy               = 112,
            Disgust             = 124,
            Fear                = 126,
            Surprise            = 127,
            Angry               = 129,
            Sad                 = 130,
            Contempt            = 131,
            BrowRaise_1         = 132,
            BrowRaise_2         = 133,
            HurtBrows           = 134,
            Furrow              = 136,                
        }

        public enum vhFaceAnimationSide
        {
            Left,
            Right,
            Both,
        }
    }
}
