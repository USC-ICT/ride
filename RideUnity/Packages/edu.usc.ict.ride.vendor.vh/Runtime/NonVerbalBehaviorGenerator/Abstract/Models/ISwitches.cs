using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.NVBGSwitch
    /// </remarks>
    public interface ISwitches
    {
        Task<bool> GetAllBehaviorAsync();

        Task SetAllBehaviorAsync(bool value);

        Task<bool> GetSaliencyGlanceAsync();

        Task SetSaliencyGlanceAsync(bool value);

        Task<bool> GetSaliencyIdleGazeAsync();

        Task SetSaliencyIdleGazeAsync(bool value);

        Task<bool> GetSpeakerGazeAsync();

        Task SetSpeakerGazeAsync(bool value);

        Task<bool> GetSpeakerGesturesAsync();

        Task SetSpeakerGesturesAsync(bool value);

        Task<bool> GetListenerGazeAsync();

        Task SetListenerGazeAsync(bool value);

        Task<bool> GetPoseRulesAsync();

        Task SetPoseRulesAsync(bool value);
    }
}
