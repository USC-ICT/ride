using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ride.UI
{
    /// <summary>
    /// Menu which displays entity data in 3D near the location of the entity being represented
    /// </summary>
    public class EntityDisplay3D : MenuUnity, IAgentDataDisplayer
    {
        /// <summary>
        /// The camera that will allow for ray casting agents
        /// </summary>
        [Tooltip("The camera that will allow for ray casting agents")]
        public Camera m_raycastCamera;

        /// <summary>
        /// UI Canvas of the display
        /// </summary>
        [Tooltip("UI Canvas of the display")]
        public GameObject m_canvas;

        /// <summary>
        /// UI element that displays behind the data
        /// </summary>
        [Tooltip("UI element that displays behind the data UI elements")]
        public RectTransform m_displayPanel;

        /// <summary>
        /// Prefab used for the individual lines of data text on the display panel
        /// </summary>
        [Tooltip("Prefab used for the individual lines of data text on the display panel")]
        public TextMeshProUGUI m_dataLinePrefab;

        /// <summary>
        /// The layout which aligns the lines of text displaying the data
        /// </summary>
        [Tooltip("The layout which aligns the lines of text displaying the data")]
        public LayoutGroup m_dataLayout;

        /// <summary>
        /// Number of seconds to scale the menu when showing or hiding
        /// </summary>
        [Tooltip("Number of seconds to scale the menu when showing or hiding")]
        public float m_displayTime = 0.1f;

        /// <summary>
        /// Positional displacement from the represented entity's position
        /// </summary>
        [Tooltip("Positional displacement from the represented entity's position")]
        public Vector3 m_offsetFromEntity;

        RideVector2 m_targetSize;
        protected bool m_forceResize = true;
        protected bool m_ClearOnDisplay = true;


        Dictionary<string, TextMeshProUGUI> m_dataLines = new();
        protected RideID currAgent;


        protected override void Start()
        {
            base.Start();

            // Hide at start
            m_dataLinePrefab.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the display for the specified agent and rebuilds the visible data lines around that agent.
        /// </summary>
        /// <param name="agent">The agent whose data should be displayed.</param>
        virtual public void Display(RideID agent)
        {
            Show();

            if (m_ClearOnDisplay)
                Clear();

            currAgent = agent;

            m_canvas.transform.position = (Vector3)agentSystem.GetAgentPosition(agent) + m_offsetFromEntity;

            // Add the lines of data to be displayed
            m_forceResize = false;
            UpdateDefaults(agent);
            m_forceResize = true;

            StartCoroutine(Scale(m_displayTime, m_targetSize));
        }

        /// <summary>
        /// Updates the default set of data entries displayed for the supplied agent.
        /// Derived classes can override this to populate menu-specific values.
        /// </summary>
        /// <param name="agent">The agent being displayed.</param>
        virtual protected void UpdateDefaults(RideID agent) { }

        /// <summary>
        /// Smoothly resizes the display panel toward the requested target size.
        /// </summary>
        /// <param name="growTime">Unused legacy duration parameter retained for call compatibility.</param>
        /// <param name="target">The target panel size.</param>
        /// <returns>An enumerator for the resize coroutine.</returns>
        protected IEnumerator Scale(float growTime, Vector2 target)
        {
            Vector2 vel = Vector2.zero;
            while (Vector2.Distance(m_displayPanel.sizeDelta, target) > 1f)
            {
                m_displayPanel.sizeDelta = Vector2.SmoothDamp(m_displayPanel.sizeDelta, target, ref vel, 0.1f);
                yield return new WaitForEndOfFrame();
            }

            if (target == Vector2.zero)
                m_canvas.SetActive(false);
        }

        /// <summary>
        /// Clears all current data lines and resets the display size.
        /// </summary>
        public void Clear()
        {
            StopAllCoroutines();
            m_displayPanel.sizeDelta = m_targetSize = RideVector2.zero;
            foreach (var kvp in m_dataLines)
                Destroy(kvp.Value.gameObject);

            m_dataLines.Clear();
        }

        /// <summary>
        /// Creates and adds a new data line to the display panel.
        /// </summary>
        /// <param name="id">The unique identifier for the line.</param>
        /// <param name="line">The formatted text to display.</param>
        /// <returns>The instantiated text element.</returns>
        protected TextMeshProUGUI AddDataLine(string id, string line)
        {
            TextMeshProUGUI text = MenuUnity.AddToLayout(m_dataLayout, m_dataLinePrefab);
            text.name = id;
            text.text = line;
            text.gameObject.SetActive(true);
            text.ForceMeshUpdate();

            m_dataLines.Add(id, text);

            Refresh();

            return text;
        }

        /// <summary>
        /// Recalculates the target panel size based on the currently visible data lines.
        /// </summary>
        public virtual void Refresh()
        {
            foreach (var kvp in m_dataLines)
            {
                TextMeshProUGUI text = kvp.Value;

                if (m_targetSize.x < text.textBounds.size.x)
                    m_targetSize.x = text.textBounds.size.x + (m_dataLayout.padding.left + m_dataLayout.padding.right);                    

                m_targetSize.y = text.renderedHeight * (m_dataLines.Count) + m_dataLayout.padding.bottom + m_dataLayout.padding.top;
            }
        }

        /// <summary>
        /// Handles click-based display behavior and keeps the display positioned relative to the current agent.
        /// </summary>
        protected override void Update()
        {
            if (Input.GetMouseButtonDown(0) && !GUI.changed)
            {
                // Cast a ray from their mouse pos
                Ray r = m_raycastCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
                {
#if false
                    AgentMono agent = hit.collider.gameObject.GetComponent<AgentMono>();
                    if (agent != null)
                    {
                        // They hit an agent, display its info
                        Display(agent.id);
                    }
                    else
                    {
                        Hide();
                    }
#else
                    Debug.LogError($"EntityDisplay3D.Update() - TODO - Ride Refactor");
#endif
                }
            }

            // Update position
            if (currAgent != RideID.Null)
            {
                m_canvas.transform.position = (Vector3)agentSystem.GetAgentPosition(currAgent) + m_offsetFromEntity;
                UpdateDefaults(currAgent);
            }
        }

        /// <summary>
        /// Updates the text for an existing data line.
        /// </summary>
        /// <param name="id">The identifier of the line to update.</param>
        /// <param name="value">The new value to display.</param>
        protected void UpdateText(string id, object value)
        {
            if (m_dataLines.ContainsKey(id))
                m_dataLines[id].text = FormatTextLine(id, value.ToString());
        }

        /// <summary>
        /// Adds or updates a named display line and optionally animates the panel to its new size.
        /// </summary>
        /// <param name="id">The identifier of the line.</param>
        /// <param name="text">The value text to display.</param>
        /// <param name="forceUpdate">True to animate the panel resize immediately after adding the line.</param>
        virtual public void AddDisplayText(string id, string text, bool forceUpdate = true)
        {
            if (!m_dataLines.ContainsKey(id))
            {
                AddDataLine(id, FormatTextLine(id, text));
                if (forceUpdate)
                    StartCoroutine(Scale(m_displayTime, m_targetSize));
            }

            UpdateText(id, text);
        }

        /// <summary>
        /// Formats a display line from an identifier and text value.
        /// </summary>
        /// <param name="id">The label portion of the line.</param>
        /// <param name="text">The value portion of the line.</param>
        /// <returns>The formatted display text.</returns>
        virtual protected string FormatTextLine(string id, string text) => $"{id}: {text}";

        /// <summary>Hides the display by animating the panel closed and clearing the current agent reference.</summary>
        public override void Hide()
        {
            StartCoroutine(Scale(m_displayTime, Vector2.zero));
            currAgent = RideID.Null;
        }

        /// <summary>Shows the display canvas without rebuilding its contents.</summary>
        public override void Show() => m_canvas.SetActive(true);
    }
}
