using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.GazeInfo
    /// </remarks>
    public interface IGazeInfo
    {
        Task<string> GetGazeReasonAsync();

        Task SetGazeReasonAsync(string value);

        Task<string> GetPreviousTargetAsync();

        Task SetPreviousTargetAsync(string value);

        Task<string> GetGazeTargetAsync();

        Task SetGazeTargetAsync(string value);

        Task<string> GetGazeSpeedAsync();

        Task SetGazeSpeedAsync(string value);

        Task<string> GetGazeTypeAsync();

        Task SetGazeTypeAsync(string value);

        Task<string> GetGazeTrackAsync();

        Task SetGazeTrackAsync(string value);

        /// <remarks>Refactor of GazeInfo.SetGaze()</remarks>
        Task SetGazeAsync(string target, string type, string track);
    }
}
