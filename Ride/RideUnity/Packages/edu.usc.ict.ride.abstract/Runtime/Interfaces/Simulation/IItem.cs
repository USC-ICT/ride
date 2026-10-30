using System;

namespace Ride.Entities
{
    /// <summary>Specifies the capabilities implemented by an item.</summary>
    [Flags]
    public enum ItemAttributes
    {
        /// <summary>The item can be used as a weapon.</summary>
        weapon = 1,
        /// <summary>The item stores ammunition for a weapon.</summary>
        weaponMagazine = 1 << 1,
        /// <summary>The item can be thrown.</summary>
        throwable = 1 << 2,
        /// <summary>The item produces an explosion.</summary>
        explosive = 1 << 3
    }

    /// <summary>Represents an item that the equipment system can assign, anchor, use, fire, or wear.</summary>
    public interface IItem : IEntity//, IComponentManipulater
    {
        /// <summary>Gets the item's general type.</summary>
        ItemType type { get; }
        /// <summary>Gets the capabilities implemented by the item.</summary>
        ItemAttributes itemAttributes { get; }
        /// <summary>Gets or sets how the item is currently intended to be used.</summary>
        ItemUsage itemUsage { get; set; }
        /// <summary>Gets or sets the item's current availability or lifecycle status.</summary>
        ItemStatus itemStatus { get; set; }
        /// <summary>Gets the number of anchor-capacity units required by the item.</summary>
        int size { get; }
        /// <summary>Gets the item display name.</summary>
        string itemName { get; }
        /// <summary>Gets the item weight.</summary>
        float weight { get; }

        /// <summary>Gets or sets the simulation time when the item was last used successfully.</summary>
        float lastUseTime { get; set; }

        /// <summary>Gets or sets the simulation time when use was last attempted.</summary>
        float lastUseAttemptTime { get; set; }

        /// <summary>Gets the current stack quantity.</summary>
        int quantity { get; }
        /// <summary>Gets the maximum stack quantity.</summary>
        int quantityMax { get; }

        /// <summary>Gets whether the item is currently being used.</summary>
        bool beingUsed { get; }

        /// <summary>Attempts to begin using the item.</summary>
        /// <returns><see langword="true"/> when use begins successfully; otherwise, <see langword="false"/>.</returns>
        bool Use(); //TODO: Remove and put in IWeaponSystem
        /// <summary>Stops the item's current use action.</summary>
        void StopUse();
        /// <summary>Determines whether the item can currently be used.</summary>
        /// <returns><see langword="true"/> when the item can be used; otherwise, <see langword="false"/>.</returns>
        bool CanUse(); // TODO: Remove and put in IWeaponSystem
        /// <summary>Cycles the item's active operating mode.</summary>
        void ToggleMode();
        /// <summary>Restores the item's transient state to its initial values.</summary>
        void ResetItem();

        /// <summary>Gets or sets the entity that owns the item.</summary>
        RideID owner { get; set; }

        /// <summary>Gets or sets the equipment anchor currently holding the item.</summary>
        RideID anchor { get; set; }
    }
}
