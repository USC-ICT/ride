using System;

namespace Ride.IO
{
    /// <summary>Defines a multi-actor input controller that publishes entity-control input events.</summary>
    public interface IEntityInputController : IInputControllerNew
    {
        /// <summary>Occurs when the controller produces input for its controlled entities.</summary>
        event EventHandler onEntityInput;
    }
}
