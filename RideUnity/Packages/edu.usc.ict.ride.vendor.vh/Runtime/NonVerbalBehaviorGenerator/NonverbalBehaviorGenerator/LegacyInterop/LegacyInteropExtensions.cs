using NonverbalBehaviorGenerator.Models;

namespace NonverbalBehaviorGenerator.LegacyInterop
{
    internal static class LegacyInteropExtensions
    {

        public static void SetGaze(this GazeInfo obj, string target, string type, string track)
        {
            obj.GazeTarget = target;
            obj.GazeType = type;
            obj.GazeTrack = track;
            obj.GazeSpeed = "default";
            obj.GazeReason = "none";
        }
    }
}
