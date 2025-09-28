using UnityEngine;
using TMPro;

namespace Ride.UI
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class RideDropdownTMPro : RideDropdown
    {
        public TMP_Dropdown m_dropdown;
        public override int selection { get => m_dropdown.value; set => m_dropdown.value = value; }
        public override int numItems => m_dropdown.options.Count;
        public override bool isInteractable { get => m_dropdown.interactable; set => m_dropdown.interactable = value; }
        public override string text { get => m_dropdown.captionText.text; set => m_dropdown.captionText.text = value; }

        public override void AddItem(string item)
        {
            m_dropdown.options.Add(new TMP_Dropdown.OptionData() { text = item });
        }

        public override void RemoveItem(int index)
        {
            m_dropdown.options.RemoveAt(index);
        }

        public override void ClearItems()
        {
            m_dropdown.ClearOptions();
        }
    }
}
