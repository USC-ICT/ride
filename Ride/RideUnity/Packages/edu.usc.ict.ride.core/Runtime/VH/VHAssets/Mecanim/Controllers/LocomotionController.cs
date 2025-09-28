using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


// https://issuetracker.unity3d.com/issues/serializedfield-fields-produce-field-is-never-assigned-to-dot-dot-dot-warning
// https://forum.unity.com/threads/warning-cs0649-not-suppressed-properly-when-field-is-marked-as-serializefield.556009/#post-3954073
#pragma warning disable 0649


namespace VHAssets
{
public class LocomotionController : MonoBehaviour
{
    #region Constants
    public delegate void OnReachedDestination(LocomotionController locomoter);
    public delegate void OnReachingDestination(LocomotionController locomoter);
    public delegate void OnPathingUpdate(LocomotionController controller);
    public delegate void OnLocomotionReset(LocomotionController controller);
    #endregion

    #region Variables
    //[SerializeField] NavMeshAgent m_Agent;
    [SerializeField] Animator m_AnimatingAgent = null;
    //bool m_matchTarget = false; //This is under testing, and isn't ironed out yet - Unity's documentation is lacking. Ref: Animator.MatchTarget()
    [SerializeField] GameObject m_FacingTarget = null;
    [SerializeField] bool m_alwaysFaceTarget = false;
    [Range(0f, 90f)] [SerializeField] float m_faceTargetTurnThreshold;
    float m_locomotionMaxAngularSpeed = 2.480145f; //This is the max direction value from locomotion blend tree

    //[SerializeField] float m_TurnToFaceSpeed = 5;
    [SerializeField] LocomotionStrategy m_LocoStrat = null;
    NavMeshPath m_Path;
    OnReachedDestination m_OnReachedDestinationCB;
    OnReachingDestination m_OnReachingDestinationCB;
    OnPathingUpdate m_OnPathingUpdate;
    OnLocomotionReset m_OnLocomotionReset;
    bool m_isTurning;

    //[Header("Param Names")]
    string m_IsLomotingParamName = "Locomoting";
    string m_IsLomotionArrivingParamName = "_LocomotionArriving";
    string m_LocomotionSpeedParamName = "Speed";
    string m_LocomotionDirectionParamName = "Direction";
    string m_LocomotionArrivalDirectionParamName = "_ArrivalDirection"; //Also used for starting-path-direction
    
    string m_IsWithinFaceTargetTrunThresholdParamName = "_IsWithinFaceTargetTrunThreshold";
    //[SerializeField] float m_AnimationSpeedNormalizer = 1;
    //[SerializeField] float m_AnimationDirectionNormalizer = 1;

    //Locomotion stage variables
    float m_timeToStartWalk = 1.0f; //Note: This should be the same as "ExitTime" in animatorController's idle-to-transition blend
    float m_timeToArrival = 2f; //Note: This should be the same as "ExitTime" in animatorController's transition-to-idle blend -- in order to let the animation finish playing, setting to 1.5f
    float m_startWalkTimer = 0f;
    bool m_IsStartingToWalk = false;
    
    //float m_personalSpaceAvoidAnmDist = 0.13f; //This is how far the animation travels - we use this to validate the agent's avoid-destination

    [Header("Visualization")]
    public TextMesh m_distanceText = null;
    public TextMesh m_statusText = null;
    #endregion

    #region Properties
    // The max speed
    public float Speed
    {
        get { return m_LocoStrat.Speed; }
        set { m_LocoStrat.Speed = value; }
    }

