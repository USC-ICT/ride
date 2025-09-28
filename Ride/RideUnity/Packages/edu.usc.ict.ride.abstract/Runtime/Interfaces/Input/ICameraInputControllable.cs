namespace Ride.IO
{
    public interface ICameraInputControllable : IEntityInputControllable
    {
        void Rotate(RideVector2 rotationInput);
    }
}
