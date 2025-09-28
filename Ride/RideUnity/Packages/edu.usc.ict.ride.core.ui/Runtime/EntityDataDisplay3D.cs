using TMPro;

namespace Ride.UI
{
    /// <summary>
    /// Menu which displays entity data in 3D near the location of the entity being represented
    /// </summary>
    public class EntityDataDisplay3D : EntityDisplay3D
    {
        enum DisplayDataId { Name, Health, Speed, Loaded, State }

        protected override void UpdateDefaults(RideID agent)
        {
            base.UpdateDefaults(agent);

            AddDisplayText(DisplayDataId.Name, scenarioSystem.GetEntityName(agent));
            AddDisplayText(DisplayDataId.Health, agentSystem.GetAgentHealth(agent).ToString("f0"));
            AddDisplayText(DisplayDataId.Speed, agentSystem.GetAgentSpeed(agent).ToString("f2"));
            //AddDisplayText(DisplayDataId.Loaded, agentSystem.GetAgent.isLoaded.ToString());

            if (agentSystem == null)
            {
                agentSystem = Globals.api.agentSystem;
            }
            AddDisplayText(DisplayDataId.State, agentSystem.GetAgentState(agent));
        }

        TextMeshProUGUI AddDataLine(DisplayDataId id, string line)
        {
            return AddDataLine(id.ToString(), line);
        }

        void UpdateText(DisplayDataId id, object value)
        {
            UpdateText(id.ToString(), value);
        }

        void AddDisplayText(DisplayDataId id, string text)
        {
            AddDisplayText(id.ToString(), text);
        }
    }
}