    // How fast they are currently moving
    public float CurrentSpeed
    {
        get { return m_LocoStrat.CurrentSpeed; }
    }
    public float AngularSpeed
    {
        get { return m_LocoStrat.TurnSpeed; }
        set { m_LocoStrat.TurnSpeed = value; }
    }
    public GameObject FacingTarget {  get { return m_FacingTarget; } }
    public Animator AnimatingAgent {  get { return m_AnimatingAgent; } }
    public float StoppingDistance { get { return m_LocoStrat.StoppingDistance; } set { m_LocoStrat.StoppingDistance = value; } }
    public bool IsLocomoting { get { return GetAnimatorBool(m_IsLomotingParamName); } }
    public bool IsLocomotionArriving { get { return GetAnimatorBool(m_IsLomotionArrivingParamName); } }
    public bool AlwaysFaceTarget { get { return m_alwaysFaceTarget; } set { m_alwaysFaceTarget = value; } }
    public bool IsStartingToWalk {  get { return m_IsStartingToWalk;  } }
    /// <summary>
    /// This controls how many degrees will the agent tolerate before turning to face the target again
    /// </summary>
    public float FaceTargetTurnThreshold { get { return m_faceTargetTurnThreshold; } set { m_faceTargetTurnThreshold = value; } }
    public bool IsWithinFaceTargetTurnThreshold
    {
        get { return GetAnimatorBool(m_IsWithinFaceTargetTrunThresholdParamName); }
        set { SetAnimatorParameter(m_IsWithinFaceTargetTrunThresholdParamName, value); }
    }
    public bool IsTurning { get { return m_isTurning; }  set { m_isTurning = value; } }
    
    #endregion

    #region Functions
    #region Init
    void Awake()
    {
        if (m_LocoStrat != null)
        {
            m_LocoStrat.SetLocomotionController(this);
        }
        else
        {
            Debug.LogErrorFormat("There is no locomotion strategy being used with locmotion controller {0}. The character won't be able to move", name);
        }
    }

    void Start()
    {
    }

    void Update()
    {
        UpdateLocomotion();
    }
    #endregion

    #region Callbacks
    public void AddOnReachedDestinationCallback(OnReachedDestination cb)
    {
        m_OnReachedDestinationCB += cb;
    }

    public void RemoveOnReachedDestinationCallback(OnReachedDestination cb)
    {
        m_OnReachedDestinationCB -= cb;
    }

    public void AddOnReachingDestinationCallback(OnReachingDestination cb)
    {
        m_OnReachingDestinationCB += cb;
    }

    public void RemoveOnReachingDestinationCallback(OnReachingDestination cb)
    {
        m_OnReachingDestinationCB -= cb;
    }

    public void AddOnPathingUpdateCallback(OnPathingUpdate cb)
    {
        m_OnPathingUpdate += cb;
    }

    public void RemoveOnPathingUpdateCallback(OnPathingUpdate cb)
    {
        m_OnPathingUpdate -= cb;
    }

    public void AddOnLocomotionReset(OnLocomotionReset cb)
    {
        m_OnLocomotionReset += cb;
    }

    public void RemoveOnLocomotionReset(OnLocomotionReset cb)
    {
        m_OnLocomotionReset -= cb;
    }

    public void RemoveAllCallbacks()
    {
        m_OnReachedDestinationCB = null;
        m_OnReachingDestinationCB = null;
        m_OnPathingUpdate = null;
        m_OnLocomotionReset = null;
    }
    #endregion

    #region Public
    public void Warp(Vector3 destination)
    {
        m_LocoStrat.Warp(destination);
    }

    public void MoveTo(Transform destination)
    {
        MoveTo(destination.position);
    }

    public void MoveTo(GameObject destination)
    {
        MoveTo(destination.transform.position);
    }

    public void MoveTo(Vector3 destination)
    {
        m_LocoStrat.MoveTo(destination);

        if (!IsLocomoting && m_AnimatingAgent != null)
        {
            StartIdleToWalking();
        }

        SetAnimatorParameter(m_IsLomotingParamName, true);
    }

    public void Stop()
    {
        ResetLocomotionParams();
        m_LocoStrat.ResetLocomotion();
    }

    public void Move()
    {
        Move(Speed * Time.deltaTime, transform.forward);
    }

    public void Move(float amount)
    {
        Move(amount, transform.forward);
    }

    public void Move(float amount, Vector3 direction)
    {
        transform.position += amount * direction;
        SetAnimatorSpeed(Speed);
        //SetAnimatorParameter(m_IsLomotingParamName, true);
    }

    public void TurnFromInput(float degrees)
    {
        SetAnimatorParameter(m_LocomotionDirectionParamName, VHMath.Clamp180(degrees));
        transform.Rotate(transform.up, degrees);
    }

