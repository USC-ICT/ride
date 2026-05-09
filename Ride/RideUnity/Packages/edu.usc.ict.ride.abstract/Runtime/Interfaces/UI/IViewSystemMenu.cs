using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    /// <summary>
    /// Exposes the built-in UI messaging surface used by the Ride view system to present
    /// temporary informational text to the user.
    /// </summary>
    public interface IViewSystemMenu
    {
        /// <summary>
        /// Displays a temporary user message using the default message styling.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        void DisplayUserMessage(string text);

        /// <summary>
        /// Displays a temporary user message using the supplied color and timing values.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        /// <param name="color">The text color to use while the message is visible.</param>
        /// <param name="displayTime">The total time in seconds that the message should remain active.</param>
        /// <param name="fadeInTime">The time in seconds spent fading the message in.</param>
        /// <param name="fadeOutTime">The time in seconds spent fading the message out.</param>
        void DisplayUserMessage(string text, RideColor color, float displayTime = 1, float fadeInTime = 0.1f, float fadeOutTime = 0.2f);
    }
}
