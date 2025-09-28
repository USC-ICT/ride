namespace Ride.UI
{
    /// <summary>
    /// A user interface element that shows the user a sprite visual
    /// </summary>
    public interface IImage : IUIElement
    {
        RideColor color { get; set; }
    }
}
