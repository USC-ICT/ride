using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// https://issuetracker.unity3d.com/issues/serializedfield-fields-produce-field-is-never-assigned-to-dot-dot-dot-warning
// https://forum.unity.com/threads/warning-cs0649-not-suppressed-properly-when-field-is-marked-as-serializefield.556009/#post-3954073
#pragma warning disable 0649


namespace VHAssets
{
/// <summary>
/// Uses the LocomotionController to put space in between 2 objects
/// </summary>
public class PersonalSpaceController : MonoBehaviour
{
    #region Constants
    public delegate void OnEnteredGreetingSpace(GameObject guest);
    public delegate void OnEnteredPersonalSpace(GameObject guest);
    public delegate void OnExitPersonalSpace(GameObject guest);
    public delegate void OnStayPersonalSpace(GameObject guest);
    #endregion

    #region Variables
    [SerializeField]
    LocomotionController m_Locomoter = null;

    [SerializeField]
    LayerMask m_AcceptedLayers; // gameobjects on these layers are the only ones that we care about

    [SerializeField]
    ColliderCallbacks m_PersonalSpaceInformer = null;

    [SerializeField]
    ColliderCallbacks m_GreetingSpaceInformer = null;

    [SerializeField]
    string m_PersonalSpaceStayInvadedParamName = "_PersonalSpaceStayInvaded";

    [SerializeField]
    string m_AvoidanceDirectionParamName = "_AvoidanceDirection";

    // when an object enters our space
    // if m_RequireFacing true: the locomoter has to be facing it or else it doesn't invoke callbacks
    // if m_RequireFacing false: the locomoter will invoke callbacks regardless of facing direction
    [SerializeField]
    bool m_RequireFacing = true;

    bool m_PersonalSpaceEntered = false; //This to control if m_RequireFacing is true

    float m_personalSpaceAvoidTime = 2f; //How quickly the avoidance direction gets reset - resetting too early creates bad blend between directions

    protected OnEnteredGreetingSpace m_OnEnteredGreetingSpace;
    protected OnEnteredPersonalSpace m_OnEnteredPersonalSpace;
    protected OnExitPersonalSpace m_OnExitPersonalSpace;
    protected OnStayPersonalSpace m_OnStayPersonalSpace;
    #endregion

    #region Properties
    Vector3 AgentLookDir {  get { return m_Locomoter.transform.forward; } }
    Vector3 AgentPosition { get { return m_Locomoter.transform.position; } }

    public bool IsPersonalSpaceStayInvaded
    {
        get { return m_Locomoter.GetAnimatorBool(m_PersonalSpaceStayInvadedParamName); }
        set { m_Locomoter.SetAnimatorParameter(m_PersonalSpaceStayInvadedParamName, value); }
    }
    #endregion

    #region Functions
    void Start()
    {
        //PerformPersonalSpaceTesting();
        m_GreetingSpaceInformer.AddTriggerEnterCallback(OnGreetingSpaceEntered);
        m_PersonalSpaceInformer.AddTriggerEnterCallback(OnPersonalSpaceEntered);
        m_PersonalSpaceInformer.AddTriggerExitCallback(OnPersonalSpaceExit);
        m_PersonalSpaceInformer.AddTriggerStayCallback(OnPersonalSpaceStay);

        m_Locomoter.AddOnLocomotionReset(OnResetLocomotion);
        //Debug.LogFormat("LayerMask {0} || 1 << LayerMask {1} || LayerMask.value {2} || DEFAULT LAYER VAL {3}", m_AcceptedLayers, 1 << m_AcceptedLayers, m_AcceptedLayers.value, LayerMask.NameToLayer("Default"));
    }

    public void AddOnEnteredGreetingSpaceCallback(OnEnteredGreetingSpace cb)
    {
        m_OnEnteredGreetingSpace += cb;
    }

    public void AddOnEnteredPersonalSpaceCallback(OnEnteredPersonalSpace cb)
    {
        m_OnEnteredPersonalSpace += cb;
    }

    bool IsAcceptedLayer(int testLayer, int acceptedLayers)
    {
        int shift = 1 << testLayer;
        int res = shift & acceptedLayers;
        return res != 0;
    }

