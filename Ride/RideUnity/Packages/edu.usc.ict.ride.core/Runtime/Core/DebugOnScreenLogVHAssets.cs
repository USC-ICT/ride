using VHAssets;

namespace Ride
{
    /// <summary>Ride-system wrapper for the VHAssets on-screen debug log component.</summary>
    /// <remarks>
    /// This class intentionally contains very little logic of its own. Its main purpose is to let Ride
    /// discover, create, and manage a debug-on-screen-log system through the <see cref="IDebugOnScreenLog"/>
    /// abstraction while delegating the actual UI/log rendering behavior to <see cref="VHAssets.DebugOnScreenLog"/>.
    /// </remarks>
    public class DebugOnScreenLogVHAssets : RideSystemMonoBehaviour, IDebugOnScreenLog
    {
        /// <summary>Reference to the underlying VHAssets on-screen debug log component that performs the actual display work.</summary>
        public DebugOnScreenLog m_log;
    }
}