    public void NoInput()
    {
        SetAnimatorParameter(m_IsLomotingParamName, false);
        ResetLocomotionParams();
    }

    public void SetPath(Vector3 destination)
    {
        SetPath(transform.position, destination);
    }

    public void SetPath(Vector3 startingPosition, Vector3 destination)
    {
        m_LocoStrat.SetPath(startingPosition, destination);
        SetAnimatorParameter(m_IsLomotingParamName, true);
    }

    public void SetFacingTarget(GameObject target)
    {
        m_FacingTarget = target;
    }

    public void SetAnimatorSpeed(float speed)
    {
        SetAnimatorParameter(m_LocomotionSpeedParamName, speed);
    }

    /// <summary>
    /// We assume turning happens in one fell swoop, without repeating
    /// </summary>
    /// <param name="degrees"></param>
    public void Turn(float degrees)
    {
        StartCoroutine(TurnCoroutine(degrees));
    }

    IEnumerator TurnCoroutine(float degrees)
    {
        if (!IsTurning)
        {
            bool origThresholdBool = IsWithinFaceTargetTurnThreshold;
            IsWithinFaceTargetTurnThreshold = false; //Triggers the condition
            SetAnimatorParameter(m_LocomotionDirectionParamName, VHMath.Clamp180(degrees));
            IsTurning = true;
            yield return new WaitForSeconds(1f);
            IsTurning = false;
            IsWithinFaceTargetTurnThreshold = origThresholdBool;
            yield return StartCoroutine(VHMecanimUtils.AnimatorFloatTweenToZeroCR(m_AnimatingAgent, m_LocomotionDirectionParamName));
            //yield return VHMecanimUtils.AnimatorFloatTweenToZero(this, m_AnimatingAgent, m_LocomotionDirectionParamName); //m_AnimatingAgent.SetFloat(m_LocomotionDirectionParamName, 0f);
            yield return new WaitForSeconds(0.25f); //Extra delay for blending to and from idle
        }
    }

    public void TurnToFace(Vector3 destination)
    {
        if (!IsLocomoting)
        {
            float wsDegreesToTarget = VHMath.GetRotationFromToEuler(transform.position, destination);
            float degreesDelta = wsDegreesToTarget - transform.eulerAngles.y;
            degreesDelta = VHMath.Clamp180(degreesDelta);
            if (Mathf.Abs(degreesDelta) <= FaceTargetTurnThreshold)
            {
                //Debug.LogFormat("Turning angle ({0}) within threshold ({1}); No turning needed.", degreesDelta, FaceTargetTurnThreshold);
                IsWithinFaceTargetTurnThreshold = true;
            }
            else
            {
                IsWithinFaceTargetTurnThreshold = false;
                Debug.LogFormat("Turning {0} to reach {1}", degreesDelta, wsDegreesToTarget);
                Turn(degreesDelta);
            }
        }
    }

    public void ResetLocomotionParams()
    {
        StartCoroutine(VHMecanimUtils.AnimatorFloatTweenToZeroCR(m_AnimatingAgent, m_LocomotionSpeedParamName));
        StartCoroutine(VHMecanimUtils.AnimatorFloatTweenToZeroCR(m_AnimatingAgent, m_LocomotionDirectionParamName));
        StartCoroutine(VHMecanimUtils.AnimatorFloatTweenToZeroCR(m_AnimatingAgent, m_LocomotionArrivalDirectionParamName));
        SetAnimatorParameter(m_IsLomotingParamName, false);
        SetAnimatorParameter(m_IsLomotionArrivingParamName, true); //Set to true to let states transition back to idle
        SetAnimatorParameter(m_IsLomotionArrivingParamName, false);
    }

    public void ResetLocomotion()
    {
        SetStatusText("Locomotion Reset");

        ResetLocomotionParams();

        m_LocoStrat.ResetLocomotion();

        m_OnLocomotionReset?.Invoke(this);
    }


    #endregion

    #region Manage Locomotion Stages
    #region Walking basics
    void UpdateLocomotion()
    {
        m_LocoStrat.UpdateLocomotion();
    }

    public void PathingUpdating()
    {
        m_OnPathingUpdate?.Invoke(this);
    }
    
