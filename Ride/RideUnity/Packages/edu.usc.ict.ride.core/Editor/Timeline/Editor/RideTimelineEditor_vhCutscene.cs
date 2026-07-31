using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Ride;
using Ride.Timeline;
using VHAssets;

[CustomTimelineEditor(typeof(Clip_vhCutscene))]
public class RideTimelineEditor_vhCutscene : ClipEditor
{
    //private PlayableDirector m_ownerDirector;
    private Dictionary<string, float> m_timestampsDictionary = new Dictionary<string, float>();  //-Key: Timestamp id, Value: time

    private BMLReader m_BMLReader = new BMLReader();
    private BMLReader.UtteranceTiming m_UtteranceTiming = new BMLReader.UtteranceTiming();
    private List<string> m_createdBodyAnimationTimestamps = new List<string>(); //Used to keep track of created clips to avoid multiple clip in same timestamp.
    private List<string> m_createdHeadAnimationTimestamps = new List<string>();


    public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
    {
        if (clonedFrom != null)  //-There is no need for duplicate Cutscene. Delete cloned clip to avoid conflict.
        {
            var asset = TimelineEditor.inspectedAsset;
            asset.DeleteClip(clip);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }
    }

    public override void OnClipChanged(TimelineClip clip)
    {
        base.OnClipChanged(clip);

        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;

        if (clipScript.m_createLayer && IsEveryVariableSet(clip))
        {
            if (clip.GetParentTrack().GetChildTracks().Count() > 0) { return; } //Don't create new layers if it exist.

            clipScript.m_createLayer = false;
            CreateLayer(clip);
        }

        //Set length
        if (clipScript.m_behaviour.m_audio != null)
        {
            clip.duration = clipScript.m_behaviour.m_audio.length;
            clip.displayName = clipScript.m_behaviour.m_audio.name;
        }

        //AdjustChildClipPosition(clip);
    }

    private void AdjustChildClipPosition(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;

        if (m_timestampsDictionary.Count == 0)
            ReadBML(clipScript);


        foreach (var track in clip.GetParentTrack().GetChildTracks())
        {
            foreach (var childClip in track.GetClips())
            {
                var utteranceClip = childClip.asset as Clip_vhUtterance;    //TODO: create base class for virtual human associated clips to avoid this
                if (utteranceClip != null)
                {
                    childClip.start = clip.start + m_timestampsDictionary[utteranceClip.m_behaviour.m_timestampID];
                    continue;
                }

                var audioClip = childClip.asset as Clip_vhPlayAudio;
                if (audioClip != null)
                {
                    childClip.start = clip.start;// + m_timestampsDictionary[audioClip.m_behaviour.m_timestampID];
                    continue;
                }

                var headClip = childClip.asset as Clip_vhHeadMovement;
                if (headClip != null)
                {
                    childClip.start = clip.start + m_timestampsDictionary[headClip.m_behaviour.m_timestampID];
                    continue;
                }

                var bodyClip = childClip.asset as Clip_vhBodyMovement;
                if (bodyClip != null)
                {
                    childClip.start = clip.start + m_timestampsDictionary[bodyClip.m_behaviour.m_timestampID];
                    continue;
                }

            }
        }
    }

    private bool IsEveryVariableSet(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;

        if (clipScript.m_behaviour.m_utteranceText == null) { Debug.Log($"Clip_vhCutscene::<color=orange>Missing m_utteranceText</color>"); return false; }
        if (clipScript.m_behaviour.m_lipSyncInfo == null) { Debug.Log($"Clip_vhCutscene::<color=orange>Missing m_lipSyncInfo</color>"); return false; }
        if (clipScript.m_behaviour.m_audio == null) { Debug.Log($"Clip_vhCutscene::<color=orange>Missing audio file</color>"); return false; }
        if (clipScript.m_behaviour.m_xml == null) { Debug.Log($"Clip_vhCutscene::<color=orange>Missing m_xml</color>"); return false; }

        return true;
    }

