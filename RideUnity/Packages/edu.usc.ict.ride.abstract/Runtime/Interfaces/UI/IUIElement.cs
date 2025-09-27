namespace Ride.UI
{
    /// <summary>
    /// A user interface object
    /// </summary>
    public interface IUIElement
    {
        void Show(bool show);
        bool isInteractable { get; set; }

        float RecalculateHeight();
    }
}
