using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    /// <summary>
    /// Provides the ability to select one or more agents by clicking and dragging the mouse over them (RTS style selection)
    /// </summary>
    public class RectSelectionDisplay : MenuUnity, ISelectorDisplay
    {
#pragma warning disable CS0649
        [Tooltip("Canvas root that contains the rectangle-selection UI visuals.")]
        [SerializeField] GameObject m_canvas;
        [Tooltip("Image used to render the drag-selection rectangle on screen.")]
        [SerializeField] Image m_selectionImage;
#pragma warning restore CS0649
        [Tooltip("Mouse button index used to start rectangle selection.")]
        public int m_selectionMouseButton = 1;

        /// <summary>
        /// Updates the selection rectangle to match the supplied screen-space area.
        /// </summary>
        /// <param name="area">The screen-space rectangle to display.</param>
        public void SetSelectorDisplaySize(Rect area)
        {
            m_selectionImage.rectTransform.anchoredPosition = new Vector2(area.x, (Screen.height - area.height - area.y) * -1);
            m_selectionImage.rectTransform.sizeDelta = new Vector2(area.width, area.height);
        }

        /// <summary>Shows the selection-rectangle canvas.</summary>
        public override void Show() => m_canvas.SetActive(true);

        /// <summary>Hides the selection-rectangle canvas.</summary>
        public override void Hide() => m_canvas.SetActive(false);
    }
}
