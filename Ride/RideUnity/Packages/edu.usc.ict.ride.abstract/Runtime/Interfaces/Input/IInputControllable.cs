namespace Ride.IO
{
    /// <summary>Identifies the kind of entity capability exposed for input control.</summary>
    public enum InputControlType
    {
        /// <summary>A character or agent.</summary>
        Character,

        /// <summary>A drivable vehicle.</summary>
        Vehicle,

        /// <summary>A controllable camera.</summary>
        Camera,

        /// <summary>Carried or mounted equipment.</summary>
        Equipment,

        /// <summary>A movement or performance booster.</summary>
        Booster,

        /// <summary>A mounted weapon.</summary>
        MountedWeapon,

        /// <summary>No input-control category.</summary>
        None
    }

    /// <summary>
    /// Defines a component that receives input from an attached controller.
    /// The input system uses this contract to connect controllers to character, vehicle, camera, and equipment capabilities.
    /// </summary>
    public interface IInputControllable
    {
        /// <summary>
        /// Gets the implementation-specific name of this controllable type.
        /// </summary>
        /// <returns>The controllable type name.</returns>
        string GetControllableType();

        /// <summary>Gets or sets whether this component accepts input.</summary>
        bool Enable { get; set; }

        /// <summary>Gets the attached single-actor input controller.</summary>
        IInputController inputController { get; }

        /// <summary>Gets the attached multi-actor input controller.</summary>
        public IInputControllerNew inputControllerNew { get; }

        /// <summary>
        /// Attaches a single-actor input controller to this component.
        /// </summary>
        /// <param name="inputController">The controller to attach.</param>
        void SetupInputController(IInputController inputController);

        /// <summary>
        /// Attaches a multi-actor input controller to this component.
        /// </summary>
        /// <param name="inputController">The controller to attach.</param>
        void SetupInputController(IInputControllerNew inputController);

        /// <summary>
        /// Gets the identifier of the attached multi-actor controller.
        /// </summary>
        /// <returns>The controller identifier, or <see cref="RideID.Null"/> when none is attached.</returns>
        RideID GetController();

        /// <summary>Gets the input-control category exposed by this component.</summary>
        InputControlType controllableType { get; }

        /// <summary>Gets or sets the movement speed used by the controllable implementation.</summary>
        float moveSpeed { get; set; }
    }
}
