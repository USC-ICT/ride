using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// https://issuetracker.unity3d.com/issues/serializedfield-fields-produce-field-is-never-assigned-to-dot-dot-dot-warning
// https://forum.unity.com/threads/warning-cs0649-not-suppressed-properly-when-field-is-marked-as-serializefield.556009/#post-3954073
#pragma warning disable 0649


namespace VHAssets
{
public class VHWayPointNavigator : MonoBehaviour
{
    #region Constants
    public enum PathLoopType
    {
        // when you reach the end of a path...
        Loop,       // return to the path start point and do it again
        PingPong,   // go back the way you came
        Stop,       // just stop once you've reached the final waypoint
    }

    public enum PathFollowingType
    {
        // how you move along the path
        Lerp,
        SmoothStep,
    }

    public delegate void OnPathCompleted(List<Vector3> pathFollowed);
    public delegate void OnWayPointReached(VHWayPointNavigator navaigator, Vector3 wp, int wpId, int totalNumWPs);
    #endregion

    #region Variables
    [SerializeField] float m_Speed = 10;
    [SerializeField] float m_AngularVelocity = 90;
    [SerializeField] bool m_TurnTowardsTargetPosition = true;
    [SerializeField] bool m_IgnoreHeight = false;
    [SerializeField] bool m_ImmediatelyStartPathing = true;
    [SerializeField] PathLoopType m_LoopType = PathLoopType.Stop;
    [SerializeField] PathFollowingType m_FollowingType = PathFollowingType.Lerp;
    [SerializeField] GameObject m_Pather;
    [SerializeField] GameObject[] WayPoints;

    List<Vector3> m_WayPoints = new List<Vector3>();
    int m_nPrevWP = -1; // the waypoint that you're coming from
    int m_nNextWP = 0; // the waypoint that you are moving towards
    float m_fTimeToReachTarget;
    float m_fCurrentTime = 0;
    bool m_bInReverse = false;
    bool m_bIsPathing = false;
    //Vector3 m_PathDirection = new Vector3();
    Coroutine m_turnCoroutine;

    protected OnPathCompleted m_PathCompletedCallback;
    protected OnWayPointReached m_WayPointReachedCallback;
    #endregion

    #region Properties
    public Vector3 PreviousPosition
    {
        get { return m_WayPoints[m_nPrevWP]; }
    }

    public Vector3 TargetPosition
    {
        get { return m_WayPoints[m_nNextWP]; }
    }

    public int PrevWP
    {
        get { return m_nPrevWP; }
    }

    public int NextWP
    {
        get { return m_nNextWP; }
    }

    public bool IsPathing
    {
        get { return m_bIsPathing; }
    }

    public int NumWayPoints
    {
        get { return m_WayPoints.Count; }
    }

    public bool TurnTowardsTargetPosition
    {
        get { return m_TurnTowardsTargetPosition; }
        set { m_TurnTowardsTargetPosition = value; }
    }

    public bool ImmediatelyStartPathing
    {
        get { return m_ImmediatelyStartPathing; }
        set { m_ImmediatelyStartPathing = value; }
    }

    public GameObject Pather
    {
        get { return m_Pather; }
        set { m_Pather = value; }
    }

    public float AngularVelocity
    {
        get { return m_AngularVelocity; }
        set { m_AngularVelocity = value; }
    }

    public Vector3 TurnTarget
    {
        get { return m_IgnoreHeight ? new Vector3(TargetPosition.x, m_Pather.transform.position.y, TargetPosition.z) : TargetPosition; }
    }

    public PathFollowingType FollowingType
    {
        get { return m_FollowingType; }
        set { m_FollowingType = value; }
    }
    #endregion

    #region Functions

    public void SetIsPathing(bool isPathing)
    {
        if (isPathing && m_LoopType == PathLoopType.Stop
            && (m_nPrevWP >= m_WayPoints.Count || m_nNextWP >= m_WayPoints.Count))
        {
            // they've already completed their path, so there's nowhere to path to
            Debug.Log("Can't start moving again. You've completed your path and your loop type is \'Stop\'");
            return;
        }

        m_bIsPathing = isPathing;
    }

