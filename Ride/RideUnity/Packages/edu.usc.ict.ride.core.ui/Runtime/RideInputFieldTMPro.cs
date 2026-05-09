using UnityEngine;
using TMPro;

namespace Ride.UI
{
    [RequireComponent(typeof(TMPro.TMP_InputField))]
    public class RideInputFieldTMPro : RideInputField
    {
        public TMPro.TMP_InputField m_inputField;
        public override string text { get => m_inputField.text; set => m_inputField.text = value; }
        public override bool isInteractable { get => m_inputField.interactable; set => m_inputField.interactable = value; }
    }
}