    public void DetermineAnimationDirection(float desiredVelocityX, float desiredVelocityZ)
    {
        //Desired velocity includes avoidance diretion
        //Quaternion rotQuat = Quaternion.FromToRotation(transform.forward, new Vector3(m_Agent.desiredVelocity.x, 0, m_Agent.desiredVelocity.z));
        Quaternion rotQuat = Quaternion.FromToRotation(transform.forward, new Vector3(desiredVelocityX, 0, desiredVelocityZ));
        float walkingDirectionVal = (VHMath.Clamp180(rotQuat.eulerAngles.y) / 180f) * m_locomotionMaxAngularSpeed;
        //Debug.LogFormat("XForm: {0}      agent: {1}       rotation: {2}",
        //    transform.forward, new Vector3(m_Agent.desiredVelocity.x, 0, m_Agent.desiredVelocity.z), Clamp180(rotQuat.eulerAngles.y));
        SetAnimatorParameter(m_LocomotionDirectionParamName, walkingDirectionVal);
    }

    public void ReachedDestination()
    {
        SetStatusText("Reached");
        Debug.Log("Locomotion destination reached");
        ResetLocomotion();

        m_OnReachedDestinationCB?.Invoke(this);
    }

    /// <summary>
    /// Upon reaching destination, agent may have to execute a turn based on the final facing direction (m_FacingTarget variable). 
    /// This function calculates a direction ranged -180 to 180 according to agent's incoming facing and the final-resting-facing.
    /// </summary>
    public void ReachingDestination()
    {
        m_OnReachingDestinationCB?.Invoke(this);

        StartCoroutine(StartArrivalTimer());
        SetAnimatorParameter(m_IsLomotionArrivingParamName, true);

        if (m_FacingTarget == null)
        {
            SetStatusText("Arriving; no face target");
            Debug.Log("Locomotion Arriving. No facing target set; arrival rotation skipped.");
        }
        else
        {
            //Direction should clamp to (-180, 180)
            Vector3 agentRot = new Vector3(0, (transform.eulerAngles.y > 180) ? (transform.eulerAngles.y - 360f) : transform.eulerAngles.y, 0);
            Vector3 facingDir = m_FacingTarget.transform.position - m_LocoStrat.AgentDestination;
            Quaternion agentRotAtDest = Quaternion.LookRotation(facingDir); 
            float agentRotDelta = VHMath.Clamp180(agentRotAtDest.eulerAngles.y - agentRot.y);
            //agentRotDelta = (agentRotDelta + (agentRotDelta / Mathf.Abs(agentRotDelta)) * 10); //Add 10 extra degrees for good measure

            Debug.LogFormat("Locomotion Arriving - Character rot: {0}\n    Destination rot: {1}\n    Rotation to execute: {2}\n    Calculated final rotation = {3}",
                agentRot.y, agentRotAtDest.eulerAngles.y, agentRotDelta, VHMath.Clamp180(agentRot.y + agentRotDelta));

            SetAnimatorParameter(m_LocomotionArrivalDirectionParamName, agentRotDelta);
            m_LocoStrat.SetAgentUpdateRotation(false);
            SetStatusText("Arriving; Rotating: " + agentRotDelta.ToString());
        }
    }

    void StartIdleToWalking()
    {
        m_IsStartingToWalk = true;
        m_LocoStrat.SetAgentUpdatePosition(false);
        StartCoroutine(StartIdleToWalkingTimer());
        SetAnimatorParameter(m_IsLomotingParamName, true);

        //Direction should clamp to (-180, 180)
        Vector3 agentRot = new Vector3(0, (transform.eulerAngles.y > 180) ? (transform.eulerAngles.y - 360f) : transform.eulerAngles.y, 0);
        Vector3 facingDir = m_LocoStrat.AgentDestination - transform.position;
        Quaternion agentRotAtDest = Quaternion.LookRotation(facingDir);
        float agentRotDelta = VHMath.Clamp180(agentRotAtDest.eulerAngles.y - agentRot.y);

        Debug.LogFormat("Locomotion starting - Character rot: {0}\n    Facing: {1}\n    Rotation to execute: {2}\n    Calculated final rotation = {3}",
            agentRot.y, VHMath.Clamp180(agentRotAtDest.eulerAngles.y), agentRotDelta, VHMath.Clamp180(agentRot.y + agentRotDelta));

        SetAnimatorParameter(m_LocomotionArrivalDirectionParamName, agentRotDelta);
        m_LocoStrat.SetAgentUpdateRotation(false);
        SetStatusText("Starting; Rotating: " + agentRotDelta.ToString());
    }

