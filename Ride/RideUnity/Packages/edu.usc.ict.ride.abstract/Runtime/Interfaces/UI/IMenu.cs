using Ride.Entities;
using Ride.Scenario;
using Ride.WorldState;

namespace Ride.UI
{
    /// <summary>
    /// Represents a Ride UI container that can be injected with core systems and then shown or hidden at runtime.
    /// </summary>
    public interface IMenu
    {
        /// <summary>
        /// Supplies the menu with the core Ride systems it may need to query or manipulate while active.
        /// </summary>
        /// <param name="viewSystem">The active Ride view system managing UI and view-related state.</param>
        /// <param name="scenarioSystem">The active scenario system.</param>
        /// <param name="agentSystem">The active agent system.</param>
        /// <param name="worldStateSystem">The active world-state event system.</param>
        void Inject(IViewSystem viewSystem, IScenarioSystem scenarioSystem, IAgentSystem agentSystem, IWorldStateSystem worldStateSystem);

        /// <summary>Makes the menu visible and active.</summary>
        void Show();

        /// <summary>Hides the menu and deactivates it.</summary>
        void Hide();
    }
}
