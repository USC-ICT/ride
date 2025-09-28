namespace Ride.UI
{
    /// <summary>
    /// A user interface widget with 2 states
    /// </summary>
    public interface IToggle : IText, IUIElement
    {
        bool isOn { get; set; }
    }
}
