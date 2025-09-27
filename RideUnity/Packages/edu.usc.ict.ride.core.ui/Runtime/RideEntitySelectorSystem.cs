using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride.WorldState;

namespace Ride.UI
{
    public class RideEntitySelectorSystem : RideSystemMonoBehaviour, ISelector<RideID>
    {
        private bool selecting = false;
        private int selectionButtonId = 0;

        private RideVector2 startSelectionPos;
        private RideVector2 endSelectionPos;
        private Rect m_selectionArea = new Rect();

        Dictionary<RideID, RideEntitySelector> entitySelectors = new Dictionary<RideID, RideEntitySelector>();

        private Camera cam
        {
            get
            {
                return (overrideCamera != null) ? overrideCamera : Camera.main;
            }
        }

        public ISelectorDisplay selectorDisplayMenu { get; set; }


        public override void SystemAwake()
        {
            base.SystemAwake();

            Globals.api.worldStateSystem?.AddListener<EntityCreatedEvent>(WorldEvent.entityDataCreated, OnEntityDataCreated);
            Globals.api.worldStateSystem?.AddListener<EntityEvent>(WorldEvent.entityDataDestroyed, OnEntityDataDestroyed);
        }

        public override void SystemInit()
        {
            base.SystemInit();

            if (selectorDisplayMenu == null)
                selectorDisplayMenu = Object.FindFirstObjectByType<RectSelectionDisplay>();
        }

        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            if (!isEnabled || selectorDisplayMenu == null)
                return;

            if (Globals.api.inputSystem.GetMouseButtonDown(0))
                StartSelection();
            else if (Globals.api.inputSystem.GetMouseButtonUp(0))
                EndSelection();

            if (selecting)
                selectorDisplayMenu?.SetSelectorDisplaySize(CalculateSelectionArea(startSelectionPos, Input.mousePosition));
            else
                selectorDisplayMenu?.SetSelectorDisplaySize(Rect.zero);
        }

        void OnEntityDataCreated(WorldEventMarker marker, EntityCreatedEvent e)
        {
            if (e.entityObjData is RideEntitySelectorData rideEntitySelectorData)
            {
                RideEntitySelector rideEntitySelector = Globals.api.componentSystem.AddComponent<RideEntitySelector>(e.entityID);
                rideEntitySelector.id = e.entityID;
                rideEntitySelector.selectorDisplay = Object.Instantiate(rideEntitySelectorData.selectorDisplayPrefab, rideEntitySelector.transform);
                rideEntitySelector.selectorDisplay.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (rideEntitySelectorData.selectorScale > 0.0f)
                    rideEntitySelector.selectorDisplay.transform.localScale = new Vector3(rideEntitySelectorData.selectorScale, rideEntitySelectorData.selectorScale, rideEntitySelectorData.selectorScale);
                rideEntitySelector.selectorDisplay.transform.localPosition = rideEntitySelectorData.selectorOffset;

                BoxCollider entityCollider = Globals.api.componentSystem.GetComponent<BoxCollider>(e.entityID);
                if (Globals.api.componentSystem.GetComponent<BoxCollider>(e.entityID) == null)
                    entityCollider = Globals.api.componentSystem.AddComponent<BoxCollider>(e.entityID);
                entityCollider.center = rideEntitySelectorData.colliderCenter.ToVector3();
                entityCollider.size = rideEntitySelectorData.colliderSize.ToVector3();

                rideEntitySelector.SelectorEnable = false;

                entitySelectors.Add(e.entityID, rideEntitySelector);
            }
        }

        void OnEntityDataDestroyed(WorldEventMarker marker, EntityEvent e)
        {
            entitySelectors.Remove(e.entityID);
        }

        public bool isSelecting => selecting;

        public bool isFinishedSelecting => Input.GetMouseButtonUp(selectionButtonId);

        public Camera overrideCamera { get; set; }
        public bool isEnabled { get; set; } = true;

        public IEnumerable<RideID> PerformSelection()
        {
            List<RideID> selections = new List<RideID>();
            selections.AddRange(PerformSelection(Globals.api.inputSystem.mousePosition));
            selections.AddRange(PerformSelection(m_selectionArea));
            return selections;
        }

