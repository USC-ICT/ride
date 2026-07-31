using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        _112_happy,
        _124_disgust,
        _126_fear,
        _127_surprise,
        _129_angry,
        _130_sad,
        _131_contempt,
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
        public int Id;
        public FaceShape Shape;
        public float StartArticulation;
        public float TargetArticulation;
        public float StartTime;
        public float Duration;
        public BMLParser.CurveData CurveData;

        // debug tracking fields
        public float BestObservedArticulation;
        public float ClosestObservedDelta;
        public bool ReachedTargetWithinTolerance;
        public bool WasOverlappedBeforeCompletion;

        public VisemeAnimation(
            int id,
            FaceShape shape,
            float startArticulation,
            float targetArticulation,
            float startTime,
            float duration,
            BMLParser.CurveData curveData)
        {
            Id = id;
            Shape = shape;
            StartArticulation = startArticulation;
            TargetArticulation = targetArticulation;
            StartTime = startTime;
            Duration = Mathf.Max(duration, 0f);
            CurveData = curveData;
            BestObservedArticulation = Mathf.Clamp01(startArticulation);
            ClosestObservedDelta = Mathf.Abs(Mathf.Clamp01(startArticulation) - Mathf.Clamp01(targetArticulation));
            ReachedTargetWithinTolerance = false;
            WasOverlappedBeforeCompletion = false;
        }

        public float EndTime => StartTime + Duration;
    }

    private struct ScheduleDebugEntry
    {
        public float Time;
        public string Name;
        public float Articulation;

        public ScheduleDebugEntry(float time, string name, float articulation)
        {
            Time = time;
            Name = name;
            Articulation = articulation;
        }
    }

    private struct VisemeProblemRecord
    {
        public string VisemeName;
        public string Reason;
        public float TargetArticulation;
        public float BestObservedArticulation;
        public float ClosestObservedDelta;
        public float Duration;

        public VisemeProblemRecord(string visemeName, string reason, float targetArticulation, float bestObservedArticulation, float closestObservedDelta, float duration)
        {
            VisemeName = visemeName;
            Reason = reason;
            TargetArticulation = targetArticulation;
            BestObservedArticulation = bestObservedArticulation;
            ClosestObservedDelta = closestObservedDelta;
            Duration = duration;
        }
    }

    private struct VisemeAggregateRecord
    {
        public int Count;
        public float WorstDelta;

        public VisemeAggregateRecord(int count, float worstDelta)
        {
            Count = count;
            WorstDelta = worstDelta;
        }
    }

    #endregion

    #region Fields
    [SerializeField] private EasingEquation m_EasingEquation = EasingEquation.Linear;
    [SerializeField] private bool m_UseCurveSmoothing = true;
    [SerializeField] private float m_EndOfUtteranceCloseBuffer = 0.5f;

    [SerializeField] private bool m_EnableCoarticulation = false;

    [Tooltip("How strongly neighboring visemes influence each other (0 = off, 1 = strong).")]
    [SerializeField, Range(0f, 1f)] private float m_CoarticulationStrength = 0.15f;

    // for some reason, visemes driven by mecanim are much less exagerated than those driven by smartbody
    // this variable helps to solve that problem, but this needs further investigation
    [SerializeField] protected float m_FacialVisemeMultiplier = 1f;
    [SerializeField] protected float m_FacialExpressionMultiplier = 1f;
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
        new(FaceShape._112_happy, 1),
        new(FaceShape._124_disgust, 1),
        new(FaceShape._126_fear, 1),
        new(FaceShape._127_surprise, 1),
        new(FaceShape._129_angry, 1),
        new(FaceShape._130_sad, 1),
        new(FaceShape._131_contempt, 1),
    };

    [Header("Debug")]
    [SerializeField] private bool m_RuntimeDebugOutput = false;
    [Tooltip("How close the observed viseme weight must get to the scheduled target to count as a hit in debug output. Smaller values are stricter; larger values treat near-misses as successful.")]
    [SerializeField, Range(0f, 1f)] private float m_TargetHitTolerance = 0.1f;
    [Tooltip("Debug threshold for flagging densely packed viseme events. If consecutive scheduled events are closer than this many seconds, the schedule is counted as crowded. Useful for spotting flappy or over-dense lipsync data.")]
    [SerializeField, Min(0f)] private float m_CrowdedScheduleThresholdSeconds = 0.05f;


    private OnSetViseme m_OnSetVisemeCallback;
    private OnFinishedPlaying m_OnFinishedPlayingCallback;

    private List<VisemeAnimation> m_ActiveVisemeAnimations = new(32);
    private int m_NextVisemeAnimationId = 1;
    private string m_DebugScheduleSource = string.Empty;
    private int m_DebugScheduledSpeechSegments;
    private int m_DebugPositiveTargetSegments;
    private int m_DebugOverlapCount;
    private int m_DebugCompletedPositiveTargetSegments;
    private readonly List<VisemeProblemRecord> m_DebugProblemSegments = new(32);

    // Raw per-face-shape weights for this frame (before coarticulation).
    private Dictionary<FaceShape, float> m_FaceShapeWeights = new();
    #endregion

    #region Properties

    //public float RampInPct { get { return m_RampInPct; } }
    //public float RampOutPct { get { return m_RampOutPct; } }

    public bool UseCurveSmoothing { get => m_UseCurveSmoothing; set => m_UseCurveSmoothing = value; }
    public bool EnableCoarticulation { get => m_EnableCoarticulation; set => m_EnableCoarticulation = value; }
    public float CoarticulationStrength { get => m_CoarticulationStrength; set => m_CoarticulationStrength = value; }
    public EasingEquation CurvePointEasingEquation { get => m_EasingEquation; set => m_EasingEquation = value; }
    public float VisemeWeightMultiplier { get => m_FacialVisemeMultiplier; set => m_FacialVisemeMultiplier = value; }
    public float ExpressionWeightMultiplier { get => m_FacialExpressionMultiplier; set => m_FacialExpressionMultiplier = value; }
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
            float observedArticulation = GetObservedScheduleSpaceViseme(anim.Shape);
            TrackObservedArticulation(anim, observedArticulation);

            float articulation = HandleEasing(anim.StartArticulation, anim.TargetArticulation, clampedTime, duration, anim.CurveData);

            // Update the raw per-shape cache only; we apply coarticulation + neutral in the post-pass below.
            UpdateVisemeWeightCache(anim.Shape, articulation);
            anyUpdated = true;

            if (elapsed >= duration)
            {
                // Ensure we end exactly at the target articulation, then remove the animation segment.
                UpdateVisemeWeightCache(anim.Shape, anim.TargetArticulation);
                LogVisemeAnimationCompletion(anim, observedArticulation, "completed");
                m_ActiveVisemeAnimations.RemoveAt(i);
            }
        }

        if (anyUpdated)
        {
            // Post-pass: apply coarticulation and update speech + neutral.
            float totalSpeech = 0f;

            foreach (FaceShape shape in FaceShapeValues)
            {
                if (!IsSpeechShape(shape))
                    continue;

                m_FaceShapeWeights.TryGetValue(shape, out float rawWeight);

                // Neighbor-based coarticulation (speech-only).
                float finalWeight = ApplyNeighborCoarticulation(shape, rawWeight);
                finalWeight = Mathf.Clamp01(finalWeight);

                string shapeName = ToVisemeName(shape);
                SetViseme(shapeName, finalWeight);
                m_OnSetVisemeCallback?.Invoke(shapeName, finalWeight);

                totalSpeech += finalWeight;
            }

            float neutralAmount = Mathf.Clamp01(1f - totalSpeech);
            string neutralName = ToVisemeName(FaceShape.face_neutral);
            SetViseme(neutralName, neutralAmount);
            m_OnSetVisemeCallback?.Invoke(neutralName, neutralAmount);

            // Post-pass: apply expressions (do not affect neutral).
            foreach (FaceShape shape in FaceShapeValues)
            {
                if (shape == FaceShape.face_neutral || IsSpeechShape(shape))
                    continue;

                m_FaceShapeWeights.TryGetValue(shape, out float rawWeight);

                float finalWeight = Mathf.Clamp01(rawWeight);

                string shapeName = ToVisemeName(shape);
                SetViseme(shapeName, finalWeight);
                m_OnSetVisemeCallback?.Invoke(shapeName, finalWeight);
            }
        }
    }

    public virtual void InitializeLoadedAsset() { }

    public virtual void ResetLoadedAsset()
    {
        // Stop any scheduled viseme coroutines / finish waits.
        StopAllCoroutines();

        // Stop driving active segments.
        m_ActiveVisemeAnimations.Clear();

        // Clear cached weights (so reload starts clean).
        m_FaceShapeWeights.Clear();
        ResetRuntimeDebugSession();

        // Intentionally do NOT call SetViseme(...) here, because on unload
        // the derived implementations may no longer have valid targets.
        // (BlendShape version will zero weights in its own ResetLoadedAsset.)
    }

    abstract protected void SetViseme(string viseme, float weight);
    abstract protected float GetViseme(string viseme);

    public void AddOnSetVisemeCallback(OnSetViseme cb) => m_OnSetVisemeCallback += cb;
    public void AddOnFinishedPlayingCallback(OnFinishedPlaying cb) => m_OnFinishedPlayingCallback += cb;

    public void Play(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return;

        BeginRuntimeDebugSession("WordTimings");
        LogScheduleSummary(BuildWordTimingScheduleSummary(timings));

        float latestEndTime = ScheduleWordTimings(timings);

        // Wait until the last scheduled word/viseme segment should have finished.
        StartCoroutine(WaitForScheduleToFinish(latestEndTime));
    }

    /// <summary>
    /// Plays word-timing lipsync while preserving absolute timing offsets between words.
    /// Intended for realtime transcript/audio streams where gaps should not be compressed.
    /// </summary>
    public void PlayRealtime(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return;

        BeginRuntimeDebugSession("RealtimeWordTimings");
        LogScheduleSummary(BuildWordTimingScheduleSummary(timings));

        float latestEndTime = ScheduleRealtimeWordTimings(timings);

        // Wait until the last scheduled word/viseme segment should have finished.
        StartCoroutine(WaitForScheduleToFinish(latestEndTime));
    }

    public void Play(BMLReader.UtteranceTiming uttTiming)
    {
        if (uttTiming == null || uttTiming.m_CurveData == null || uttTiming.m_CurveData.Count == 0)
            return;

        BeginRuntimeDebugSession("CurveData");
        LogScheduleSummary(BuildCurveScheduleSummary(uttTiming.m_CurveData));

        var curveDataToPlay = CreateNormalizedUtteranceCurves(uttTiming.m_CurveData, m_EndOfUtteranceCloseBuffer);

        float latestEndTime = Time.time;

        foreach (var curveData in curveDataToPlay)
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
            string shapeName = ToVisemeName(faceShape);
            SetViseme(shapeName, articulation);
            m_OnSetVisemeCallback?.Invoke(shapeName, articulation);
            UpdateVisemeWeightCache(faceShape, articulation);

            // Speech-only: expressions do not affect neutral.
            if (IsSpeechShape(faceShape))
            {
                float neutralAmount = ComputeNeutralAmountFromVisemes();
                string neutralName = ToVisemeName(FaceShape.face_neutral);
                SetViseme(neutralName, neutralAmount);
                m_OnSetVisemeCallback?.Invoke(neutralName, neutralAmount);
            }

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

        ResetRuntimeDebugSession();
    }

    public void ResetViseme()
    {
        StopActiveSpeechAnimations();

        // Speech-only: do not clear expressions.
        // Clear all tracked speech viseme weights to 0.
        foreach (FaceShape shape in FaceShapeValues)
        {
            if (!IsSpeechShape(shape))
                continue;

            m_FaceShapeWeights[shape] = 0f;
            string shapeName = ToVisemeName(shape);
            SetViseme(shapeName, 0f);
            m_OnSetVisemeCallback?.Invoke(shapeName, 0f);
        }

        string neutralName = ToVisemeName(FaceShape.face_neutral);
        SetViseme(neutralName, 1f);
        m_OnSetVisemeCallback?.Invoke(neutralName, 1f);
    }

    public void ResetExpressions()
    {
        // Optional but recommended: stop only expression animations.
        StopActiveExpressionAnimations();

        foreach (FaceShape shape in FaceShapeValues)
        {
            if (shape == FaceShape.face_neutral)
                continue;

            if (IsSpeechShape(shape))
                continue;

            m_FaceShapeWeights[shape] = 0f;

            string shapeName = ToVisemeName(shape);
            SetViseme(shapeName, 0f);
            m_OnSetVisemeCallback?.Invoke(shapeName, 0f);
        }
    }

    public void ResetAllFaceShapes()
    {
        m_ActiveVisemeAnimations.Clear();

        foreach (FaceShape shape in FaceShapeValues)
        {
            if (shape == FaceShape.face_neutral)
                continue;

            m_FaceShapeWeights[shape] = 0f;

            string shapeName = ToVisemeName(shape);
            SetViseme(shapeName, 0f);
            m_OnSetVisemeCallback?.Invoke(shapeName, 0f);
        }

        // Baseline: full neutral.
        string neutralName = ToVisemeName(FaceShape.face_neutral);
        SetViseme(neutralName, 1f);
        m_OnSetVisemeCallback?.Invoke(neutralName, 1f);
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
        if (IsSpeechShape(faceShape))
        {
            m_DebugScheduledSpeechSegments++;
            if (Mathf.Clamp01(targetArticulation) > m_TargetHitTolerance)
                m_DebugPositiveTargetSegments++;
        }

        LogOverlappingVisemeAnimations(faceShape, startTime, duration);

        var anim = new VisemeAnimation(m_NextVisemeAnimationId++, faceShape, startArticulation, targetArticulation, startTime, duration, curveData);
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

    /// <summary>
    /// Builds realtime viseme animation segments while preserving the timing offsets in each WordTiming entry.
    /// </summary>
    private float ScheduleRealtimeWordTimings(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return Time.time;

        float now = Time.time;
        float latestEndTime = now;
        float timelineStart = timings[0].start;

        // These match the 0.4f / 0.4f ramp fractions used previously.
        const float DefaultRampInPct = 0.4f;
        const float DefaultRampOutPct = 0.4f;

        foreach (var wordTiming in timings)
        {
            if (wordTiming.m_VisemesUsed == null)
                continue;

            float wordStartDelay = Mathf.Max(0f, wordTiming.start - timelineStart);

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

                float attackStartTime = now + wordStartDelay + Mathf.Max(0f, localDelay);
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
        }

        return latestEndTime;
    }

    /// <summary>
    /// Creates a local curve list for playback and ensures each speech viseme curve
    /// has a terminal closing key if needed.
    /// </summary>
    /// <param name="curves">The source FaceFX viseme curves to prepare for playback.</param>
    /// <param name="closeBufferTime">
    /// The amount of time, in seconds, to place after a curve's last key before
    /// appending a closing key at articulation 0.
    /// </param>
    /// <returns>
    /// A list of curves safe to use for playback scheduling. Speech curves that do
    /// not already end at 0 are copied with an appended closing key; other curves
    /// are returned unchanged.
    /// </returns>
    private static List<BMLParser.CurveData> CreateNormalizedUtteranceCurves(List<BMLParser.CurveData> curves, float closeBufferTime)
    {
        if (curves == null || curves.Count == 0)
            return new List<BMLParser.CurveData>();

        var copiedCurves = new List<BMLParser.CurveData>(curves.Count);
        foreach (var curveData in curves)
            copiedCurves.Add(AppendCurveCloseIfNeeded(curveData, closeBufferTime));

        return copiedCurves;
    }

    /// <summary>
    /// Appends a terminal closing key to a speech viseme curve when its last key
    /// does not already return the viseme to 0.
    /// </summary>
    /// <param name="curveData">The curve to inspect and normalize.</param>
    /// <param name="closeBufferTime">
    /// The amount of time, in seconds, to place after the curve's final key before
    /// adding the closing key.
    /// </param>
    /// <returns>
    /// The original curve if no change is needed, or a copied curve with an added
    /// terminal key at articulation 0.
    /// </returns>
    private static BMLParser.CurveData AppendCurveCloseIfNeeded(BMLParser.CurveData curveData, float closeBufferTime)
    {
        if (curveData == null || curveData.numKeys <= 0)
            return curveData;

        if (!TryParseViseme(curveData.name, out FaceShape shape) || !IsSpeechShape(shape))
            return curveData;

        if (HasCurveCloseAtEnd(curveData))
            return curveData;

        var normalizedCurve = new BMLParser.CurveData(curveData.name, curveData.owner, curveData.numKeys + 1);
        for (int i = 0; i < curveData.numKeys; i++)
        {
            normalizedCurve.AddKey(curveData.curveKeys[i], i);

            var slopeData = curveData.GetSlopeData(i);
            if (slopeData != null)
                normalizedCurve.Set(i, slopeData.ml, slopeData.mr, slopeData.dl, slopeData.dr);
        }

        float lastKeyTime = curveData.GetTime(curveData.numKeys - 1);
        float closeKeyTime = lastKeyTime + Mathf.Max(0f, closeBufferTime, Mathf.Epsilon);
        normalizedCurve.AddKey(closeKeyTime, 0f, curveData.numKeys);

        normalizedCurve.SortKeysByTime();
        return normalizedCurve;
    }

    private static bool HasCurveCloseAtEnd(BMLParser.CurveData curveData)
    {
        if (curveData == null || curveData.numKeys <= 0)
            return true;

        if (!TryParseViseme(curveData.name, out FaceShape shape) || !IsSpeechShape(shape))
            return true;

        float lastArticulation = curveData.GetArticulation(curveData.numKeys - 1);
        return Mathf.Approximately(lastArticulation, 0f);
    }

    // Waits until the given absolute time has passed, then fires the finished callback.
    // Used by both Play(...) overloads after scheduling all viseme segments.
    private IEnumerator WaitForScheduleToFinish(float scheduledEndTime)
    {
        while (Time.time < scheduledEndTime)
            yield return null;

        LogRuntimeDebugSummary();
        ResetRuntimeDebugSession();
        m_OnFinishedPlayingCallback?.Invoke();
    }

    /// <summary>
    /// Tracks the latest articulation value for non-neutral visemes.
    /// </summary>
    private void UpdateVisemeWeightCache(FaceShape shape, float weight)
    {
        if (shape == FaceShape.face_neutral)
            return;

        m_FaceShapeWeights[shape] = Mathf.Clamp01(weight);
    }

    private void TrackObservedArticulation(VisemeAnimation anim, float observedArticulation)
    {
        float clampedObserved = Mathf.Clamp01(observedArticulation);
        anim.BestObservedArticulation = Mathf.Max(anim.BestObservedArticulation, clampedObserved);

        float delta = Mathf.Abs(clampedObserved - Mathf.Clamp01(anim.TargetArticulation));
        if (delta < anim.ClosestObservedDelta)
            anim.ClosestObservedDelta = delta;

        if (delta <= m_TargetHitTolerance)
            anim.ReachedTargetWithinTolerance = true;
    }

    private void LogOverlappingVisemeAnimations(FaceShape faceShape, float startTime, float duration)
    {
        if (!m_RuntimeDebugOutput)
            return;

        float endTime = startTime + Mathf.Max(duration, 0f);
        float observedArticulation = GetObservedScheduleSpaceViseme(faceShape);

        for (int i = 0; i < m_ActiveVisemeAnimations.Count; i++)
        {
            var existing = m_ActiveVisemeAnimations[i];
            if (existing.Shape != faceShape)
                continue;

            if (!IntervalsOverlap(existing.StartTime, existing.EndTime, startTime, endTime))
                continue;

            TrackObservedArticulation(existing, observedArticulation);
            existing.WasOverlappedBeforeCompletion = true;
            m_DebugOverlapCount++;
        }
    }

    private void LogVisemeAnimationCompletion(VisemeAnimation anim, float observedArticulation, string reason)
    {
        if (!m_RuntimeDebugOutput)
            return;

        TrackObservedArticulation(anim, observedArticulation);

        float positiveTarget = Mathf.Clamp01(anim.TargetArticulation);
        if (positiveTarget <= m_TargetHitTolerance)
            return;

        m_DebugCompletedPositiveTargetSegments++;

        bool missedTarget = !anim.ReachedTargetWithinTolerance;
        bool interruptedBeforeTarget = anim.WasOverlappedBeforeCompletion && !anim.ReachedTargetWithinTolerance;
        if (!missedTarget && !interruptedBeforeTarget)
            return;

        m_DebugProblemSegments.Add(new VisemeProblemRecord(
            ToVisemeName(anim.Shape),
            interruptedBeforeTarget ? "overlapped" : reason,
            positiveTarget,
            anim.BestObservedArticulation,
            anim.ClosestObservedDelta,
            anim.Duration));
    }

    private void LogScheduleSummary(string summary)
    {
        if (!m_RuntimeDebugOutput || string.IsNullOrEmpty(summary))
            return;

        Debug.Log(summary);
    }

    private string BuildWordTimingScheduleSummary(List<TtsReader.WordTiming> timings)
    {
        if (timings == null || timings.Count == 0)
            return "FacialAnimationPlayer Schedule: no word timings.";

        int wordCount = 0;
        int visemeCount = 0;
        int crowdedTransitions = 0;
        int simultaneousEntries = 0;
        int zeroEntries = 0;
        var uniqueVisemes = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<ScheduleDebugEntry>();

        for (int i = 0; i < timings.Count; i++)
        {
            var wordTiming = timings[i];
            if (wordTiming == null)
                continue;

            wordCount++;
            if (wordTiming.m_VisemesUsed == null)
                continue;

            for (int j = 0; j < wordTiming.m_VisemesUsed.Count; j++)
            {
                var visemeData = wordTiming.m_VisemesUsed[j];
                if (visemeData == null)
                    continue;

                visemeCount++;
                if (Mathf.Abs(visemeData.articulation) <= 0.0001f)
                    zeroEntries++;
                uniqueVisemes.Add(visemeData.type ?? string.Empty);
                entries.Add(new ScheduleDebugEntry(visemeData.start, visemeData.type ?? string.Empty, visemeData.articulation));
            }
        }

        entries.Sort((a, b) => a.Time.CompareTo(b.Time));

        float minGap = float.PositiveInfinity;
        for (int i = 1; i < entries.Count; i++)
        {
            float gap = entries[i].Time - entries[i - 1].Time;
            if (gap < minGap)
                minGap = gap;

            if (gap <= m_CrowdedScheduleThresholdSeconds)
                crowdedTransitions++;

            if (Mathf.Abs(gap) <= 0.0001f)
                simultaneousEntries++;
        }

        StringBuilder sb = new();
        sb.Append("FacialAnimationPlayer Schedule - source=WordTimings ");
        sb.Append($"character={name} words={wordCount} visemeEntries={visemeCount} uniqueVisemes={uniqueVisemes.Count} ");
        sb.Append($"zeroEntries={zeroEntries} nonZeroEntries={Mathf.Max(0, visemeCount - zeroEntries)} ");
        sb.Append($"crowdedTransitions={crowdedTransitions} threshold={m_CrowdedScheduleThresholdSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s ");
        sb.Append($"minGap={(float.IsPositiveInfinity(minGap) ? "n/a" : minGap.ToString("0.000", CultureInfo.InvariantCulture) + "s")} ");
        sb.Append($"sameTimeEntries={simultaneousEntries}");

        return sb.ToString();
    }

    private string BuildCurveScheduleSummary(List<BMLParser.CurveData> curves)
    {
        if (curves == null || curves.Count == 0)
            return "FacialAnimationPlayer Schedule: no curves.";

        int curveCount = 0;
        int speechCurveCount = 0;
        int keyCount = 0;
        int crowdedTransitions = 0;
        var keys = new List<ScheduleDebugEntry>();

        for (int i = 0; i < curves.Count; i++)
        {
            var curve = curves[i];
            if (curve == null)
                continue;

            curveCount++;
            if (TryParseViseme(curve.name, out FaceShape shape) && IsSpeechShape(shape))
                speechCurveCount++;

            keyCount += curve.numKeys;
            for (int j = 0; j < curve.numKeys; j++)
                keys.Add(new ScheduleDebugEntry(curve.GetTime(j), curve.name, curve.GetArticulation(j)));
        }

        keys.Sort((a, b) => a.Time.CompareTo(b.Time));

        float minGap = float.PositiveInfinity;
        for (int i = 1; i < keys.Count; i++)
        {
            float gap = keys[i].Time - keys[i - 1].Time;
            if (gap < minGap)
                minGap = gap;

            if (gap <= m_CrowdedScheduleThresholdSeconds)
                crowdedTransitions++;
        }

        StringBuilder sb = new();
        sb.Append("FacialAnimationPlayer Schedule - source=CurveData ");
        sb.Append($"character={name} curves={curveCount} speechCurves={speechCurveCount} keys={keyCount} ");
        sb.Append($"crowdedTransitions={crowdedTransitions} threshold={m_CrowdedScheduleThresholdSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s ");
        sb.Append($"minGap={(float.IsPositiveInfinity(minGap) ? "n/a" : minGap.ToString("0.000", CultureInfo.InvariantCulture) + "s")}");

        return sb.ToString();
    }

    private void BeginRuntimeDebugSession(string scheduleSource)
    {
        if (!m_RuntimeDebugOutput)
            return;

        m_DebugScheduleSource = scheduleSource ?? string.Empty;
        m_DebugScheduledSpeechSegments = 0;
        m_DebugPositiveTargetSegments = 0;
        m_DebugOverlapCount = 0;
        m_DebugCompletedPositiveTargetSegments = 0;
        m_DebugProblemSegments.Clear();
    }

    private void ResetRuntimeDebugSession()
    {
        m_DebugScheduleSource = string.Empty;
        m_DebugScheduledSpeechSegments = 0;
        m_DebugPositiveTargetSegments = 0;
        m_DebugOverlapCount = 0;
        m_DebugCompletedPositiveTargetSegments = 0;
        m_DebugProblemSegments.Clear();
    }

    private void LogRuntimeDebugSummary()
    {
        if (!m_RuntimeDebugOutput)
            return;

        StringBuilder sb = new();
        sb.Append($"FacialAnimationPlayer Result - source={m_DebugScheduleSource} character={name} ");
        sb.Append($"speechSegments={m_DebugScheduledSpeechSegments} positiveTargets={m_DebugPositiveTargetSegments} ");
        sb.Append($"completedPositiveTargets={m_DebugCompletedPositiveTargetSegments} overlaps={m_DebugOverlapCount} ");
        sb.Append($"missedTargets={m_DebugProblemSegments.Count} tolerance={m_TargetHitTolerance.ToString("0.00", CultureInfo.InvariantCulture)}");

        sb.AppendLine();
        sb.Append("Config");
        sb.AppendLine();
        sb.Append($"  easing={m_EasingEquation} curveSmoothing={m_UseCurveSmoothing}");
        if (m_UseCurveSmoothing)
            sb.Append($" ({NumSegmentsPerSecond}/sec)");
        sb.Append($" endCloseBuffer={m_EndOfUtteranceCloseBuffer.ToString("0.00", CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.Append($"  visemeMultiplier={m_FacialVisemeMultiplier.ToString("0.00", CultureInfo.InvariantCulture)} ");
        sb.Append($"  expressionMultiplier={m_FacialExpressionMultiplier.ToString("0.00", CultureInfo.InvariantCulture)} ");
        sb.Append($"coarticulation={(m_EnableCoarticulation ? "on" : "off")} strength={m_CoarticulationStrength.ToString("0.00", CultureInfo.InvariantCulture)} ");
        sb.Append($"crowdedThreshold={m_CrowdedScheduleThresholdSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s");

        string visemeModifierSummary = BuildVisemeModifierDebugSummary();
        if (!string.IsNullOrEmpty(visemeModifierSummary))
        {
            sb.AppendLine();
            sb.Append($"  visemeModifiers={visemeModifierSummary}");
        }

        if (m_DebugProblemSegments.Count > 0)
        {
            var aggregateByViseme = new Dictionary<string, VisemeAggregateRecord>(StringComparer.Ordinal);
            for (int i = 0; i < m_DebugProblemSegments.Count; i++)
            {
                var record = m_DebugProblemSegments[i];
                if (aggregateByViseme.TryGetValue(record.VisemeName, out var aggregate))
                {
                    aggregate.Count++;
                    aggregate.WorstDelta = Mathf.Max(aggregate.WorstDelta, record.ClosestObservedDelta);
                    aggregateByViseme[record.VisemeName] = aggregate;
                }
                else
                {
                    aggregateByViseme.Add(record.VisemeName, new VisemeAggregateRecord(1, record.ClosestObservedDelta));
                }
            }

            var aggregateList = new List<KeyValuePair<string, VisemeAggregateRecord>>(aggregateByViseme);
            aggregateList.Sort((a, b) =>
            {
                int countCompare = b.Value.Count.CompareTo(a.Value.Count);
                if (countCompare != 0)
                    return countCompare;

                return b.Value.WorstDelta.CompareTo(a.Value.WorstDelta);
            });

            sb.AppendLine();
            sb.Append("MissesByViseme");
            for (int i = 0; i < aggregateList.Count; i++)
            {
                var aggregate = aggregateList[i];
                sb.AppendLine();
                sb.Append($"  {aggregate.Key} count={aggregate.Value.Count} worstDelta={aggregate.Value.WorstDelta.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            m_DebugProblemSegments.Sort((a, b) => b.ClosestObservedDelta.CompareTo(a.ClosestObservedDelta));
            int maxProblems = Mathf.Min(m_DebugProblemSegments.Count, 24);
            sb.AppendLine();
            sb.Append("MissedTargets");
            for (int i = 0; i < maxProblems; i++)
            {
                var record = m_DebugProblemSegments[i];
                sb.AppendLine();
                sb.Append($"  {record.VisemeName} reason={record.Reason} target={record.TargetArticulation.ToString("0.00", CultureInfo.InvariantCulture)} ");
                sb.Append($"best={record.BestObservedArticulation.ToString("0.00", CultureInfo.InvariantCulture)} ");
                sb.Append($"delta={record.ClosestObservedDelta.ToString("0.00", CultureInfo.InvariantCulture)} ");
                sb.Append($"duration={record.Duration.ToString("0.000", CultureInfo.InvariantCulture)}s");
            }

            if (m_DebugProblemSegments.Count > maxProblems)
            {
                sb.AppendLine();
                sb.Append($"  ... ({m_DebugProblemSegments.Count - maxProblems} more)");
            }
        }

        sb.AppendLine();
        sb.Append("HowToRead");
        sb.AppendLine();
        sb.Append("  speechSegments = all scheduled speech segments the player processed; positiveTargets = segments that aimed at a meaningful non-zero viseme weight.");
        sb.AppendLine();
        sb.Append("  completedPositiveTargets = positive-target segments that ran to their scheduled end; overlaps = same-viseme segments whose time windows overlapped.");
        sb.AppendLine();
        sb.Append("  missedTargets = completed positive-target segments that never got within tolerance of their requested target. In MissedTargets: target = requested weight, best = best observed runtime weight, delta = closest miss amount, duration = scheduled segment length.");
        sb.AppendLine();
        sb.Append("  Helpful reading: repeated misses on the same viseme suggest a shape-specific realization issue; many very short durations (often 0.033s in CurveData mode because smoothing uses 30 segments/second) suggest a crowded schedule that may be too dense to realize visually.");

        Debug.Log(sb.ToString());
    }

    private string BuildVisemeModifierDebugSummary()
    {
        if (m_VisemeModifiers == null || m_VisemeModifiers.Length == 0)
            return string.Empty;

        var modified = new List<string>();
        for (int i = 0; i < m_VisemeModifiers.Length; i++)
        {
            var modifier = m_VisemeModifiers[i];
            if (modifier == null || string.IsNullOrEmpty(modifier.m_Name))
                continue;

            if (Mathf.Abs(modifier.m_WeightMultiplier - 1f) <= 0.0001f)
                continue;

            modified.Add($"{modifier.m_Name}={modifier.m_WeightMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        if (modified.Count == 0)
            return "<all default>";

        const int maxToShow = 8;
        if (modified.Count <= maxToShow)
            return string.Join(", ", modified);

        return string.Join(", ", modified.GetRange(0, maxToShow)) + $" ... ({modified.Count - maxToShow} more)";
    }

    private float GetObservedScheduleSpaceViseme(FaceShape shape)
    {
        string visemeName = ToVisemeName(shape);
        float observed = Mathf.Clamp01(GetViseme(visemeName));
        float divisor = Mathf.Max(GetGlobalWeightMultiplier(shape) * GetVisemeModifierWeightMultiplier(visemeName), Mathf.Epsilon);
        return Mathf.Clamp01(observed / divisor);
    }

    protected float GetResolvedWeightMultiplier(string viseme)
    {
        if (TryParseViseme(viseme, out FaceShape shape))
            return GetGlobalWeightMultiplier(shape) * GetVisemeModifierWeightMultiplier(viseme);

        return m_FacialVisemeMultiplier * GetVisemeModifierWeightMultiplier(viseme);
    }

    protected float GetGlobalWeightMultiplier(FaceShape shape)
    {
        if (shape == FaceShape.face_neutral)
            return 1f;

        return IsSpeechShape(shape) ? m_FacialVisemeMultiplier : m_FacialExpressionMultiplier;
    }

    private static bool IntervalsOverlap(float startA, float endA, float startB, float endB) => Mathf.Max(startA, startB) < Mathf.Min(endA, endB);

    /// <summary>
    /// Computes face_neutral as 1 - sum(all speech viseme weights), clamped to [0,1].
    /// Only FaceShape entries are considered speech visemes.
    /// </summary>
    private float ComputeNeutralAmountFromVisemes()
    {
        // Speech-only: expressions do not affect neutral.
        float total = 0f;

        foreach (FaceShape shape in FaceShapeValues)
        {
            if (!IsSpeechShape(shape))
                continue;

            m_FaceShapeWeights.TryGetValue(shape, out float rawWeight);

            float finalWeight = ApplyNeighborCoarticulation(shape, rawWeight);
            total += Mathf.Clamp01(finalWeight);
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
            m_FaceShapeWeights.TryGetValue(neighbor, out float neighborWeight);
            neighborSum += neighborWeight;
            neighborCount++;
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

    private void StopActiveSpeechAnimations()
    {
        // Remove any scheduled or active speech (viseme) animations so resets are not overwritten next frame.
        for (int i = m_ActiveVisemeAnimations.Count - 1; i >= 0; i--)
        {
            FaceShape shape = m_ActiveVisemeAnimations[i].Shape;
            if (IsSpeechShape(shape))
                m_ActiveVisemeAnimations.RemoveAt(i);
        }
    }

    private void StopActiveExpressionAnimations()
    {
        // Remove any scheduled or active expression animations so resets are not overwritten next frame.
        for (int i = m_ActiveVisemeAnimations.Count - 1; i >= 0; i--)
        {
            FaceShape shape = m_ActiveVisemeAnimations[i].Shape;
            if (shape != FaceShape.face_neutral && !IsSpeechShape(shape))
                m_ActiveVisemeAnimations.RemoveAt(i);
        }
    }

    private static bool IsSpeechShape(FaceShape shape)
    {
        switch (shape)
        {
            case FaceShape.FV:
            case FaceShape.open:
            case FaceShape.PBM:
            case FaceShape.ShCh:
            case FaceShape.tBack:
            case FaceShape.tRoof:
            case FaceShape.tTeeth:
            case FaceShape.W:
            case FaceShape.wide:
                return true;

            default:
                return false;
        }
    }

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
