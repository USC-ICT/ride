using System;
using UnityEngine.UI;

namespace Ride.UI
{
    public class RideSlider : RideUIElement, ISlider, IText
    {
        public Slider m_slider;
        public RideText m_text;

        public override bool isInteractable { get => m_slider.interactable; set => m_slider.interactable = value; }
        public float normalizedValue { get => m_slider.normalizedValue; set => m_slider.normalizedValue = value; }
        public float value { get => m_slider.value; set => m_slider.value = value; }
        public string text
        {
            get { return m_text != null ? m_text.text : ""; }
            set
            {
                if (m_text != null)
                {
                    m_text.text = value;
                }
            }
        }

        public float minValue { get => m_slider.minValue; set => m_slider.minValue = value; }
        public float maxValue { get => m_slider.maxValue; set => m_slider.maxValue = value; }

        public void AddOnValueChanged(Action<float> action)
        {
            m_slider.onValueChanged.AddListener((value) => action(value));
        }
    }
}
