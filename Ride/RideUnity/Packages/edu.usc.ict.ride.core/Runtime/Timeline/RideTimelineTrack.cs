using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Ride.Timeline;
using System.Collections.Generic;

[Serializable]
[TrackClipType(typeof(RideTimelineClip))]
[TrackBindingType(typeof(RideTimelineManager))]
public class RideTimelineTrack : TrackAsset, ILayerable
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        PlayableDirector director = go.GetComponent<PlayableDirector>();
        if (go != null)
        {
            if (director != null)
            {
                director.SetGenericBinding(this, FindFirstObjectByType<RideTimelineManager>());
            }
        }
        var scriptPlayable = ScriptPlayable<RideTimelineTrackMixer>.Create(graph, inputCount);

        RideTimelineTrackMixer mixer = scriptPlayable.GetBehaviour();
        mixer.ClipTimeData = new Dictionary<string, double>();


        foreach (TimelineClip c in GetClips())
        {
            RideTimelineClip rideClip = (RideTimelineClip)c.asset;
            rideClip.ConfigureClip(c, ref mixer.ClipTimeData);
        }

        return scriptPlayable;
    }


    Playable ILayerable.CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        if (go != null)
        {
            var director = go.GetComponent<PlayableDirector>();
            if (director != null)
            {
                director.SetGenericBinding(this, FindFirstObjectByType<RideTimelineManager>());
            }
        }

        var playable = ScriptPlayable<TimelineCustomLayerMixer>.Create(graph, inputCount);
        return playable;
    }

    protected override void OnCreateClip(TimelineClip clip)
    {

    }

    /// <summary>
    /// Adding '_#' suffix to the clip label to prevent any duplicate label.
    /// </summary>
    private string AddSuffix(Dictionary<string, double> clipTimeData, string label)
    {
        int counter = 0;
        string newLabel = label + "_" + counter;

        while (true)
        {
            if (clipTimeData.ContainsKey(newLabel) == false)
            {
                break;
            }
            counter++;
            newLabel = label + "_" + counter;
        }
        return newLabel;
    }
}
