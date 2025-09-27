using Ride.Entities;
using Ride.Scenario;
using Ride.WorldState;

namespace Ride.UI
{
    /// <summary>
    /// A container for user interface elements
    /// </summary>
    public interface IMenu //: IIdentity
    {
        void Inject(IViewSystem viewSystem, IScenarioSystem scenarioSystem, IAgentSystem agentSystem, IWorldStateSystem worldStateSystem);
        void Show();
        void Hide();
    }
}
