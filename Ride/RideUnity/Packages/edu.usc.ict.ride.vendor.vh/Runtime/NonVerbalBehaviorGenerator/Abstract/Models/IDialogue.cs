using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.Dialogue
    /// </remarks>
    public interface IDialogue
    {
        Task<string> GetSpeakerAsync();

        Task SetSpeakerIdAsync(string value);

        Task<string> GetListenerAsync();

        Task SetListenerIdAsync(string value);
    }
}
