namespace Ride.Entities
{
    /// <summary>Represents an equipment anchor from which throwable items can be launched.</summary>
    public interface IItemThrower : ITransform, IItemAnchor
    {
        /// <summary>Gets or sets whether the thrower currently contains an item.</summary>
        bool holdingItem { get; set; }
    }
}
