using UnityEngine;
using UnityEngine.AI;

namespace VHAssets
{
[RequireComponent(typeof(NavMeshAgent))]
public class LocomotionStrategy_NavMesh : LocomotionStrategy
{
    #region Variables
    NavMeshPath m_Path;
    NavMeshAgent m_Agent;
    #endregion

    #region Properties
    public NavMeshAgent Agent { get { return m_Agent; } }

    public override float Speed
    {
        get { return m_Agent.speed; }
        set { m_Agent.speed = value; }
    }

    public override float CurrentSpeed
    {
        get { return m_Agent.velocity.magnitude; }
    }

    public override float TurnSpeed
    {
        get { return m_Agent.angularSpeed; }
        set { m_Agent.angularSpeed = value; }
    }

    public override Vector3 AgentDestination
    {
        get { return m_Agent.destination; }
    }

    public override bool IsPathPending
    {
        get {  return m_Agent.pathPending; }
    }

    public override float StoppingDistance
    {
        get { return m_Agent.stoppingDistance; }
        set { m_Agent.stoppingDistance = value; }
    }
    #endregion

    #region Functions
    void Awake()
    {
        m_Path = new NavMeshPath();
        m_Agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (!NavMesh.SamplePosition(Agent.transform.position, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas))
        {
            Debug.LogWarning("No nav mesh in the scene. Locomotion won't work");
            enabled = false;
            if (LocoController != null)
            {
                LocoController.enabled = false;
            }
        }
    }

    public override void UpdateLocomotion()
    {
        base.UpdateLocomotion();

        if (!IsPathPending)
        {
            //Check if we're about to reach the destination
            if (!m_Agent.isStopped && m_Agent.remainingDistance <= LocoController.StoppingDistance)
            {
                if (LocoController.IsLocomoting)
                {
                    if (!LocoController.IsLocomotionArriving)
                    {
                        //Arriving & triggers ReachedDestination() after timer
                        LocoController.ReachingDestination();
                    }
                }
            }
        }

        //Basic locomotion direction/rotation update
        if (m_Agent.hasPath && LocoController.IsLocomoting && !LocoController.IsLocomotionArriving && !LocoController.IsStartingToWalk)
        {
            m_Agent.updateRotation = true;
            m_Agent.updatePosition = false; //Disabled since navMesh agent and locomotion are both moving the character, causing feet to slide
            m_Agent.nextPosition = transform.position;
            LocoController.SetAnimatorSpeed(m_Agent.velocity.magnitude);

            if (!LocoController.IsStartingToWalk)
            {
                LocoController.SetStatusText("General locomoting");
                LocoController.DetermineAnimationDirection(m_Agent.desiredVelocity.x, m_Agent.desiredVelocity.z);
            }

            LocoController.PathingUpdating();
        }
        else if (LocoController.IsStartingToWalk)
        {
            LocoController.SetAnimatorSpeed(m_Agent.velocity.magnitude);
        }

        if (LocoController.AlwaysFaceTarget && LocoController.FacingTarget != null)
        {
            LocoController.TurnToFace(LocoController.FacingTarget.transform.position);
        }
    }

    public override void MoveTo(Vector3 destination)
    {
        m_Agent.updateRotation = true;
        m_Agent.SetDestination(destination);
    }

    public override void Warp(Vector3 destination)
    {
        if (!m_Agent.Warp(destination))
        {
            Debug.LogErrorFormat("Failed to warp {0} to destination {1}", m_Agent.name, destination);
        }
    }
    public override float GetRemainingDistance()
    {
        return m_Agent.remainingDistance;
    }

    public override void ResetLocomotion()
    {
        m_Agent.ResetPath();
        m_Agent.updatePosition = true;
        m_Agent.updateRotation = true;
        m_Agent.isStopped = true;
    }

    public override void SetPath(Vector3 start, Vector3 destination)
    {
        if (m_Path == null)
        {
            m_Path = new NavMeshPath();
        }

        if (!NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, m_Path))
        {
            Debug.LogErrorFormat("Agent {0} failed to Calculate path from {1} to {2}", m_Agent.name, start, destination);
        }
        m_Agent.SetPath(m_Path);
    }

    public static float CalculatePathLength(Vector3 startingPosition, Vector3 destination)
    {
        // Create a path and set it based on a target position.
        NavMeshPath path = new NavMeshPath();

        NavMesh.CalculatePath(startingPosition, destination, NavMesh.AllAreas, path);

        // Create an array of points which is the length of the number of corners in the path + 2.
        Vector3[] allWayPoints = new Vector3[path.corners.Length + 2];

        // The first point is the enemy's position.
        allWayPoints[0] = startingPosition;

        // The last point is the target position.
        allWayPoints[allWayPoints.Length - 1] = destination;

        // The points inbetween are the corners of the path.
        for (int i = 0; i < path.corners.Length; i++)
        {
            allWayPoints[i + 1] = path.corners[i];
        }

        // Create a float to store the path length that is by default 0.
        float pathLength = 0;

        // Increment the path length by an amount equal to the distance between each waypoint and the next.
        for (int i = 0; i < allWayPoints.Length - 1; i++)
        {
            pathLength += Vector3.Distance(allWayPoints[i], allWayPoints[i + 1]);
        }

        return pathLength;
    }

    public override void SetAgentUpdatePosition(bool update)
    {
        m_Agent.updatePosition = update;
    }

    public override void SetAgentUpdateRotation(bool update)
    {
        m_Agent.updateRotation = update;
    }

    public override void SetAgentNextPosition(Vector3 nextPos)
    {
        m_Agent.nextPosition = nextPos;
    }
#endregion
}
}
