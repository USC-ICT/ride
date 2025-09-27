using UnityEngine;

namespace VHAssets
{
public class AnimatorMessenger : MonoBehaviour
{
    #region Variables
    MecanimCharacter m_Character;
    #endregion

    #region Functions
    public void SetMessengerTarget(MecanimCharacter character)
    {
        m_Character = character;
    }

    void OnStateIK()
    {
        //SendMessageUpwards("OnStateIK");
    }

    void OnAnimatorIK(int layer)
    {
        //SendMessageUpwards("OnAnimatorIK");
        if (m_Character != null)
        {
            m_Character.UpdateGaze();
        }
    }
    #endregion
}
}
