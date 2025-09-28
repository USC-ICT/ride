using UnityEngine;

namespace Ride.UI
{
    public abstract class RideButton : RideMonoBehaviour, IButton, IText
    {
        public RideText m_label;
        public abstract bool isInteractable { get; set; }
        public string text { get => m_label.text; set => m_label.text = value; }

        public void Show(bool show) { gameObject.SetActive(show); }

        public float RecalculateHeight()
        {
            return m_label.RecalculateHeight();
        }
    }
}
