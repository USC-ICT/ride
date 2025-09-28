using UnityEngine;

namespace VHAssets
{
public class ColliderCallbacks : MonoBehaviour
{
    #region Constants
    public delegate void OnTriggerEntered(GameObject callbackObject, Collider other);
    public delegate void OnTriggerStayed(GameObject callbackObject, Collider other);
    public delegate void OnTriggerExited(GameObject callbackObject, Collider other);
    #endregion

    #region Variables
    OnTriggerEntered m_Entered;
    OnTriggerStayed m_Stayed;
    OnTriggerExited m_Exited;
    #endregion

    #region Functions

    public void AddTriggerEnterCallback(OnTriggerEntered cb)
    {
        m_Entered += cb;
    }

    public void AddTriggerStayCallback(OnTriggerStayed cb)
    {
        m_Stayed += cb;
    }

    public void AddTriggerExitCallback(OnTriggerExited cb)
    {
        m_Exited += cb;
    }


    void OnTriggerEnter(Collider other)
    {
        m_Entered?.Invoke(gameObject, other);
    }

    void OnTriggerStay(Collider other)
    {
        m_Stayed?.Invoke(gameObject, other);
    }

    void OnTriggerExit(Collider other)
    {
        m_Exited?.Invoke(gameObject, other);
    }
    #endregion
}
}
