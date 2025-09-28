using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemoryCharacterInfo : ICharacterInfo
    {
        public InMemoryCharacterInfo(string characterId, string idlePostureId)
        {
            this.characterId = characterId;
            postureId = idlePostureId;
        }

        private string characterId;

        public Task<string> GetCharacterIdAsync() => Task.FromResult(characterId);

        public Task SetCharacterIdAsync(string value)
        {
            characterId = value;
            return Task.CompletedTask;
        }

        private string emotion = "neutral";

        public Task<string> GetEmotionAsync() => Task.FromResult(emotion);

        public Task SetEmotionAsycn(string value)
        {
            emotion = value;
            return Task.CompletedTask;
        }

        private string postureId = "HandsAtSide";

        public Task<string> GetPostureIdAsync() => Task.FromResult(postureId);

        public Task SetPostureIdAsync(string value)
        {
            postureId = value;
            return Task.CompletedTask;
        }

        private string personality = "";

        public Task<string> GetPersonalityAsync() => Task.FromResult(personality);

        public Task SetPersonalityAsync(string value)
        {
            personality = value;
            return Task.CompletedTask;
        }

        private string negotiationStance = "none";

        public Task<string> GetNegotiationStanceAsync() => Task.FromResult(negotiationStance);

        public Task SetNegotiationStanceAsync(string value)
        {
            negotiationStance = value;
            return Task.CompletedTask;
        }

        private string conversationRole = "";

        public Task<string> GetConversationRoleAsync() => Task.FromResult(conversationRole);

        public Task SetConversationRoleAsync(string value)
        {
            conversationRole = value;
            return Task.CompletedTask;
        }

        private string participationGoal = "0";

        public Task<string> GetParticipationGoalAsync() => Task.FromResult(participationGoal);

        public Task SetParticipationGoalAsync(string value)
        {
            participationGoal = value;
            return Task.CompletedTask;
        }

        private string comprehensionGoal = "0";

        public Task<string> GetComprehensionGoalAsync() => Task.FromResult(comprehensionGoal);

        public Task SetComprehensionGoalAsync(string value)
        {
            comprehensionGoal = value;
            return Task.CompletedTask;
        }

        private string participationStatus = "0";

        public Task<string> GetParticipationStatusAsync() => Task.FromResult(participationStatus);

        public Task SetParticipationStatusAsync(string value)
        {
            participationStatus = value;
            return Task.CompletedTask;
        }

        private string comprehensionStatus = "0";

        public Task<string> GetComprehensionStatusAsync() => Task.FromResult(comprehensionStatus);

        public Task SetComprehensionStatusAsync(string value)
        {
            comprehensionStatus = value;
            return Task.CompletedTask;
        }

        private string culture = "general";

        public Task<string> GetCultureAsync() => Task.FromResult(culture);

        public Task SetCultureAsync(string value)
        {
            culture = value;
            return Task.CompletedTask;
        }

        private CharacterStatus status = CharacterStatus.Present;

        public Task<CharacterStatus> GetStatusAsync() => Task.FromResult(status);

        public Task SetStatusAsync(CharacterStatus value)
        {
            status = value;
            return Task.CompletedTask;
        }

        private string role = "overhearer";

        public Task<string> GetRoleAsync() => Task.FromResult(role);

        public Task SetRoleAsync(string value)
        {
            role = value;
            return Task.CompletedTask;
        }

        private bool hasSpoken = false;

        public Task<bool> GetHasSpokenAsync() => Task.FromResult(hasSpoken);

        public Task SetHasSpokenAsync(bool value)
        {
            hasSpoken = value;
            return Task.CompletedTask;
        }
    }
}
