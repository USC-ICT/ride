namespace Ride
{
    /// <summary>
    /// Interface to a system that records a session.
    /// </summary>
    public interface ISessionRecordingSystem : IRideSystem
    {
        int TimeUnitPrefix { get; }
        void SetTimeUnitPrefix(int prefix);
        uint Time { get; }
        bool Recording { get; }
        void RecordEvent(SessionEvent e);
        void StartRecording(string path, string name, string extension);
        void StopRecording();
    }
}
