using UnityEngine;
using UnityEngine.UI;


namespace Ride.UI
{
    /// <summary>
    /// Provides the ability to select one or more agents by clicking and dragging the mouse over them (RTS style selection)
    /// </summary>
    public class RectSelectionDisplay : MenuMono, ISelectorDisplay
    {
#pragma warning disable CS0649
        [SerializeField] GameObject m_canvas;
        [SerializeField] Image m_selectionImage;
#pragma warning restore CS0649
        public int m_selectionMouseButton = 1;

        public void SetSelectorDisplaySize(Rect area)
        {
            m_selectionImage.rectTransform.anchoredPosition = new Vector2(area.x, (Screen.height - area.height - area.y) * -1);
            m_selectionImage.rectTransform.sizeDelta = new Vector2(area.width, area.height);
        }

        public override void Show()
        {
            m_canvas.SetActive(true);
        }

        public override void Hide()
        {
            m_canvas.SetActive(false);
        }
    }
}
