using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Entities
{
    /// <summary>Represents an item that the attack and equipment systems can throw and ignite.</summary>
    public interface IThrowable : IItem
    {
        /// <summary>Gets the fuse duration in seconds.</summary>
        float fuseTimer { get; }

        /// <summary>Gets whether impact should ignite the throwable.</summary>
        bool igniteOnImpact { get; }

        /// <summary>Launches the item along a direction with the specified strength.</summary>
        /// <param name="throwDirection">The world-space launch direction.</param>
        /// <param name="throwStrength">The launch strength.</param>
        /// <param name="throwTime">The optional requested throw duration, or a negative value to use implementation defaults.</param>
        void Throw(RideVector3 throwDirection, float throwStrength, float throwTime = -1.0f);

        /// <summary>Notifies the throwable that it impacted another object or surface.</summary>
        void Impact();

        /// <summary>Starts the throwable's fuse countdown.</summary>
        void StartFuse();

        /// <summary>Ignites or otherwise activates the throwable.</summary>
        void Ignite();

        /// <summary>Gets the Unity objects used to render impact effects.</summary>
        /// <returns>The configured impact-effect objects.</returns>
        GameObject[] GetImpactFX();
    }
}
