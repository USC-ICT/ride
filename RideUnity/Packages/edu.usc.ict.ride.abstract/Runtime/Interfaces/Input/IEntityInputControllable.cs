using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.IO
{
    public interface IEntityInputControllable : IInputControllable
    {
        void SetControllableProperties(InputControllableProperties properties);
    }
}