    public void Start()
    {
        if (m_Pather == null)
        {
            m_Pather = gameObject;
        }

        if (m_ImmediatelyStartPathing)
        {
            NavigatePath(true);
        }
    }

    List<Vector3> ConvertWayPointGOsToPositions(GameObject[] waypoints)
    {
        List<Vector3> waypointPositions = new List<Vector3>();
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypointPositions.Add(waypoints[i].transform.position);
        }
        return waypointPositions;
    }

    public void ResetPathNavigator()
    {
        m_nPrevWP = -1;
        m_nNextWP = 0;
        m_WayPoints.Clear();
    }

    public void NavigatePath(bool forcePositionToFirstPoint)
    {
        NavigatePath(ConvertWayPointGOsToPositions(WayPoints), forcePositionToFirstPoint);
    }

    /// <summary>
    /// Start navigating the given waypoints
    /// </summary>
    /// <param name="wayPoints"></param>
    public void NavigatePath(List<Vector3> wayPoints, bool forcePositionToFirstPoint)
    {
        if (wayPoints == null || wayPoints.Count < 2)
        {
            Debug.LogError("Bad path passed into NavigatePath");
            return;
        }

        if (forcePositionToFirstPoint)
        {
            m_Pather.transform.position = wayPoints[0];
            m_Pather.transform.forward = (wayPoints[1] - wayPoints[0]).normalized;
        }

        SetPath(wayPoints, true);

        m_nPrevWP = -1;
        m_nNextWP = 0;

        SetIsPathing(true);
        MoveToNextWayPoint();

        TurnTowardsTarget(TargetPosition, 360);
    }

    protected virtual void TurnTowardsTarget(Vector3 targetPos, float turnRateDegrees)
    {
        if (m_turnCoroutine != null)
            StopCoroutine(m_turnCoroutine);

        m_turnCoroutine = VHMath.TurnTowardsTarget(this, m_Pather, targetPos, turnRateDegrees);
    }

    /// <summary>
    /// This doesn't reset the current and next wp indices
    /// </summary>
    /// <param name="wayPoints"></param>
    public void SetPath(List<Vector3> wayPoints, bool bContinuePath)
    {
        m_WayPoints.Clear();
        m_WayPoints.AddRange(wayPoints);

        if (m_nPrevWP > wayPoints.Count - 1)
        {
            Debug.LogError("you're new path is shorter than what you have already traversed");
        }

        SetIsPathing(true);
        if(!bContinuePath)
            MoveToNextWayPoint();
    }

    //public override void VHUpdate()
    public void Update()
    {
        if (!IsPathing)
        {
            return;
        }

        //m_PathDirection = MovementDirection();
        m_Pather.transform.position = InterpolatePosition(PreviousPosition, TargetPosition, m_fCurrentTime / m_fTimeToReachTarget);

        if (m_fCurrentTime >= m_fTimeToReachTarget)
        {
            // they reached the next point, clamp their position to what their target was
            MoveToNextWayPoint();
        }

        m_fCurrentTime += Time.deltaTime;
    }

    protected virtual Vector3 InterpolatePosition(Vector3 prevPos, Vector3 targetPos, float t)
    {
        Vector3 interpolatedPos = Vector3.zero;
        switch (m_FollowingType)
        {
            case PathFollowingType.SmoothStep:
                interpolatedPos.x = Mathf.SmoothStep(prevPos.x, targetPos.x, t);
                interpolatedPos.y = Mathf.SmoothStep(prevPos.y, targetPos.y, t);
                interpolatedPos.z = Mathf.SmoothStep(prevPos.z, targetPos.z, t);
                break;

            case PathFollowingType.Lerp:
                interpolatedPos = Vector3.Lerp(prevPos, targetPos, t);
                break;
        }
        return interpolatedPos;
    }


