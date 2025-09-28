using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.ConversationInfo
    /// </remarks>
    public interface IConversationInfo
    {
        Task<string> GetSpeakerAsync();

        Task SetSpeakerAsync(string value);

        Task<string> GetAddresseeAsync();

        Task SetAddresseeAsync(string value);

        Task<string> GetLastMyExpressIdAsync();

        Task SetLastMyExpressIdAsync(string value);
    }
}
