using System;

namespace Ride.UI
{
    /// <summary>
    /// Represents a user interface slider element
    /// |------[]------------|
    /// </summary>
    public interface ISlider : IUIElement
    {
        float normalizedValue { get; set; }
        float value { get; set; }
        float minValue { get; set; }
        float maxValue { get; set; }
        void AddOnValueChanged(Action<float> action);
    }
}