    void MoveToNextWayPoint()
    {

        // clamp their position to where they were going
        m_Pather.transform.position = m_IgnoreHeight ? new Vector3(TargetPosition.x, m_Pather.transform.position.y, TargetPosition.z) : TargetPosition;
        m_fCurrentTime = 0;

        bool bPathComplete = false;
        switch (m_LoopType)
        {
            case PathLoopType.Loop:
                if (++m_nPrevWP >= m_WayPoints.Count)
                {
                    m_nPrevWP = 0;
                }
                if (++m_nNextWP >= m_WayPoints.Count)
                {
                    m_nNextWP = 0;
                    bPathComplete = true;
                }

                break;

            case PathLoopType.PingPong:
                if (m_bInReverse)
                {
                    if (--m_nPrevWP < 0)
                        m_nPrevWP = 0;
                    if (--m_nNextWP < 0)
                    {
                        m_nNextWP = 1;
                        m_bInReverse = false;
                        bPathComplete = true;
                    }
                }
                else
                {
                    if (++m_nPrevWP >= m_WayPoints.Count)
                        m_nPrevWP = m_WayPoints.Count - 1;
                    if (++m_nNextWP >= m_WayPoints.Count)
                    {
                        m_nNextWP = m_WayPoints.Count - 2;
                        m_bInReverse = true;
                        bPathComplete = true;
                    }
                }
                //m_fTimeToReachTarget = Utils.GetTimeToReachPosition(PreviousPosition, TargetPosition, m_Speed);
                break;

            case PathLoopType.Stop:
                ++m_nPrevWP;
                ++m_nNextWP;
                if (m_nPrevWP >= m_WayPoints.Count || m_nNextWP >= m_WayPoints.Count)
                {
                    // they reached the end of the path
                    SetIsPathing(false);
                    bPathComplete = true;
                }
                break;
           }

        if (IsPathing)
        {
            // calculate time to reach next wp
            m_fTimeToReachTarget = VHMath.GetTimeToReachPosition(PreviousPosition, TargetPosition, m_Speed);

            if (m_TurnTowardsTargetPosition)
                TurnTowardsTarget(TurnTarget, m_AngularVelocity);
        }

        m_WayPointReachedCallback?.Invoke(this, PreviousPosition, PrevWP, m_WayPoints.Count);

        if (bPathComplete && m_PathCompletedCallback != null)
        {
            m_PathCompletedCallback(m_WayPoints);
        }
    }

    /// <summary>
    /// Setups a callback that will be called when the path has been completely traversed
    /// </summary>
    /// <param name="cb"></param>
    public void AddPathCompletedCallback(OnPathCompleted cb)
    {
        m_PathCompletedCallback += cb;
    }

    public void AddWayPointReachedCallback(OnWayPointReached cb)
    {
        m_WayPointReachedCallback += cb;
    }

    /// <summary>
    /// Set how fast you move along your path
    /// </summary>
    /// <param name="speed"></param>
    public virtual void SetSpeed(float speed)
    {
        if (Mathf.Abs(m_Speed - speed) <= Mathf.Epsilon)
            return;

        m_Speed = speed;
        SetIsPathing(speed != 0);
    }

    public Vector3 MovementDirection()
    {
        Vector3 direction = (TargetPosition - m_Pather.transform.position);

        if (m_IgnoreHeight)
        {
            direction.y = 0;
        }
        direction.Normalize();
        return direction;
    }

    public float GetTotalPathLength()
    {
        if (m_WayPoints.Count == 0)
        {
            m_WayPoints = ConvertWayPointGOsToPositions(WayPoints);
        }

        float totalPathLength = 0;
        for (int i = 1; i < m_WayPoints.Count; i++)
        {
            totalPathLength += Vector3.Distance(m_WayPoints[i], m_WayPoints[i - 1]);
        }
        return totalPathLength;
    }

    #endregion
}
}
