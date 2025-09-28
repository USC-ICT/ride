using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemoryGazeInfo : IGazeInfo
    {
        private string gazeReason = "none";

        public Task<string> GetGazeReasonAsync() => Task.FromResult(gazeReason);

        public Task SetGazeReasonAsync(string value)
        {
            gazeReason = value;
            return Task.CompletedTask;
        }

        private string previousTarget = "none";

        public Task<string> GetPreviousTargetAsync() => Task.FromResult(previousTarget);

        public Task SetPreviousTargetAsync(string value)
        {
            previousTarget = value;
            return Task.CompletedTask;
        }

        private string gazeTarget = "";

        public Task<string> GetGazeTargetAsync() => Task.FromResult(gazeTarget);

        public Task SetGazeTargetAsync(string value)
        {
            gazeTarget = value;
            return Task.CompletedTask;
        }

        private string gazeSpeed = "default";

        public Task<string> GetGazeSpeedAsync() => Task.FromResult(gazeSpeed);

        public Task SetGazeSpeedAsync(string value)
        {
            gazeSpeed = value;
            return Task.CompletedTask;
        }

        private string gazeType = "";

        public Task<string> GetGazeTypeAsync() => Task.FromResult(gazeType);

        public Task SetGazeTypeAsync(string value)
        {
            gazeType = value;
            return Task.CompletedTask;
        }

        private string gazeTrack = "";

        public Task<string> GetGazeTrackAsync() => Task.FromResult(gazeTrack);

        public Task SetGazeTrackAsync(string value)
        {
            gazeTrack = value;
            return Task.CompletedTask;
        }

        public Task SetGazeAsync(string target, string type, string track) {
            gazeTarget = target;
            gazeType = type;
            gazeTrack = track;
            gazeSpeed = "default";
            gazeReason = "none";
            return Task.CompletedTask;
        }
    }
}
