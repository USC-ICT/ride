namespace Ride.IO
{
    /// <summary>
    /// Bitmask for representing what input will ignored.
    /// </summary>
    public enum RideInputLayer
    {
        /// <summary>Input that directly controls a character or its actions.</summary>
        Player =    1 << 1,     // Input that directly controls character, actions.
        /// <summary>Input that directly controls a camera.</summary>
        Camera =    1 << 2,     // Input that directly controls camera.
        /// <summary>Input used to interact with user-interface controls.</summary>
        UI =        1 << 3,     // Input that affects UI buttons, toggles, dropdowns, etc.
        /// <summary>Input that controls system-level functions such as displaying UI.</summary>
        System =    1 << 4,     // Input that affects system (toggling UI, etc).
    }
}
