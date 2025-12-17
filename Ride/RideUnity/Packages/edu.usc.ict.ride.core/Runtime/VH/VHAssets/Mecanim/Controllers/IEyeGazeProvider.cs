
namespace VHAssets
{
    public interface IEyeGazeProvider
    {
        /// <summary>
        /// Returns the normalized vertical gaze in the range [-1, 1].
        /// -1 = maximum down gaze, 0 = straight ahead, 1 = maximum up gaze.
        /// </summary>
        float GetVerticalGaze();
    }
}
