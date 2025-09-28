using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemoryConversationInfo : IConversationInfo
    {
        private string speaker = "";

        public Task<string> GetSpeakerAsync() => Task.FromResult(speaker);

        public Task SetSpeakerAsync(string value) { 
            speaker = value;
            return Task.CompletedTask;
        }

        private string addressee = "";

        public Task<string> GetAddresseeAsync() => Task.FromResult(addressee);

        public Task SetAddresseeAsync(string value)
        {
            addressee = value;
            return Task.CompletedTask;
        }

        private string lastMyExpressId = "";

        public Task<string> GetLastMyExpressIdAsync() => Task.FromResult(lastMyExpressId);

        public Task SetLastMyExpressIdAsync(string value)
        {
            lastMyExpressId = value;
            return Task.CompletedTask;
        }
    }
}
