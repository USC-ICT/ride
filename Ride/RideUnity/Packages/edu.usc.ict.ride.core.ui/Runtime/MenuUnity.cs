using UnityEngine;
using UnityEngine.UI;
using Ride.Entities;
using Ride.Scenario;
using Ride.WorldState;

namespace Ride.UI
{
    /// <summary>
    /// Base Unity implementation for Ride menus that supports system injection and
    /// provides helper methods for showing, hiding, and instantiating layout-driven UI elements.
    /// </summary>
    public class MenuUnity : RideMonoBehaviour, IMenu
    {
        protected IViewSystem viewSystem { get; set; }
        protected IScenarioSystem scenarioSystem { get; set; }
        protected IAgentSystem agentSystem { get; set; }
        protected IWorldStateSystem worldStateSystem { get; set; }


        /// <summary>
        /// Stores references to the core Ride systems that this menu can use during its lifetime.
        /// </summary>
        /// <param name="viewSystem">The active Ride view system managing UI and view-related state.</param>
        /// <param name="scenarioSystem">The active scenario system.</param>
        /// <param name="agentSystem">The active agent system.</param>
        /// <param name="worldStateSystem">The active world-state event system.</param>
        public void Inject(IViewSystem viewSystem, IScenarioSystem scenarioSystem, IAgentSystem agentSystem, IWorldStateSystem worldStateSystem)
        {
            this.viewSystem = viewSystem;
            this.scenarioSystem = scenarioSystem;
            this.agentSystem = agentSystem;
            this.worldStateSystem = worldStateSystem;
        }

        /// <summary>Hides the menu by deactivating its root GameObject.</summary>
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>Shows the menu by activating its root GameObject.</summary>
        public virtual void Show() => gameObject.SetActive(true);

        /// <summary>
        /// Instantiates a widget under a standard Unity layout group.
        /// </summary>
        /// <typeparam name="T">The MonoBehaviour type of the widget to instantiate.</typeparam>
        /// <param name="layout">The layout group that will own the instantiated widget.</param>
        /// <param name="widgetTemplate">The prefab or template to instantiate.</param>
        /// <param name="setActive">Whether the instantiated widget should be activated immediately.</param>
        /// <returns>The instantiated widget, or <c>null</c> if the layout is missing.</returns>
        public static T AddToLayout<T>(LayoutGroup layout, T widgetTemplate, bool setActive = true) where T : MonoBehaviour => 
            AddToLayoutInternal(layout, widgetTemplate, setActive);

        /// <summary>
        /// Instantiates a widget under a Ride layout group.
        /// </summary>
        /// <typeparam name="T">The MonoBehaviour type of the widget to instantiate.</typeparam>
        /// <param name="layout">The Ride layout group that will own the instantiated widget.</param>
        /// <param name="widgetTemplate">The prefab or template to instantiate.</param>
        /// <param name="setActive">Whether the instantiated widget should be activated immediately.</param>
        /// <returns>The instantiated widget, or <c>null</c> if the layout is missing.</returns>
        public static T AddToLayout<T>(RideLayoutGroup layout, T widgetTemplate,  bool setActive = true) where T : MonoBehaviour =>
            AddToLayoutInternal(layout, widgetTemplate, setActive);

        private static T AddToLayoutInternal<T>(Component layout, T widgetTemplate,  bool setActive = true) where T : MonoBehaviour
        {
            if (layout == null)
            {
                RideLog.LogError("MenuUnity.AddToLayoutInternal - NO LAYOUT");
                return null;
            }

            T instance = GameObject.Instantiate<T>(widgetTemplate, layout.transform);
            instance.gameObject.SetActive(setActive);
            instance.transform.SetParent(layout.transform);
            instance.transform.localScale = instance.transform.localScale;
            return instance;
        }

        /// <summary>
        /// Removes a widget previously added to a Ride layout group.
        /// </summary>
        /// <param name="layout">The layout group that owns the widget.</param>
        /// <param name="widget">The widget GameObject to destroy.</param>
        public static void RemoveFromLayout(RideLayoutGroup layout, GameObject widget)
        {
            if (layout == null)
            {
                RideLog.LogError("MenuUnity.RemoveFromLayout - NO LAYOUT");
                return;
            }

            Destroy(widget);
        }
    }
}
