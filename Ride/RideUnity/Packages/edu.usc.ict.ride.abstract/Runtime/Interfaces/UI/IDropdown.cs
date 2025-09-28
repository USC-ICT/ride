namespace Ride.UI
{
    /// <summary>
    /// A user interface element that allows users to select from a set of options
    /// </summary>
    public interface IDropdown : IUIElement, IText
    {
        int selection { get; set; }
        int numItems { get; }
        void AddItem(string item);
        void RemoveItem(int index);
        void ClearItems();
    }
}
