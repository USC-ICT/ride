namespace Ride.IO
{
    public interface IVehicleInputControllable : IEntityInputControllable
    {
        void Drive(float speed);

        void TurnWheel(float angle);

        void Brake();

        void Boost(bool boostFlag);
    }
}
