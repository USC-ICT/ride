using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Ride;

using UnityRandom = UnityEngine.Random;

namespace VHAssets
{
    /// <summary>
    /// Procedural saccade generator for character eye transforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This controller synthesizes small, rapid eye movements (saccades) using a statistical model inspired by
    /// the "Eyes Alive" paper (Park/Lee/Badler) and the SmartBody <c>MeCtSaccade</c> implementation.
    /// At runtime it periodically selects a new saccade direction and magnitude, computes a duration from
    /// amplitude, and interpolates each eye from the previous fixation to the new fixation.
    /// </para>
    ///
    /// <para>
    /// Model overview (high level):
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Direction</b>: Chosen from 8 quantized bins (0..315 degrees in 45-degree steps) using weighted
    /// probabilities (bin attributes). This corresponds to the paper's direction histogram concept.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Magnitude</b>: Sampled from an exponential-shaped distribution (paper fit) and clamped to a
    /// configurable limit. Vertical/diagonal directions use reduced limits to better match typical eye/eyelid constraints.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Interval</b>: Chooses between Mutual and Away modes and samples the inter-saccadic interval from a Gaussian
    /// (mean/variance), rejecting very small values. This matches SmartBody behavior.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Duration</b>: Computed using a linear relationship (duration = intercept + slope * amplitude),
    /// matching the paper's linear duration model parameters.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Motion application</b>: Uses quaternion interpolation with a smooth easing curve between fixations.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Differences vs. the original paper:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// The paper describes following head rotation direction when head motion exceeds a threshold; this controller
    /// uses the statistical direction bins only.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The paper describes mutual-gaze / gaze-away timing as elapsed-time-dependent probability curves; this controller
    /// uses a simplified Gaussian interval model (SmartBody-style).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The paper discusses an instantaneous saccade velocity curve derived from recorded data; this controller uses an
    /// easing curve with quaternion slerp rather than explicitly modeling velocity.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
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
            Bin_0,              // percentage bin for 0 degree.
            Bin_45,             // percentage bin for 45 degree.
            Bin_90,             // percentage bin for 90 degree.
            Bin_135,            // percentage bin for 135 degree.
            Bin_180,            // percentage bin for 180 degree.
            Bin_225,            // percentage bin for 225 degree.
            Bin_270,            // percentage bin for 270 degree.
            Bin_315,            // percentage bin for 315 degree.
            Magnitude_Limit,    // magnitude limit.
            Percentage_Mutual,  // percentage for gaze mutual.
            Mutual_Mean,        // Gaussian mean for gaze mutual.
            Mutual_Variant,     // Gaussian variant for gaze mutual.
            Away_Mean,          // Gaussian mean for gaze away.
            Away_Variant,       // Gaussian variant for gaze away.
            NUM_ATT
        }

        // Data from papar "Eyes Alive"
        // c++ initAttributes()
        static readonly Dictionary<CharacterDefines.SaccadeType, float[]> m_AttMapping = new()
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
        EyeTransformData [] m_EyeTransformNames = 
        {
            new("JtEyeLf", false),
            new("JtEyeRt", true),
        };

        [Serializable]
        protected class EyeData
        {
            [NonSerialized] public EyeTransformData m_EyeTransformData;
            public Transform m_Eye;
            public Quaternion m_InitialRotation = Quaternion.identity; // where the joint is looking before a saccade occurs. This works with gaze
            public Quaternion m_LastRotation = Quaternion.identity;
            public Quaternion m_TargetRotation = Quaternion.identity;
            public Quaternion m_LastFixedRotation = Quaternion.identity;
            public Quaternion m_FixedRotation = Quaternion.identity;
            public Quaternion m_PrevAfterApplyRotation = Quaternion.identity;
            [NonSerialized] public int m_ExternalOverwriteStreak = 0;
            [NonSerialized] public int m_NoOverwriteStreak = 0;
            [NonSerialized] public bool m_AutoUseExternalBaseline = false;

            [NonSerialized] public EyeBaselineMode m_DebugResolvedBaselineMode = EyeBaselineMode.AutoDetect;
            [NonSerialized] public float m_DebugOverwriteDeltaDeg = 0.0f;
        }

        [Serializable]
        private class SaccadeDebugInfo
        {
            public IntervalMode intervalMode;
            public SaccadeState state;

            public float now;
            public float nextStartTime;
            public float endTime;
            public float timeUntilNextStart;
            public float timeRemainingInSaccade;

            public float directionDeg;
            public float magnitudeDeg;
            public float durationSec;
            public float waitSec;

            public float leftOffsetAngleDeg;
            public float rightOffsetAngleDeg;

            // show left eye only as a reference
            public EyeBaselineMode leftResolvedBaselineMode;
            public float leftOverwriteDeltaDeg;
            public int leftExternalOverwriteStreak;
            public int leftNoOverwriteStreak;
            public bool leftAutoUseExternalBaseline;
        }

        public enum EyeBaselineMode
        {
            /// <summary>
            /// Automatically chooses a baseline strategy per eye.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Use this for mixed rigs (some avatars have eye bones overwritten each frame by Animator/IK/Gaze, others do not).
            /// This mode detects whether the eye bone was externally overwritten since the last frame and selects the safest
            /// strategy automatically. It is the recommended default.
            /// </para>
            /// </remarks>
            AutoDetect = 0,

            /// <summary>
            /// Assumes the eye bone localRotation is overwritten every frame by another system (Animator, IK, Gaze, constraints).
            /// </summary>
            /// <remarks>
            /// <para>
            /// This applies the saccade additively on top of the current per-frame baseline: baseline * saccadeOffset.
            /// Choose this when you know another system writes the eye bone every frame and you want saccades to ride on top
            /// of that motion (for example, while SetLookAt is active).
            /// </para>
            /// <para>
            /// Warning: If the eye bones are NOT overwritten each frame, this mode will accumulate offsets over time and can
            /// cause drift/spinning.
            /// </para>
            /// </remarks>
            AdditiveOnExternalBaseline = 1,

            /// <summary>
            /// This controller owns the eye bone rotation over time (no external baseline each frame).
            /// </summary>
            /// <remarks>
            /// <para>
            /// Each frame we remove the previously applied saccade offset, then apply the new offset. This prevents the
            /// baseline from accumulating saccade rotations when nothing else is rewriting the eye bones.
            /// </para>
            /// <para>
            /// This is the safest choice when you are unsure whether the rig overwrites eye bones each frame, but if another
            /// system does write the eyes every frame, this can be slightly less "pure" than AdditiveOnExternalBaseline.
            /// </para>
            /// </remarks>
            OwnBaselineWithUndo = 2,
        }

        [SerializeField][Range(0, 10)] float m_MagnitudeScaler = 1;
        [SerializeField] CharacterDefines.SaccadeType m_SaccadeMode = CharacterDefines.SaccadeType.Default;
        [SerializeField] VHMath.Axis m_EyeForwardAxis = VHMath.Axis.X;

        [Header("Eye Baseline Mode")]
        [Tooltip(
            "How saccade rotations are applied relative to the eye bone's baseline pose." +
            "AutoDetect (recommended): Detect whether the eye bones are overwritten each frame (Animator/IK/Gaze) and choose the correct strategy automatically." +
            "AdditiveOnExternalBaseline: Assumes something else overwrites the eye bones every frame; applies baseline * saccadeOffset. Use when gaze/IK drives eye bones each frame." +
            "OwnBaselineWithUndo: Removes last saccade offset then applies the new one; prevents drift when nothing else overwrites eye bones."
        )]
        [SerializeField] private EyeBaselineMode m_EyeBaselineMode = EyeBaselineMode.AutoDetect;

        [Serializable]
        private class AutoDetectSettings
        {
            [Tooltip("AutoDetect: angle threshold (degrees) used to decide whether the eye bone was overwritten externally since the previous frame.")]
            public float m_ExternalOverwriteThresholdDeg = 0.25f;

            [Tooltip("AutoDetect: number of consecutive frames required before switching to AdditiveOnExternalBaseline.")]
            public int m_ExternalOverwriteConfirmFrames = 2;

            [Tooltip("AutoDetect: number of consecutive frames required before switching to OwnBaselineWithUndo. Use a larger value to avoid mode flapping.")]
            public int m_NoOverwriteConfirmFrames = 10;
        }
        [SerializeField] private AutoDetectSettings m_AutoDetectSettings = new();

        [Tooltip("Runtime data for each eye transform. Initialized automatically from child transforms based on the names in m_EyeTransformNames.")]
        [SerializeField] protected EyeData[] m_Eyes;

        [SerializeField] private bool m_DebugInspector = false;
        [SerializeField] private SaccadeDebugInfo m_DebugInfo = new();


        private IntervalMode m_IntervalMode = IntervalMode.Mutual;
        private float m_Direction;
        private float m_Magnitude;
        private float m_Duration; // how long the eye takes to move to it's target rotation
        private float m_WaitTime; // how long the eye should stay before fading back

        private bool m_UseModel = true;
        private float m_Time = -1.0f;

        protected SaccadeState m_SaccadeState = SaccadeState.Finished;

        private bool m_assetInitialized = false;
        #endregion

        #region Properties
        public bool AreSaccadesOn => m_SaccadeMode != CharacterDefines.SaccadeType.Default && m_SaccadeMode != CharacterDefines.SaccadeType.End;

        public int NumEyes => m_EyeTransformNames.Length;

        public float MagnitudeScaler { get => m_MagnitudeScaler; set => m_MagnitudeScaler = value; }
        #endregion

        #region Functions
        protected virtual void Awake()
        {
            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        /// <summary>
        /// Initializes eye references from child transforms and starts a short delayed capture
        /// of the baseline eye rotations after Animator/IK has settled.
        /// </summary>
        public void InitializeLoadedAsset()
        {
            m_SaccadeState = SaccadeState.Finished;
            m_assetInitialized = false;

            var activeTransforms = GetComponentsInChildren<Transform>(false);

            m_Eyes = new EyeData[NumEyes];
            for (int i = 0; i < NumEyes; i++)
            {
                Transform eyeTransform = Array.Find(activeTransforms, t => t.name == m_EyeTransformNames[i].transformName);
                if (eyeTransform != null)
                {
                    m_Eyes[i] = new EyeData();
                    m_Eyes[i].m_EyeTransformData = m_EyeTransformNames[i];
                    m_Eyes[i].m_Eye = eyeTransform;

                    // NOTE: Initial rotation will be captured next frame, after other systems (Animator/IK/Gaze) have had a chance to apply their startup pose.
                    m_Eyes[i].m_InitialRotation = Quaternion.identity;

                    //Debug.Log($"Saccade eye[{i}] '{m_EyeTransformNames[i].transformName}' resolved to: {GetTransformPath(eyeTransform)}", this);
                }
                else
                {
                    Debug.LogError($"Couldn't find active eye transform named {m_EyeTransformNames[i].transformName} on character {name}. Saccades won't work");
                }

                int count = activeTransforms.Count(t => t.name == m_EyeTransformNames[i].transformName);
                if (count > 1)
                    Debug.LogWarning($"SaccadeController - Multiple transforms named '{m_EyeTransformNames[i].transformName}' found on '{name}'. Count={count}. This can break eyes if the wrong match is found.", this);
            }

            // Capture "true baseline" a frame later to avoid first-frame pose races.
            StartCoroutine(CaptureInitialEyeRotationsNextFrame());
        }

        private IEnumerator CaptureInitialEyeRotationsNextFrame()
        {
            // Wait one frame so animation/gaze/IK can settle.
            yield return null;
            yield return null;

            if (m_Eyes == null)
                yield break;

            foreach (var eye in m_Eyes)
            {
                if (eye != null && eye.m_Eye != null)
                    eye.m_InitialRotation = eye.m_Eye.localRotation;
            }

            yield return new WaitForSeconds(1.5f);

            m_assetInitialized = true;
        }

        public void ResetLoadedAsset()
        {
            // Ensure we immediately stop touching any eye transforms.
            m_assetInitialized = false;

            // Cancel any in-progress saccade so Process() becomes a no-op.
            m_SaccadeState = SaccadeState.Finished;

            m_Eyes = null;

            // clear static vars used in GenerateGaussianRandom()
            GaussRand_V1 = 0;
            GaussRand_V2 = 0;
            GaussRand_S = 0;
            GaussRand_phase = 0;
        }

        /// <summary>
        /// Computes the local-space rotation axis used to build the saccade quaternion.
        /// Mirrors the C++ code:
        ///   directionRad = (directionDeg - 90) * Deg2Rad
        ///   vec2 = axis0 * sin(directionRad) + axis1 * cos(directionRad)
        ///   axis = cross(axis2, vec2)
        /// where axis2 is the chosen "forward axis" for the eye joint.
        /// </summary>
        Vector3 ComputeSaccadeAxisLocal(float directionDegrees)
        {
            // c++ spawnOnce
            //   float direction = (_direction - 90.0f) * pi/180
            //   vec2 = localAxis0*sin(direction) + localAxis1*cos(direction)
            //   axis = cross(localAxis2, vec2)

            float directionRad = (directionDegrees - 90.0f) * Mathf.Deg2Rad;

            Vector3 axis0;
            Vector3 axis1;
            Vector3 axis2;

            // These axes are in the eye joint's local space. axis2 is the "forward" axis.
            // axis0/axis1 span the plane perpendicular to axis2.
            // Map "forward axis" selection into a right-handed local basis.
            // The intent is the same as C++ localAxis[0], [1], [2].
            switch (m_EyeForwardAxis)
            {
                case VHMath.Axis.X:
                    axis2 = Vector3.right;   // forward
                    axis0 = Vector3.forward; // in-plane axis 0
                    axis1 = Vector3.up;      // in-plane axis 1
                    break;

                case VHMath.Axis.Y:
                    axis2 = Vector3.up;      // forward
                    axis0 = Vector3.right;   // in-plane axis 0
                    axis1 = Vector3.forward; // in-plane axis 1
                    break;

                case VHMath.Axis.Z:
                default:
                    axis2 = Vector3.forward; // forward
                    axis0 = Vector3.right;   // in-plane axis 0
                    axis1 = Vector3.up;      // in-plane axis 1
                    break;
            }

            float s = Mathf.Sin(directionRad);
            float c = Mathf.Cos(directionRad);

            Vector3 vec1 = axis2;
            Vector3 vec2 = axis0 * s + axis1 * c;

            Vector3 axis = Vector3.Cross(vec1, vec2);
            float axisLen = axis.magnitude;
            if (axisLen > 1e-6f)
                axis /= axisLen;
            else
                axis = Vector3.up; // safe fallback

            // Degenerate fallback should never happen unless axes are invalid.
            return axis;
        }

        /// <summary>
        /// Triggers a saccade immediately, closely mirroring the original C++ spawnOnce() behavior.
        /// In model mode, the saccade "lands" and holds until the next scheduled saccade.
        /// </summary>
        public void Perform(float direction, float magnitude, float duration)
        {
            if (m_Eyes == null || m_Eyes.Length == 0)
                return;

            //Debug.Log($"[SaccadeController] New saccade triggered on '{name}': Direction={direction:F1} deg, Magnitude={magnitude:F2} deg, Duration={duration:F3}s, Mode={m_SaccadeMode}, Time={Time.time:F3}");

            // Mimic C++ spawnOnce():
            //   _direction = dir;
            //   _magnitude = amplitude;
            //   _dur = dur;
            //   _axis = cross(vec1, vec2);
            //   _lastFixedRotation = _rotation;
            //   _fixedRotation = SrQuat(_axis, _magnitude * pi/180);
            //   _time = now;


            m_Direction = direction;
            m_Magnitude = magnitude;
            m_Duration = duration;

            Vector3 axis = ComputeSaccadeAxisLocal(m_Direction);

            foreach (var eye in m_Eyes)
            {
                if (eye == null || eye.m_Eye == null)
                    continue;

                // Remove any previously applied per-frame offset so we base off the current pose.
                if (eye.m_LastRotation != Quaternion.identity)
                {
                    eye.m_Eye.localRotation *= Quaternion.Inverse(eye.m_LastRotation);
                    eye.m_LastRotation = Quaternion.identity;
                }

                // C++ uses _lastFixedRotation = _rotation (current interpolated state).
                eye.m_LastFixedRotation = eye.m_TargetRotation;

                float signedMagnitude = eye.m_EyeTransformData.isInverted ? m_Magnitude : -m_Magnitude;

                // C++: SrQuat(_axis, magnitudeRadians) == AngleAxis(magnitudeDegrees, axis)
                eye.m_FixedRotation = Quaternion.AngleAxis(signedMagnitude, axis);

                // If we start immediately, this frame's rotation begins at lastFixedRotation and moves toward fixedRotation.
                // We'll compute eye.m_TargetRotation in Process() based on time.
            }

            m_Time = Time.time;
            m_SaccadeState = SaccadeState.FadeIn;
        }

        /// <summary>
        /// Advances the saccade model: schedules the next saccade based on the statistical model
        /// from the "Eyes Alive" paper. Mirrors C++ spawning().
        /// </summary>
        void UpdateBehaviour()
        {
            // C++ spawning() only happens when _useModel is true.
            // Here, we also require "mode on" + initialized.
            if (!AreSaccadesOn || !m_assetInitialized || m_Eyes == null || m_Eyes.Length == 0)
                return;

            if (!m_UseModel)
                return;

            float t = Time.time;

            // C++:
            // if (_time == -1.0f || t > (_time + _dur)) { choose new dir/mag, compute axis, update fixed/lastFixed, compute dur from angle, interval, _time = now + interval; }
            if (m_Time < 0.0f || t > (m_Time + m_Duration))
            {
                m_Direction = GenerateRandomDirection();         // degrees
                m_Magnitude = GenerateRandomMagnitude();         // degrees

                Vector3 axis = ComputeSaccadeAxisLocal(m_Direction);

                foreach (var eye in m_Eyes)
                {
                    if (eye == null || eye.m_Eye == null)
                        continue;

                    // lastFixed = fixed
                    eye.m_LastFixedRotation = eye.m_FixedRotation;

                    float signedMagnitude = eye.m_EyeTransformData.isInverted ? m_Magnitude : -m_Magnitude;
                    eye.m_FixedRotation = Quaternion.AngleAxis(signedMagnitude, axis);
                }

                // C++:
                // actualRotation = _fixedRotation * inverse(_lastFixedRotation)
                // angle = actualRotation.angle() in degrees
                //
                // We'll compute angle using the left eye as the representative (close to C++ approach),
                // since both eyes get symmetric signed magnitudes.
                Quaternion lastFixed = m_Eyes[0].m_LastFixedRotation;
                Quaternion fixedRot = m_Eyes[0].m_FixedRotation;

                Quaternion actualRotation = fixedRot * Quaternion.Inverse(lastFixed);
                float angle = Quaternion.Angle(Quaternion.identity, actualRotation); // degrees

                m_Duration = CalculateDuration(angle);     // sec
                m_WaitTime = GenerateRandomInterval();     // sec (C++ intervalRandom)

                m_Time = t + m_WaitTime;

                // State is mostly for debugging; actual timing is driven by m_Time/m_Duration.
                m_SaccadeState = SaccadeState.FadeIn;
            }
        }

        void LateUpdate()
        {
            /*
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Perform(45, 8, 1);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                Perform(90, 8, 1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                Perform(135, 8, 1);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                Perform(180, 8, 1);
            */

            UpdateBehaviour();
            Process();

            UpdateDebugInfo();
            DebugLogFrame("LateUpdate");
        }

        /// <summary>
        /// Applies the C++ saccade model timing and interpolation. Mirrors C++ processing().
        /// </summary>
        void Process()
        {
            // SmartBody uses a simple easing curve and quaternion slerp between fixations.
            // The paper describes an instantaneous velocity curve derived from recorded saccades;
            // this implementation approximates the "feel" without explicitly modeling velocity.

            if (!m_assetInitialized || m_Eyes == null || m_Eyes.Length == 0)
                return;

            float t = Time.time;

            // C++: if (_time == -1.0f) return;
            if (m_Time < 0.0f)
                return;

            // C++:
            // if ((t > (_time + _dur)) && !_useModel) { clear to identity; }
            if ((t > (m_Time + m_Duration)) && !m_UseModel)
            {
                foreach (var eye in m_Eyes)
                {
                    if (eye == null || eye.m_Eye == null)
                        continue;

                    // Remove previously applied per-frame offset.
                    if (eye.m_LastRotation != Quaternion.identity)
                    {
                        eye.m_Eye.localRotation *= Quaternion.Inverse(eye.m_LastRotation);
                        eye.m_LastRotation = Quaternion.identity;
                    }

                    eye.m_LastFixedRotation = Quaternion.identity;
                    eye.m_FixedRotation = Quaternion.identity;
                    eye.m_TargetRotation = Quaternion.identity;
                }

                m_SaccadeState = SaccadeState.Finished;
                return;
            }

            // Waiting period before the next saccade start time.
            // In model mode, hold the last fixation.
            if (t < m_Time)
            {
                if (m_UseModel)
                {
                    foreach (var eye in m_Eyes)
                    {
                        if (eye == null)
                            continue;

                        eye.m_TargetRotation = eye.m_LastFixedRotation;
                    }

                    ApplyProcessedSaccade();
                }
                else
                {
                    foreach (var eye in m_Eyes)
                    {
                        if (eye == null || eye.m_Eye == null)
                            continue;

                        if (eye.m_LastRotation != Quaternion.identity)
                        {
                            eye.m_Eye.localRotation *= Quaternion.Inverse(eye.m_LastRotation);
                            eye.m_LastRotation = Quaternion.identity;
                        }
                    }
                }

                return;
            }

            // After the active window, model mode holds the fixed rotation.
            if (t > (m_Time + m_Duration) && m_UseModel)
            {
                foreach (var eye in m_Eyes)
                {
                    if (eye == null)
                        continue;

                    eye.m_TargetRotation = eye.m_FixedRotation;
                }

                ApplyProcessedSaccade();
                return;
            }

            // Active window: interpolate lastFixed -> fixed with the C++ easing.
            if (t >= m_Time && t <= (m_Time + m_Duration))
            {
                // C++ easing:
                //   r = (t - _time) / _dur
                //   y = 1 - sqrt(1 - (r - 1)^2)
                //   s = 1 - y
                // This produces a smooth ease-in/out style curve without overshoot.

                float r = (t - m_Time) / m_Duration;
                r = Mathf.Clamp01(r);

                float y = 1.0f - Mathf.Sqrt(1.0f - (r - 1.0f) * (r - 1.0f));
                float s = 1.0f - y;

                foreach (var eye in m_Eyes)
                {
                    if (eye == null)
                        continue;

                    eye.m_TargetRotation = Quaternion.Slerp(eye.m_LastFixedRotation, eye.m_FixedRotation, s);
                }

                m_SaccadeState = SaccadeState.FadeIn;

                ApplyProcessedSaccade();
                return;
            }
        }


        protected virtual void ApplyProcessedSaccade()
        {
            // Saccades are applied as an additive rotation offset in the eye bone's local space.
            //
            // The critical question is what "baseline" means each frame:
            //
            // 1) If another system overwrites the eye bones every frame (Animator, OnAnimatorIK look-at, rig constraints),
            //    then eye.m_Eye.localRotation at this point is already the correct baseline pose for this frame.
            //    In that case we should apply: baseline * saccadeOffset
            //
            // 2) If NOTHING overwrites the eye bones every frame, then eye.m_Eye.localRotation already includes the last
            //    saccade offset we applied. If we apply baseline * saccadeOffset again, offsets will accumulate and the eyes
            //    will drift/spin over time.
            //
            // OwnBaselineWithUndo prevents accumulation by undoing the previously applied saccade offset before applying the new one.
            //
            // AutoDetect is the recommended default because different rigs (and even gaze on/off) can change whether eyes are
            // overwritten externally each frame.

            if (m_Eyes == null)
                return;

            // Clamp to sane ranges to avoid accidental editor values.
            int externalConfirm = Mathf.Max(1, m_AutoDetectSettings.m_ExternalOverwriteConfirmFrames);
            int noOverwriteConfirm = Mathf.Max(1, m_AutoDetectSettings.m_NoOverwriteConfirmFrames);
            float thresholdDeg = Mathf.Max(0.0f, m_AutoDetectSettings.m_ExternalOverwriteThresholdDeg);

            foreach (var eye in m_Eyes)
            {
                if (eye == null || eye.m_Eye == null)
                    continue;

                Quaternion current = eye.m_Eye.localRotation;
                Quaternion offsetNow = eye.m_TargetRotation;

                EyeBaselineMode resolvedMode = m_EyeBaselineMode;

                if (resolvedMode == EyeBaselineMode.AutoDetect)
                {
                    float overwriteDelta = 0;
                    bool looksExternallyOverwritten = false;

                    if (eye.m_PrevAfterApplyRotation != Quaternion.identity)
                    {
                        overwriteDelta = Quaternion.Angle(eye.m_PrevAfterApplyRotation, current);
                        looksExternallyOverwritten = overwriteDelta > thresholdDeg;
                    }

                    eye.m_DebugOverwriteDeltaDeg = overwriteDelta;

                    // Debounce switching. We require a few consecutive frames before changing the resolved mode to avoid flapping.
                    if (looksExternallyOverwritten)
                    {
                        eye.m_ExternalOverwriteStreak++;
                        eye.m_NoOverwriteStreak = 0;

                        if (eye.m_ExternalOverwriteStreak >= externalConfirm)
                            eye.m_AutoUseExternalBaseline = true;
                    }
                    else
                    {
                        eye.m_NoOverwriteStreak++;
                        eye.m_ExternalOverwriteStreak = 0;

                        if (eye.m_NoOverwriteStreak >= noOverwriteConfirm)
                            eye.m_AutoUseExternalBaseline = false;
                    }

                    resolvedMode = eye.m_AutoUseExternalBaseline
                        ? EyeBaselineMode.AdditiveOnExternalBaseline
                        : EyeBaselineMode.OwnBaselineWithUndo;

                    eye.m_DebugResolvedBaselineMode = resolvedMode;
                }

                Quaternion baseline = current;

                if (resolvedMode == EyeBaselineMode.AdditiveOnExternalBaseline)
                {
                    // Another system owns the baseline pose each frame. Apply saccade as an offset on top.
                    eye.m_Eye.localRotation = baseline * offsetNow;

                    // We do not own the baseline in this mode, so we do not track lastRotation as something to undo.
                    eye.m_LastRotation = Quaternion.identity;
                }
                else
                {
                    // We own the baseline. Undo the previously applied offset so we do not accumulate rotations over time.
                    if (eye.m_LastRotation != Quaternion.identity)
                        baseline *= Quaternion.Inverse(eye.m_LastRotation);

                    eye.m_Eye.localRotation = baseline * offsetNow;

                    // Track what we applied so we can undo it on the next frame.
                    eye.m_LastRotation = offsetNow;
                }

                // Store what we set so AutoDetect can detect whether something overwrote the eye between frames.
                eye.m_PrevAfterApplyRotation = eye.m_Eye.localRotation;
            }
        }

        public void SetBehaviourMode(CharacterDefines.SaccadeType saccadeMode) => m_SaccadeMode = saccadeMode;

        #region Math Functions
        static float CalculateDuration(float amplitudeDegrees)
        {
            // Eyes Alive uses a linear duration model: duration = D + d * amplitude.
            // Here: Intercept == D (seconds), Slope == d (seconds/degree).

            return Intercept + Slope * amplitudeDegrees;
        }


        // should only be used in GenerateGaussianRandom()
        static double GaussRand_V1 = 0;
        static double GaussRand_V2 = 0;
        static double GaussRand_S = 0;
        static int GaussRand_phase = 0;

        /// <summary>
        /// Gaussian random using Box-Muller transform.
        /// Returns a sample with the given mean and variance (variant == variance).
        /// Matches typical C++ implementations that reuse the second sample.
        /// </summary>
        static float GenerateGaussianRandom(float mean, float variant)
        {
            // c++ gaussianRandom

            double X = 0;
            if (GaussRand_phase == 0)
            {
                do
                {
                    // UnityEngine.Random.Range(float) is inclusive on the upper bound.
                    // Divide by (RandMax + 1) so U is in [0,1) and never exactly 1.0.
                    double U1 = (double)UnityRandom.Range(0.0f, RandMax) / (RandMax + 1.0);
                    double U2 = (double)UnityRandom.Range(0.0f, RandMax) / (RandMax + 1.0);

                    GaussRand_V1 = 2 * U1 - 1;
                    GaussRand_V2 = 2 * U2 - 1;
                    GaussRand_S = GaussRand_V1 * GaussRand_V1 + GaussRand_V2 * GaussRand_V2;
                }
                while (GaussRand_S >= 1 || GaussRand_S == 0);

                X = GaussRand_V1 * Math.Sqrt(-2 * Math.Log(GaussRand_S) / GaussRand_S);
            }
            else
            {
                X = GaussRand_V2 * Math.Sqrt(-2 * Math.Log(GaussRand_S) / GaussRand_S);
            }

            GaussRand_phase = 1 - GaussRand_phase;
            double Xp = X * Math.Sqrt(variant) + mean;   // X is for standard normal distribution
            return (float)Xp;
        }


        float GenerateRandomDirection()
        {
            // Eyes Alive: saccade directions are quantized into 8 bins (45 deg increments) and
            // chosen according to an empirically measured distribution (Table 1).
            // Note: The paper also discusses following head rotation when head motion exceeds a threshold;
            // this controller uses the statistical bin distribution only.

            // c++ directionRandom

            float bound0 = GetCurrentModeAttribute(ModeAttributes.Bin_0);
            float bound45 = bound0 + GetCurrentModeAttribute(ModeAttributes.Bin_45);
            float bound90 = bound45 + GetCurrentModeAttribute(ModeAttributes.Bin_90);
            float bound135 = bound90 + GetCurrentModeAttribute(ModeAttributes.Bin_135);
            float bound180 = bound135 + GetCurrentModeAttribute(ModeAttributes.Bin_180);
            float bound225 = bound180 + GetCurrentModeAttribute(ModeAttributes.Bin_225);
            float bound270 = bound225 + GetCurrentModeAttribute(ModeAttributes.Bin_270);
            float bound315 = bound270 + GetCurrentModeAttribute(ModeAttributes.Bin_315);

            float dir = 0.0f;
            float binIndex = UnityRandom.Range(0.0f, 100.0f);
            if (binIndex >= 0.0f && binIndex < bound0)             dir =   0.0f;
            else if (binIndex >= bound0 && binIndex < bound45)     dir =  45.0f;
            else if (binIndex >= bound45 && binIndex < bound90)    dir =  90.0f;
            else if (binIndex >= bound90 && binIndex < bound135)   dir = 135.0f;
            else if (binIndex >= bound135 && binIndex < bound180)  dir = 180.0f;
            else if (binIndex >= bound180 && binIndex < bound225)  dir = 225.0f;
            else if (binIndex >= bound225 && binIndex < bound270)  dir = 270.0f;
            else if (binIndex >= bound270 && binIndex <= bound315) dir = 315.0f;
            return dir;
        }

        float GenerateRandomMagnitude()
        {
            // Eyes Alive: saccade magnitudes are drawn from an empirically fitted exponential distribution.
            // SmartBody implementation uses an inverse-transform-like sample: a = -6.9 * ln(f / 15.7).
            // The result is then clamped by a direction-dependent limit (vertical/diagonal reduced) to
            // keep rotations within plausible eyeball/eyelid constraints.

            // c++ magnitudeRandom

            float f = UnityRandom.Range(0.0f, 15.0f);
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

        /// <summary>
        /// Generates the time (seconds) until the next saccade.
        /// Based on the "Eyes Alive" parameters for the current mode.
        /// </summary>
        float GenerateRandomInterval()
        {
            // Eyes Alive discusses mutual-gaze vs gaze-away timing as a function of elapsed time (mode duration curves).
            // SmartBody simplifies this: choose Mutual vs Away based on Percentage_Mutual, then sample an interval
            // from a Gaussian (mean/variance), rejecting values below MinInterval.

            // c++ intervalRandom

            float dt = Time.deltaTime;

            float percentMutual = GetCurrentModeAttribute(ModeAttributes.Percentage_Mutual);
            float mutualMean = GetCurrentModeAttribute(ModeAttributes.Mutual_Mean);
            float mutualVariant = GetCurrentModeAttribute(ModeAttributes.Mutual_Variant);
            float awayMean = GetCurrentModeAttribute(ModeAttributes.Away_Mean);
            float awayVariant = GetCurrentModeAttribute(ModeAttributes.Away_Variant);

            float f = UnityRandom.Range(0.0f, 100.0f);
            float mutualPercent = percentMutual;

            if (f >= 0.0f && f <= mutualPercent)
                m_IntervalMode = IntervalMode.Mutual;
            else
                m_IntervalMode = IntervalMode.Away;

            float interval = -1.0f;
            while (interval < MinInterval)
            {
                if (m_IntervalMode == IntervalMode.Mutual)
                    interval = GenerateGaussianRandom(mutualMean * dt, mutualVariant * dt);

                if (m_IntervalMode == IntervalMode.Away)
                    interval = GenerateGaussianRandom(awayMean * dt, awayVariant * dt);
            }

            return interval;
        }

        float GetCurrentModeAttribute(ModeAttributes att) => GetModeAttribute(m_SaccadeMode, att);

        static float GetModeAttribute(CharacterDefines.SaccadeType mode, ModeAttributes att)
        {
            if (!m_AttMapping.TryGetValue(mode, out float[] attributes) || attributes == null)
            {
                Debug.LogError($"No attributes set up for saccade mode {mode}");
                return 1.0f;
            }

            return attributes[(int)att];
        }

        [ContextMenu("Reset Eyes Now")]
        public void ResetEyesNow()
        {
            if (m_Eyes == null)
                return;

            foreach (var eye in m_Eyes)
            {
                if (eye == null || eye.m_Eye == null)
                    continue;

                // Clear all saccade state.
                eye.m_LastRotation = Quaternion.identity;
                eye.m_TargetRotation = Quaternion.identity;
                eye.m_LastFixedRotation = Quaternion.identity;
                eye.m_FixedRotation = Quaternion.identity;

                // Reset AutoDetect bookkeeping.
                eye.m_PrevAfterApplyRotation = Quaternion.identity;
                eye.m_ExternalOverwriteStreak = 0;
                eye.m_NoOverwriteStreak = 0;
                eye.m_AutoUseExternalBaseline = false;

                // Put the bone back to eye.m_InitialRotation
                eye.m_Eye.localRotation = eye.m_InitialRotation;
            }

            // Also cancel any in-flight saccade window.
            m_SaccadeState = SaccadeState.Finished;
        }

        private void UpdateDebugInfo()
        {
            if (!m_DebugInspector)
                return;

            float now = Time.time;

            m_DebugInfo.intervalMode = m_IntervalMode;
            m_DebugInfo.state = m_SaccadeState;

            m_DebugInfo.now = now;
            m_DebugInfo.nextStartTime = m_Time;
            m_DebugInfo.endTime = m_Time + m_Duration;

            m_DebugInfo.timeUntilNextStart = (m_Time >= 0.0f) ? (m_Time - now) : 0.0f;
            m_DebugInfo.timeRemainingInSaccade = (now <= m_Time + m_Duration) ? ((m_Time + m_Duration) - now) : 0.0f;

            m_DebugInfo.directionDeg = m_Direction;
            m_DebugInfo.magnitudeDeg = m_Magnitude;
            m_DebugInfo.durationSec = m_Duration;
            m_DebugInfo.waitSec = m_WaitTime;

            if (m_Eyes != null && m_Eyes.Length >= 2)
            {
                m_DebugInfo.leftOffsetAngleDeg = Quaternion.Angle(Quaternion.identity, m_Eyes[0].m_TargetRotation);
                m_DebugInfo.rightOffsetAngleDeg = Quaternion.Angle(Quaternion.identity, m_Eyes[1].m_TargetRotation);
            }

            if (m_Eyes != null && m_Eyes.Length >= 2)
            {
                EyeData left = m_Eyes[0];
                if (left != null)
                {
                    m_DebugInfo.leftResolvedBaselineMode = left.m_DebugResolvedBaselineMode;
                    m_DebugInfo.leftOverwriteDeltaDeg = left.m_DebugOverwriteDeltaDeg;
                    m_DebugInfo.leftExternalOverwriteStreak = left.m_ExternalOverwriteStreak;
                    m_DebugInfo.leftNoOverwriteStreak = left.m_NoOverwriteStreak;
                    m_DebugInfo.leftAutoUseExternalBaseline = left.m_AutoUseExternalBaseline;
                }
            }
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null) return "<null>";
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, '/');
                sb.Insert(0, t.name);
            }
            return sb.ToString();
        }

