namespace Ride.IO
{
    /// <summary>Defines an entity input target that applies look input to a camera.</summary>
    public interface ICameraInputControllable : IEntityInputControllable
    {
        /// <summary>
        /// Applies a two-dimensional rotation input to the controlled camera.
        /// </summary>
        /// <param name="rotationInput">The horizontal and vertical look input.</param>
        void Rotate(RideVector2 rotationInput);
    }
}
