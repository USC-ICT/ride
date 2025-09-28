using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.IO
{
    public interface IInputControllerNew
    {
        InputControlType controllerType { get; }

        void AddControlledActorID(RideID id);

        void RemoveControlledActorID(RideID id);

        RideID[] GetControlledActors();

        RideID controllerId { get; }
    }
}
