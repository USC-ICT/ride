namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.NVBGSwitch
    /// </remarks>
    internal sealed class Switches
    {
        public bool AllBehaviour { get; set; } = true;
        public bool SaliencyIdleGaze { get; set; } = true;

        /// <remarks>Refactor of NVBGSwitch.speakerGestures</remarks>
        public bool SaliencyGlance { get; set; } = false;
        public bool SpeakerGesture { get; set; } = true;
        public bool ListenerGaze { get; set; } = true;
        public bool SpeakerGaze { get; set; } = true;
        public bool PosRules { get; set; } = true;
    }
}
