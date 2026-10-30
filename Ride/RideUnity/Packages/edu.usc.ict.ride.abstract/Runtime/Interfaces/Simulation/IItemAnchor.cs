namespace Ride.Entities
{
    /// <summary>Represents an identified attachment location managed by the equipment system.</summary>
    public interface IItemAnchor : ITransform, IIdentity
    {
        /// <summary>Gets or sets the entity that owns this anchor.</summary>
        RideID owner { get; set; }

        /// <summary>Gets or sets the largest item size accepted by this anchor.</summary>
        int maxSize { get; set; }

        /// <summary>Gets whether an item attached to this anchor should be visible.</summary>
        bool itemVisible { get; }
    }
}
