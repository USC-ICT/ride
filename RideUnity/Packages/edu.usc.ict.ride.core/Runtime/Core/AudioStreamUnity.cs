using UnityEngine;

namespace Ride.Audio
{
    /// <summary>
    /// Container for streaming audio from a device, used by Unity implementations of IDeviceAudioSystem
    /// </summary>
    public class AudioStreamUnity : IAudioStream
    {
        #region Properties
        public AudioClip Clip { get; set; }
        public bool StreamComplete { get; set; }

        public int Frequency => Clip.frequency;
        public int Channels => Clip.channels;
        public int Samples => Clip.samples;
        #endregion

        #region Functions
        public AudioStreamUnity() { }

        public AudioStreamUnity(AudioClip clip)
        {
            Clip = clip;
            StreamComplete = false;
        }
        #endregion
    }
}