#if true
        void DebugLogFrame(string tag) { }
#else
        [SerializeField] private bool m_DebugLogEachFrame = true;
        [SerializeField] private int m_DebugLogEveryNFrames = 5;
        private int m_DebugFrameCounter = 0;

        void DebugLogFrame(string tag)
        {
            if (!m_DebugLogEachFrame)
                return;

            m_DebugFrameCounter++;
            if (m_DebugLogEveryNFrames > 1 && (m_DebugFrameCounter % m_DebugLogEveryNFrames) != 0)
                return;

            float t = Time.time;

            // Keep it one line, but include the important scalars and per-eye offsets.
            // Use localEulerAngles only for readability; the quats are what matter.
            string lEye = (m_Eyes != null && m_Eyes.Length > 0 && m_Eyes[0] != null && m_Eyes[0].m_Eye != null) ? m_Eyes[0].m_Eye.localEulerAngles.ToString("F2") : "null";
            string rEye = (m_Eyes != null && m_Eyes.Length > 1 && m_Eyes[1] != null && m_Eyes[1].m_Eye != null) ? m_Eyes[1].m_Eye.localEulerAngles.ToString("F2") : "null";

            string lOff = (m_Eyes != null && m_Eyes.Length > 0 && m_Eyes[0] != null) ? m_Eyes[0].m_LastRotation.eulerAngles.ToString("F2") : "null";
            string rOff = (m_Eyes != null && m_Eyes.Length > 1 && m_Eyes[1] != null) ? m_Eyes[1].m_LastRotation.eulerAngles.ToString("F2") : "null";

            Debug.Log(
                $"[Saccade][{tag}] t={t:F3} mode={m_SaccadeMode} useModel={m_UseModel} state={m_SaccadeState} " +
                $"time={m_Time:F3} dur={m_Duration:F4} wait={m_WaitTime:F3} " +
                $"dir={m_Direction:F1} mag={m_Magnitude:F3} " +
                $"eyeL={lEye} eyeR={rEye} offL={lOff} offR={rOff} " +
                $"offAngL={Quaternion.Angle(Quaternion.identity, m_Eyes[0].m_TargetRotation):F2}"
            );
        }
#endif
        #endregion
        #endregion
    }
}
