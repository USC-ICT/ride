using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.IO
{
    /// <summary>Defines an entity component whose input behavior is configured from serializable properties.</summary>
    public interface IEntityInputControllable : IInputControllable
    {
        /// <summary>
        /// Applies type-specific configuration to this controllable component.
        /// </summary>
        /// <param name="properties">The properties for the concrete controllable type.</param>
        void SetControllableProperties(InputControllableProperties properties);
    }
}
