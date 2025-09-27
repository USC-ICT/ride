using System;

namespace Ride.IO
{
    public interface IInputController
    {
        event EventHandler onViewChangeCall;
        event EventHandler onViewZoomChangeCall;

        RideID GetControlledActor();

        void SetupData(RideID id, object data);
    }
}
