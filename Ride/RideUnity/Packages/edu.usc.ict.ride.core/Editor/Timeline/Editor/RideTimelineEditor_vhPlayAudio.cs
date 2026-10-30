using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using Ride.Timeline;
using UnityEngine.Timeline;


/// <summary>Customizes the duration and Timeline background of virtual-human audio clips.</summary>
/// <remarks>
/// The editor keeps a <see cref="Clip_vhPlayAudio"/> Timeline clip aligned with its assigned
/// <see cref="AudioClip"/> and draws a cyan waveform when the entire audio clip is visible. Cropped
/// waveform rendering remains intentionally unimplemented as recorded by the retained TODO.
/// See <a href="https://docs.unity3d.com/Packages/com.unity.timeline@1.8/api/UnityEditor.Timeline.ClipEditor.html">Unity Timeline ClipEditor documentation</a>.
/// </remarks>
[CustomTimelineEditor(typeof(Clip_vhPlayAudio))]
public class RideTimelineEditor_vhPlayAudio : ClipEditor
{
    /// <inheritdoc/>
    public override void OnClipChanged(TimelineClip clip)
    {
        base.OnClipChanged(clip);

        var script = clip.asset as Clip_vhPlayAudio;
        if (script == null) { return; }

        var audioClip = script.m_behaviour.m_audioClip;
        if (audioClip == null) { return; }

        clip.duration = audioClip.length;
    }

    /// <inheritdoc/>
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

    /// <summary>Creates a waveform texture for a full, uncropped audio clip region.</summary>
    /// <param name="audioClip">Audio clip whose samples are rendered.</param>
    /// <param name="region">Visible Timeline region and dimensions.</param>
    /// <returns>A generated waveform texture, or <see langword="null"/> when it cannot be rendered.</returns>
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

        // Fill the texture before drawing the mirrored waveform around its vertical center.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                waveTexture.SetPixel(x, y, Color.black);
            }
        }

        // Draw the waveform.
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


/// <summary>Provides audio assignment and preview controls for <see cref="Clip_vhPlayAudio"/> assets.</summary>
/// <remarks>
/// Preview uses Unity Editor's internal <c>AudioUtil.PlayPreviewClip</c> method through reflection
/// because that preview API is not publicly exposed. The reflected method can change between Unity
/// editor versions and is used only by this editor-only inspector.
/// </remarks>
[CustomEditor(typeof(Clip_vhPlayAudio))]
public class TimelineCustomInspectorEditor_AudioPlay : Editor
{
    /// <summary>Serialized virtual-human audio behavior displayed by the inspector.</summary>
    SerializedProperty m_behaviour;
    //SerializedProperty m_audioSource;

    /// <summary>Caches the serialized behavior property when the inspector is enabled.</summary>
    public void OnEnable()
    {
        //m_audioClip = serializedObject.FindProperty("m_audioClip");
        //m_audioSource = serializedObject.FindProperty("m_audioSource");
        m_behaviour = serializedObject.FindProperty("m_behaviour");
    }

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        ExposeParametersToInspector();
    }

    /// <summary>Draws the audio clip field and preview button.</summary>
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

    /// <summary>Invokes Unity Editor's internal preview method for an audio clip.</summary>
    /// <param name="clip">Audio clip to preview.</param>
    /// <param name="startSample">Sample index at which playback begins.</param>
    /// <param name="loop">Whether preview playback should loop.</param>
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
