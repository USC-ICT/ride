using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    public interface IViewSystemMenu
    {
        void DisplayUserMessage(string text);
        void DisplayUserMessage(string text, RideColor color, float displayTime = 1, float fadeInTime = 0.1f, float fadeOutTime = 0.2f);
    }
}
