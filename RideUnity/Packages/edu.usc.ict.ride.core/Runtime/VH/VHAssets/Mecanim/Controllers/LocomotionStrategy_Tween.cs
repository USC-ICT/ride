using UnityEngine;

namespace VHAssets
{
public class LocomotionStrategy_Tween : LocomotionStrategy
{
    #region Constants
    public enum TweenType
    {
        Lerp,
        SmoothStep,
    }
    #endregion

    #region Variables
    [SerializeField]
    TweenType m_TweenType = TweenType.SmoothStep;

    [SerializeField]
    float m_MovementSpeed = 1;

    [SerializeField]
    float m_TurnSpeed = 90;

    [SerializeField]
    float m_StoppingDistance;

    bool m_IsPathing = false;
    Vector3 m_Destination;

    Vector3 m_StartingPos;
    Vector3 m_PreviousPosition;
    float m_TimePassed;
    float m_TimeToReachDestination = 1;
    float m_CurrentSpeed;
    #endregion

    #region Properties
    public override float Speed
    {
        get { return m_MovementSpeed; }
        set { m_MovementSpeed = value; }
    }

    public override float CurrentSpeed
    {
        get { return m_CurrentSpeed; }
    }

    public override float TurnSpeed
    {
        get { return m_TurnSpeed; }
        set { m_TurnSpeed = value; }
    }

    public override Vector3 AgentDestination
    {
        get { return m_Destination; }
    }

    public override bool IsPathPending
    {
        get {  return m_IsPathing; }
    }

    public override float StoppingDistance
    {
        get {  return m_StoppingDistance; }
        set { m_StoppingDistance = value; }
    }

    Vector3 AgentPosition
    {
        get { return LocoController.transform.position; }
        set { LocoController.transform.position = value; }
    }
    #endregion

    #region Functions
    void Start()
    {
    }

    public override void UpdateLocomotion()
    {
        base.UpdateLocomotion();

        m_CurrentSpeed = Mathf.Abs((AgentPosition - m_PreviousPosition).magnitude / Time.deltaTime);

        if (m_IsPathing)
        {
            AgentPosition = TweenPosition(m_TweenType, m_StartingPos, m_Destination, m_TimePassed / m_TimeToReachDestination);

            m_TimePassed += Time.deltaTime;
            if (m_TimePassed >= m_TimeToReachDestination)
            {
                // we reached our destination
                AgentPosition = m_Destination;
                LocoController.ReachedDestination();
            }
        }

        m_PreviousPosition = AgentPosition;
    }

    public override void MoveTo(Vector3 destination)
    {
        //StopAllCoroutines();
        m_Destination = destination;
        m_IsPathing = true;
        gameObject.SetActive(true);

        m_StartingPos = AgentPosition;
        m_TimePassed = 0;
        m_TimeToReachDestination = GetRemainingDistance() / Speed;

        //StartCoroutine(MoveToInternal(destination));
    }

    /*IEnumerator MoveToInternal(Vector3 destination)
    {
        float timeToReach = GetRemainingDistance() / Speed;
        float t = 0;
        Vector3 start = AgentPosition;

        while (t <= timeToReach)
        {
            AgentPosition = TweenPosition(m_TweenType, start, m_Destination, t / timeToReach);

            t += Time.deltaTime;
            LocoController.PathingUpdating();
            yield return new WaitForEndOfFrame();
        }

        AgentPosition = destination;
        m_IsPathing = false;
        LocoController.ReachedDestination();
    }*/

    Vector3 TweenPosition(TweenType tweenType, Vector3 start, Vector3 end, float t)
    {
        Vector3 interp = start;
        t = Mathf.Clamp01(t);
        switch (tweenType)
        {
            case TweenType.Lerp:
                interp = Vector3.Lerp(start, end, t);
                break;

            case TweenType.SmoothStep:
            default:
                interp.x = Mathf.SmoothStep(start.x, end.x, t);
                interp.y = Mathf.SmoothStep(start.y, end.y, t);
                interp.z = Mathf.SmoothStep(start.z, end.z, t);
                break;
        }
        return interp;
    }

    public override void Warp(Vector3 destination)
    {
        AgentPosition = destination;
    }

    public override float GetRemainingDistance()
    {
        return m_IsPathing ? Vector3.Distance(m_Destination, AgentPosition) : 0;
    }

    public override void ResetLocomotion()
    {
        m_IsPathing = false;
        m_StartingPos = AgentPosition;
        m_TimePassed = 0;
    }

    public override void SetAgentNextPosition(Vector3 nextPos)
    {
    }

    public override void SetAgentUpdatePosition(bool update)
    {
    }

    public override void SetAgentUpdateRotation(bool update)
    {
    }

    public override void SetPath(Vector3 start, Vector3 destination)
    {
        Warp(start);
        MoveTo(destination);
    }
    #endregion
}
}
