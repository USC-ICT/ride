using System.Collections.Generic;

namespace Ride.Entities
{
    public interface IEquipmentSystem : IRideSystem
    {
        /// <summary>
        /// Use this method when you want to add an item that is both logically and visually represented
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        RideID AddItem(ItemType itemType, RideID owner);

        RideID AddExistingItem(IItem item);

        void DestroyItem(RideID itemId);

        void RemoveItemFromAgent(RideID itemId, RideID agentId);

        /// <summary>
        /// Use this method when you want to add a weapon that is logically, not visually represented
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        RideID AddWeapon(IWeapon weapon);

        /// <summary>
        /// Use this method when you want to add a weapon that is logically, not visually represented
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        RideID AddWeapon(Weapon weapon);

        /// <summary>
        /// Reloads the clip of the weapon
        /// </summary>
        /// <param name="weapon">The weapon to reload</param>
        void Reload(RideID weapon, bool immediate = false);

        /// <summary>
        /// Uses the given item. If this is a weapon, it is fired
        /// </summary>
        /// <param name="item">The item you want to use</param>
        bool Use(RideID item);

        /// <summary>
        /// Stops using the given item if the item is still being used
        /// </summary>
        /// <param name="item">The item you want to stop using</param>
        bool StopUse(RideID item);

        void EquipItem(RideID item, bool immediate = false);

        void StowItem(RideID item);

        void StowItem(RideID item, RideID anchor);

        void OrphanItem(RideID item);

        /// <summary>
        /// Toggles the mode of the given item if the item has multiple modes. If this is a weapon, it toggles the firing mode (semi, burst, auto)
        /// </summary>
        /// <param name="item">The item you want to toggle the mode on</param>
        void ToggleMode(RideID itemId);

        /// <summary>
        /// Fires/swings/etc the given weapon.
        /// </summary>
        /// <param name="weapon">The weapon to fire</param>
        bool Fire(RideID weapon);

        /// <summary>
        /// Checks if the item is ready to use
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is ready to use</returns>
        bool CanUse(RideID item);

        /// <summary>
        /// Checks if the weapon is ready to use
        /// </summary>
        /// <param name="weapon">The weapon to test</param>
        /// <returns>True if the weapon can be fired</returns>
        bool CanFire(RideID weapon);

        /// <summary>
        ///
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        IWeapon GetWeapon(RideID weapon);

        /// <summary>
        /// Checks if weapon fires in a high arc
        /// </summary>
        /// <param name="weapon">The weapon to test</param>
        /// <returns>True if the weapon fires in a high arc (is artillery)</returns>
        bool IsArtillery(RideID weapon);

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        T GetItem<T>(RideID item) where T : IItem;

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        IItem GetItem(RideID item);

        /// <summary>
        /// Resets the item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        void ResetItem(RideID item);

        IEnumerable<RideID> GetItemsFromAgent<T>(RideID agent) where T : IItem;

        IEnumerable<RideID> GetOrphanedItems<T>() where T : IItem;

        IEnumerable<RideID> GetItemsBeingUsed(RideID agent);

        IEnumerable<RideID> GetItemsOfStatus(RideID agent, ItemStatus status);

        bool HasAttributes(RideID itemId, ItemAttributes attr);

        RideID GetOwner(RideID itemId);

        IWeaponSystem weaponSystem { get; }

        bool IsItem(RideID itemId);

        IItemAnchor GetAnchor(RideID anchorId);

        void ResetEquipmentSystem();
    }
}
