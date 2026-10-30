using System;

namespace Ride.IO
{
    /// <summary>
    /// Base class for serializable, type-specific configuration supplied to an <see cref="IEntityInputControllable"/>.
    /// Concrete camera, vehicle, and equipment integrations derive their setup data from this type.
    /// </summary>
    [Serializable]
    public abstract class InputControllableProperties
    {
    }
}
