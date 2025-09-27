using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    [RequireComponent(typeof(Button))]
    public class RideButtonUGUI : RideButton
    {
        public Button m_button;

        public override bool isInteractable { get => m_button.interactable; set => m_button.interactable = value; }

        public UnityEngine.UI.Button.ButtonClickedEvent onClick => (m_button != null) ? m_button.onClick : null;

        private void OnDestroy()
        {
            if (m_button != null)
                m_button.onClick.RemoveAllListeners();
        }
    }
}
