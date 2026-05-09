using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    /// <summary>
    /// Represents a world-space text billboard that can display a label at a specified position.
    /// </summary>
    public interface IWorldLabelBillboard
    {
        /// <summary>Gets or sets the text displayed by the billboard.</summary>
        string Text { get; set; }

        /// <summary>Gets or sets the world-space position of the billboard.</summary>
        RideVector3 Position { get; set; }

        /// <summary>
        /// Shows or hides the billboard GameObject.
        /// </summary>
        /// <param name="value">True to activate the billboard; otherwise, false.</param>
        void SetActive(bool value);
    }
}
