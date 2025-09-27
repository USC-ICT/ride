using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class AudioStreamer : MonoBehaviour
{
    #region Constants
    public delegate void OnAudioChunkStreamed(AudioClip clip);
    public delegate void OnSoundDetected();
    public delegate void OnSilenceDetected();
    public delegate void OnStreamingFinished();
    #endregion

    #region Variables
    public RecordingDevice m_RecordingDevice;
    public float m_SilenceThreshhold = 0.01f;
    public float m_SilenceLength = 1.0f;
    public float m_MinimumAudioLength = 0.5f;
    float[] m_TotalStreamBuffer;

    OnSoundDetected m_SoundDetectedCBs;
    OnSilenceDetected m_SilenceDetectedCBs;
    OnStreamingFinished m_OnStreamingFinishedCBs;
    bool m_IsStreaming = false;
    bool m_IsStreamSilent = true;
    #endregion

    #region Properties
    public bool IsStreaming
    {
        get { return m_IsStreaming; }
    }

    public bool IsStreamAudioDetected
    {
        get { return !IsStreamSilent; }
    }

    public bool IsStreamSilent
    {
        get { return m_IsStreamSilent; }
    }
    #endregion

    #region Functions
    void Start()
    {
    }

    public void Stream(AudioStream stream, OnAudioChunkStreamed cb)
    {
        if (!m_IsStreaming)
        {
            m_TotalStreamBuffer = new float[stream.Clip.samples];
            StartCoroutine(StreamCoroutine(stream, cb));
        }
        else
        {
            Debug.LogWarning(string.Format("AudioStreamer {0} is already streaming. Another stream cannot be opened", name));
        }
    }

    protected IEnumerator StreamCoroutine(AudioStream stream, OnAudioChunkStreamed cb)
    {
        m_IsStreaming = true;
        while (!stream.StreamComplete)
        {
            if (!IsMicSilent(m_RecordingDevice.GetRecordingVolumeLevel()))
            {
                // audio data heard by mic
                m_IsStreamSilent = false;
                m_SoundDetectedCBs?.Invoke();

                // wait here until enough silent time has passed
                yield return StartCoroutine(ReadUntilSilence(stream, cb));

                // enough time spent in silence has passed
                m_IsStreamSilent = true;
                m_SilenceDetectedCBs?.Invoke();
            }

            yield return new WaitForEndOfFrame();
        }

        m_OnStreamingFinishedCBs?.Invoke();

        m_IsStreaming = false;
    }

    IEnumerator ReadUntilSilence(AudioStream stream, OnAudioChunkStreamed cb)
    {
        float freqAsfloat = stream.Clip.frequency;
        float contiguousSilenceInSeconds = 0;
        int currentReadIndex = 0;
        List<float> streamedAudioBuffer = new List<float>();

        // adds the number of indices that should be moved forward each time step based on the frequency of the wave
        int indexIncrement = (int)(1.0f / freqAsfloat * 1000);

        while (!stream.StreamComplete)
        {
            float vol = m_RecordingDevice.GetRecordingVolumeLevel();
            indexIncrement = (int)(Time.deltaTime * freqAsfloat);
            if (IsMicSilent(vol))
            {
                contiguousSilenceInSeconds += Time.deltaTime;
            }
            else
            {
                // reset, audio is still being heard
                contiguousSilenceInSeconds = 0;
            }

            // check is enough time has passed where there has been no mic input
            if (contiguousSilenceInSeconds >= m_SilenceLength)
            {
                if (cb != null)
                {
                    if ((float)streamedAudioBuffer.Count / freqAsfloat >= m_MinimumAudioLength)
                    {
                        // chop off the last second or less
                        int numSilentIndicies = Mathf.Min(stream.Clip.frequency, (int)(freqAsfloat * (m_SilenceLength)));
                        streamedAudioBuffer.RemoveRange(Mathf.Max(streamedAudioBuffer.Count - numSilentIndicies - 1, 0), Mathf.Min(numSilentIndicies, streamedAudioBuffer.Count));

                        // create the clip
                        cb(CreateClip(stream.Clip.channels, stream.Clip.frequency, streamedAudioBuffer.ToArray()));
                    }
                }
                yield break;
            }

            // write the mic stream to our buffer
            stream.Clip.GetData(m_TotalStreamBuffer, 0);
            CopyStreamToBuffer(m_TotalStreamBuffer, currentReadIndex, indexIncrement, streamedAudioBuffer);

            currentReadIndex += indexIncrement;

            int carryOver = currentReadIndex - m_TotalStreamBuffer.Length;
            carryOver = Mathf.Max(0, carryOver);
            if (carryOver > 0)
            {
                // the current mic read index has looped back around so we need to adjust our indices and where we are reading from
                // in order to account for this
                currentReadIndex = carryOver;
                CopyStreamToBuffer(m_TotalStreamBuffer, 0, carryOver, streamedAudioBuffer);
            }

            yield return new WaitForEndOfFrame();
        }
    }

    bool IsMicSilent(float micVolume)
    {
        return micVolume >= -m_SilenceThreshhold && micVolume <= m_SilenceThreshhold;
    }

    void CopyStreamToBuffer(float[] sourceBuffer, int sourceBufferIndex, int length, List<float> destBuffer)
    {
        int sourceStopIndex = Mathf.Min(sourceBufferIndex + length, sourceBuffer.Length);
        for (int i = sourceBufferIndex; i < sourceStopIndex; i++)
        {
            destBuffer.Add(sourceBuffer[i]);
        }
    }

    AudioClip CreateClip(int channels, int frequency, float[] data)
    {
        AudioClip clip = AudioClip.Create("", data.Length, channels, frequency, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Add a callback that is called when sound is detected in the audio stream
    /// </summary>
    /// <param name="cb"></param>
    public void AddOnStreamingSoundDetected(OnSoundDetected cb)
    {
        m_SoundDetectedCBs += new OnSoundDetected(cb);
    }

    /// <summary>
    /// Add a callback that is called when silence is detected in the audio stream
    /// </summary>
    /// <param name="cb"></param>
    public void AddOnStreamingSilenceDetected(OnSilenceDetected cb)
    {
        m_SilenceDetectedCBs += new OnSilenceDetected(cb);
    }

    /// <summary>
    /// Add a callback that is called when streaming is turned off
    /// </summary>
    /// <param name="cb"></param>
    public void AddOnStreamingFinished(OnStreamingFinished cb)
    {
        m_OnStreamingFinishedCBs += new OnStreamingFinished(cb);
    }
    #endregion
}
}
