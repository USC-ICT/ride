using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class GazeController : MonoBehaviour
{
    #region Constants
    public const float DefaultHeadGazeSpeed = 400;
    public const float DefaultEyeGazeSpeed = 400;
    public const float DefaultBodyGazeSpeed = 400;
    public const float DefaultFadeOutTime = 1.0f; // seconds

    delegate void OnGazeUpdated(float weight);

    [Flags]
    public enum GazeParts
    {
        Body = 1,
        Head = 1 << 1,
        Eyes = 1 << 2,
        All = (1 << 3) - 1,
        None = 0,
    }

    public enum GazeState
    {
        Off,
        On,
        FadeOut,
        FadeIn, // moving from no gaze target to a gaze target
        Transitioning, // moving from one gaze target to another
    }
    #endregion

    #region Variables
    [SerializeField] protected GameObject m_GazeTarget;
    protected Vector3 m_GazeOffset = Vector3.zero;

    // m_GazeTarget gets set to this after we're finished tweening between the current target and the future target
    protected GameObject m_FutureGazeTarget;
    GazeState m_GazeState = GazeState.Off;
    float m_SwitchWeight;
    #endregion

    #region Properties
    virtual public float HeadGazeWeight { get; set; }
    virtual public float EyeGazeWeight { get; set; }
    virtual public float BodyGazeWeight { get; set; }

    virtual public float CurrentHeadGazeWeight { get; set; }
    virtual public float CurrentEyeGazeWeight { get; set; }
    virtual public float CurrentBodyGazeWeight { get; set; }
    virtual public float CurrentTotalGazeWeight { get; set; }

    protected GazeState CurrentGazeState { get { return m_GazeState; } }
    #endregion

    #region Functions
    void Start()
    {
    }

    protected virtual void InitGaze(GameObject gazeTarget)
    {
        if (m_GazeTarget != null && m_GazeTarget != gazeTarget)
        {
            // they already have a gaze target, so we need to offset our gaze to it
            m_FutureGazeTarget = gazeTarget;
            m_SwitchWeight = 0;
            m_GazeState = GazeState.Transitioning;
            //m_GazeOffset = gazeTarget.transform.position - m_GazeTarget.transform.position;
        }
        else
        {
            m_GazeTarget = gazeTarget;
            StopAllCoroutines();
            m_GazeState = GazeState.FadeIn;
        }
    }

    public void SetGazeTarget(GameObject gazeTarget)
    {
        SetGazeTargetWithSpeed(gazeTarget, GazeParts.All);
    }

    public void SetGazeTargetWithSpeed(GameObject gazeTarget, GazeParts gazeParts)
    {
        SetGazeTargetWithSpeed(gazeTarget, IsGazePartFlagOn(gazeParts, GazeParts.Head) ? DefaultHeadGazeSpeed : 0,
                               IsGazePartFlagOn(gazeParts, GazeParts.Eyes) ? DefaultEyeGazeSpeed : 0,
                               IsGazePartFlagOn(gazeParts, GazeParts.Body) ? DefaultBodyGazeSpeed : 0);
    }

    public void SetGazeTargetWithSpeed(GameObject gazeTarget, float headSpeed, float eyeSpeed, float bodySpeed)
    {
        if (gazeTarget == m_GazeTarget)
        {
            return;
        }

        InitGaze(gazeTarget);

        if (headSpeed > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(headSpeed, 1, m_GazeTarget.transform.position, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(headSpeed, HeadGazeWeight, m_GazeTarget.transform.position, OnHeadGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentHeadGazeWeight, OnHeadGazeUpdated));
        }

        if (eyeSpeed > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(eyeSpeed, 1, m_GazeTarget.transform.position, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(eyeSpeed, EyeGazeWeight, m_GazeTarget.transform.position, OnEyeGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentEyeGazeWeight, OnEyeGazeUpdated));
        }

        if (bodySpeed > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(bodySpeed, 1, m_GazeTarget.transform.position, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(bodySpeed, BodyGazeWeight, m_GazeTarget.transform.position, OnBodyGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentBodyGazeWeight, OnBodyGazeUpdated));
        }
    }


    public void SetGazeTargetWithDuration(GameObject gazeTarget, float headFadeInDuration, float eyeFadeInDuration, float bodyFadeInDuration)
    {
        if (gazeTarget == m_GazeTarget)
        {
            return;
        }

        InitGaze(gazeTarget);

        if (headFadeInDuration > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(headFadeInDuration, 1, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(headFadeInDuration, HeadGazeWeight, OnHeadGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentHeadGazeWeight, OnHeadGazeUpdated));
        }

        if (eyeFadeInDuration > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(eyeFadeInDuration, 1, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(eyeFadeInDuration, EyeGazeWeight, OnEyeGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentEyeGazeWeight, OnEyeGazeUpdated));
        }

        if (bodyFadeInDuration > 0)
        {
            if (m_GazeState == GazeState.Transitioning)
            {
                StartCoroutine(Gaze(bodyFadeInDuration, 1, OnSwitchGazeUpdated));
            }
            else
            {
                StartCoroutine(Gaze(bodyFadeInDuration, BodyGazeWeight, OnBodyGazeUpdated));
            }
        }
        else
        {
            StartCoroutine(TurnGazeOffCR(DefaultFadeOutTime, CurrentBodyGazeWeight, OnBodyGazeUpdated));
        }
    }

    public void StopGaze()
    {
        StopGaze(DefaultFadeOutTime);
    }

    public void StopGaze(float fadeoutTime)
    {
        StopAllCoroutines();
        TurnGazeOff(fadeoutTime);
    }

    public virtual void UpdateGaze()
    {
    }

    /*IEnumerator TurnGazeOn(float headSpeed, float headWeight, float eyeSpeed, float eyeWeight, Vector3 gazePosition)
    {
        Vector3 toTarget = gazePosition - transform.position;
        float angleBetween = Vector3.Angle(transform.forward, toTarget);
        float timeToReachNeck = angleBetween / headSpeed;
        float timeToReachEye = angleBetween / eyeSpeed;


        float t = 0;
        float timeToReach = Mathf.Max(timeToReachNeck, timeToReachEye);
        while (t <= timeToReach)
        {
            float interp = Mathf.Clamp01(t / timeToReach);

            CurrentHeadGazeWeight = Mathf.Lerp(0, headWeight, interp);
            CurrentEyeGazeWeight = Mathf.Lerp(0, eyeWeight, interp);

            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
        }
    }*/

    IEnumerator Gaze(float speed, float weight, Vector3 gazePosition, OnGazeUpdated onGazeUpdated)
    {
        Vector3 toTarget = gazePosition - transform.position;
        float angleBetween = Vector3.Angle(transform.forward, toTarget);
        float timeToReach = angleBetween / speed;

        yield return StartCoroutine(Gaze(timeToReach, weight, onGazeUpdated));
    }

    IEnumerator Gaze(float duration, float weight, OnGazeUpdated onGazeUpdated)
    {
        float timeToReach = duration;
        float t = 0;
        while (t <= timeToReach)
        {
            float interp = Mathf.Clamp01(t / timeToReach);

            onGazeUpdated(Mathf.SmoothStep(0, weight, interp));

            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
        }

        onGazeUpdated(weight);
        OnGazeFinished();
    }

    protected virtual void OnGazeFinished()
    {
        if (m_GazeState == GazeState.Transitioning)
        {
            m_GazeTarget = m_FutureGazeTarget;
            //m_FutureGazeTarget = null;
            m_SwitchWeight = 0;
            m_GazeState = GazeState.On;
        }
        else if (m_GazeState == GazeState.FadeOut)
        {
            m_GazeState = GazeState.Off;
            m_GazeTarget = null;
        }
        else if (m_GazeState == GazeState.FadeIn)
        {
            m_GazeState = GazeState.On;
        }
    }

    void TurnGazeOff(float fadeoutTime)
    {
        StartCoroutine(TurnGazeOffCR(fadeoutTime, CurrentTotalGazeWeight, OnTotalGazeUpdated));
        StartCoroutine(TurnGazeOffCR(fadeoutTime, CurrentHeadGazeWeight, OnHeadGazeUpdated));
        StartCoroutine(TurnGazeOffCR(fadeoutTime, CurrentEyeGazeWeight, OnEyeGazeUpdated));
        StartCoroutine(TurnGazeOffCR(fadeoutTime, CurrentBodyGazeWeight, OnBodyGazeUpdated));
        m_GazeState = GazeState.FadeOut;
    }

    IEnumerator TurnGazeOffCR(float fadeoutTime, float initialWeight, OnGazeUpdated onGazeUpdated)
    {
        float t = 0;
        float startingGazeWeight = initialWeight;
        while (t < fadeoutTime)
        {
            float interp = t / fadeoutTime;

            onGazeUpdated(Mathf.SmoothStep(startingGazeWeight, 0, interp));
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
        }

        OnGazeFinished();
    }

    public Vector3 GetGazePosition()
    {
        if (m_GazeState == GazeState.Transitioning)
        {
            return new Vector3(Mathf.Lerp(m_GazeTarget.transform.position.x, m_FutureGazeTarget.transform.position.x, m_SwitchWeight),
                               Mathf.Lerp(m_GazeTarget.transform.position.y, m_FutureGazeTarget.transform.position.y, m_SwitchWeight),
                               Mathf.Lerp(m_GazeTarget.transform.position.z, m_FutureGazeTarget.transform.position.z, m_SwitchWeight));
        }
        else
        {
            return m_GazeTarget.transform.position;
        }
    }

    void OnTotalGazeUpdated(float weight) { CurrentTotalGazeWeight = weight; }
    void OnHeadGazeUpdated(float weight) { CurrentHeadGazeWeight = weight; }
    void OnEyeGazeUpdated(float weight) { CurrentEyeGazeWeight = weight; }
    void OnBodyGazeUpdated(float weight) { CurrentBodyGazeWeight = weight; }
    void OnSwitchGazeUpdated(float weight) { m_SwitchWeight = weight; }

    bool IsGazePartFlagOn(GazeParts flagsToCheck, GazeParts flag)
    {
        return (flagsToCheck & flag) == flag;
    }
    #endregion
}
}
