namespace Ride.Audio
{
    /// <summary>
    /// Interface for controlling audio at runtime, including playing, stopping, pausing of sfx.
    /// </summary>
    public interface IAudioSystem : IRideSystem
    {
        /// <summary>
        /// Use an open audio source to play the clip
        /// </summary>
        /// <param name="clip">The name of the audio clip to play</param>
        /// <returns>The audio souce that is playing the clip</returns>
        RideID Play(string clip);

        /// <summary>
        /// Use the given source to play the clip
        /// </summary>
        /// <param name="source">The audio source that will play the clip</param>
        /// <param name="clip">The name of the audio clip to play</param>
        /// <returns>The audio souce that is playing the clip</returns>
        RideID Play(RideID source, string clip);

        /// <summary>
        /// Use an open audio source to play clip at the given position
        /// </summary>
        /// <param name="clip">The name of the audio clip to play</param>
        /// <param name="pos">The world position from where the sound will be played</param>
        void PlayAtPosition(string clip, RideVector3 pos);

        /// <summary>
        /// Stop playing the audio source
        /// </summary>
        /// <param name="source">The audio source</param>
        void Stop(RideID source);

        /// <summary>
        /// Locates all the current audio sources in the simulation
        /// </summary>
        void FindAllAudioSources();

        /// <summary>
        /// Determine whether the audio source is currently playing a clip
        /// <summary>
        bool IsPlaying(RideID source);
    }
}
