namespace Ride
{
    public delegate void ConsoleCommandCallback(string command);

    /// <summary>
    /// Provides an interface to a debug console system.
    /// </summary>
    public interface IDebugConsoleSystem : IRideSystem
    {
        void AddCommand(string command, ConsoleCommandCallback cb);
    }
}
