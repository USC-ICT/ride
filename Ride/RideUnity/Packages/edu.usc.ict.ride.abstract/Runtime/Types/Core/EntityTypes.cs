using System;

namespace Ride.Entities
{
    /// <summary>
    /// Flags used to categorize simulation entities by role or function.
    /// These can be combined using bitwise logic.
    /// </summary>
    [Flags]
    public enum EntityAttributes
    {
        agent          = 1,
        structure      = 1 << 1,
        cover          = 1 << 2,
        tree           = 1 << 3,
        waypoint       = 1 << 4,
        goal           = 1 << 5,
        ai             = 1 << 6,
        human_player   = 1 << 7,
        ordnance       = 1 << 8,
        vehicle        = 1 << 9
    }

    /// <summary>
    /// Flags used to describe the current operational status of an entity.
    /// These may change dynamically during simulation.
    /// </summary>
    [Flags]
    public enum EntityStatus
    {
        Normal           = 0,
        Ambushed         = 1,
        Assaulting       = 1 << 1,
        UseSmokeGrenade  = 1 << 2,
        Suppressed       = 1 << 3,
        SuppressFire     = 1 << 4,
        Annihilated      = 1 << 5
    }
}
