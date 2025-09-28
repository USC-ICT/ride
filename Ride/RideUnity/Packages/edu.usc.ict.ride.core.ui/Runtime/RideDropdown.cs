namespace Ride.UI
{
    public abstract class RideDropdown : RideUIElement, IDropdown
    {
        public abstract int selection { get; set; }
        public abstract int numItems { get; }
        public abstract string text { get; set; }
        public abstract void AddItem(string item);
        public abstract void RemoveItem(int index);
        public abstract void ClearItems();
    }
}
