using System.Collections.Generic;

namespace Ride.Entities
{
    /// <summary>Creates, assigns, anchors, equips, and uses items owned by RIDE entities.</summary>
    /// <remarks>Simulation's equipment system is the primary registry for <see cref="IItem"/> and <see cref="IItemAnchor"/> instances.</remarks>
    public interface IEquipmentSystem : IRideSystem
    {
        /// <summary>
        /// Use this method when you want to add an item that is both logically and visually represented
        /// </summary>
        /// <param name="itemType">The item type to create.</param>
        /// <param name="owner">The entity that will own the item.</param>
        /// <returns>The created item identifier.</returns>
        RideID AddItem(ItemType itemType, RideID owner);

        /// <summary>Registers an existing item instance.</summary>
        /// <param name="item">The item to register.</param>
        /// <returns>The registered item identifier.</returns>
        RideID AddExistingItem(IItem item);

        /// <summary>Destroys an item and removes it from the equipment registry.</summary>
        /// <param name="itemId">The item identifier.</param>
        void DestroyItem(RideID itemId);

        /// <summary>Removes an item from an agent without destroying the item.</summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="agentId">The owning agent.</param>
        void RemoveItemFromAgent(RideID itemId, RideID agentId);

        /// <summary>
        /// Use this method when you want to add a weapon that is logically, not visually represented
        /// </summary>
        /// <param name="weapon">The weapon data to register.</param>
        /// <returns>The registered weapon identifier.</returns>
        RideID AddWeapon(IWeapon weapon);

        /// <summary>
        /// Use this method when you want to add a weapon that is logically, not visually represented
        /// </summary>
        /// <param name="weapon">The weapon data to register.</param>
        /// <returns>The registered weapon identifier.</returns>
        RideID AddWeapon(Weapon weapon);

        /// <summary>
        /// Reloads the clip of the weapon
        /// </summary>
        /// <param name="weapon">The weapon to reload</param>
        /// <param name="immediate">Whether to complete the reload without waiting for its configured duration.</param>
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

        /// <summary>Equips an item on its owner.</summary>
        /// <param name="item">The item identifier.</param>
        /// <param name="immediate">Whether to bypass any timed equip transition.</param>
        void EquipItem(RideID item, bool immediate = false);

        /// <summary>Stows an item in an automatically selected compatible anchor.</summary>
        /// <param name="item">The item identifier.</param>
        void StowItem(RideID item);

        /// <summary>Stows an item in a specific anchor.</summary>
        /// <param name="item">The item identifier.</param>
        /// <param name="anchor">The destination anchor.</param>
        void StowItem(RideID item, RideID anchor);

        /// <summary>Detaches an item from its owner and anchor while retaining it in the registry.</summary>
        /// <param name="item">The item identifier.</param>
        void OrphanItem(RideID item);

        /// <summary>
        /// Toggles the mode of the given item if the item has multiple modes. If this is a weapon, it toggles the firing mode (semi, burst, auto)
        /// </summary>
        /// <param name="itemId">The item whose mode should change.</param>
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

        /// <summary>Gets a registered weapon.</summary>
        /// <param name="weapon">The weapon identifier.</param>
        /// <returns>The weapon, or <see langword="null"/> when unavailable.</returns>
        IWeapon GetWeapon(RideID weapon);

        /// <summary>
        /// Checks if weapon fires in a high arc
        /// </summary>
        /// <param name="weapon">The weapon to test</param>
        /// <returns>True if the weapon fires in a high arc (is artillery)</returns>
        bool IsArtillery(RideID weapon);

        /// <summary>Gets a registered item as a specified type.</summary>
        /// <typeparam name="T">The required item interface or type.</typeparam>
        /// <param name="item">The item identifier.</param>
        /// <returns>The item cast to <typeparamref name="T"/>, or <see langword="null"/> when unavailable.</returns>
        T GetItem<T>(RideID item) where T : IItem;

        /// <summary>Gets a registered item.</summary>
        /// <param name="item">The item identifier.</param>
        /// <returns>The item, or <see langword="null"/> when unavailable.</returns>
        IItem GetItem(RideID item);

        /// <summary>Resets an item's transient state.</summary>
        /// <param name="item">The item identifier.</param>
        void ResetItem(RideID item);

        /// <summary>Gets items of a specified type owned by an agent.</summary>
        /// <typeparam name="T">The required item interface or type.</typeparam>
        /// <param name="agent">The owning agent.</param>
        /// <returns>The matching item identifiers.</returns>
        IEnumerable<RideID> GetItemsFromAgent<T>(RideID agent) where T : IItem;

        /// <summary>Gets unowned items of a specified type.</summary>
        /// <typeparam name="T">The required item interface or type.</typeparam>
        /// <returns>The matching item identifiers.</returns>
        IEnumerable<RideID> GetOrphanedItems<T>() where T : IItem;

        /// <summary>Gets items currently being used by an agent.</summary>
        /// <param name="agent">The owning agent.</param>
        /// <returns>The active item identifiers.</returns>
        IEnumerable<RideID> GetItemsBeingUsed(RideID agent);

        /// <summary>Gets an agent's items with a specified status.</summary>
        /// <param name="agent">The owning agent.</param>
        /// <param name="status">The required item status.</param>
        /// <returns>The matching item identifiers.</returns>
        IEnumerable<RideID> GetItemsOfStatus(RideID agent, ItemStatus status);

        /// <summary>Determines whether an item includes all specified attributes.</summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="attr">The required attributes.</param>
        /// <returns><see langword="true"/> when the item has the attributes; otherwise, <see langword="false"/>.</returns>
        bool HasAttributes(RideID itemId, ItemAttributes attr);

        /// <summary>Gets the entity that owns an item.</summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>The owner identifier, or <see cref="RideID.Null"/> when unowned.</returns>
        RideID GetOwner(RideID itemId);

        /// <summary>Gets the weapon services used by the equipment system.</summary>
        IWeaponSystem weaponSystem { get; }

        /// <summary>Determines whether an identifier belongs to a registered item.</summary>
        /// <param name="itemId">The identifier to test.</param>
        /// <returns><see langword="true"/> when the item is registered; otherwise, <see langword="false"/>.</returns>
        bool IsItem(RideID itemId);

        /// <summary>Gets an equipment anchor by identifier.</summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>The anchor, or <see langword="null"/> when unavailable.</returns>
        IItemAnchor GetAnchor(RideID anchorId);

        /// <summary>Resets transient state for all registered equipment.</summary>
        void ResetEquipmentSystem();
    }
}
