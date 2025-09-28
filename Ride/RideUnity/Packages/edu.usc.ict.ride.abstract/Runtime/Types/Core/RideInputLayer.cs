namespace Ride.IO
{
    /// <summary>
    /// Bitmask for representing what input will ignored.
    /// </summary>
    public enum RideInputLayer
    {
        Player =    1 << 1,     // Input that directly controls character, actions.
        Camera =    1 << 2,     // Input that directly controls camera.
        UI =        1 << 3,     // Input that affects UI buttons, toggles, dropdowns, etc.
        System =    1 << 4,     // Input that affects system (toggling UI, etc).
    }
}
