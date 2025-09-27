using System.Collections.Generic;
using Ride;
using UnityEngine;

namespace VHAssets
{
/// <summary>
/// Procedurally controls head movement such as nods, shakes, and tilts. Gazing is NOT handled here
/// </summary>
public class HeadController : MonoBehaviour
{

    #region Constants
    const float DefaultSpeed = 0.5f;
    const float DirectionDampTime = 0.25f;

    public enum MovementType
    {
        Nod,
        Shake,
        Tilt,
    }

    class MovementData
    {
        public MovementType m_MovementType;
        public float m_CurrentNeckRot;
        public float m_TimePassed = 0;
        public float m_TimeToComplete = 0;
        public float m_Amplitude;
        public float m_Frequency;
        public float m_NumTimes;
        //public float m_RotOffset;
        public bool m_Reverse; // used for returning back to the original orientation
        //public float m_ReverseRot; // used for storying the current rot when the reverse occured


        public MovementData(MovementType movementType, float amplitude, float frequency, float numTimes, float timeToComplete)
        {
            m_MovementType = movementType;
            m_NumTimes = numTimes;
            m_Amplitude = amplitude;
            m_Frequency = frequency;
            m_TimeToComplete = timeToComplete;
            m_Reverse = false;
        }
    }
    #endregion

    #region Variables
    [SerializeField] string m_NeckTransformName = "JtSkullA";
    [SerializeField] bool m_flipTransform = false;
    [SerializeField] float m_NodAmplifier = 30.0f;
    [SerializeField] float m_ShakeAmplifier = 45.0f;
    [SerializeField] float m_TiltAmplifier = 30.0f;

    Transform m_Neck;
    List<MovementData> m_CurrentHeadMovements = new List<MovementData>();

    //Dictionary<MovementType, Stack<MovementData>> m_CurrentMovements = new Dictionary<MovementType, Stack<MovementData>>();
    #endregion

    #region Functions
    void Awake()
    {
        if (!TryGetComponent(out ILoadableAsset loadedAsset))
            InitializeLoadedAsset();
    }

    public void InitializeLoadedAsset()
    {
        GameObject neckGO = VHUtils.FindChildRecursive(gameObject, m_NeckTransformName);
        if (neckGO != null)
            m_Neck = neckGO.transform;
        else
            Debug.LogError("Couldn't find neck go named " + m_NeckTransformName + " on character " + name + ". Gazing, nodding, and shaking won't work");
    }

    void LateUpdate()
    {
        for (int i = 0; i < m_CurrentHeadMovements.Count; i++)
        {
            if (DoHeadMovement(m_CurrentHeadMovements[i]))
            {
                // movement finished, remove it
                m_CurrentHeadMovements.RemoveAt(i--);
            }
        }
    }

    public void NodHead(float amount, float numTimes, float duration)
    {
        CreateHeadMovement(MovementType.Nod, amount, numTimes, duration);
    }

    public void ShakeHead(float amount, float numTimes, float duration)
    {
        CreateHeadMovement(MovementType.Shake, amount, numTimes, duration);
    }

    public void TiltHead(float amount, float numTimes, float duration)
    {
        CreateHeadMovement(MovementType.Tilt, amount, numTimes, duration);
    }

    void CreateHeadMovement(MovementType type, float amount, float numTimes, float duration)
    {
        duration = Mathf.Abs(duration);
        //int index = m_CurrentHeadMovements.FindIndex(m => m.m_MovementType == type);
        //if (index != -1)
        //{
        //    // a movement of this type is already happening, disregard it
        //    Debug.LogWarningFormat("There is already a head movement of type {0}. Wait for it to finish befure issuing a movement of the same type", type);
        //    return;
        //}

        amount = Mathf.Clamp(amount, -1, 1);
        if (amount == 0)
            amount = DefaultSpeed;

        float amplitude = 1;
        float frequency = 1;

        switch (type)
        {
        case MovementType.Nod:
            amplitude = amount * m_NodAmplifier;
            frequency = (Mathf.PI * 2.0f) / (duration);
            break;

        case MovementType.Shake:
            amplitude = amount * m_ShakeAmplifier;
            frequency = (Mathf.PI * 2.0f) / (duration);
            break;

        case MovementType.Tilt:
        default:
            amplitude = amount * m_TiltAmplifier;
            frequency = (Mathf.PI * 2.0f) / (duration);
            break;
        }

        m_CurrentHeadMovements.Insert(0, new MovementData(type, amplitude, frequency, numTimes, duration));
    }

    bool DoHeadMovement(MovementData movementData)
    {
        bool isMovementFinished = false;
        float t = movementData.m_TimePassed / movementData.m_TimeToComplete;

        if (movementData.m_Reverse)
        {
            movementData.m_CurrentNeckRot = Mathf.SmoothStep(movementData.m_CurrentNeckRot, 0, t);
        }
        else
        {
            movementData.m_CurrentNeckRot = movementData.m_Amplitude * Mathf.Sin((t) * 2.0f * Mathf.PI * movementData.m_NumTimes);
        }
        
        movementData.m_TimePassed += Time.deltaTime;

        m_Neck.transform.Rotate(GetRotationAxis(movementData.m_MovementType), movementData.m_CurrentNeckRot, Space.World);

        if (movementData.m_TimePassed >= movementData.m_TimeToComplete)
        {
            isMovementFinished = true;
        }

        return isMovementFinished;
    }

    /// <summary>
    /// Stops all current head movements and gracefully returns the neck back to its original orientation
    /// </summary>
    public void Stop()
    {
        //HashSet<MovementType> numOfEachType = new HashSet<MovementType>();
        for (int i = 0; i < m_CurrentHeadMovements.Count; i++)
        {
            //if (!numOfEachType.Contains(m_CurrentHeadMovements[i].m_MovementType))
            {
                //numOfEachType.Add(m_CurrentHeadMovements[i].m_MovementType);

                m_CurrentHeadMovements[i].m_Reverse = true;
                m_CurrentHeadMovements[i].m_TimePassed = 0;
                m_CurrentHeadMovements[i].m_TimeToComplete = DirectionDampTime;
                //m_CurrentHeadMovements[i].m_ReverseRot = m_CurrentHeadMovements[i].m_CurrentNeckRot;
            }
            /*else
            {
                // we already have a movement of this type, remove this one
                m_CurrentHeadMovements.RemoveAt(i--);
                
            }*/
        }
    }

    Vector3 GetRotationAxis(MovementType type)
    {
        Vector3 axis = m_Neck.forward;
        if (type == MovementType.Nod)
        {
            if (m_flipTransform)
                axis = m_Neck.right;
            else
                axis = m_Neck.forward;
        }
        else if (type == MovementType.Shake)
        {
            if (m_flipTransform)
                axis = m_Neck.up;
            else
                axis = m_Neck.right;
        }
        else
        {
            axis = m_Neck.up;
        }
        return axis;
    }

#if false
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NodHead(1, 2, 3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NodHead(2, 4, 6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Stop();
        }
    }
#endif
    #endregion
}
}
