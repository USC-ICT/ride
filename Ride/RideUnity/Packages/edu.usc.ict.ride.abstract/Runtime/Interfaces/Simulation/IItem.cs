using System;

namespace Ride.Entities
{
    [Flags]
    public enum ItemAttributes
    {
        weapon = 1,
        weaponMagazine = 1 << 1,
        throwable = 1 << 2,
        explosive = 1 << 3
    }

    /// <summary>
    /// Represents an item in the game world that is used, fired, worn, etc
    /// </summary>
    public interface IItem : IEntity//, IComponentManipulater
    {
        ItemType type { get; }
        ItemAttributes itemAttributes { get; }
        ItemUsage itemUsage { get; set; }
        ItemStatus itemStatus { get; set; }
        int size { get; }
        string itemName { get; }
        float weight { get; }

        /// <summary>
        /// The time when the item was last used
        /// </summary>
        float lastUseTime { get; set; }

        float lastUseAttemptTime { get; set; }

        int quantity { get; }
        int quantityMax { get; }

        bool beingUsed { get; }

        bool Use(); //TODO: Remove and put in IWeaponSystem
        void StopUse();
        bool CanUse(); // TODO: Remove and put in IWeaponSystem
        void ToggleMode();
        void ResetItem();

        RideID owner { get; set; }

        RideID anchor { get; set; }
    }
}
