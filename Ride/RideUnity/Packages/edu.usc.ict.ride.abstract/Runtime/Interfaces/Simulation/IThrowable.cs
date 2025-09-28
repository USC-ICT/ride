using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Entities
{
    public interface IThrowable : IItem
    {
        float fuseTimer { get; }

        bool igniteOnImpact { get; }

        void Throw(RideVector3 throwDirection, float throwStrength, float throwTime = -1.0f);

        void Impact();

        void StartFuse();

        void Ignite();

        GameObject[] GetImpactFX();
    }
}