using System;

namespace Ride.Scenario
{
    /// <summary>Represents a callback invoked after a scenario event executes.</summary>
    /// <param name="e">The event that executed.</param>
    public delegate void OnScenarioEventExecuted(IScenarioEvent e);

    /// <summary>Represents an executable, repeatable event managed by <see cref="IScenarioSystem"/>.</summary>
    public interface IScenarioEvent
    {
        /// <summary>Determines whether the event's execution conditions are currently satisfied.</summary>
        /// <returns><see langword="true"/> when the event is ready to execute; otherwise, <see langword="false"/>.</returns>
        bool IsReady();
        /// <summary>Executes the event and its registered effects.</summary>
        void Execute();
        /// <summary>Initializes the event before scenario evaluation begins.</summary>
        void Init();
        /// <summary>Registers a callback invoked after the event executes.</summary>
        /// <param name="onEventExecuted">The callback to register.</param>
        void AddOnEventExecutedCallback(OnScenarioEventExecuted onEventExecuted);
        /// <summary>Gets or sets the number of additional executions allowed.</summary>
        int numExecutionsRemaining { get; set; }
    }
}
