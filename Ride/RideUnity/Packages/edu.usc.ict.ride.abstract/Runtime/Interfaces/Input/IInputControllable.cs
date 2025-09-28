namespace Ride.IO
{
    public enum InputControlType
    {
        Character,
        Vehicle,
        Camera,
        Equipment,
        Booster,
        MountedWeapon,
        None
    }

    public interface IInputControllable
    {
        string GetControllableType();
        bool Enable { get; set; }
        IInputController inputController { get; }
        public IInputControllerNew inputControllerNew { get; }
        void SetupInputController(IInputController inputController);
        void SetupInputController(IInputControllerNew inputController);
        RideID GetController();
        InputControlType controllableType { get; }
        float moveSpeed { get; set; }
    }
}
