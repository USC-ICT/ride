using System;

namespace Ride.IO
{
    public interface IEntityInputController : IInputControllerNew
    {
        event EventHandler onEntityInput;
    }
}
