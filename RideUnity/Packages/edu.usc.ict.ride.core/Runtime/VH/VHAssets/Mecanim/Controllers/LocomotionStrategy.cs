using UnityEngine;

namespace VHAssets
{
public abstract class LocomotionStrategy : MonoBehaviour
{
    #region Constants
    //public delegate void OnReachedDestination(LocomotionController locomoter);
    //public delegate void OnReachingDestination(LocomotionController locomoter);
    //public delegate void OnPathingUpdate(LocomotionController controller);
    #endregion

    #region Variables
    LocomotionController m_LocomotionController;

    //OnReachedDestination m_OnReachedDestinationCB;
    //OnReachingDestination m_OnReachingDestinationCB;
    //OnPathingUpdate m_OnPathingUpdate;
    #endregion

    #region Properties
    public abstract float Speed { get; set; }
    public abstract float CurrentSpeed { get; }
    public abstract float TurnSpeed { get; set; }
    public abstract float StoppingDistance { get; set; }
    public abstract Vector3 AgentDestination { get; }
    public abstract bool IsPathPending { get; }

    
    protected LocomotionController LocoController {  get { return m_LocomotionController; } }
    #endregion

    #region Functions
    public void SetLocomotionController(LocomotionController loco)
    {
        m_LocomotionController = loco;
    }

    public void MoveTo(GameObject destination)
    {
        MoveTo(destination.transform.position);
    }

    //public void AddOnReachedDestinationCallback(OnReachedDestination cb)
    //{
    //    m_OnReachedDestinationCB += cb;
    //}

    //public void AddOnReachingDestinationCallback(OnReachingDestination cb)
    //{
    //    m_OnReachingDestinationCB += cb;
    //}

    //public void AddonPathingUpdateCallback(OnPathingUpdate cb)
    //{
    //    m_OnPathingUpdate += cb;
    //}

    public void MoveTo(Transform destination)
    {
        MoveTo(destination.position);
    }

    public virtual void UpdateLocomotion() { }

    public abstract void MoveTo(Vector3 destination);
    public abstract void Warp(Vector3 destination);

    public abstract float GetRemainingDistance();
    public abstract void ResetLocomotion();

    public abstract void SetPath(Vector3 start, Vector3 destination);

    public abstract void SetAgentUpdatePosition(bool update);
    public abstract void SetAgentUpdateRotation(bool update);
    public abstract void SetAgentNextPosition(Vector3 nextPos);
    #endregion
}
}
