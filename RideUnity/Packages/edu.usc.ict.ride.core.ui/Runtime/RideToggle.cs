using UnityEngine;

namespace Ride.UI
{
    public abstract class RideToggle : RideUIElement, IToggle
    {
        public RideText m_label;

        public abstract bool isOn { get; set; }
        public string text { get => m_label.text; set => m_label.text = value; }
    }
}
