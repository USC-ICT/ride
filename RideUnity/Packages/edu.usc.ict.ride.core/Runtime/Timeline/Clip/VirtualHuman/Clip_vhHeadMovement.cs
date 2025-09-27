using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VHAssets;

namespace Ride.Timeline
{
    [DisplayName("Virtual Human/Head Movement")]
    public class Clip_vhHeadMovement : RideTimelineClip
    {
        [Serializable]
        public class Behaviour_vhHeadMovement : RideTimelineBehaviour
        {
            public HeadController.MovementType m_movementType;
            
            public string m_characterName;

            [Header("Nod Parameters")]
            public float m_amount;
            public float m_numberOfTimes;
            public float m_nodDuration;
            public string m_timestampID;

            public bool m_waitUntilFinished;
            public override bool waitUntilFinished { get => m_waitUntilFinished; }

            public override void ProcessBehaviour()
            {
                RideTimelineManager manager = FindFirstObjectByType<RideTimelineManager>();
                var character = manager.GetCharacter(m_characterName);

                if (m_movementType == HeadController.MovementType.Nod)
                {
                    character.GetComponent<HeadController>().NodHead(m_amount, m_numberOfTimes, m_nodDuration);            
                }
                else if (m_movementType == HeadController.MovementType.Shake)
                {

                }
                else if (m_movementType != HeadController.MovementType.Tilt)
                {

                }
                m_isFinished = true;
            }
        }


        public Behaviour_vhHeadMovement m_behaviour = new Behaviour_vhHeadMovement();
        [HideInInspector] public override string m_commandType { get => "VhHeadMovement"; }
        [HideInInspector] public Clip_vhCutscene m_parentClip;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            m_behaviour.m_label = m_label;            
            return ScriptPlayable<Behaviour_vhHeadMovement>.Create(graph, m_behaviour);
        }

        public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
        {            
            string uniqueLabel = GetUniqueLabel(m_behaviour.m_movementType.ToString(), ref clipTimeData);
            m_label = uniqueLabel;
            clip.displayName = uniqueLabel;
            clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
        }
    }
}
