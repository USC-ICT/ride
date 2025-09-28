using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using Ride.Timeline;
using UnityEngine.Timeline;


[CustomTimelineEditor(typeof(Clip_vhPlayAudio))]
public class RideTimelineEditor_vhPlayAudio : ClipEditor
{
    public override void OnClipChanged(TimelineClip clip)
    {
        base.OnClipChanged(clip);

        var script = clip.asset as Clip_vhPlayAudio;
        if (script == null) { return; }

        var audioClip = script.m_behaviour.m_audioClip;
        if (audioClip == null) { return; }

        clip.duration = audioClip.length;
    }

    public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
    {
        base.DrawBackground(clip, region);

        var clipScript = clip.asset as Clip_vhPlayAudio;
        var audioClip = clipScript.m_behaviour.m_audioClip;

        if (audioClip == null) { return; }

        Texture waveformTexture = GetWaveForm(audioClip, region);
        if (waveformTexture == null) { return; }

        Rect backgroundRegion = new Rect(region.position.position.x, region.position.position.y, region.position.width, region.position.height);
        EditorGUI.DrawTextureTransparent(backgroundRegion, waveformTexture);
    }

    private Texture GetWaveForm(AudioClip audioClip, ClipBackgroundRegion region)
    {
        int width = (int)region.position.width;
        int height = (int)region.position.height;
        if (width <= 0 || height <= 0) { return null; }

        double regionLength = region.endTime - region.startTime;
        double audioLength = audioClip.length;
        float regionRatio = (float)(regionLength / audioLength);

        if (regionRatio != 1) { return null; }   //ToDo: Need to implement a way to crop waveform when the audio clip is clipped.

        Texture2D waveTexture = new Texture2D(width, height);

        float[] samples = new float[audioClip.samples * audioClip.channels];
        float[] waveform = new float[width];

        bool retreiveData = audioClip.GetData(samples, 0);
        if (retreiveData == false) { Debug.LogWarning("RideTimelineEditor_vhPlayAudio.cs::Failed to retrieve data from audio clip."); return null; }

        int packSize = (audioClip.samples / width) + 1;
        int s = 0;
        for (int i = 0; i < audioClip.samples; i += packSize)
        {
            waveform[s] = Mathf.Abs(samples[i]);
            s++;
        }

        //Draw background
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                waveTexture.SetPixel(x, y, Color.black);
            }
        }

        //Draw waveform
        for (int x = 0; x < waveform.Length; x++)
        {
            for (int y = 0; y <= waveform[x] * ((float)height * 1.5f/*.75f*/); y++)
            {
                waveTexture.SetPixel(x, (height / 2) + y, Color.cyan);
                waveTexture.SetPixel(x, (height / 2) - y, Color.cyan);
            }
        }

        waveTexture.Apply();
        return waveTexture;
    }
}


[CustomEditor(typeof(Clip_vhPlayAudio))]
public class TimelineCustomInspectorEditor_AudioPlay : Editor
{
    SerializedProperty m_behaviour;
    //SerializedProperty m_audioSource;

    public void OnEnable()
    {
        //m_audioClip = serializedObject.FindProperty("m_audioClip");
        //m_audioSource = serializedObject.FindProperty("m_audioSource");
        m_behaviour = serializedObject.FindProperty("m_behaviour");
    }

    public override void OnInspectorGUI()
    {
        ExposeParametersToInspector();
    }

    private void ExposeParametersToInspector()
    {
        Clip_vhPlayAudio targetScript = (Clip_vhPlayAudio)target;

        EditorGUILayout.PropertyField(m_behaviour.FindPropertyRelative("m_audioClip"), new GUIContent("Audio Clip"));
        //EditorGUILayout.PropertyField(m_behaviour.FindPropertyRelative("m_audioClip"), new GUIContent("Audio Source"));
        if (GUILayout.Button("Preview Audio", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
        {
            if (targetScript.m_behaviour.m_audioClip == null) { return; }
            PlayAudioClip(targetScript.m_behaviour.m_audioClip);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void PlayAudioClip(AudioClip clip, int startSample = 0, bool loop = false)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );

        method.Invoke(
            null,
            new object[] { clip, startSample, loop }
        );
    }
}