    /// <summary>
    /// Delay of generic path-rotation update to utilize nice blend for beginning direction transition animations
    /// </summary>
    /// <returns></returns>
    IEnumerator StartIdleToWalkingTimer()
    {
        SetStatusText("Starting to walk");
        float origSpeed = m_LocoStrat.Speed;

        m_IsStartingToWalk = true;
        m_startWalkTimer = 0f;
        //m_Agent.speed = 0f;
        while (m_startWalkTimer < m_timeToStartWalk)
        {
            m_startWalkTimer += Time.deltaTime;
            m_LocoStrat.SetAgentNextPosition(transform.position);
            //m_Agent.speed = Mathf.Lerp(m_Agent.speed, origSpeed, /*m_Agent.acceleration**/ Time.deltaTime / m_timeToStartWalk);
            yield return new WaitForEndOfFrame();
        }

        m_IsStartingToWalk = false;
        //m_Agent.speed = origSpeed;
    }

    IEnumerator StartArrivalTimer()
    {
        m_LocoStrat.SetAgentUpdatePosition(false);
        //m_Agent.nextPosition = transform.position;
        float timer = 0f;
        while (timer <= m_timeToArrival)
        {
            timer += Time.deltaTime;
            m_LocoStrat.SetAgentNextPosition(transform.position);
            SetAnimatorParameter(m_LocomotionSpeedParamName, Mathf.Lerp(GetAnimatorFloat(m_LocomotionSpeedParamName), 0, Time.deltaTime / m_timeToArrival));
            AnimatorMatchTarget_Arrival(); //Match target per frame and only during arrivals
            yield return new WaitForEndOfFrame();
        }
        m_LocoStrat.SetAgentUpdatePosition(true);

        ReachedDestination();
    }

    public void SetAnimatorParameter(string paramName, bool val)
    {
        if (m_AnimatingAgent != null)
        {
            m_AnimatingAgent.SetBool(paramName, val);
        }
    }

    public void SetAnimatorParameter(string paramName, float val)
    {
        if (m_AnimatingAgent != null)
        {
            m_AnimatingAgent.SetFloat(paramName, val);
        }
    }

    public bool GetAnimatorBool(string paramName)
    {
        return m_AnimatingAgent != null ? m_AnimatingAgent.GetBool(paramName) : false;
    }

    public float GetAnimatorFloat(string paramName)
    {
        return m_AnimatingAgent != null ? m_AnimatingAgent.GetFloat(paramName) : 0;
    }

    void AnimatorMatchTarget_Arrival()
    {
        //float xPosMatched = Mathf.Lerp(transform.position.x, m_Agent.destination.x, Time.deltaTime);
        //float yPosMatched = Mathf.Lerp(transform.position.y, m_Agent.destination.y, Time.deltaTime);
        //float zPosMatched = Mathf.Lerp(transform.position.z, m_Agent.destination.z, Time.deltaTime);
        //Vector3 posMatched = new Vector3(xPosMatched, yPosMatched, zPosMatched);
        //transform.position = posMatched;

        //if (m_matchTarget && !m_AnimatingAgent.IsInTransition(2))
        //{
        //    m_AnimatingAgent.MatchTarget(m_destinationObj.position, m_destinationObj.rotation, AvatarTarget.LeftFoot, new MatchTargetWeightMask(Vector3.one, 1f), m_AnimatingAgent.GetFloat("MatchStart"), m_AnimatingAgent.GetFloat("MatchEnd"));
        //}
    }
    #endregion
    #endregion

    #region Visualization
    //void OnDrawGizmo()
    //{
    //    //This empty function alone turns on locomotion gizmos
    //}

    public void SetStatusText(string message)
    {
        if (m_statusText == null)
        {
            return;
        }

        m_statusText.text = message;
    }
    #endregion
    #endregion
}
}
