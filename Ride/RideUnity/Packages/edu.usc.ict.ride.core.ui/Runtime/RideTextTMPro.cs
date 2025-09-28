using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ride.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class RideTextTMPro : RideText
    {
        public TMP_Text m_label;

        public override string text { get => m_label.text; set => m_label.text = value; }
        public TextAlignmentOptions alignment { get => m_label.alignment; set => m_label.alignment = value; }
        public float fontSize { get => m_label.fontSize; set => m_label.fontSize = value; }
        public RideColor color { get => m_label.color; set => m_label.color = value; }
        public override bool isInteractable { get; set; }

        public override float RecalculateHeight()
        {
            float prefHeight = GetComponent<TextMeshProUGUI>().textBounds.size.y;
            LayoutElement element = GetComponent<LayoutElement>();
            if (element != null )
            {
                element.preferredHeight = prefHeight;
            }
            return prefHeight;
        }

    }
}
