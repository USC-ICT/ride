using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Interface for displaying a selection UI
    /// </summary>
    public interface ISelectorDisplay
    {
        /// <summary>
        /// Sets the area of the selection display
        /// </summary>
        /// <param name="area"></param>
        void SetSelectorDisplaySize(Rect area);
    }
}
