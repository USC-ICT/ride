namespace Ride.Movement
{
    /// <summary>
    /// Defines the interface for a factory that creates formation procedures.
    /// Implementations of this interface are responsible for returning
    /// appropriate IFormationProcedure instances based on the specified type.
    /// 
    /// This abstraction allows formation logic to be decoupled from specific system
    /// implementations and supports future test mocks or alternative engines.
    /// </summary>
    public interface IFormationFactory : IRideSystem
    {
        /// <summary>
        /// Creates a new formation procedure instance of the specified type.
        /// </summary>
        /// <param name="type">The desired formation type.</param>
        /// <param name="movementSystem">The movement system to associate with the procedure.</param>
        /// <returns>An instance of IFormationProcedure, or null if the type is unrecognized.</returns>
        IFormationProcedure Create(FormationProcedureType type, IMovementSystem movementSystem);
    }
}
