namespace Ride.Entities
{
    /// <summary>Represents a single-item equipment anchor, such as an entity's hand.</summary>
    public interface IItemHolder : IItemAnchor
    {
        /// <summary>Gets or sets whether the holder currently contains an item.</summary>
        bool holdingItem { get; set; }
    }
}
