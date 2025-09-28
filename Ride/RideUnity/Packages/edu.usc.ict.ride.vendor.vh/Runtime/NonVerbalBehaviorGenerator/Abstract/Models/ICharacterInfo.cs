using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.CharacterInfo
    /// </remarks>
    public interface ICharacterInfo
    {
        Task<string> GetCharacterIdAsync();

        Task SetCharacterIdAsync(string value);

        Task<string> GetEmotionAsync();

        Task SetEmotionAsycn(string value);

        Task<string> GetPostureIdAsync();

        Task SetPostureIdAsync(string value);

        Task<string> GetPersonalityAsync();

        Task SetPersonalityAsync(string value);

        Task<string> GetNegotiationStanceAsync();

        Task SetNegotiationStanceAsync(string value);

        Task<string> GetConversationRoleAsync();

        Task SetConversationRoleAsync(string value);

        Task<string> GetParticipationGoalAsync();

        Task SetParticipationGoalAsync(string value);

        Task<string> GetComprehensionGoalAsync();

        Task SetComprehensionGoalAsync(string value);

        Task<string> GetParticipationStatusAsync();

        Task SetParticipationStatusAsync(string value);

        Task<string> GetComprehensionStatusAsync();

        Task SetComprehensionStatusAsync(string value);

        Task<string> GetCultureAsync();

        Task SetCultureAsync(string value);

        Task<CharacterStatus> GetStatusAsync();

        Task SetStatusAsync(CharacterStatus value);

        Task<string> GetRoleAsync();

        Task SetRoleAsync(string value);

        Task<bool> GetHasSpokenAsync();

        Task SetHasSpokenAsync(bool value);
    }
}
