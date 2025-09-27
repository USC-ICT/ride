namespace Ride
{
    /// <summary>
    /// Interface to a system that plays back a recorded session.
    /// </summary>
    public interface ISessionPlaybackSystem : IRideSystem
    {
        int TimeUnitPrefix { get; }
        uint Time { get; }
        uint Duration { get; }
        bool Paused { get; }
        bool Playing { get; }
        void LoadSession(string path);
        void Play();
        void Pause();
        void SeekTo(float time);
    }
}
