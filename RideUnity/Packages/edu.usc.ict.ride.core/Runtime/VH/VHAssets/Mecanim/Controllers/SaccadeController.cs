using System;
using System.Collections.Generic;
using Ride;
using UnityEngine;

using Random = UnityEngine.Random;

namespace VHAssets
{
public class SaccadeController : MonoBehaviour
{
    #region Constants
    const float RandMax = 32767;
    const float MinInterval = 0.001f;
    const float Slope = 0.0024f;
    const float Intercept = 0.025f;

    enum IntervalMode
    {
        Mutual,
        Away,
    }

    protected enum SaccadeState
    {
        Finished,
        FadeIn,
        FadeOut,
    }

    enum ModeAttributes
    {
        Bin_0,
        Bin_45,
        Bin_90,
        Bin_135,
        Bin_180,
        Bin_225,
        Bin_270,
        Bin_315,
        Magnitude_Limit,
        Percentage_Mutual,
        Mutual_Mean,
        Mutual_Variant,
        Away_Mean,
        Away_Variant,
        NUM_ATT
    }

    static readonly Dictionary<CharacterDefines.SaccadeType, float[]> m_AttMapping = new Dictionary<CharacterDefines.SaccadeType, float[]>()
    {
        { CharacterDefines.SaccadeType.Listen, new float[(int)ModeAttributes.NUM_ATT]
            { 15.54f, 6.46f, 17.69f, 7.44f, 16.80f, 7.89f, 20.38f, 7.79f, 10.0f, 75.0f, 75.5f, 47.1f, 13.0f, 7.1f }
        },
        { CharacterDefines.SaccadeType.Talk, new float[(int)ModeAttributes.NUM_ATT]
            { 15.54f, 6.46f, 17.69f, 7.44f, 16.80f, 7.89f, 20.38f, 7.79f, 12.0f, 41.0f, 73.9f, 74.9f, 27.8f, 24.0f, }
        },
        { CharacterDefines.SaccadeType.Think, new float[(int)ModeAttributes.NUM_ATT]
            { 5.46f, 10.54f, 24.69f, 6.44f, 6.89f, 12.8f, 26.38f, 6.79f, 12.0f, 20.0f, 60f, 47f, 60.0f, 47.0f, }
        }
    };

    [Serializable]
    protected class EyeTransformData
    {
        public string transformName = "";
        public bool isInverted = false;

        public EyeTransformData(string _transformName, bool _isInverted)
        {
            transformName = _transformName;
            isInverted = _isInverted;
        }

    }
    #endregion

    #region Variables
    [SerializeField]
    EyeTransformData [] m_EyeTransformNames = new EyeTransformData[]
    {
        new EyeTransformData("JtEyeLf", false),
        new EyeTransformData("JtEyeRt", true),
    };

    [System.Serializable]
    protected class EyeData
    {
        public Transform m_Eye;
        public Quaternion m_InitialRotation = Quaternion.identity; // where the joint is looking before a saccade occurs. This works with gaze
        public Quaternion m_LastRotation = Quaternion.identity;
        public Quaternion m_TargetRotation = Quaternion.identity;
        public bool m_IsInverted;
    }

    [SerializeField][Range(0, 10)] float m_MagnitudeScaler = 1;
    [SerializeField] CharacterDefines.SaccadeType m_SaccadeMode = CharacterDefines.SaccadeType.Default;
    [SerializeField] VHMath.Axis m_EyeForwardAxis = VHMath.Axis.X;
    IntervalMode m_IntervalMode = IntervalMode.Mutual;
    float m_Direction;
    float m_Magnitude;
    float m_Duration; // how long the eye takes to move to it's target rotation
    float m_WaitTime; // how long the eye should stay before fading back

    [SerializeField] protected EyeData[] m_Eyes;
    float m_CurrentSaccadeTimePassed = -1;
    protected SaccadeState m_SaccadeState = SaccadeState.Finished;
    Vector3 m_RotAngle = new Vector3();

    private bool m_assetInitialized = false;
    #endregion

    #region Properties
    public bool AreSaccadesOn
    {
        get { return m_SaccadeMode != CharacterDefines.SaccadeType.Default && m_SaccadeMode != CharacterDefines.SaccadeType.End; }
    }

    public int NumEyes { get { return m_EyeTransformNames.Length; } }

    public float MagnitudeScaler
    {
        get { return m_MagnitudeScaler; }
        set { m_MagnitudeScaler = value; }
    }
    #endregion

    #region Functions
    protected virtual void Awake()
    {
        if (!TryGetComponent(out ILoadableAsset loadedAsset))
            InitializeLoadedAsset();
    }

