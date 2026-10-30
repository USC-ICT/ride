namespace Ride.IO
{
    /// <summary>Defines an entity input target that translates controller input into vehicle movement.</summary>
    public interface IVehicleInputControllable : IEntityInputControllable
    {
        /// <summary>
        /// Applies forward or reverse drive input.
        /// </summary>
        /// <param name="speed">The signed drive input or requested speed, as interpreted by the vehicle.</param>
        void Drive(float speed);

        /// <summary>
        /// Applies steering input to the vehicle's wheels.
        /// </summary>
        /// <param name="angle">The signed steering input or wheel angle, as interpreted by the vehicle.</param>
        void TurnWheel(float angle);

        /// <summary>Applies the vehicle brake.</summary>
        void Brake();

        /// <summary>
        /// Enables or disables the vehicle's boost input.
        /// </summary>
        /// <param name="boostFlag"><c>true</c> to engage boost; <c>false</c> to disengage it.</param>
        void Boost(bool boostFlag);
    }
}
