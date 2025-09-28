using Ride.Timeline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


[DisplayName("Utility/Notification")]
public class DaybreakTimeline_Notification : RideTimelineClip
{
    [Serializable]
    public class Behaviour_Notification : RideTimelineBehaviour
    {
        public string m_message;

        public override void ProcessBehaviour()
        {
            m_manager.SendNotification(m_message);
            m_isFinished = true;
        }
    }

    [HideInInspector] public override string m_commandType { get => "Notification"; }
    public Behaviour_Notification m_behaviour = new Behaviour_Notification();

    public override void ConfigureClip(TimelineClip clip, ref Dictionary<string, double> clipTimeData)
    {
        string uniqueLabel = GetUniqueLabel(m_commandType, ref clipTimeData);
        m_label = uniqueLabel;
        clip.displayName = uniqueLabel;
        clipTimeData.Add(uniqueLabel, (double)clip.start + 0.1);
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        m_behaviour.m_label = m_label;
        return ScriptPlayable<Behaviour_Notification>.Create(graph, m_behaviour);
    }
}
