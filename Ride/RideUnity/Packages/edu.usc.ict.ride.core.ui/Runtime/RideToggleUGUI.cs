using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    [RequireComponent(typeof(Toggle))]
    public class RideToggleUGUI : RideToggle
    {
        public Toggle m_toggle;

        public override bool isOn { get => m_toggle.isOn; set => m_toggle.isOn = value; }
        public override bool isInteractable { get => m_toggle.interactable; set => m_toggle.interactable = value; }

        public void AddOnValueChanged(System.Action<bool> action)
        {
            m_toggle.onValueChanged.AddListener((value) => action(value));
        }
    }
}
