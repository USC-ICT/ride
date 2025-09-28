using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;
using Ride.Entities;
using Ride.Scenario;
using Ride.WorldState;

namespace Ride.UI
{
    public class ViewSystemMono : RideSystemMonoBehaviour, IViewSystem
    {
        List<IMenu> m_menus = new List<IMenu>();
        IScenarioSystem scenarioSystem { get { return Globals.api.scenarioSystem; } }
        IAgentSystem agentSystem { get { return Globals.api.agentSystem; } }
        IWorldStateSystem worldStateSystem { get { return Globals.api.worldStateSystem; } }
        public ISelector<RideID> agentSelector { get; set; } // deprecated || TODO: Remove
        public ISelector<RideID> entitySelector { get; set; }
        public IGrouperSystem<RideID> grouperSystem { get; set; }
        public IEnumerable<RideID> selectedEntities { get => grouperSystem.GetCurrentGroup(); }
        public IGroupIdCreatorSystem groupIdCreatorSystem { get; set; }
        public ViewSystemParams config { get; set; }
        public IViewSystemMenu viewSystemMenu { get; set; }

        [SerializeField] bool useLegacyAgentSelector = false;
        [SerializeField] bool showDebugEntityData = false;
        [SerializeField] UI.EntityDebugUICard debugEntityUICard = null;
        Dictionary<RideID, UI.EntityDebugUICard> debugEntityCardList = new Dictionary<RideID, UI.EntityDebugUICard>();


        public override void SystemAwake()
        {
            base.SystemAwake();

            config = ViewSystemParams.Default;
            config.flags = ViewSystemConfigFlags.All;

            Globals.api.worldStateSystem?.AddListener<SelectionEvent>(WorldEvent.entitySelectionChanged, onSelectionChanged);
            Globals.api.worldStateSystem?.AddListener<EntityDataEvent>(WorldEvent.entityDataUpdate, onEntityDataUpdate);

            // Entity selector system
            var entitySelectorComponent = new GameObject("RideEntitySelectorSystem").AddComponent<RideEntitySelectorSystem>();
            entitySelectorComponent.selectorDisplayMenu = GetComponentFromMenu<ISelectorDisplay>();
            entitySelectorComponent.transform.SetParent(this.transform);
            entitySelector = entitySelectorComponent;

            // Create ability to do agent selection
            var agentSelectorComponent = new GameObject("AgentSelectorInputSystem").AddComponent<AgentSelectorInputSystem>();
            agentSelectorComponent.selectorDisplayMenu = GetComponentFromMenu<ISelectorDisplay>();
            agentSelectorComponent.transform.SetParent(this.transform);
            agentSelector = agentSelectorComponent;
            agentSelector.enabled = useLegacyAgentSelector;
        }

        public GameObject m_viewSystemMenuPrefab;

        public override void SystemInit()
        {
            base.SystemInit();

            // Find all the mono menus, inject and store them
            List<MenuMono> menuMonos = VHUtils.FindObjectsOfTypeAll<MenuMono>();
            foreach (MenuMono menuMono in menuMonos)
            {
                menuMono.Inject(this, scenarioSystem, agentSystem, worldStateSystem);
                m_menus.Add(menuMono);
            }

            // Create ability to do agent grouping
            var grouperSystemComponent = new GameObject("EntityGrouperSystem").AddComponent<EntityGrouperSystem>();
            grouperSystemComponent.transform.SetParent(this.transform);
            grouperSystem = grouperSystemComponent;

            // Create ability to save groups and re-select them
            var groupIdCreatorSystemComponent = new GameObject("GroupIdCreatorKeyboardInputSystem").AddComponent<GroupIdCreatorKeyboardInputSystem>();
            groupIdCreatorSystemComponent.transform.SetParent(this.transform);
            groupIdCreatorSystem = groupIdCreatorSystemComponent;
                        
            GameObject viewSystemMenuPrefab = GameObject.Instantiate(m_viewSystemMenuPrefab);
            viewSystemMenuPrefab.name = viewSystemMenuPrefab.name.Replace("(Clone)", "");
            viewSystemMenuPrefab.transform.SetParent(this.transform);
            var viewSystemMenuLocal = viewSystemMenuPrefab.GetComponent<ViewSystemMenu>();
            viewSystemMenuLocal.Inject(this, scenarioSystem, agentSystem, worldStateSystem);
            m_menus.Add(viewSystemMenuLocal);
            viewSystemMenu = viewSystemMenuLocal;

            if (showDebugEntityData)
                StartCoroutine(UpdateEntityDebugData());
        }

