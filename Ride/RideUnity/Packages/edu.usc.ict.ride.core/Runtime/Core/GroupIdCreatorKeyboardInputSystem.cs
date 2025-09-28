using UnityEngine;

namespace Ride.UI
{
    public class GroupIdCreatorKeyboardInputSystem : RideSystemMonoBehaviour, IGroupIdCreatorSystem
    {
        public int minGroupId = 0;
        public int maxGroupId = 9;
        public KeyCode minKey = KeyCode.Alpha0;
        public KeyCode maxKey = KeyCode.Alpha9;
#if UNITY_STANDALONE_OSX
        public KeyCode creatorKey = KeyCode.LeftCommand;
#else
        public KeyCode creatorKey = KeyCode.LeftShift;
#endif


        /// <inheritdoc/>
        public bool IsGroupCreationTriggered() => IsGroupCreatorKeyDown() && GetGroupIdKeyDown() != KeyCode.None;

        /// <inheritdoc/>
        public int CreateGroupId() => ConvertKeyPressToGroupId();

        /// <inheritdoc/>
        public bool IsGroupSelectionTriggered() => !IsGroupCreatorKeyDown() && GetGroupIdKeyDown() != KeyCode.None;

        /// <inheritdoc/>
        public int GetGroupSelection() => ConvertKeyPressToGroupId();


        private int ConvertKeyPressToGroupId()
        {
            int id = GetGroupIdKeyDown() - minKey + minGroupId;
            return Mathf.Clamp(id, minGroupId, maxGroupId);
        }

        private KeyCode GetGroupIdKeyDown()
        {
            for (KeyCode i = minKey; i <= maxKey; i++)
                if (Input.GetKeyDown(i))
                    return i;

            return KeyCode.None;
        }

        private bool IsGroupCreatorKeyDown() => Input.GetKey(creatorKey);
    }
}
