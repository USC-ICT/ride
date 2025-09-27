
namespace Ride
{
    /// <summary>
    /// Interface for components that can rotate to face a world-space position.
    /// </summary>
    public interface IFaceTarget
    {
        /// <summary>
        /// Rotates the implementing object to face the specified world-space position.
        /// </summary>
        /// <param name="worldPosition">The position in world space to face.</param>
        void FaceTarget(RideVector3 worldPosition);
    }
}
