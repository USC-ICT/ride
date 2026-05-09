using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    /// <summary>
    /// Defines the UI-facing toggle behavior for billboard icons that can be selected from world-space input.
    /// </summary>
    public interface IBillboardIconToggle
    {
        /// <summary>
        /// Gets the hit result recorded for the current frame.
        /// A value of <c>0</c> indicates no click interaction, <c>1</c> indicates the billboard was clicked,
        /// and <c>-1</c> indicates the click landed outside the billboard.
        /// </summary>
        int HitThisFrame { get; } 
    }
}
