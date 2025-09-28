namespace Ride.Entities
{
    public interface IItemContainer : ITransform, IItemAnchor
    {
        int totalSpace { get; set; }
        int availableSpace { get; set; }
        int usedSpace { get; }
    }
}