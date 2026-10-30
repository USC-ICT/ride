using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Ride.Timeline;
using System.Collections.Generic;

/// <summary>Hosts RIDE Timeline clips and binds their mixer to the scene's <see cref="RideTimelineManager"/>.</summary>
/// <remarks>
/// The utterance-import and virtual-human cutscene editors create this track when generating Timeline assets.
/// It also implements <see cref="ILayerable"/> so child tracks can participate in Timeline layer graphs.
/// See <see href="https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/trk_custom.html"/>.
/// </remarks>
[Serializable]
[TrackClipType(typeof(RideTimelineClip))]
[TrackBindingType(typeof(RideTimelineManager))]
public class RideTimelineTrack : TrackAsset, ILayerable
{
    /// <inheritdoc/>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        PlayableDirector director = go.GetComponent<PlayableDirector>();
        if (go != null)
        {
            if (director != null)
            {
                director.SetGenericBinding(this, FindAnyObjectByType<RideTimelineManager>());
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


    /// <inheritdoc/>
    Playable ILayerable.CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        if (go != null)
        {
            var director = go.GetComponent<PlayableDirector>();
            if (director != null)
            {
                director.SetGenericBinding(this, FindAnyObjectByType<RideTimelineManager>());
            }
        }

        var playable = ScriptPlayable<TimelineCustomLayerMixer>.Create(graph, inputCount);
        return playable;
    }

    /// <inheritdoc/>
    protected override void OnCreateClip(TimelineClip clip)
    {

    }

    /// <summary>Appends a numeric suffix that makes a clip label unique within the supplied timing map.</summary>
    /// <param name="clipTimeData">The existing clip labels and end times.</param>
    /// <param name="label">The base label to disambiguate.</param>
    /// <returns>A label in the form <c>label_#</c> that does not exist in <paramref name="clipTimeData"/>.</returns>
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
