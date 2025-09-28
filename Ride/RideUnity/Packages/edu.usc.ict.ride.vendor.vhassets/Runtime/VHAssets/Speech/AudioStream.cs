using UnityEngine;

namespace VHAssets
{
public class AudioStream
{
    #region Variables
    AudioClip m_Clip;
    bool m_StreamComplete;
    #endregion

    #region Properties
    public AudioClip Clip
    {
        get { return m_Clip; }
        set { m_Clip = value; }
    }

    public bool StreamComplete
    {
        get { return m_StreamComplete; }
        set { m_StreamComplete = value; }
    }

    public int Frequency { get { return m_Clip.frequency; } }
    public int Channels { get { return m_Clip.channels; } }
    public int Samples { get { return m_Clip.samples; } }
    #endregion

    #region Functions
    public AudioStream()
    {

    }

    public AudioStream(AudioClip clip)
    {
        Clip = clip;
        m_StreamComplete = false;
    }
    #endregion
}
}
