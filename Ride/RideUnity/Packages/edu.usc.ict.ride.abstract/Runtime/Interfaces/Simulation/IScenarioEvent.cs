using System;

namespace Ride.Scenario
{
    public delegate void OnScenarioEventExecuted(IScenarioEvent e);

    public interface IScenarioEvent
    {
        bool IsReady();
        void Execute();
        void Init();
        void AddOnEventExecutedCallback(OnScenarioEventExecuted onEventExecuted);
        int numExecutionsRemaining { get; set; }
    }
}
