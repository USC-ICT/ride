namespace Ride.Entities
{
    public interface IItemAnchor : ITransform, IIdentity
    {
        RideID owner { get; set; }

        int maxSize { get; set; }

        bool itemVisible { get; }
    }
}