        public IEnumerable<RideID> PerformSelection(RideVector3 selectionPoint)
        {
            if (cam == null) return new List<RideID>();

            Ray ray = cam.ScreenPointToRay(selectionPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                RideEntitySelector selector = hit.transform.GetComponent<RideEntitySelector>();
                if (selector != null)
                    return new RideID[] { selector.id };
            }

            return new List<RideID>();
        }

        public IEnumerable<RideID> PerformSelection(Rect selectionArea)
        {
            if (cam == null) return new List<RideID>();

            List<RideID> selectedEntities = new List<RideID>();
            foreach (RideID entityId in entitySelectors.Keys)
            {
                RideEntitySelector entitySelector = entitySelectors[entityId];
                RideVector2 screenPos = cam.WorldToScreenPoint(entitySelector.position.ToVector3());
                if (selectionArea.Contains(screenPos))
                    selectedEntities.Add(entityId);
            }

            return selectedEntities;
        }

        void StartSelection()
        {
            selecting = true;
            startSelectionPos = Globals.api.inputSystem.mousePosition;
        }

        void EndSelection()
        {
            selecting = false;
            endSelectionPos = Globals.api.inputSystem.mousePosition;

            UpdateSelection();
        }

        Rect CalculateSelectionArea(RideVector2 startPos, RideVector2 endPos)
        {
            if (startPos.x > endPos.x) RideUtils.Swap(ref startPos.x, ref endPos.x);
            if (startPos.y > endPos.y) RideUtils.Swap(ref startPos.y, ref endPos.y);

            m_selectionArea.Set(startPos.x, startPos.y, Mathf.Max(Mathf.Abs(endPos.x - startPos.x), 1), Mathf.Max(Mathf.Abs(endPos.y - startPos.y), 1));
            return m_selectionArea;
        }

        void UpdateSelection()
        {
            List<RideID> deselectedEntities = new List<RideID>(DeselectAll());

            List<RideID> selectedEntities;

            if (startSelectionPos == endSelectionPos)
                selectedEntities = new List<RideID>(PerformSelection(startSelectionPos));
            else
            {
                m_selectionArea = CalculateSelectionArea(startSelectionPos, endSelectionPos);
                selectedEntities = new List<RideID>(PerformSelection(m_selectionArea));
            }

            foreach (RideID entityId in selectedEntities)
            {
                if (entitySelectors.ContainsKey(entityId))
                    entitySelectors[entityId].SelectorEnable = true;
            }

            deselectedEntities.RemoveAll(i => selectedEntities.Contains(i));
            Globals.api.worldStateSystem.DispatchEvent<SelectionEvent>(WorldEvent.entitySelectionChanged, new SelectionEvent(deselectedEntities.ToArray(), selectedEntities.ToArray()));
        }

        IEnumerable<RideID> DeselectAll()
        {
            List<RideID> deselectedEntities = new List<RideID>();
            foreach (RideEntitySelector entity in entitySelectors.Values)
            {
                if (entity.SelectorEnable)
                    deselectedEntities.Add(entity.id);
                entity.SelectorEnable = false;
            }

            return deselectedEntities;
        }

        Rect CalculateSelectionArea(Vector2 startPos, Vector2 endPos)
        {
            if (startPos.x > endPos.x) RideUtils.Swap(ref startPos.x, ref endPos.x);
            if (startPos.y > endPos.y) RideUtils.Swap(ref startPos.y, ref endPos.y);

            m_selectionArea.Set(startPos.x, startPos.y, Mathf.Max(Mathf.Abs(endPos.x - startPos.x), 1), Mathf.Max(Mathf.Abs(endPos.y - startPos.y), 1));
            return m_selectionArea;
        }

        public void SelectEntities(RideID[] entities)
        {
            List<RideID> deselectedEntities = new List<RideID>(DeselectAll());
            foreach (RideID entityId in entities)
            {
                if (entitySelectors.ContainsKey(entityId))
                    entitySelectors[entityId].SelectorEnable = true;
            }

            Globals.api.worldStateSystem.DispatchEvent<SelectionEvent>(WorldEvent.entitySelectionChanged, new SelectionEvent(deselectedEntities.ToArray(), entities));
        }
    }
}