    void OnResetLocomotion(LocomotionController controller)
    {
        StartCoroutine(VHMecanimUtils.AnimatorFloatTweenToZeroCR(m_Locomoter.AnimatingAgent, m_AvoidanceDirectionParamName));
        m_Locomoter.SetAnimatorParameter(m_PersonalSpaceStayInvadedParamName, false);
    }

    void OnGreetingSpaceEntered(GameObject callbackObject, Collider other)
    {
        //if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        //{
        //    int x = 10;
        //    Debug.Log("Player");
        //}
        if (IsAcceptedLayer(other.gameObject.layer, m_AcceptedLayers.value))
        {
            if ((!m_RequireFacing || VHMath.IsFacing(AgentLookDir, AgentPosition, other.transform.position)) && m_OnEnteredGreetingSpace != null)
            {
                m_OnEnteredGreetingSpace(other.gameObject);
            }
        }
    }

    void OnPersonalSpaceEntered(GameObject callbackObject, Collider other)
    {
        if (IsAcceptedLayer(other.gameObject.layer, m_AcceptedLayers.value))
        {
            if (!m_RequireFacing || VHMath.IsFacing(AgentLookDir, AgentPosition, other.transform.position))
            {
                m_OnEnteredPersonalSpace?.Invoke(other.gameObject);

                //m_Locomoter.WalkTo(AgentPosition + -AgentLookDir * 2);
                PersonalSpaceEntered(other.transform.position);
                m_PersonalSpaceEntered = true;
            }
        }
    }

    void OnPersonalSpaceStay(GameObject callbackObject, Collider other)
    {
        if (IsAcceptedLayer(other.gameObject.layer, m_AcceptedLayers.value))
        {
            if (m_PersonalSpaceEntered)
            {
                m_OnStayPersonalSpace?.Invoke(other.gameObject);

                PersonalSpaceStay(other.transform.position) ;
            }
        }
    }

    void OnPersonalSpaceExit(GameObject callbackObject, Collider other)
    {
        if (IsAcceptedLayer(other.gameObject.layer, m_AcceptedLayers.value))
        {
            if (m_PersonalSpaceEntered)
            {
                m_OnExitPersonalSpace?.Invoke(other.gameObject);

                PersonalSpaceExit();
            }
        }
    }

    public void PersonalSpaceEntered(Vector3 otherGoPosition)
    {
        float otherGoAngle = VHMath.GetRotationFromToEuler(transform.position, otherGoPosition);
        PersonalSpaceEntered(VHMath.Clamp180(otherGoAngle));
    }
    public void PersonalSpaceEntered(float fromAngle)
    {
        StartCoroutine(PersonalSpaceEnteredCoroutine(fromAngle));
    }
    IEnumerator PersonalSpaceEnteredCoroutine(float fromAngle)
    {
        bool wasFacingTarget = m_Locomoter.AlwaysFaceTarget;
        m_Locomoter.AlwaysFaceTarget = false; //Don't face target while evading
        m_Locomoter.SetAnimatorParameter(m_AvoidanceDirectionParamName, fromAngle - 180);
        Debug.LogFormat("Personal space entered from angle {0}; Moving away in direction {1}", fromAngle, fromAngle - 180);
        yield return new WaitForSeconds(m_personalSpaceAvoidTime);
        while (IsPersonalSpaceStayInvaded)
        {
            yield return new WaitForEndOfFrame();
        }
        m_Locomoter.SetAnimatorParameter(m_AvoidanceDirectionParamName, 0);
        m_Locomoter.AlwaysFaceTarget = wasFacingTarget;
    }

    public void PersonalSpaceExit()
    {
        IsPersonalSpaceStayInvaded = false;
    }

    public void PersonalSpaceStay(Vector3 otherGoPosition)
    {
        float otherGoAngle = VHMath.GetRotationFromToEuler(transform.position, otherGoPosition);
        PersonalSpaceStay(VHMath.Clamp180(otherGoAngle));
    }
    public void PersonalSpaceStay(float fromAngle)
    {
        IsPersonalSpaceStayInvaded = true;
        m_Locomoter.SetAnimatorParameter(m_AvoidanceDirectionParamName, fromAngle - 180);
    }

    public static bool IsPointInSphere(Transform me, Transform other, float radius)
    {
        return Vector3.Distance(me.transform.position, other.transform.position) <= radius;
    }
    #endregion
}
}
