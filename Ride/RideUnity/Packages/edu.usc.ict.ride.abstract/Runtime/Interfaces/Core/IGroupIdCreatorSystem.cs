using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    public interface IGroupIdCreatorSystem  : IRideSystem
    {
        /// <summary>
        /// Tests if a group is ready to be created
        /// </summary>
        /// <returns>True if a group is ready to be created</returns>
        bool IsGroupCreationTriggered();

        /// <summary>
        /// Creates an id for a group
        /// </summary>
        /// <returns>The group id</returns>
        int CreateGroupId();

        /// <summary>
        /// Tests if a group selection was triggered
        /// </summary>
        /// <returns></returns>
        bool IsGroupSelectionTriggered();

        /// <summary>
        /// Gets the id of the selected group
        /// </summary>
        /// <returns>the group id</returns>
        int GetGroupSelection();
    }
}
