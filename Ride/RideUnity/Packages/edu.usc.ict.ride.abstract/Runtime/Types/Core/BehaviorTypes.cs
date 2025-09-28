using System;

namespace Ride.AI
{
    /// <summary>Represents the status of a behavior in the simulation.</summary>
    public enum BehaviorStatus
    {
        /// <summary>The behavior is not currently running.</summary>
        Inactive,

        /// <summary>The behavior is currently active and executing.</summary>
        Running
    }

    /// <summary>Indicates a discrete event that occurred within the lifecycle of a behavior.</summary>
    public enum BehaviorEvent
    {
        /// <summary>The behavior has been started.</summary>
        Started,

        /// <summary>The behavior has been explicitly stopped.</summary>
        Stopped,

        /// <summary>The behavior has completed on its own or reached its end condition.</summary>
        Finished
    }

    /// <summary>
    /// Delegate for receiving notifications when a behavior's status changes.
    /// </summary>
    /// <param name="previous">The status before the change.</param>
    /// <param name="current">The status after the change.</param>
    public delegate void OnBehaviorStatusChanged(BehaviorStatus previous, BehaviorStatus current);

    /// <summary>
    /// Delegate for receiving discrete behavior events such as Started, Stopped, or Finished.
    /// </summary>
    /// <param name="behaviorEvent">The event that occurred.</param>
    public delegate void OnBehaviorEvent(BehaviorEvent behaviorEvent);
}
