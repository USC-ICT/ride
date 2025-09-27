using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemorySwitches : ISwitches
    {
        public InMemorySwitches(bool allBehavior, bool saliencyGlance, bool saliencyIdleGaze, bool speakerGaze, bool speakerGestures, bool listenerGaze, bool poseRules)
        {
            this.allBehavior = allBehavior;
            this.saliencyGlance = saliencyGlance;
            this.saliencyIdleGaze = saliencyIdleGaze;
            this.speakerGaze = speakerGaze;
            this.speakerGestures = speakerGestures;
            this.listenerGaze = listenerGaze;
            this.poseRules = poseRules;
        }

        private bool allBehavior;

        public Task<bool> GetAllBehaviorAsync() => Task.FromResult(allBehavior);

        public Task SetAllBehaviorAsync(bool value)
        {
            allBehavior = value;
            return Task.CompletedTask;
        }

        private bool saliencyGlance;

        public Task<bool> GetSaliencyGlanceAsync() => Task.FromResult(saliencyGlance);

        public Task SetSaliencyGlanceAsync(bool value)
        {
            saliencyGlance = value;
            return Task.CompletedTask;
        }

        private bool saliencyIdleGaze;

        public Task<bool> GetSaliencyIdleGazeAsync() => Task.FromResult(saliencyIdleGaze);

        public Task SetSaliencyIdleGazeAsync(bool value)
        {
            saliencyIdleGaze = value;
            return Task.CompletedTask;
        }

        private bool speakerGaze;

        public Task<bool> GetSpeakerGazeAsync() => Task.FromResult(speakerGaze);

        public Task SetSpeakerGazeAsync(bool value)
        {
            speakerGaze = value;
            return Task.CompletedTask;
        }

        private bool speakerGestures;

        public Task<bool> GetSpeakerGesturesAsync() => Task.FromResult(speakerGestures);

        public Task SetSpeakerGesturesAsync(bool value)
        {
            speakerGestures = value;
            return Task.CompletedTask;
        }

        private bool listenerGaze;

        public Task<bool> GetListenerGazeAsync() => Task.FromResult(listenerGaze);

        public Task SetListenerGazeAsync(bool value)
        {
            listenerGaze = value;
            return Task.CompletedTask;
        }

        private bool poseRules;

        public Task<bool> GetPoseRulesAsync() => Task.FromResult(poseRules);

        public Task SetPoseRulesAsync(bool value)
        {
            poseRules = value;
            return Task.CompletedTask;
        }
    }
}
