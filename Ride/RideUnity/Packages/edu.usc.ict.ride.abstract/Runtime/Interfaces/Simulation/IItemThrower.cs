namespace Ride.Entities
{
    public interface IItemThrower : ITransform, IItemAnchor
    {
        bool holdingItem { get; set; }
    }
}