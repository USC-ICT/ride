namespace Ride.Entities
{
    /// <summary>Represents an item anchor with capacity for multiple items.</summary>
    public interface IItemContainer : ITransform, IItemAnchor
    {
        /// <summary>Gets or sets the container's total capacity in item-size units.</summary>
        int totalSpace { get; set; }

        /// <summary>Gets or sets the currently available capacity in item-size units.</summary>
        int availableSpace { get; set; }

        /// <summary>Gets the capacity currently occupied by items.</summary>
        int usedSpace { get; }
    }
}
