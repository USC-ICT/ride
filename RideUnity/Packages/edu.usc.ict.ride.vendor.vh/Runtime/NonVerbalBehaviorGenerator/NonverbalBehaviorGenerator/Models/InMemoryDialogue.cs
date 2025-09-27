using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemoryDialogue : IDialogue
    {
        private string speaker = "none";

        public Task<string> GetSpeakerAsync() => Task.FromResult(speaker);

        public Task SetSpeakerIdAsync(string value)
        {
            speaker = value;
            return Task.CompletedTask;
        }

        private string listener = "none";

        public Task<string> GetListenerAsync() => Task.FromResult(listener);

        public Task SetListenerIdAsync(string value)
        {
            listener = value;
            return Task.CompletedTask;
        }
    }
}
