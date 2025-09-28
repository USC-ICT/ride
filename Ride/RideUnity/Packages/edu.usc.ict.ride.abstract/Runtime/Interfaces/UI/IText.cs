namespace Ride.UI
{
    /// <summary>
    /// A user interface widget that displays text
    /// </summary>
    public interface IText : IUIElement
    {
        string text { get; set; }
    }
}
