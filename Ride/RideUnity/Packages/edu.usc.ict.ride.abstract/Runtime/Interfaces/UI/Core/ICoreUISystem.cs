using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    /// <summary>
    /// Creates core UI prefabs used by Ride for prompt menus, world-space labels, and billboard interaction widgets.
    /// </summary>
    public interface ICoreUISystem
    {
        /// <summary>
        /// Creates a new exit prompt menu instance.
        /// </summary>
        /// <returns>A newly instantiated exit prompt menu.</returns>
        IExitPromptMenu CreateExitPromptMenu();

        /// <summary>
        /// Creates a new world label billboard instance.
        /// </summary>
        /// <returns>A newly instantiated world label billboard.</returns>
        IWorldLabelBillboard CreateWorldLabelBillboard();

        /// <summary>
        /// Creates a new billboard icon toggle instance.
        /// </summary>
        /// <returns>A newly instantiated billboard icon toggle.</returns>
        IBillboardIconToggle CreateBillboardIconToggle();
    }
}
