
namespace Ride
{
    /// <summary>
    /// Provides access to current, smoothed, average, minimum, and maximum frame rate metrics.
    /// Implementations track timing using RideUtils and internally maintain a moving window of frame times.
    /// </summary>
    public interface IFramePerSecondCounter : IRideSystem
    {
        /// <summary>
        /// The most recent frame rate in frames per second (FPS), calculated using RideUtils.GetDeltaTime().
        /// This reflects the raw unscaled framerate from the current frame.
        /// </summary>
        float Fps { get; }

        /// <summary>
        /// A smoothed FPS value based on Unity’s smoothDeltaTime.
        /// May fluctuate more than AverageFps but typically responds faster to spikes.
        /// </summary>
        float SmoothFps { get; }

        /// <summary>
        /// A moving average FPS computed from the last N frames.
        /// This value is generally smoother and less volatile than raw or smoothed FPS.
        /// </summary>
        float AverageFps { get; }

        /// <summary>
        /// The lowest FPS observed within the current moving window.
        /// This value is updated only if min/max tracking is enabled.
        /// </summary>
        float MinFps { get; }

        /// <summary>
        /// The highest FPS observed within the current moving window.
        /// This value is updated only if min/max tracking is enabled.
        /// </summary>
        float MaxFps { get; }
    }
}
