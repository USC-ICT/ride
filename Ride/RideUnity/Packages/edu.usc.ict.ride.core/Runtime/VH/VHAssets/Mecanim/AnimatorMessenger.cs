using UnityEngine;

namespace VHAssets
{
    public class AnimatorMessenger : MonoBehaviour
    {
        private MecanimCharacter m_Character;

        #region Functions
        public void SetMessengerTarget(MecanimCharacter character) => m_Character = character;

        void OnAnimatorIK(int layer)
        {
            if (m_Character != null)
                m_Character.UpdateGaze();
        }
        #endregion
    }
}
