using UnityEngine;
using UnityEngine.UI;
using Ride.Entities;
using Ride.Scenario;
using Ride.WorldState;

namespace Ride.UI
{
    public class MenuMono : RideMonoBehaviour, IMenu
    {
        protected IViewSystem viewSystem { get; set; }
        protected IScenarioSystem scenarioSystem { get; set; }
        protected IAgentSystem agentSystem { get; set; }
        protected IWorldStateSystem worldStateSystem { get; set; }

        public void Inject(IViewSystem viewSystem, IScenarioSystem scenarioSystem, IAgentSystem agentSystem, IWorldStateSystem worldStateSystem)
        {
            this.viewSystem = viewSystem;
            this.scenarioSystem = scenarioSystem;
            this.agentSystem = agentSystem;
            this.worldStateSystem = worldStateSystem;
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public virtual void Hide() { gameObject.SetActive(false); }

        public virtual void Show() { gameObject.SetActive(true); }

        public static T AddToLayout<T>(LayoutGroup layout, T widgetTemplate, bool setActive = true) where T : MonoBehaviour
        {
            if (layout == null)
            {
                RideLog.LogError("NO LAYOUT");
                return null;
            }
            T instance = GameObject.Instantiate<T>(widgetTemplate, layout.transform);
            instance.gameObject.SetActive(setActive);
            instance.transform.SetParent(layout.transform);
            instance.transform.localScale = instance.transform.localScale;
            return instance;
        }

        public static T AddToLayout<T>(RideLayoutGroup layout, T widgetTemplate,  bool setActive = true) where T : MonoBehaviour
        {
            if (layout == null)
            {
                RideLog.LogError("NO LAYOUT");
                return null;
            }

            T instance = GameObject.Instantiate<T>(widgetTemplate, layout.transform);
            instance.gameObject.SetActive(setActive);
            instance.transform.SetParent(layout.transform);
            instance.transform.localScale = instance.transform.localScale;
            return instance;
        }

        public static void RemoveFromLayout(RideLayoutGroup layout, GameObject widget)
        {
            if (layout == null)
            {
                RideLog.LogError("NO LAYOUT");
                return;
            }

            Destroy(widget);
        }
    }
}
