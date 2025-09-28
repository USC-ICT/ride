using Ride.Entities;

namespace Ride.UI
{
    public interface IAgentDataDisplayer
    {
        /// <summary>
        /// Display the agents data
        /// </summary>
        /// <param name="agent"></param>
        void Display(RideID agent);

        /// <summary>
        /// Clear all data from visibility
        /// </summary>
        void Clear();

        /// <summary>
        /// Add text to be displayed
        /// </summary>
        /// <param name="textId"></param>
        /// <param name="text"></param>
        /// <param name="forceUpdate"></param>
        void AddDisplayText(string textId, string text, bool forceUpdate = true);

        /// <summary>
        /// Hide the display of the data
        /// </summary>
        void Hide();

        void Refresh();
    }
}