    public void InitializeLoadedAsset()
    {
        m_Eyes = new EyeData[NumEyes];
        for (int i = 0; i < NumEyes; i++)
        {
            m_Eyes[i] = new EyeData();
            GameObject go = VHUtils.FindChildRecursive(gameObject, m_EyeTransformNames[i].transformName);
            if (go != null)
            {
                m_Eyes[i].m_Eye = go.transform;
                m_Eyes[i].m_InitialRotation = m_Eyes[i].m_Eye.localRotation;
                m_Eyes[i].m_IsInverted = m_EyeTransformNames[i].isInverted;
            }
            else
                Debug.LogError("Couldn't find neck go named " + m_EyeTransformNames[i] + " on character " + name + ". Saccades won't work");
        }
        m_assetInitialized = true;
    }

    void SetPreviousRotations()
    {
        foreach (EyeData eye in m_Eyes)
            eye.m_InitialRotation = eye.m_Eye.localRotation;
    }

    Vector3 DetermineRotationAngle(float angle)
    {
        switch (m_EyeForwardAxis)
        {
            case VHMath.Axis.X:
                m_RotAngle.Set(0, Mathf.Sin(angle), Mathf.Cos(angle));
                break;

            case VHMath.Axis.Y:
                m_RotAngle.Set(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                break;

            case VHMath.Axis.Z:
            default:
                m_RotAngle.Set(Mathf.Sin(angle), Mathf.Cos(angle), 0);
                break;
        }
        return m_RotAngle;
    }

    public void Perform(float direction, float magnitude, float duration)
    {
        if (m_SaccadeState != SaccadeState.Finished)
            return;

        m_CurrentSaccadeTimePassed = 0;
        m_Direction = direction;
        m_Magnitude = magnitude;
        m_Duration = duration;
        SetPreviousRotations();

        float modifiedDirection = (m_Direction + 180) * Mathf.PI / 180.0f;      // 180.0f here is for adjustment;

        foreach (EyeData eye in m_Eyes)
        {
            eye.m_LastRotation = eye.m_Eye.transform.localRotation;
            eye.m_TargetRotation = eye.m_LastRotation * Quaternion.Euler(DetermineRotationAngle(modifiedDirection) * (eye.m_IsInverted ? m_Magnitude : -m_Magnitude));
        }

        m_SaccadeState = SaccadeState.FadeIn;
    }

    void UpdateBehaviour()
    {
        if (m_SaccadeState == SaccadeState.Finished && AreSaccadesOn && m_assetInitialized)
        {
            m_Direction = GenerateRandomDirection();         // degree
            m_Magnitude = GenerateRandomMagnitude();         // degree

            Quaternion targetRot = Quaternion.Euler(DetermineRotationAngle(m_Direction) * m_Magnitude);

            m_Duration = CalculateDuration(Quaternion.Angle(m_Eyes[0].m_Eye.transform.localRotation, targetRot));
            m_WaitTime = GenerateRandomInterval();      // sec
            Perform(m_Direction, m_Magnitude, m_Duration);
        }
    }

    void LateUpdate()
    {
        /*if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Perform(45, 8, 1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Perform(90, 8, 1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Perform(135, 8, 1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Perform(180, 8, 1);
        }*/

        UpdateBehaviour();
        Process();
    }

    void Process()
    {
        if (m_SaccadeState == SaccadeState.Finished)
            return;

        m_CurrentSaccadeTimePassed += Time.deltaTime;

        if (m_CurrentSaccadeTimePassed > m_Duration + m_WaitTime)
        {
            if (m_SaccadeState == SaccadeState.FadeIn)
            {
                if (AreSaccadesOn)
                {
                    m_CurrentSaccadeTimePassed = 0;
                    m_SaccadeState = SaccadeState.FadeOut;
                }
                else
                    m_SaccadeState = SaccadeState.FadeOut;
            }
            else if (m_SaccadeState == SaccadeState.FadeOut)
            {
                m_CurrentSaccadeTimePassed = 0;
                m_SaccadeState = SaccadeState.Finished;
            }
        }
        float r = m_CurrentSaccadeTimePassed / m_Duration;
        r = Mathf.Clamp01(r);
        float y = 1 - Mathf.Sqrt(1 - (r - 1) * (r - 1));

        ApplyProcessedSaccade(1 - y);
    }

    protected virtual void ApplyProcessedSaccade(float lerpValue)
    {
        if (m_SaccadeState == SaccadeState.FadeIn)
        {
            foreach (EyeData eye in m_Eyes)
            {
                Quaternion rot = Quaternion.Slerp(eye.m_LastRotation, eye.m_TargetRotation, lerpValue);
                eye.m_Eye.localRotation = rot;
            }
        }
        else if (m_SaccadeState == SaccadeState.FadeOut)
            foreach (EyeData eye in m_Eyes)
                eye.m_Eye.localRotation = Quaternion.Slerp(eye.m_TargetRotation, eye.m_InitialRotation, lerpValue);
    }


    public void SetBehaviourMode(CharacterDefines.SaccadeType saccadeMode)
    {
        m_SaccadeMode = saccadeMode;
    }

    #region Math Functions
    float CalculateDuration(float amplitude)    // amplitude unit: degree
    {
        return Intercept + Slope * amplitude;
    }

    float GenerateGaussianRandom(float mean, float variant)
    {
        float V1 = 0, V2 = 0, S = 0;
        int phase = 0;
        float X = 0;
        if (phase == 0)
        {
            do
            {
                float U1 = Random.Range(0.0f, RandMax) / RandMax;
                float U2 = Random.Range(0.0f, RandMax) / RandMax;
                V1 = 2 * U1 - 1;
                V2 = 2 * U2 - 1;
                S = V1 * V1 + V2 * V2;
            }
            while (S >= 1 || S == 0);
            X = V1 * Mathf.Sqrt(-2 * Mathf.Log(S) / S);
        }
        else
            X = V2 * Mathf.Sqrt(-2 * Mathf.Log(S) / S);

        phase = 1 - phase;
        double Xp = X * Mathf.Sqrt(variant) + mean;   // X is for standard normal distribution
        return (float)Xp;
    }


    float GenerateRandomDirection()
    {
        float bound0, bound45, bound90, bound135, bound180, bound225, bound270, bound315;
        bound0 = GetCurrentModeAttribute(ModeAttributes.Bin_0);
        bound45 = bound0 + GetCurrentModeAttribute(ModeAttributes.Bin_45);
        bound90 = bound45 + GetCurrentModeAttribute(ModeAttributes.Bin_90);
        bound135 = bound90 + GetCurrentModeAttribute(ModeAttributes.Bin_135);
        bound180 = bound135 + GetCurrentModeAttribute(ModeAttributes.Bin_180);
        bound225 = bound180 + GetCurrentModeAttribute(ModeAttributes.Bin_225);
        bound270 = bound225 + GetCurrentModeAttribute(ModeAttributes.Bin_270);
        bound315 = bound270 + GetCurrentModeAttribute(ModeAttributes.Bin_315);

        float dir = 0.0f;
        float binIndex = Random.Range(0.0f, 100.0f);
        if (binIndex >= 0.0f && binIndex <= bound0)
            dir = 0.0f;
        if (binIndex >= bound0 && binIndex <= bound45)
            dir = 45.0f;
        if (binIndex >= bound45 && binIndex <= bound90)
            dir = 90.0f;
        if (binIndex >= bound90 && binIndex <= bound135)
            dir = 135.0f;
        if (binIndex >= bound135 && binIndex <= bound180)
            dir = 180.0f;
        if (binIndex >= bound180 && binIndex <= bound225)
            dir = 225.0f;
        if (binIndex >= bound225 && binIndex <= bound270)
            dir = 270.0f;
        if (binIndex >= bound270 && binIndex <= bound315)
            dir = 315.0f;
        return dir;
    }

    float GenerateRandomMagnitude()
    {
        float f = Random.Range(0.0f, 15.0f);
        float a = -6.9f * Mathf.Log(f / 15.7f);
        float limit = GetCurrentModeAttribute(ModeAttributes.Magnitude_Limit);

        // 0.5f, 0.75f are regulated by the eye shape
        // direction 0 and 180 is moving up and down, it should have a limit
        if (m_Direction == 90.0f || m_Direction == 270.0f)
            limit *= 0.5f;
        if (m_Direction == 45.0f || m_Direction == 135.0f || m_Direction == 225.0f || m_Direction == 315.0f)
            limit *= 0.75f;

        if (a > limit)
            a = limit;
        return a * m_MagnitudeScaler;
    }

    float GenerateRandomInterval()
    {
        float f = Random.Range(0.0f, 100.0f);
        float mutualPercent = GetCurrentModeAttribute(ModeAttributes.Percentage_Mutual);// _percentMutual;

        if (f >= 0.0f && f <= mutualPercent)
            m_IntervalMode = IntervalMode.Mutual;
        else
            m_IntervalMode = IntervalMode.Away;

        float interval = -1.0f;
        while (interval < MinInterval)
        {
            if (m_IntervalMode == IntervalMode.Mutual)
            {
                interval = GenerateGaussianRandom(GetCurrentModeAttribute(ModeAttributes.Mutual_Mean) * Time.deltaTime, GetCurrentModeAttribute(ModeAttributes.Mutual_Variant) * Time.deltaTime);
            }

            if (m_IntervalMode == IntervalMode.Away)
            {
                interval = GenerateGaussianRandom(GetCurrentModeAttribute(ModeAttributes.Away_Mean) * Time.deltaTime, GetCurrentModeAttribute(ModeAttributes.Away_Variant) * Time.deltaTime);
            }
        }
        return interval;
    }

    float GetCurrentModeAttribute(ModeAttributes att)
    {
        return GetModeAttribute(m_SaccadeMode, att);
    }

    float GetModeAttribute(CharacterDefines.SaccadeType mode, ModeAttributes att)
    {
        float attVal = 1;
        if (m_AttMapping.ContainsKey(mode))
            attVal = m_AttMapping[mode][(int)att];
        else
            Debug.LogError("No attributes set up for saccade mode " + mode);
        return attVal;
    }
    #endregion
    #endregion
}
}
