namespace Ride
{
    /// <summary>
    /// Provides an interface to systems handling external processes. 
    /// </summary>
    public interface IExternalProcess : IRideSystem
    {
        bool ProcessLoaded { get; }

        void StartProcess();
        void StopProcess();
    }
}