        public override void SystemUpdate(float dt)
        {
            if (CurrentMouseButtonEventIsForGuiControls())
            {
                return; // Prevent mouse GUI click through
            }

            if (IsOn(ViewSystemConfigFlags.Unit_Selection))
            {
                // Test for agent selection
                if (agentSelector.isFinishedSelecting && agentSelector.enabled)
                {
                    IEnumerable<RideID> selectedAgents = agentSelector.PerformSelection();

                    List<RideID> deselectedAgents = new List<RideID>(agentSystem.GetSelectedAgents());

                    // Unselect the currently selected
                    agentSystem.SetAgentsSelected(deselectedAgents, false);

                    // Select the new ones
                    grouperSystem.SetGroup(selectedAgents);
                    agentSystem.SetAgentsSelected(selectedAgents, true);

                    deselectedAgents.RemoveAll(i => (new List<RideID>(agentSystem.GetSelectedAgents())).Contains(i));

                    worldStateSystem.DispatchEvent<SelectionEvent>(WorldEvent.entitySelectionChanged, new SelectionEvent(deselectedAgents.ToArray(), agentSystem.GetSelectedAgents()));
                }

                // Test for group creation and selection
                if (groupIdCreatorSystem.IsGroupCreationTriggered())
                {
                    grouperSystem.SaveGroup(groupIdCreatorSystem.CreateGroupId());
                }
                if (groupIdCreatorSystem.IsGroupSelectionTriggered())
                {
                    // unselect all current
                    agentSystem.SetAgentsSelected(grouperSystem.GetCurrentGroup(), false);

                    grouperSystem.SetGroup(groupIdCreatorSystem.GetGroupSelection());
                    agentSystem.SetAgentsSelected(grouperSystem.GetCurrentGroup(), true);
                }
            }
        }

        public override void SystemShutdown()
        {
            base.SystemShutdown();
            grouperSystem.SystemInit();
            agentSelector.SystemShutdown();
            entitySelector.SystemShutdown();
        }

        T GetComponentFromMenu<T>()
        {
            T comp = default;
            foreach (var imenu in m_menus)
            {
                MenuMono menu = imenu as MenuMono;
                comp = menu.GetComponent<T>();
                if (!EqualityComparer<T>.Default.Equals(comp, default)) return comp;  //if (comp != default) return comp;  // https://developercommunity.visualstudio.com/content/problem/744160/1640-preview-does-not-compile-x-default.html
            }
            return comp;
        }

        bool IsOn(ViewSystemConfigFlags flags)
        {
            return (config.flags & flags) == flags;
        }

        void onSelectionChanged(WorldEventMarker sim, SelectionEvent e)
        {
            grouperSystem.SetGroup(e.selected);

            if (showDebugEntityData)
            {
                foreach (RideID deselectedId in e.deselected)
                    RemoveEntityDebugData(deselectedId);
                foreach (RideID selectedId in e.selected)
                    AddNewEntityDebugData(selectedId);
            }
        }

        void onEntityDataUpdate(WorldEventMarker sim, EntityDataEvent e)
        {
            if (debugEntityCardList.ContainsKey(e.entityID))
            {
                foreach(EntityDataEvent.EntityDataPoint dataPoint in e.dataPoints)
                    debugEntityCardList[e.entityID].UpdateDataPoint(dataPoint.category, dataPoint.value);
            }
        }

        IEnumerator UpdateEntityDebugData()
        {
            while (true)
            {
                foreach(RideID entityId in debugEntityCardList.Keys)
                    Globals.api.worldStateSystem.DispatchEvent<EntityEvent>(WorldEvent.entityDataRequest, new EntityEvent(entityId));
                yield return new WaitForSeconds(0.1f);
            }
        }

        Canvas cnv = null;
        void AddNewEntityDebugData(RideID entityId)
        {
            if (cnv == null)
            {
                cnv = transform.GetComponentInChildren<Canvas>();
                if (cnv == null)
                    cnv = FindFirstObjectByType<Canvas>();

                if (cnv == null)
                {
                    GameObject cnvObj = new GameObject();
                    cnv = cnvObj.AddComponent<Canvas>();
                }
            }

            if (!debugEntityCardList.ContainsKey(entityId))
            {
                EntityDebugUICard newDebugDataCard = Instantiate(debugEntityUICard, cnv.transform);
                newDebugDataCard.AttachedGameObject = Globals.api.componentSystem.GetComponent<Transform>(entityId).gameObject;
                debugEntityCardList.Add(entityId, newDebugDataCard);

                newDebugDataCard.UpdateDataPoint("RideID", entityId.ToString());
            }
        }

        void RemoveEntityDebugData(RideID entityId)
        {
            if(debugEntityCardList.ContainsKey(entityId))
            {
                Destroy(debugEntityCardList[entityId].gameObject);
                debugEntityCardList.Remove(entityId);
            }
        }

        #region Hack for GUI mouse click through issue
        /// <remarks>
        /// Use this property to prevent GUI click through.
        /// </remarks>
        private bool mouseButtonDownOnGuiControl => GUIUtility.hotControl != 0;
        private bool m_bypassNextMouseButtonUpEvent = false;

        /// <remarks>
        /// Unity default GUI cannot stop click through by itself. We have to use this method to detect.
        /// </remarks>
        private bool CurrentMouseButtonEventIsForGuiControls()
        {
            if (mouseButtonDownOnGuiControl)
            {
                m_bypassNextMouseButtonUpEvent = true;
                return true;//prevent mouse DOWN GUI click through
            }
            if (m_bypassNextMouseButtonUpEvent && Input.GetMouseButtonUp(0)) //hard code button id to 0, because related field in AgentSelectorInputSystem is not accessible
            {
                m_bypassNextMouseButtonUpEvent = false;
                return true;//prevent mouse UP GUI click through
            }
            return false;
        }
        #endregion
    }
}
