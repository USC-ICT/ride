namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.GazeInfo
    /// </remarks>
    internal sealed class GazeInfo
    {
        public string GazeReason { get; set; } = "none";
        public string PreviousTarget { get; set; } = "none";
        public string GazeTarget { get; set; } = "";
        public string GazeSpeed { get; set; } = "default";
        public string GazeType { get; set; } = "";
        public string GazeTrack { get; set; } = "";
    }
}
