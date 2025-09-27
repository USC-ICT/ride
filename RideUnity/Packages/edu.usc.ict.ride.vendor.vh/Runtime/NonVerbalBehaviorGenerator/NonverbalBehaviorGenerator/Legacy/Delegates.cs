#nullable disable
using System.Collections.Generic;

namespace NonverbalBehaviorGenerator.Legacy
{
    internal delegate void StringParameterDelegate(string value);
    internal delegate void SaliencyMapParameterDelegate(List<SaliencyItem> salMap, int randRange, int keywordRange);
    internal delegate void MessageHandlerDelegate();
    internal delegate void NVBGSetOptionCallback(string _charName, string _option, string _optionValue);
    internal delegate void IdleTimerValueChangeCallback(string _characterName, string _value);
    internal delegate void IdleTimerEnableCallback(string _characterName, bool _enable);
    internal delegate void SaccadeCheckBox(bool _checked);
    internal delegate void GUILabelUpdate(string _characterName, string type, bool _checked);
    internal delegate void RefreshGUI(string _characterName);
}
