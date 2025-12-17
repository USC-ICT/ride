using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public abstract class FacialAnimationPlayer : MonoBehaviour
{
    #region Constants

    public enum FaceShape
    {
        FV,
        open,
        PBM,
        ShCh,
        tBack,
        tRoof,
        tTeeth,
        W,
        wide,
        face_neutral,  // special case, not a speech viseme.  when all other visemes are 0, this is 1
    }

    public static readonly FaceShape[] FaceShapeValues = (FaceShape[])Enum.GetValues(typeof(FaceShape));

    public enum EasingEquation
    {
        Linear,
        SmoothStep,
        Quadratic,
        //Bezier_Quadratic,
        //Bezier_Cubic,
        Sinusoidal,
    }

    private const int NumSegmentsPerSecond = 30;

    public delegate void OnSetViseme(string viseme, float weight);
    public delegate void OnFinishedPlaying();

    [Serializable]
    public class VisemeModifierData
    {
        public string m_Name;
        public float m_WeightMultiplier;

        public VisemeModifierData(FaceShape faceShape, float multiplier) : this(faceShape.ToString(), multiplier) { }
        public VisemeModifierData(string visemeName, float multiplier)
        {
            m_Name = visemeName;
            m_WeightMultiplier = multiplier;
        }
    }

    /// <summary>Represents a single scheduled viseme animation segment.</summary>
    private class VisemeAnimation
    {
        public FaceShape Shape;
        public float StartArticulation;
        public float TargetArticulation;
        public float StartTime;
        public float Duration;
        public BMLParser.CurveData CurveData;

        public VisemeAnimation(
            FaceShape shape,
            float startArticulation,
            float targetArticulation,
            float startTime,
            float duration,
            BMLParser.CurveData curveData)
        {
            Shape = shape;
            StartArticulation = startArticulation;
            TargetArticulation = targetArticulation;
            StartTime = startTime;
            Duration = Mathf.Max(duration, 0f);
            CurveData = curveData;
        }
    }

    #endregion

    #region Fields
    [SerializeField] private EasingEquation m_EasingEquation = EasingEquation.Linear;
    [SerializeField] private bool m_UseCurveSmoothing = true;

    [SerializeField] private bool m_EnableCoarticulation = false;

    [Tooltip("How strongly neighboring visemes influence each other (0 = off, 1 = strong).")]
    [SerializeField, Range(0f, 1f)] private float m_CoarticulationStrength = 0.15f;

    // for some reason, visemes driven by mecanim are much less exagerated than those driven by smartbody
    // this variable helps to solve that problem, but this needs further investigation
    [SerializeField] protected float m_FacialVisemeMultiplier = 1f;
    [SerializeField] protected VisemeModifierData[] m_VisemeModifiers = 
    {
        new(FaceShape.FV, 1),
        new(FaceShape.open, 1),
        new(FaceShape.PBM, 1),
        new(FaceShape.ShCh, 1),
        new(FaceShape.tBack, 1),
        new(FaceShape.tRoof, 1),
        new(FaceShape.tTeeth, 1),
        new(FaceShape.W, 1),
        new(FaceShape.wide, 1),
    };

    private OnSetViseme m_OnSetVisemeCallback;
    private OnFinishedPlaying m_OnFinishedPlayingCallback;

    private List<VisemeAnimation> m_ActiveVisemeAnimations = new(32);

    // Raw per-viseme weights for this frame (before coarticulation).
    private Dictionary<FaceShape, float> m_VisemeWeights = new();
    #endregion

    #region Properties

    //public float RampInPct { get { return m_RampInPct; } }
    //public float RampOutPct { get { return m_RampOutPct; } }

    public bool UseCurveSmoothing { get => m_UseCurveSmoothing; set => m_UseCurveSmoothing = value; }
    public bool EnableCoarticulation { get => m_EnableCoarticulation; set => m_EnableCoarticulation = value; }
    public float CoarticulationStrength { get => m_CoarticulationStrength; set => m_CoarticulationStrength = value; }
    public EasingEquation CurvePointEasingEquation { get => m_EasingEquation; set => m_EasingEquation = value; }
    public float VisemeWeightMultiplier { get => m_FacialVisemeMultiplier; set => m_FacialVisemeMultiplier = value; }
    public IEnumerable<VisemeModifierData> VisemeModifiers => m_VisemeModifiers;
    #endregion


    #region Functions

    private void Update()
    {
        if (m_ActiveVisemeAnimations.Count == 0)
            return;

        float now = Time.time;
        bool anyUpdated = false;

        // Iterate backwards so we can remove finished animations in-place.
        for (int i = m_ActiveVisemeAnimations.Count - 1; i >= 0; i--)
        {
            var anim = m_ActiveVisemeAnimations[i];

            // Not started yet.
            if (now < anim.StartTime)
                continue;

            float elapsed = now - anim.StartTime;
            float duration = Mathf.Max(anim.Duration, Mathf.Epsilon);
            float clampedTime = Mathf.Min(elapsed, duration);

            float articulation = HandleEasing(anim.StartArticulation, anim.TargetArticulation, clampedTime, duration, anim.CurveData);

            SetViseme(ToVisemeName(anim.Shape), articulation);
            m_OnSetVisemeCallback?.Invoke(ToVisemeName(anim.Shape), articulation);
            UpdateVisemeWeightCache(anim.Shape, articulation);
            anyUpdated = true;

            if (elapsed >= duration)
            {
                // Ensure we end exactly at the target articulation, then remove the animation segment.
                UpdateVisemeWeightCache(anim.Shape, anim.TargetArticulation);
                m_ActiveVisemeAnimations.RemoveAt(i);
            }
        }

        if (anyUpdated)
        {
            // Post-pass: apply coarticulation and update visemes + neutral.
            float totalSpeech = 0f;

            foreach (FaceShape shape in FaceShapeValues)
            {
                if (shape == FaceShape.face_neutral)
                    continue;

                m_VisemeWeights.TryGetValue(shape, out float rawWeight);

                // First apply neighbor-based coarticulation.
                float finalWeight = ApplyNeighborCoarticulation(shape, rawWeight);
                finalWeight = Mathf.Clamp01(finalWeight);

                // Update viseme with final weight.
                SetViseme(ToVisemeName(shape), finalWeight);
                m_OnSetVisemeCallback?.Invoke(ToVisemeName(shape), finalWeight);

                totalSpeech += finalWeight;
            }

            float neutralAmount = 1f - totalSpeech;
            SetViseme(ToVisemeName(FaceShape.face_neutral), neutralAmount);
            m_OnSetVisemeCallback?.Invoke(ToVisemeName(FaceShape.face_neutral), neutralAmount);
        }
    }

    abstract protected void SetViseme(string viseme, float weight);
    abstract protected float GetViseme(string viseme);

    public void AddOnSetVisemeCallback(OnSetViseme cb) => m_OnSetVisemeCallback += cb;
    public void AddOnFinishedPlayingCallback(OnFinishedPlaying cb) => m_OnFinishedPlayingCallback += cb;

    public void Play(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return;

        float latestEndTime = ScheduleWordTimings(timings);

        // Wait until the last scheduled word/viseme segment should have finished.
        StartCoroutine(WaitForScheduleToFinish(latestEndTime));
    }

    public void Play(BMLReader.UtteranceTiming uttTiming)
    {
        if (uttTiming == null || uttTiming.m_CurveData == null || uttTiming.m_CurveData.Count == 0)
            return;

        float latestEndTime = Time.time;

        foreach (var curveData in uttTiming.m_CurveData)
        {
            var smoothedCurve = curveData;
            if (UseCurveSmoothing)
                smoothedCurve = SmoothCurve(curveData, NumSegmentsPerSecond);

            float curveEndTime = ScheduleVisemeCurve(smoothedCurve);
            if (curveEndTime > latestEndTime)
                latestEndTime = curveEndTime;
        }

        // Wait until the last scheduled curve should have finished, then fire the callback.
        StartCoroutine(WaitForScheduleToFinish(latestEndTime));
    }

    /// <summary>
    /// Schedules or immediately applies a viseme animation for the given viseme name.
    /// 
    /// Behavior depends on the value of <paramref name="duration"/>:
    /// 
    /// 1) duration <= 0.0:
    ///    The viseme is set immediately and held indefinitely until another call
    ///    modifies it. No animation segments are scheduled. The viseme weight cache
    ///    is updated and the neutral viseme ("face_neutral") is recomputed based
    ///    on the new viseme mix.
    /// 
    /// 2) duration  > 0.0:
    ///    A timed viseme event is scheduled. The animation has two phases:
    ///      - Attack: current viseme value ramps to <paramref name="articulation"/>
    ///        starting at time + <paramref name="delay"/>, taking
    ///        (<paramref name="blendTime"/> / <paramref name="duration"/>) of the
    ///        total duration.
    ///      - Release: the viseme ramps from <paramref name="articulation"/> back to
    ///        zero over the remainder of <paramref name="duration"/>.
    /// 
    ///    The viseme returns to zero at the end of the scheduled event. This mode
    ///    is intended for speech events, phoneme curves, and short facial actions.
    /// 
    /// Notes:
    /// - <paramref name="viseme"/> must match a valid viseme key used by the
    ///   underlying facial implementation.
    /// - <paramref name="articulation"/> is clamped to [0, 1].
    /// - This method does not block; timed events are executed via coroutine.
    /// 
    /// Parameters:
    /// <param name="viseme">The viseme name to animate.</param>
    /// <param name="articulation">Target viseme weight in [0, 1].</param>
    /// <param name="delay">Delay in seconds before the animation begins.</param>
    /// <param name="duration">Total animation time. If <= 0, the viseme is set permanently.</param>
    /// <param name="blendTime">
    /// Portion of <paramref name="duration"/> used for the attack phase.
    /// If > duration, it is clamped.
    /// </param>
    /// </summary>
    public void RampViseme(string viseme, float articulation, float delay, float duration, float blendTime)
    {
        if (TryParseViseme(viseme, out FaceShape shape))
            RampViseme(shape, articulation, delay, duration, blendTime);
        else
            Debug.LogWarning($"RampViseme() - Cannot ramp unknown viseme '{viseme}'");
    }

    /// <summary>
    /// Schedules or immediately applies a viseme animation for the given viseme name.
    /// 
    /// Behavior depends on the value of <paramref name="duration"/>:
    /// 
    /// 1) duration <= 0.0:
    ///    The viseme is set immediately and held indefinitely until another call
    ///    modifies it. No animation segments are scheduled. The viseme weight cache
    ///    is updated and the neutral viseme ("face_neutral") is recomputed based
    ///    on the new viseme mix.
    /// 
    /// 2) duration  > 0.0:
    ///    A timed viseme event is scheduled. The animation has two phases:
    ///      - Attack: current viseme value ramps to <paramref name="articulation"/>
    ///        starting at time + <paramref name="delay"/>, taking
    ///        (<paramref name="blendTime"/> / <paramref name="duration"/>) of the
    ///        total duration.
    ///      - Release: the viseme ramps from <paramref name="articulation"/> back to
    ///        zero over the remainder of <paramref name="duration"/>.
    /// 
    ///    The viseme returns to zero at the end of the scheduled event. This mode
    ///    is intended for speech events, phoneme curves, and short facial actions.
    /// 
    /// Notes:
    /// - <paramref name="faceShape"/> must match a valid viseme key used by the
    ///   underlying facial implementation.
    /// - <paramref name="articulation"/> is clamped to [0, 1].
    /// - This method does not block; timed events are executed via coroutine.
    /// 
    /// Parameters:
    /// <param name="faceShape">The viseme name to animate.</param>
    /// <param name="articulation">Target viseme weight in [0, 1].</param>
    /// <param name="delay">Delay in seconds before the animation begins.</param>
    /// <param name="duration">Total animation time. If <= 0, the viseme is set permanently.</param>
    /// <param name="blendTime">
    /// Portion of <paramref name="duration"/> used for the attack phase.
    /// If > duration, it is clamped.
    /// </param>
    /// </summary>
    public void RampViseme(FaceShape faceShape, float articulation, float delay, float duration, float blendTime)
    {
        duration = Mathf.Abs(duration);
        if (duration <= 0f)
        {
            SetViseme(ToVisemeName(faceShape), articulation);
            m_OnSetVisemeCallback?.Invoke(ToVisemeName(faceShape), articulation);
            UpdateVisemeWeightCache(faceShape, articulation);

            float neutralAmount = ComputeNeutralAmountFromVisemes();
            SetViseme(ToVisemeName(FaceShape.face_neutral), neutralAmount);
            m_OnSetVisemeCallback?.Invoke(ToVisemeName(FaceShape.face_neutral), neutralAmount);
            return;
        }

        float rampPct = Mathf.Clamp01(blendTime / duration);

        StartCoroutine(RampViseme(faceShape, articulation, delay, duration, rampPct, rampPct));
    }

    /// <summary>
    /// Stops the face from animating and ramps all visemes back to 0 
    /// </summary>
    public void Stop()
    {
        StopAllCoroutines();

        // Stop any in-progress viseme animation segments.
        m_ActiveVisemeAnimations.Clear();

        const float RampDownTime = 0.25f;
        RampViseme(FaceShape.FV, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.open, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.PBM, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.ShCh, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.tBack, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.tRoof, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.tTeeth, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.W, 0, 0, RampDownTime, RampDownTime);
        RampViseme(FaceShape.wide, 0, 0, RampDownTime, RampDownTime);
    }

    public void ResetViseme()
    {
        // Clear all tracked speech viseme weights to 0.
        foreach (FaceShape shape in FaceShapeValues)
        {
            if (shape == FaceShape.face_neutral)
                continue;

            m_VisemeWeights[shape] = 0f;
        }

        SetViseme(ToVisemeName(FaceShape.face_neutral), 1f);
        m_OnSetVisemeCallback?.Invoke(ToVisemeName(FaceShape.face_neutral), 1f);
    }

    public void SetVisemeModifierWeightMultiplier(string viseme, float multiplier)
    {
        var mod = GetVisemeModifierData(viseme);
        if (mod != null)
            mod.m_WeightMultiplier = multiplier;
    }

    public float GetVisemeModifierWeightMultiplier(string viseme)
    {
        float weightMultiplier = 1f;
        var mod = GetVisemeModifierData(viseme);
        if (mod != null)
            weightMultiplier = mod.m_WeightMultiplier;

        return weightMultiplier;
    }

    protected VisemeModifierData GetVisemeModifierData(string viseme) =>
        Array.Find(m_VisemeModifiers, m => string.Compare(m.m_Name, viseme, true) == 0);


    /// <summary>
    /// Coroutine that performs the two-phase timed viseme animation scheduled by
    /// the public <see cref="RampViseme(string, float, float, float, float)"/> method.
    /// 
    /// This routine is only used when the caller requested a timed animation
    /// (duration > 0). It performs:
    /// 
    /// 1) Optional delay.
    /// 2) Attack phase: weight ramps from 0 to <paramref name="articulation"/>
    ///    over (duration * <paramref name="rampInPct"/>).
    /// 3) Release phase: weight ramps from <paramref name="articulation"/> back to
    ///    zero over (duration * <paramref name="rampOutPct"/>).
    /// 
    /// At each update:
    /// - The viseme weight is applied via <see cref="SetViseme"/>.
    /// - The viseme weight cache is updated.
    /// - The neutral viseme ("face_neutral") is recomputed from the current weights.
    /// 
    /// This routine does not run when duration <= 0, because in that case the
    /// viseme is applied immediately and permanently by the public overload.
    /// 
    /// Parameters:
    /// <param name="faceShape">The viseme being animated.</param>
    /// <param name="articulation">Maximum intensity reached during the attack phase.</param>
    /// <param name="delay">Delay before animation begins.</param>
    /// <param name="duration">Total animation duration.</param>
    /// <param name="rampInPct">Portion of the duration used for the attack phase.</param>
    /// <param name="rampOutPct">Portion of the duration used for the release phase.</param>
    /// </summary>
    private IEnumerator RampViseme(FaceShape faceShape, float articulation, float delay, float duration, float rampInPct, float rampOutPct)
    {
        duration = Mathf.Abs(duration);

        float currentArticulation = GetViseme(ToVisemeName(faceShape));
        float rampInTime = duration * Mathf.Clamp01(rampInPct);
        float rampOutTime = duration * Mathf.Clamp01(rampOutPct);
        float delayAbs = Mathf.Abs(delay);

        // Schedule ramp-in and ramp-out segments.
        // Hold is implied by the absence of additional segments between them.
        float now = Time.time;

        if (rampInTime > 0f)
            ScheduleVisemeAnimation(faceShape, currentArticulation, articulation, now + delayAbs, rampInTime, null);

        float holdTime = duration - (rampInTime + rampOutTime);
        float rampOutStartOffset = delayAbs + rampInTime + Mathf.Max(holdTime, 0f);

        if (rampOutTime > 0f)
            ScheduleVisemeAnimation(faceShape, articulation, 0f, now + rampOutStartOffset, rampOutTime, null);

        // Preserve coroutine wait behavior for code that relies on it:
        // delay + duration covers ramp in, hold, and ramp out.
        yield return new WaitForSeconds(delayAbs + duration);
    }

    private void ScheduleVisemeAnimation(
        FaceShape faceShape,
        float startArticulation,
        float targetArticulation,
        float startTime,
        float duration,
        BMLParser.CurveData curveData)
    {
        var anim = new VisemeAnimation(faceShape, startArticulation, targetArticulation, startTime, duration, curveData);
        m_ActiveVisemeAnimations.Add(anim);
    }

    /// <summary>
    /// Schedules animation segments for a BML viseme curve
    /// </summary>
    private float ScheduleVisemeCurve(BMLParser.CurveData curveData)
    {
        float baseTime = Time.time;

        if (curveData == null || curveData.numKeys == 0)
            return baseTime;

        float prevStartTime = 0f;
        float prevArticulation = 0f;
        float lastKeyTime = 0f;

        // This reproduces the per-key segment scheduling from the old PlayVisemeCurve coroutine.
        for (int i = 0; i < curveData.numKeys; i++)
        {
            float keyTime = curveData.GetTime(i);
            float articulation = curveData.GetArticulation(i);

            // Duration is the gap from previous key's time to this key's time.
            float rampDuration = keyTime - prevStartTime;

            if (rampDuration > 0f)
            {
                float segmentStartTime = baseTime + prevStartTime;
                if (TryParseViseme(curveData.name, out FaceShape shape))
                    ScheduleVisemeAnimation(shape, prevArticulation, articulation, segmentStartTime, rampDuration, curveData);
            }

            prevStartTime = keyTime;
            prevArticulation = articulation;
            lastKeyTime = keyTime;
        }

        // The last key's time marks the end of this curve in local time.
        // Convert that to an absolute Time.time value.
        return baseTime + lastKeyTime;
    }

    /// <summary>
    /// Builds viseme animation segments from a list of WordTiming entries.
    /// Schedules all segments up front and returns the absolute time when the last segment should finish.
    /// </summary>
    private float ScheduleWordTimings(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return Time.time;

        float now = Time.time;
        float currentBaseTime = now;
        float latestEndTime = now;

        // These match the 0.4f / 0.4f ramp fractions used previously.
        const float DefaultRampInPct = 0.4f;
        const float DefaultRampOutPct = 0.4f;

        foreach (var wordTiming in timings)
        {
            if (wordTiming.m_VisemesUsed == null)
            {
                currentBaseTime += wordTiming.Duration;
                continue;
            }

            foreach (var visemeData in wordTiming.m_VisemesUsed)
            {
                // Original logic:
                //  delay   = visemeData.start - wordTiming.start;
                //  duration = wordTiming.end - visemeData.start;
                float localDelay = visemeData.start - wordTiming.start;
                float visemeDuration = wordTiming.end - visemeData.start;

                if (visemeDuration <= 0f)
                    continue;

                float rampInTime = visemeDuration * Mathf.Clamp01(DefaultRampInPct);
                float rampOutTime = visemeDuration * Mathf.Clamp01(DefaultRampOutPct);
                float holdTime = Mathf.Max(0f, visemeDuration - (rampInTime + rampOutTime));

                float attackStartTime = currentBaseTime + Mathf.Max(0f, localDelay);
                float attackEndTime = attackStartTime + rampInTime;
                float releaseStartTime = attackStartTime + rampInTime + holdTime;
                float releaseEndTime = releaseStartTime + rampOutTime;

                if (TryParseViseme(visemeData.type, out FaceShape shape))
                {
                    // Attack: from current viseme value to target articulation.
                    // Since we're scheduling up front, we assume starting from 0 (neutral) is acceptable.
                    if (rampInTime > 0f)
                        ScheduleVisemeAnimation(shape, 0f, visemeData.articulation, attackStartTime, rampInTime, null);

                    // Release: from articulation back to 0.
                    if (rampOutTime > 0f)
                        ScheduleVisemeAnimation(shape, visemeData.articulation, 0f, releaseStartTime, rampOutTime, null);
                }

                if (releaseEndTime > latestEndTime)
                    latestEndTime = releaseEndTime;
            }

            // Wait wordTiming.Duration between words, by advancing the base time by the same amount.
            currentBaseTime += wordTiming.Duration;
        }

        return latestEndTime;
    }

    // Waits until the given absolute time has passed, then fires the finished callback.
    // Used by both Play(...) overloads after scheduling all viseme segments.
    private IEnumerator WaitForScheduleToFinish(float scheduledEndTime)
    {
        while (Time.time < scheduledEndTime)
            yield return null;

        m_OnFinishedPlayingCallback?.Invoke();
    }

    /// <summary>
    /// Tracks the latest articulation value for non-neutral visemes.
    /// </summary>
    private void UpdateVisemeWeightCache(FaceShape shape, float weight)
    {
        if (shape == FaceShape.face_neutral)
            return;

        weight = Mathf.Clamp01(weight);

        if (m_VisemeWeights.ContainsKey(shape))
            m_VisemeWeights[shape] = weight;
        else
            m_VisemeWeights.Add(shape, weight);
    }

    /// <summary>
    /// Computes face_neutral as 1 - sum(all speech viseme weights), clamped to [0,1].
    /// Only FaceShape entries are considered speech visemes.
    /// </summary>
    private float ComputeNeutralAmountFromVisemes()
    {
        float total = 0f;

        foreach (FaceShape shape in FaceShapeValues)
        {
            if (shape == FaceShape.face_neutral)
                continue;

            if (m_VisemeWeights.TryGetValue(shape, out float value))
                total += Mathf.Clamp01(value);
        }

        return Mathf.Clamp01(1f - total);
    }


    private static BMLParser.CurveData SmoothCurve(BMLParser.CurveData curveData, int segmentsPerSecond)
    {
        float curveTimeSpan = curveData.GetSpan();

        // add 2 (1 for floating point rounding up 1 for adding an additional key to return the viseme to 0 at the end
        int numSegs = (int)(curveTimeSpan * segmentsPerSecond) + 2;

        var newCurve = new BMLParser.CurveData(curveData.name, curveData.owner, numSegs);

        float dt = curveTimeSpan / numSegs;
        float t = curveData.GetTime(0);
        for (int i = 0; i < numSegs - 1; i++)
        {
            float articulation = EvaluateCurve(t, curveData);
            newCurve.AddKey(t, articulation, i);
            t += dt;
        }

        // return to 0
        newCurve.AddKey(t, 0f, numSegs - 1);
        newCurve.SortKeysByTime();
        return newCurve;
    }

    private static float EvaluateCurve(float t, BMLParser.CurveData curveData)
    {
        float weight = 0f;
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
            weight = Hermite(t, curveIndex, curveIndex + 1, curveData);

        return weight;
    }

    /// <summary>
    /// Defines simple neighbor relationships between visemes for the purpose of 
    /// applying coarticulation. 
    ///
    /// Coarticulation is the natural phenomenon where the articulation of a 
    /// phoneme is influenced by the phonemes immediately before and after it. 
    /// Human speech does not move from one pure mouth shape to another in isolation; 
    /// mouth shapes blend and overlap. For example, lip-rounding for "W" may begin 
    /// before the actual "W" sound and may slightly persist into following phonemes.
    ///
    /// This dictionary maps each <see cref="FaceShape"/> viseme to a small set of 
    /// neighboring visemes whose shapes commonly affect it. During playback, each 
    /// viseme's raw weight is blended slightly toward the average weight of its 
    /// defined neighbors, controlled by <c>m_CoarticulationStrength</c>. 
    ///
    /// This implementation is intentionally simple and conservative. It does not 
    /// attempt full diphone or triphone modeling; instead, it provides a minimal 
    /// "bleed" of neighboring mouth shapes to soften transitions and produce more 
    /// natural-looking lipsync without altering input timing or TTS/BML schedules.
    ///
    /// Key idea:
    /// - If neighbor visemes are active (e.g., "W" and "open"), a viseme like "FV" 
    ///   may receive a small influence toward those shapes.
    /// - If neighbors are inactive, no influence is applied.
    /// - The strength of this influence is adjustable and can be disabled entirely.
    ///
    /// The values in this table are not meant to be linguistically exhaustive. 
    /// They serve as a lightweight approximation suitable for real-time lipsync, 
    /// and can be tuned or expanded as needed.
    /// </summary>
    private static readonly Dictionary<FaceShape, FaceShape[]> CoarticulationNeighbors = new()
    {
        { FaceShape.FV,     new[] { FaceShape.PBM, FaceShape.W } },
        { FaceShape.PBM,    new[] { FaceShape.FV, FaceShape.W } },
        { FaceShape.W,      new[] { FaceShape.FV, FaceShape.open } },
        { FaceShape.open,   new[] { FaceShape.W, FaceShape.wide } },
        { FaceShape.wide,   new[] { FaceShape.open, FaceShape.ShCh } },
        { FaceShape.ShCh,   new[] { FaceShape.wide, FaceShape.tTeeth } },
        { FaceShape.tTeeth, new[] { FaceShape.ShCh, FaceShape.tRoof } },
        { FaceShape.tRoof,  new[] { FaceShape.tTeeth, FaceShape.tBack } },
        { FaceShape.tBack,  new[] { FaceShape.tRoof } },
        // face_neutral is excluded on purpose
    };

    /// <summary>
    /// Applies a simple coarticulation adjustment to the given viseme weight.
    ///
    /// Coarticulation refers to the way the articulation of one phoneme is affected 
    /// by neighboring phonemes in natural speech. Human mouths transition smoothly 
    /// between shapes, and the target pose for a phoneme is rarely held in isolation. 
    /// For example, a rounded vowel may cause rounding to begin before the vowel 
    /// and slightly continue afterward.
    ///
    /// This function implements a lightweight approximation of this effect.  
    ///
    /// Process:
    /// - Look up the viseme's defined neighbors in <see cref="CoarticulationNeighbors"/>.
    /// - Compute the average weight of those neighbor visemes for the current frame.
    /// - Blend the viseme's own raw weight toward that neighbor average, using 
    ///   <c>m_CoarticulationStrength</c> as the blend factor.
    ///     - strength = 0 -> no coarticulation (raw weight is returned)  
    ///     - strength = 1 -> full neighbor influence  
    /// - The blend is scaled by m_CoarticulationStrength and by a "mid range"
    ///   factor, so coarticulation is strongest when the weight is in the middle
    ///   (around 0.5) and weakest at the extremes (0 or 1).
    /// - Finally, coarticulation is not allowed to weaken the viseme: if the
    ///   blended value is lower than the base weight, the base weight is kept.
    ///
    /// The intent is not to replace proper phonetic diphone or triphone coarticulation 
    /// models, but to introduce a small, controlled smoothing that softens transitions 
    /// between shapes. This reduces visual "popping" between visemes and produces a 
    /// more natural-looking lipsync motion without altering the underlying timing or 
    /// structure of scheduled viseme animation segments.
    ///
    /// Returns the final adjusted weight for this viseme.
    /// </summary>
    private float ApplyNeighborCoarticulation(FaceShape shape, float baseWeight)
    {
        if (!m_EnableCoarticulation || m_CoarticulationStrength <= 0f)
            return baseWeight;

        if (!CoarticulationNeighbors.TryGetValue(shape, out var neighbors) || neighbors == null || neighbors.Length == 0)
            return baseWeight;

        float neighborSum = 0f;
        int neighborCount = 0;

        foreach (var neighbor in neighbors)
        {
            if (m_VisemeWeights.TryGetValue(neighbor, out float neighborWeight))
            {
                neighborSum += neighborWeight;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
            return baseWeight;

        float neighborAvg = neighborSum / neighborCount;

        // Coarticulation should be strongest in the mid range and weaker at extremes.
        // When baseWeight is near 0 or 1, midFactor is near 0.
        // When baseWeight is near 0.5, midFactor is near 1.
        float midFactor = 1f - Mathf.Abs(baseWeight - 0.5f) * 2f;
        midFactor = Mathf.Clamp01(midFactor);

        float effectiveStrength = m_CoarticulationStrength * midFactor;
        if (effectiveStrength <= 0f)
            return baseWeight;

        float blended = Mathf.Lerp(baseWeight, neighborAvg, effectiveStrength);

        // Do not allow coarticulation to weaken a strong viseme.
        // If the blended value is lower than the original, keep the original.
        if (blended < baseWeight)
            return baseWeight;

        return blended;
    }

    private static float Hermite(float t, int startCurveIndex, int endCurveIndex, BMLParser.CurveData curveData)
    {
        float startTime = curveData.GetTime(startCurveIndex);
        float startWeight = curveData.GetArticulation(startCurveIndex);
        float endTime = curveData.GetTime(endCurveIndex);
        float endWeight = curveData.GetArticulation(endCurveIndex);

        float dp = endTime - startTime;
        if (dp < 0.0f || dp < 0.000000001f)
            return startWeight;

        float s = (t - startTime) / dp; // normalize parametric interpolant

        // FaceFX algorithm from
        //  http://www.facefx.com/documentation/2010/W99
        //float m1 = K1.mr() * K1.dr();
        //float m2 = K2.ml() * K2.dl();
        float m1 = curveData.GetSlopeOut(startCurveIndex) * dp;
        float m2 = curveData.GetSlopeIn(endCurveIndex) * dp;
        //BMLParser.CurveData.SlopeData startSlope = curveData.GetSlopeData(startCurveIndex);
        //BMLParser.CurveData.SlopeData endSlope = curveData.GetSlopeData(endCurveIndex);
        //float m1 = 1;
        //float m2 = 1;
        //if (startSlope != null && endSlope != null)
        //{
        //    m1 = startSlope.mr * startSlope.dr;
        //    m2 = endSlope.ml * endSlope.dl;
        //}

        return Hermite(s, startWeight, endWeight, m1, m2);
    }

    private static float Hermite(float s, float v1, float v2, float m1, float m2) =>
        Bezier(s, v1, v1 + m1 * 0.333333333f, v2 - m2 * 0.333333333f, v2);

    private static float Bezier(float s, float f0, float f1, float f2, float f3)
    {
        // de Casteljau linear recursion
        float A = f0 + s * (f1 - f0);
        float B = f1 + s * (f2 - f1);
        float C = A + s * (B - A);
        return C + s * ((B + s * ((f2 + s * (f3 - f2)) - B)) - C);
    }

    #region Easing Equations
    private float HandleEasing(float startArticulation, float targetArticulation, float currentTime, float duration, BMLParser.CurveData curveData)
    {
        float interpolation = 0;
        float t = duration > 0f ? currentTime / duration : 1f;
        float change = targetArticulation - startArticulation;
        switch (m_EasingEquation)
        {
            case EasingEquation.Linear:     interpolation = Mathf.Lerp(startArticulation, targetArticulation, t); break;
            case EasingEquation.SmoothStep: interpolation = Mathf.SmoothStep(startArticulation, targetArticulation, t); break;
            case EasingEquation.Quadratic:  interpolation = QuadraticEaseOut(startArticulation, change, currentTime, duration); break;
            case EasingEquation.Sinusoidal: interpolation = SinusoidalEaseInOut(startArticulation, targetArticulation, currentTime, duration); break;

            //case EasingEquation.Bezier_Cubic:
            //    int numSections = curveData.numKeys - 3;
            //    if (numSections > 0)
            //    {
            //        int keyFrame = GetCurrentPointIndex(t, numSections);
            //        interpolation = InterpolateBezierCubic(curveData.GetTime(keyFrame), curveData.GetTime(keyFrame + 1), curveData.GetTime(keyFrame + 2),
            //            curveData.GetTime(keyFrame + 3), t * numSections - keyFrame);
            //    }
            //    break;

            default: Debug.LogError($"Easing type not handled: {m_EasingEquation}. Lip sync won't work for character {name}"); break;
        }

        return interpolation;
    }

    private static int GetCurrentPointIndex(float t, int numSections) =>
        Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(t) * numSections), numSections - 1);

    // cubic berzier curve
    private static float InterpolateBezierCubic(float a, float b, float c, float d, float u) =>
        0.5f * ((-a + 3.0f * b - 3.0f * c + d) * (u * u * u) + (2.0f * a - 5.0f * b + 4.0f * c - d) * (u * u) + (-a + c) * u + 2.0f * b);

    private static float QuadraticEaseOut(float start, float change, float time, float duration)
    {
        time /= duration / 2f;
        if (time < 1f)
            return change / 2f * time * time + start;

        time -= 1f;
        return -change / 2f * (time * (time - 2f) - 1f) + start;
    }

    private static float SinusoidalEaseInOut(float start, float change, float time, float duration) =>
        -change / 2f * (Mathf.Cos(Mathf.PI * time / duration) - 1f) + start;

    #endregion

    protected static string ToVisemeName(FaceShape shape) => shape.ToString(); // if names match exactly, this is enough
    protected static bool TryParseViseme(string name, out FaceShape shape) => Enum.TryParse(name, ignoreCase: false, out shape);

    /*
    void CalculateKeyDeltas(BMLParser.CurveData curveData)
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
    }
    */

    #endregion
}
}
