namespace Ride.UI
{
    public abstract class RideInputField : RideUIElement, IInputField, IText
    {
        public abstract string text { get; set; }
    }
}
