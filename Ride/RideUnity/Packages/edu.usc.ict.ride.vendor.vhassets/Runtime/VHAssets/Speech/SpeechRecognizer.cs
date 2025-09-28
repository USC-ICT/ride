using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public abstract class SpeechRecognizer : MonoBehaviour
{
    #region Constants
    public delegate void OnSpeechRecognitionFinished(SpeechRecognizer recognizer, List<RecognizerResult> results);

    public class RecognizerResult
    {
        #region Variables
        public string m_Utterance = "";
        public float m_Confidence = 0;
        #endregion

        #region Functions
        public RecognizerResult(string utt)
        {
            m_Utterance = utt;
        }

        public RecognizerResult(string utt, float confidence)
        {
            m_Utterance = utt;
            m_Confidence = confidence;
        }

        public string[] GetWords()
        {
            return m_Utterance.Split(' ');
        }
        #endregion
    }
    #endregion

    #region Variables
    public AudioStreamer m_Streamer;
    OnSpeechRecognitionFinished m_SpeechRecognitionFinishedCBs;
    #endregion

    #region Functions
    void Start()
    {
    }

    /// <summary>
    /// Add a callback that is called when speech has been recognized
    /// </summary>
    /// <param name="cb"></param>
    public void AddSpeechRecognitionFinishedCallback(OnSpeechRecognitionFinished cb)
    {
        m_SpeechRecognitionFinishedCBs += new OnSpeechRecognitionFinished(cb);
    }

    /// <summary>
    /// Perform speech recognition on the specified audio clip
    /// </summary>
    /// <param name="clip"></param>
    public void Recognize(AudioClip clip)
    {
        PerformRecognition(clip);
    }

    /// <summary>
    /// Perform speech recognition on the specified audio stream
    /// </summary>
    /// <param name="stream"></param>
    //public void Recognize(AudioStream stream)
    //{
    //    PerformRecognitionStreamed(stream);
    //}

    public void Recognize(List<AudioClip> clips)
    {
        for (int i = 0; i < clips.Count; i++)
        {
            PerformRecognition(clips[i]);
        }
    }

    public void PlayAndRecognize(AudioClip clip, AudioSource clipSource)
    {
        StartCoroutine(PlayAndRecognizeCoroutine(clip, clipSource));
    }

    public void PlayAndRecognize(List<AudioClip> clips, AudioSource clipSource)
    {
        StartCoroutine(PlayAndRecognizeManyCoroutine(clips, clipSource, 1.0f));
    }

    IEnumerator PlayAndRecognizeManyCoroutine(List<AudioClip> clips, AudioSource clipSource, float paddingTimeBetweenClips)
    {
        for (int i = 0; i < clips.Count; i++)
        {
            yield return StartCoroutine(PlayAndRecognizeCoroutine(clips[i], clipSource));
            yield return new WaitForSeconds(paddingTimeBetweenClips);
        }
    }

    IEnumerator PlayAndRecognizeCoroutine(AudioClip clip, AudioSource clipSource)
    {
        clipSource.Stop();
        clipSource.clip = clip;
        clipSource.Play();
        PerformRecognition(clip);
        //yield return new WaitForSeconds(clip.length);
        yield break; // TODO: look at this
    }

    protected virtual void PerformRecognition(AudioClip clip)
    {
    }

    //protected void PerformRecognitionStreamed(AudioStream stream)
    //{
    //    if (m_IsStreaming)
    //    {
    //        Debug.LogWarning("You can only recognize one audio stream at a time");
    //    }
    //    else
    //    {
    //        if (m_Streamer != null)
    //        {
    //            m_IsStreaming = true;
    //            m_Streamer.Stream(stream, PerformRecognition, m_SoundDetectedCBs, m_SilenceDetectedCBs, OnStreamingFinished);
    //        }
    //        else
    //        {
    //            Debug.LogError("Streaming failed because m_Streamer is null on Speech Recognizer " + name);
    //        }
    //    }
    //}

    protected void DispatchResults(List<RecognizerResult> results)
    {
        if (results.Count == 0)
        {
            Debug.Log(string.Format("{0} didn't return any results", name));
        }

        // sort them by confidence first
        results.Sort(delegate(RecognizerResult r1, RecognizerResult r2)
        {
            return r1.m_Confidence > r2.m_Confidence ? 1 : -1;
        });

        m_SpeechRecognitionFinishedCBs?.Invoke(this, results);
    }
    #endregion
}
}
