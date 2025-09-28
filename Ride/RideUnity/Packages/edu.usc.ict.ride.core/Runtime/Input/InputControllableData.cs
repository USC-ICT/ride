using System;

namespace Ride.IO
{
    [Serializable]
    public struct InputControllableData
    {
        public InputControlType controllableType;

        public InputControllableProperties controllableProperties;
    }
}
