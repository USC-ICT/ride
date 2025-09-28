using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride.Entities;

// DEPRECATED
namespace Ride.UI
{
    public class AgentSelectorInputSystem : RideSystemMonoBehaviour, ISelector<RideID>
    {
        RideVector2 m_startSelectionMousePos;
        bool m_isSelectionActive;
        Rect m_selectionArea = new Rect();
        Camera cam
        {
            get
            {
                return (overrideCamera != null) ? overrideCamera : Camera.main;
            }
        }
        int selectionButtonId { get; set; }

        public ISelectorDisplay selectorDisplayMenu { get; set; }
        public Camera overrideCamera { get; set; }

        public bool isSelecting => m_isSelectionActive;

        public bool isFinishedSelecting => Input.GetMouseButtonUp(selectionButtonId);

        public bool isEnabled { get; set; } = true;


        override public void SystemInit()
        {
            base.SystemInit();
            selectionButtonId = 0;
        }

        override public void SystemUpdate(float dt)
        {
            if (!isEnabled)
            {
                //m_isSelectionActive = false;
                //m_selectionArea = Rect.zero;
                //m_selectorDisplayMenu?.SetSelectorDisplaySize(Rect.zero);
                return;
            }

            if (Input.GetMouseButtonDown(selectionButtonId))
            {
                // start
                m_isSelectionActive = true;
                m_startSelectionMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(selectionButtonId))
            {
                // drag
                m_selectionArea = CalculateSelectionArea(m_startSelectionMousePos, Input.mousePosition);
                selectorDisplayMenu?.SetSelectorDisplaySize(m_selectionArea);
            }
            else if (isFinishedSelecting)
            {
                // end
                m_isSelectionActive = false;
                m_selectionArea = CalculateSelectionArea(m_startSelectionMousePos, Input.mousePosition);
                selectorDisplayMenu?.SetSelectorDisplaySize(Rect.zero);
            }
        }

        Rect CalculateSelectionArea(Vector2 startPos, Vector2 endPos)
        {
            if (startPos.x > endPos.x) RideUtils.Swap(ref startPos.x, ref endPos.x);
            if (startPos.y > endPos.y) RideUtils.Swap(ref startPos.y, ref endPos.y);

            m_selectionArea.Set(startPos.x, startPos.y, Mathf.Max(Mathf.Abs(endPos.x - startPos.x), 1), Mathf.Max(Mathf.Abs(endPos.y - startPos.y), 1));
            return m_selectionArea;
        }

        public IEnumerable<RideID> PerformSelection()
        {
            List<RideID> selections = new List<RideID>();
            selections.AddRange(PerformSelection(Input.mousePosition));
            selections.AddRange(PerformSelection(m_selectionArea));
            return selections;
        }

        public IEnumerable<RideID> PerformSelection(RideVector3 selectionPoint)
        {
            if (cam == null) return new List<RideID>();

            Ray ray = cam.ScreenPointToRay(selectionPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
#if false
                AgentMono agent = hit.transform.GetComponent<AgentMono>();
                if (agent != null)
                {
                    return new RideID[] { agent.id };
                }
#else
                Debug.LogError($"AgentSelectorInputSystem.PerformSelection() - TODO - Ride Refactor");
#endif
            }
            return new List<RideID>();
        }

        public IEnumerable<RideID> PerformSelection(Rect selectionArea)
        {
            if (cam == null) return new List<RideID>();
            //Debug.Log(selectionArea);
            IEnumerable<RideID> agents = Globals.api.scenarioSystem.GetAgents();
            List<RideID> selectedAgents = new List<RideID>();
            foreach (RideID agent in agents)
            {
                if (!Globals.api.gameObjectSystem.Exists(agent))
                    continue;

                RideVector2 screenPos = cam.WorldToScreenPoint(Globals.api.agentSystem.GetAgentPosition(agent));
                //Debug.Log("Agent screen pos: " + screenPos);
                if (selectionArea.Contains(screenPos))
                {
                    selectedAgents.Add(agent);
                }
            }
            return selectedAgents;
        }

        public void SelectEntities(RideID[] entities)
        {
            throw new System.NotImplementedException();
        }
    }
}
