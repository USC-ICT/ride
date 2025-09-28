using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Represents a camera in the scene.
    /// This is a parallel class to <a href="https://docs.unity3d.com/ScriptReference/Camera.html">UnityEngine.Camera</a>.
    /// Implemented separately to abstract Ride classes away from UnityEngine specific implementations.
    /// </summary>
    public interface ICamera : ITransform
    {
        /// <summary>
        /// Move the camera to a position in the world based on normalized coordinates
        /// </summary>
        /// <param name="normalizedCoords">Each element should range 0 to 1</param>
        /// <param name="ignoreHeight">true if you don't want to manipulate the camera's height</param>
        void SetNormalizedWorldPosition(RideVector3 normalizedCoords, bool ignoreHeight);

        /// <summary>
        /// Converts the world position into normalized coordinates
        /// </summary>
        /// <param name="worldPos">The global position in the scene</param>
        /// <returns>The normalized coordinates. If the worldPos is inside the
        /// world boundaries, then all values will be between 0 and 1</returns>
        RideVector3 ConvertToCoordinates(RideVector3 worldPos);

        /// <summary>
        /// Creates a texture of what the camera is currently looking at
        /// </summary>
        /// <param name="width">Width in pixels of the texture</param>
        /// <param name="height">Height in pixels of the texture</param>
        /// <returns>The screen shot</returns>
        Texture2D CaptureScreenshotFromCurrentView(int width, int height);

        /// <summary>
        /// Returns a ray going from the camera through the screen point
        /// </summary>
        /// <param name="pos">The screen point</param>
        /// <returns>Returns a ray going from the camera through the screen point</returns>
        RideRay ScreenPointToRay(RideVector3 pos);

        /// <summary>
        /// Activate or deactivate the camera
        /// </summary>
        /// <param name="active"></param>
        void SetActive(bool active);

        /// <summary>
        /// Resets camera to a default position
        /// </summary>
        void ResetCamera();
    }
}