    private void CreateLayer(TimelineClip clip)
    {
        ReadBML(clip.asset as Clip_vhCutscene);

        CreateAudioTrack(clip);
        CreateUtteranceTrack(clip);
        CreateVisemeTrack(clip);
        CreateBodyMovementTrack(clip);
        CreateHeadMovementTrack(clip);
        CreateFaceAnimationTrack(clip);

        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    private void CreateAudioTrack(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        clipScript.m_audioTrack = GetChildTrack(clip, "Audio Track", clip.GetParentTrack());

        var audioClip = clipScript.m_audioTrack.CreateClip<Clip_vhPlayAudio>();

        var audioClipAsset = audioClip.asset as Clip_vhPlayAudio;
        audioClipAsset.m_behaviour.m_audioClip = clipScript.m_behaviour.m_audio;
        //audioClipAsset.m_behaviour.m_audioSource = clipScript.m_behaviour.m_audioSource;
        //audioClipAsset.m_behaviour.m_avatar = clipScript.m_behaviour.m_avatar;

        //audioClipAsset.m_behaviour.m_audioSource_resolved = clipScript.m_behaviour.m_audioSource_resolved;
        //audioClipAsset.m_behaviour.m_avatar_resolved = clipScript.m_behaviour.m_avatar_resolved;
        audioClipAsset.m_behaviour.m_characterName = clipScript.m_behaviour.m_characterName;
        audioClipAsset.m_behaviour.m_lipSyncInfo = clipScript.m_behaviour.m_lipSyncInfo;
        //audioClip.displayName = audioClipAsset.m_behaviour.m_audioClip.name;
        //audioClipAsset.m_behaviour.m_timestampID = "T0";
    }


    private void CreateUtteranceTrack(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        clipScript.m_utteranceTrack = GetChildTrack(clip, "Utterance Track", clip.GetParentTrack());
        for (int i = 0; i < m_UtteranceTiming.m_Timings.Count; ++i) 
        {
            var t = m_UtteranceTiming.m_Timings[i];

            if (t.text == string.Empty) { continue; }

            TimelineClip utteranceClip = clipScript.m_utteranceTrack.CreateClip<Clip_vhUtterance>();
            utteranceClip.start = clip.start + t.time;
            utteranceClip.displayName = t.text;

            if (i < m_UtteranceTiming.m_Timings.Count - 1)
            {
                float duration = m_UtteranceTiming.m_Timings[i + 1].time - t.time;
                utteranceClip.duration = duration;
            }
            else
            {
                utteranceClip.duration = 0.1f;
            }

            Clip_vhUtterance utteranceScript = utteranceClip.asset as Clip_vhUtterance;
            utteranceScript.m_behaviour.m_timestampID = t.id;
            utteranceScript.m_behaviour.m_utterance = t.text;
            utteranceScript.m_behaviour.m_time = t.time;
            utteranceScript.m_behaviour.m_utteranceTextFile = clipScript.m_behaviour.m_lipSyncInfo;

        }
    }


    /*
             public string viseme = "";
        public float articulation = 1.0f;
        public float startTime;
        public float readyTime;
        public float relaxTime;
        public float endTime;
     */

    private void CreateVisemeTrack(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        clipScript.m_visemeTrack = GetChildTrack(clip, "Viseme Track", clip.GetParentTrack());
        for (int i = 0; i < m_UtteranceTiming.m_LipData.Count-1; ++i)  // Skipping last element because it creates unneccesary '-' character at the end
        {
            var t = m_UtteranceTiming.m_LipData[i];

            if (t.viseme == string.Empty) { continue; }

            TimelineClip visemeClip = clipScript.m_visemeTrack.CreateClip<Clip_vhViseme>();
            visemeClip.start = clip.start + t.startTime;
            visemeClip.displayName = t.viseme;

            if (i < m_UtteranceTiming.m_LipData.Count - 1)
            {
                float duration = t.endTime - t.startTime;
                duration = Mathf.Clamp(duration, 0.1f, float.MaxValue);
                visemeClip.duration = duration;
            }


            Clip_vhViseme utteranceScript = visemeClip.asset as Clip_vhViseme;
            utteranceScript.m_behaviour.m_viseme = t.viseme;
            utteranceScript.m_behaviour.m_startTime = t.startTime;
            utteranceScript.m_behaviour.m_readyTime = t.readyTime;
            utteranceScript.m_behaviour.m_relaxTime = t.relaxTime;
            utteranceScript.m_behaviour.m_endTime = t.endTime;
            utteranceScript.m_behaviour.m_visemeTextFile = clipScript.m_behaviour.m_lipSyncInfo;
        }
    }


    private void CreateBodyMovementTrack(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        if (clipScript.m_behaviour.m_xml == null) { return; }

        clipScript.m_animationTrack = GetChildTrack(clip, "Body Animation Track", clip.GetParentTrack());

        //todo set animation clip length + time offset

        string readableXML = AudioSpeechFile.ConvertXmlToSmartbodyReadable(clipScript.m_behaviour.m_xml.text);
        StringReader text = new StringReader(readableXML);
        XmlTextReader reader = new XmlTextReader(text);

        m_createdBodyAnimationTimestamps.Clear();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        if (reader.Name == "animation")
                            CreateAnimationClip(clip, reader, "animation", clipScript);
                    }
                    break;
            }
        }
    }


    private void CreateHeadMovementTrack(TimelineClip clip)
    {

        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        if (clipScript.m_behaviour.m_xml == null) { return; }

        clipScript.m_animationTrack = GetChildTrack(clip, "Head Animation Track", clip.GetParentTrack());

        //todo set animation clip length + time offset

        string readableXML = AudioSpeechFile.ConvertXmlToSmartbodyReadable(clipScript.m_behaviour.m_xml.text);
        StringReader text = new StringReader(readableXML);
        XmlTextReader reader = new XmlTextReader(text);

        m_createdHeadAnimationTimestamps.Clear();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        if (reader.Name == "head")
                            CreateAnimationClip(clip, reader, "head", clipScript);
                    }
                    break;
            }
        }
    }

    private void CreateFaceAnimationTrack(TimelineClip clip)
    {
        Clip_vhCutscene clipScript = clip.asset as Clip_vhCutscene;
        clipScript.m_faceAnimationTrack = GetChildTrack(clip, "Face Animation Track", clip.GetParentTrack());
    }


    private TrackAsset GetChildTrack(TimelineClip clip, string trackName, TrackAsset currTrack)
    {
        TrackAsset track = currTrack.GetChildTracks().FirstOrDefault(x => x.name == trackName);
        if (track == null)
        {
            TimelineAsset timelineAsset = currTrack.timelineAsset;/*m_ownerDirector.playableAsset as TimelineAsset;*/
            track = timelineAsset.CreateTrack<RideTimelineTrack>(currTrack as RideTimelineTrack, trackName);

            var clipScript = clip.asset as Clip_vhCutscene;
        }
        return track;
    }


    private void ReadBML(Clip_vhCutscene clip)
    {
        if (clip.m_behaviour.m_lipSyncInfo == null) { return; }

        m_UtteranceTiming = m_BMLReader.ReadBml(clip.m_behaviour.m_lipSyncInfo.text);

        foreach (var t in m_UtteranceTiming.m_Timings)
            m_timestampsDictionary.TryAdd(t.id, t.time);
    }

    private float GetAnimationLength(string animationName)
    {
        foreach (Animator animator in RideUtils.FindObjectsByType<Animator>())
        {
            UnityEditor.Animations.AnimatorController animController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

            var matchingAnim = animController.animationClips.FirstOrDefault(c => c.name == animationName);
            if (matchingAnim == null)
                continue;

            return Mathf.Max(1f, matchingAnim.length);
        }
        return 1f;
    }

    private void CreateAnimationClip(TimelineClip clip, XmlTextReader reader, string type, Clip_vhCutscene clipScript)
    {
        switch (type)
        {
            case "sbm:animation":
            case "animation":
                {
                    var timestamp = reader["stroke"];
                    timestamp = timestamp.Substring(timestamp.LastIndexOf(':') + 1);

                    if (m_createdBodyAnimationTimestamps.Contains(timestamp)) { return; }

                    string animationName = reader["name"];

                    var animClip = clipScript.m_animationTrack.CreateClip<Clip_vhBodyMovement>();
                    var animClipScript = animClip.asset as Clip_vhBodyMovement;
                    animClipScript.m_behaviour.m_characterName = clipScript.m_behaviour.m_characterName;
                    //animClipScript.m_behaviour.m_animator_internal = clipScript.m_behaviour.m_avatar_resolved.gameObject;
                    animClipScript.m_behaviour.m_animationName = animationName;
                    animClipScript.m_behaviour.m_timestampID = timestamp;

                    animClip.start = clip.start + GetTimeOffset(timestamp);
                    animClip.duration = GetAnimationLength(animationName);
                    animClip.displayName = animationName;

                    m_createdBodyAnimationTimestamps.Add(timestamp);
                }
                break;

            case "gaze":
                break;

            case "head":
                if (string.Compare(reader["type"], "NOD", true) == 0)
                {
                    var amount = reader["amount"];
                    var repeat = reader["repeat"];
                    var timestamp = reader["relax"];
                    timestamp = timestamp.Substring(timestamp.LastIndexOf(':') + 1);

                    if (m_createdHeadAnimationTimestamps.Contains(timestamp)) { return; }

                    var animClip = clipScript.m_animationTrack.CreateClip<Clip_vhHeadMovement>();
                    var animClipScript = animClip.asset as Clip_vhHeadMovement;

                    animClipScript.m_behaviour.m_movementType = HeadController.MovementType.Nod;
                    animClipScript.m_behaviour.m_characterName = clipScript.m_behaviour.m_characterName;
                    animClipScript.m_behaviour.m_amount = float.Parse(amount);
                    animClipScript.m_behaviour.m_timestampID = timestamp;
                    //animClipScript.m_behaviour.m_avatar_internal = clipScript.m_behaviour.m_avatar_resolved.gameObject;
                    animClipScript.m_behaviour.m_numberOfTimes = 1f;
                    animClipScript.m_behaviour.m_nodDuration = 1f;

                    animClip.duration = 0.2f;
                    animClip.start = clip.start + GetTimeOffset(timestamp);
                    animClip.displayName = "Nod";

                    m_createdHeadAnimationTimestamps.Add(timestamp);
                }
                else if (string.Compare(reader["type"], "SHAKE", true) == 0)
                {
                }
                else
                {
                }
                break;

            case "saccade":
                break;

            case "face":
                break;

            case "sbm:event":
            case "event":
                break;

            case "gesture":
                break;

            case "body":
                break;

            case "speech":

                break;
        }

    }

    public float GetTimeOffset(string timestampID)
    {
        if (m_timestampsDictionary.ContainsKey(timestampID))
            return m_timestampsDictionary[timestampID];
        return -1f;
    }
}


[CustomEditor(typeof(Clip_vhCutscene))]
public class ClipInspectorEditor_vhCutscene : Editor
{
    SerializedProperty m_behaviour;
    //SerializedProperty m_avatar;
    //SerializedProperty m_audioSource;


    public void OnEnable()
    {
        m_behaviour = serializedObject.FindProperty("m_behaviour");
    }

    public override void OnInspectorGUI()
    {
        Clip_vhCutscene targetScript = (Clip_vhCutscene)target;

        EditorGUILayout.PropertyField(m_behaviour, new GUIContent("m_behaviour"));

        if (GUILayout.Button("Create Cutscene Objects", EditorStyles.miniButtonLeft, GUILayout.Width(500)))
        {
            targetScript.CreateLayer();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
