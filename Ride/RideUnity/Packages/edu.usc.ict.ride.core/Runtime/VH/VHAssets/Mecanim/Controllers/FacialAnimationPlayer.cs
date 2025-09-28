using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public abstract class FacialAnimationPlayer : MonoBehaviour
{
    #region Constants
    public enum EasingEquation
    {
        Linear,
        SmoothStep,
        Quadratic,
        //Bezier_Quadratic,
        //Bezier_Cubic,
        Sinusoidal,
    }

    const int NumSegmentsPerSecond = 30;

    public delegate void OnSetViseme(string viseme, float weight);
    public delegate void OnFinishedPlaying();

    [Serializable]
    public class VisemeModifierData
    {
        public string m_Name = "";
        public float m_WeightMultiplier = 1;

        public VisemeModifierData(string visemeName, float multiplier)
        {
            m_Name = visemeName;
            m_WeightMultiplier = multiplier;
        }
    }
    #endregion

    #region Variables
    [SerializeField] EasingEquation m_EasingEquation = EasingEquation.Linear;
    [SerializeField] bool m_UseCurveSmoothing = true;
    // for some reason, visemes driven by mecanim are much less exagerated than those driven by smartbody
    // this variable helps to solve that problem, but this needs further investigation
    [SerializeField] protected float m_FacialVisemeMultiplier = 1;
    [SerializeField] protected VisemeModifierData[] m_VisemeModifiers = new VisemeModifierData[]
    {
        new VisemeModifierData("FV", 1),
        new VisemeModifierData("open", 1),
        new VisemeModifierData("PBM", 1),
        new VisemeModifierData("ShCh", 1),
        new VisemeModifierData("tBack", 1),
        new VisemeModifierData("tRoof", 1),
        new VisemeModifierData("W", 1),
        new VisemeModifierData("wide", 1),
    };

    OnSetViseme m_OnSetVisemeCallback;
    OnFinishedPlaying m_OnFinishedPlayingCallback;
    #endregion

    #region Properties
    //public float RampInPct { get { return m_RampInPct; } }
    //public float RampOutPct { get { return m_RampOutPct; } }
    public bool UseCurveSmoothing
    {
        get { return m_UseCurveSmoothing; }
        set { m_UseCurveSmoothing = value; }
    }

    public EasingEquation CurvePointEasingEquation
    {
        get { return m_EasingEquation; }
        set { m_EasingEquation = value; }
    }

    public float VisemeWeightMultiplier
    {
        get { return m_FacialVisemeMultiplier; }
        set { m_FacialVisemeMultiplier = value; }
    }

    public IEnumerable<VisemeModifierData> VisemeModifiers
    {
        get { return m_VisemeModifiers; }
    }
    #endregion

    #region Functions

    public void AddOnSetVisemeCallback(OnSetViseme cb)
    {
        m_OnSetVisemeCallback += cb;
    }

    public void AddOnFinishedPlayingCallback(OnFinishedPlaying cb)
    {
        m_OnFinishedPlayingCallback += cb;
    }

    public void Play(List<TtsReader.WordTiming> timings)
    {
        StartCoroutine(PlayAnim(timings));
    }

    /// <summary>
    /// Stops the face from animating and ramps all visemes back to 0 
    /// </summary>
    public void Stop()
    {
        StopAllCoroutines();

        const float RampDownTime = 0.25f;
        RampViseme("FV", 0, 0, RampDownTime, 0);
        RampViseme("open", 0, 0, RampDownTime, 0);
        RampViseme("PBM", 0, 0, RampDownTime, 0);
        RampViseme("ShCh", 0, 0, RampDownTime, 0);
        RampViseme("tBack", 0, 0, RampDownTime, 0);
        RampViseme("tRoof", 0, 0, RampDownTime, 0);
        RampViseme("tTeeth", 0, 0, RampDownTime, 0);
        RampViseme("W", 0, 0, RampDownTime, 0);
        RampViseme("wide", 0, 0, RampDownTime, 0);
    }

    IEnumerator PlayAnim(List<TtsReader.WordTiming> timings)
    {
        foreach (TtsReader.WordTiming wordTiming in timings)
        {
            foreach (TtsReader.VisemeData visemeData in wordTiming.m_VisemesUsed)
            {
                yield return RampViseme(visemeData.type, visemeData.articulation, visemeData.start - wordTiming.start, wordTiming.end - visemeData.start, 0.4f, 0.4f);
            }

            yield return new WaitForSeconds(wordTiming.Duration);
        }

        m_OnFinishedPlayingCallback?.Invoke();
    }

    public void Play(BMLReader.UtteranceTiming uttTiming)
    {
        foreach (BMLParser.CurveData curveData in uttTiming.m_CurveData)
        {
            BMLParser.CurveData smoothedCurve = curveData;
            if (UseCurveSmoothing)
            {
                smoothedCurve = SmoothCurve(curveData, NumSegmentsPerSecond);
            }
            StartCoroutine(PlayVisemeCurve(smoothedCurve));
        }
    }

    IEnumerator PlayVisemeCurve(BMLParser.CurveData curveData)
    {
        if (curveData.numKeys == 0)
        {
            yield break;
        }


        float initialStartTime = Time.time;
        float prevStartTime = 0;
        float prevArticulation = 0;

        for (int i = 0; i < curveData.numKeys; i++)
        {
            float startTime = curveData.GetTime(i);
            float articulation = curveData.GetArticulation(i);

            while (Time.time - initialStartTime < startTime)
                yield return new WaitForEndOfFrame();

            //float defaultRampDuration = 0.1f;
            float rampDuration = startTime - prevStartTime;//Mathf.Min(startTime - prevStartTime, defaultRampDuration);

            StartCoroutine(PlayVisemeInternal(curveData.name, prevArticulation, articulation, rampDuration, curveData));

            prevStartTime = startTime;
            prevArticulation = articulation;
        }

        m_OnFinishedPlayingCallback?.Invoke();
    }

    BMLParser.CurveData SmoothCurve(BMLParser.CurveData curveData, int segmentsPerSecond)
    {
        float curveTimeSpan = curveData.GetSpan();
        // add 2 (1 for floating point rounding up 1 for adding an additional key to return the viseme to 0 at the end
        int numSegs = (int)(curveTimeSpan * (float)segmentsPerSecond) + 2;

        BMLParser.CurveData newCurve = new BMLParser.CurveData(curveData.name, curveData.owner, numSegs);

        float dt = curveTimeSpan / (float)numSegs;
        float t = curveData.GetTime(0);
        for (int i = 0; i < numSegs - 1; i++)
        {
            float articulation = EvaluateCurve(t, curveData);
            newCurve.AddKey(t, articulation, /*curveData.numKeys +*/ i);
            t += dt;
        }

        // return to 0
        newCurve.AddKey(t, 0, numSegs - 1);
        newCurve.SortKeysByTime();
        return newCurve;
    }

    float EvaluateCurve(float t, BMLParser.CurveData curveData)
    {
        float weight = 0;
        int curveIndex = 0;
        for (int i = 1; i < curveData.numKeys; i++)
        {
            if (t < curveData.GetTime(i))
            {
                curveIndex = i - 1;
                break;
            }
        }

        if (curveIndex >= 0 && curveIndex < curveData.numKeys - 1)
        {
            weight = Hermite(t, curveIndex, curveIndex + 1, curveData);
        }

        return weight;
    }

    /*void CalculateKeyDeltas(BMLParser.CurveData curveData)
    {
        const float c = 0.5f;
        for (int i = 0; i < curveData.numKeys - 2; i++)
        {
            float k0Time = curveData.GetTime(i);
            float k1Time = curveData.GetTime(i + 1);
            float k2Time = curveData.GetTime(i + 2);

            float k0Value = curveData.GetArticulation(i);
            float k1Value = curveData.GetArticulation(i + 1);
            float k2Value = curveData.GetArticulation(i + 2);

            float m = ( 1.0f - c ) * ( k2Value - k0Value ) / ( k2Time - k0Time );

            curveData.Set(i, m, m, k1Time - k0Time, k2Time - k1Time);
        }

    }*/

    float Hermite(float t, int startCurveIndex, int endCurveIndex, BMLParser.CurveData curveData)
    {
        float startTime = curveData.GetTime(startCurveIndex);
        float startWeight = curveData.GetArticulation(startCurveIndex);
        float endTime = curveData.GetTime(endCurveIndex);
        float endWeight = curveData.GetArticulation(endCurveIndex);

        float dp = endTime - startTime;
        if (dp < 0.0 || dp < 0.000000001f)
        {
            return startWeight;
        }

        float s = (t - startTime) / dp; // normalize parametric interpolant

        // FaceFX algorithm from
        //  http://www.facefx.com/documentation/2010/W99
        //float m1 = K1.mr() * K1.dr();
        //float m2 = K2.ml() * K2.dl();
        float m1 = curveData.GetSlopeOut(startCurveIndex) * dp;
        float m2 = curveData.GetSlopeIn(endCurveIndex) * dp;
        /*BMLParser.CurveData.SlopeData startSlope = curveData.GetSlopeData(startCurveIndex);
        BMLParser.CurveData.SlopeData endSlope = curveData.GetSlopeData(endCurveIndex);

        float m1 = 1;
        float m2 = 1;
        if (startSlope != null && endSlope != null)
        {
            m1 = startSlope.mr * startSlope.dr;
            m2 = endSlope.ml * endSlope.dl;
        }*/

        return Hermite(s, startWeight, endWeight, m1, m2);
    }

    float Hermite(float s, float v1, float v2, float m1, float m2)
    {
        return Bezier(s, v1, v1 + m1 * 0.333333333f, v2 - m2 * 0.333333333f, v2);
    }

    float Bezier(float s, float f0, float f1, float f2, float f3)
    {
        // de Casteljau linear recursion
        float A = f0 + s * (f1 - f0);
        float B = f1 + s * (f2 - f1);
        float C = A + s * (B - A);
        return C + s * ((B + s * ((f2 + s * (f3 - f2)) - B)) - C);
    }

    public void RampViseme(string viseme, float articulation, float delay, float duration, float blendTime)
    {
        StartCoroutine(RampViseme(viseme, articulation, delay, duration, blendTime / duration, blendTime / duration));
    }

    public void ResetViseme()
    {
        SetViseme("face_neutral", 1f);
    }

    /*public void RampViseme(string viseme, float articulation, float delay, float duration)
    {
        StartCoroutine(RampViseme(viseme, articulation, delay, duration, RampInPct, RampOutPct));
    }*/

    IEnumerator RampViseme(string viseme, float articulation, float delay, float duration, float rampInPct, float rampOutPct)
    {
        duration = Mathf.Abs(duration);

        float currArticulation = GetViseme(viseme);
        float rampInTime = duration * Mathf.Clamp01(rampInPct);
        float rampOutTime = duration * Mathf.Clamp01(rampOutPct);

        if (delay != 0)
        {
            yield return new WaitForSeconds(Mathf.Abs(delay));
        }

        // ramp in
        yield return StartCoroutine(PlayVisemeInternal(viseme, currArticulation, articulation, rampInTime));

        // hold
        yield return new WaitForSeconds(duration - (rampInTime + rampOutTime));

        // ramp out
        yield return StartCoroutine(PlayVisemeInternal(viseme, articulation, 0, rampOutTime));
    }

    IEnumerator PlayVisemeInternal(string viseme, float startArticulation, float targetArticulation, float time)
    {
        yield return StartCoroutine(PlayVisemeInternal(viseme, startArticulation, targetArticulation, time, null));
    }

    IEnumerator PlayVisemeInternal(string viseme, float startArticulation, float targetArticulation, float time, BMLParser.CurveData curveData)
    {
        float articulation = startArticulation;
        float startTime = Time.time;
        float curTime = Time.time - startTime;
        while (curTime < time)
        {
            curTime = Time.time - startTime;

            articulation = HandleEasing(viseme, startArticulation, targetArticulation, curTime, time, curveData);//Mathf.Lerp(startArticulation, targetArticulation, curTime / time);
            SetViseme(viseme, articulation);
            m_OnSetVisemeCallback?.Invoke(viseme, articulation);

            yield return new WaitForEndOfFrame();
        }

        SetViseme(viseme, targetArticulation);
        m_OnSetVisemeCallback?.Invoke(viseme, targetArticulation);
    }

    protected VisemeModifierData GetVisemeModifierData(string viseme)
    {
        return Array.Find(m_VisemeModifiers, m => string.Compare(m.m_Name, viseme, true) == 0);
    }

    public void SetVisemeModifierWeightMultiplier(string viseme, float multiplier)
    {
        VisemeModifierData mod = GetVisemeModifierData(viseme);
        if (mod != null)
        {
            mod.m_WeightMultiplier = multiplier;
        }
    }

    public float GetVisemeModifierWeightMultiplier(string viseme)
    {
        float weightMultiplier = 1;
        VisemeModifierData mod = GetVisemeModifierData(viseme);
        if (mod != null)
        {
            weightMultiplier = mod.m_WeightMultiplier;
        }
        return weightMultiplier;
    }

    abstract protected void SetViseme(string viseme, float weight);
    abstract protected float GetViseme(string viseme);

    #region Easing Equations
    float HandleEasing(string viseme, float startArticulation, float targetArticulation, float currentTime, float duration, BMLParser.CurveData curveData)
    {
        float interpolation = 0;
        float t = currentTime / duration;
        float change = targetArticulation - startArticulation;
        switch (m_EasingEquation)
        {
        case EasingEquation.Linear:
            interpolation = Mathf.Lerp(startArticulation, targetArticulation, t);
            break;

        case EasingEquation.SmoothStep:
            interpolation = Mathf.SmoothStep(startArticulation, targetArticulation, t);
            break;

        case EasingEquation.Quadratic:
            interpolation = QuadraticEaseOut(startArticulation, change, currentTime, duration);
            break;

        case EasingEquation.Sinusoidal:
            interpolation = SinusoidalEaseInOut(startArticulation, targetArticulation, currentTime, duration);
            break;

       /* case EasingEquation.Bezier_Cubic:
            int numSections = curveData.numKeys - 3;
            if (numSections > 0)
            {
                int keyFrame = GetCurrentPointIndex(t, numSections);
                interpolation = InterpolateBezierCubic(curveData.GetTime(keyFrame), curveData.GetTime(keyFrame + 1), curveData.GetTime(keyFrame + 2),
                    curveData.GetTime(keyFrame + 3), t * numSections - keyFrame);
            }
            break;*/

        default:
            Debug.LogError("Easing type not handled: " + m_EasingEquation.ToString() + ". Lip sync won't work for character " + name);
            break;
        }
        return interpolation;
    }

    int GetCurrentPointIndex(float t, int numSections)
    {
        return Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(t) * numSections), numSections - 1);
    }

    public static float InterpolateBezierCubic(float a, float b, float c, float d, float u)
    {
        // cubic berzier curve
        return 0.5f * ((-a + 3.0f * b - 3.0f * c + d) * (u * u * u) + (2.0f * a - 5.0f * b + 4.0f * c - d) * (u * u) + (-a + c) * u + 2.0f * b);
    }

    public static float QuadraticEaseOut(float start, float change, float time, float duration)
    {
        /*time /= duration;
        return -change * time * (time- 2) + start;*/

        time /= duration / 2;
        if (time < 1)
            return change / 2 * time * time + start;

        time--;

        return -change / 2 * (time * (time - 2) - 1) + start;
    }

    public static float SinusoidalEaseInOut(float start, float change, float time, float duration)
    {
        return -change / 2 * (Mathf.Cos(Mathf.PI * time / duration) - 1) + start;
    }

    #endregion

    #endregion
}
}
