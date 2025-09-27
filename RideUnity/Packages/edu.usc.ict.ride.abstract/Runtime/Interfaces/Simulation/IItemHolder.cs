namespace Ride.Entities
{
    public interface IItemHolder : IItemAnchor
    {
        bool holdingItem { get; set; }
    }
}