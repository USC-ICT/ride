using System;

namespace Ride
{
    public interface IApplicationLogMessageSystem : IRideSystem
    {
        public enum LogType
        {
            Error,
            Assert,
            Warning,
            Log,
            Exception
        }

        public delegate void LogCallback(string condition, string stackTrace, LogType type);

        void AddCallback(LogCallback callback);
        void RemoveCallback(LogCallback callback);
    }
}
