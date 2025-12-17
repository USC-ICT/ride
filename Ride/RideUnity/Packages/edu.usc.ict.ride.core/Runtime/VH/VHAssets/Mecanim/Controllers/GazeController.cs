using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
    /// <summary>
    /// Base class that manages gaze targeting behavior for a character.
    /// 
    /// This controller is responsible for:
    /// - Selecting a new gaze target
    /// - Managing transitions between targets
    /// - Computing gaze weights for head, eyes, and body
    /// - Driving time-based interpolation (fade-in, fade-out, transitioning)
    /// 
    /// Gaze behavior is driven by three primary concepts:
    /// 1. <b>Gaze Parts</b> - which anatomical systems participate (Head, Eyes, Body).
    /// 2. <b>Gaze State</b> - current phase of the gaze system (Off, FadeIn, FadeOut, On).
    /// 3. <b>Interpolation</b> - per-frame updates that advance fade timers and transitions over time.
    /// 
    /// This class does not directly manipulate IK or animation.
    /// Subclasses (e.g., <see cref="GazeController_IK"/>) implement the actual animation system hookup.
    /// 
    /// Behavior note:
    /// The gaze system supports overlapping fades for the different channels
    /// (head, eyes, body, and total). Multiple channels can be in the middle
    /// of a fade at the same time, which preserves the "soft blending" behavior
    /// expected by existing RIDE content.
    /// </summary>
    public abstract class GazeController : MonoBehaviour, IEyeGazeProvider
    {
        #region Constants
        public const float DefaultHeadGazeSpeed = 400f;
        public const float DefaultEyeGazeSpeed = 400f;
        public const float DefaultBodyGazeSpeed = 400f;
        public const float DefaultFadeOutTime = 1.0f;  // seconds
        #endregion

        /// <summary>
        /// Specifies which anatomical components participate in a gaze change.
        /// 
        /// Multiple flags may be combined.
        /// </summary>
        [Flags]
        public enum GazeParts
        {
            None = 0,
            Body = 1 << 0,
            Head = 1 << 1,
            Eyes = 1 << 2,

            All = Body | Head | Eyes
        }

        /// <summary>
        /// Represents the current operational phase of the gaze controller.
        /// </summary>
        public enum GazeState
        {
            Off,
            FadeIn,
            On,
            FadeOut,
        }


        #region Fields
        /// <summary>
        /// The current world-space object the character is attempting to look at.
        /// 
        /// When a new target is assigned through any of the <c>SetGazeTarget*</c> methods,
        /// the gaze controller will smoothly transition toward it using the configured
        /// gaze weights (head, eyes, body) and timing parameters.  
        /// 
        /// A null value indicates that the gaze system is inactive or fading out.
        /// </summary>
        [Header("Gaze Target")]
        [Tooltip("Current world-space object the character should look at. "
               + "Assigned automatically by SetGazeTarget*, or manually for debugging.")]
        [SerializeField] protected GameObject m_GazeTarget;

        /// <summary>
        /// Optional world-space positional adjustment applied to the computed gaze target.
        /// 
        /// This value is used as a calibration or "fudge" factor when an avatar's skeleton
        /// causes the visual gaze line to appear slightly above, below, or offset from the
        /// true target position.  
        /// 
        /// A non-zero offset shifts the final gaze point by a constant world-space amount.
        /// This is useful when models have slightly different neck/head joint placements,
        /// causing gaze to appear inaccurate even when mathematically correct.
        /// 
        /// A value of <c>Vector3.zero</c> preserves the default behavior.
        /// </summary>
        [Header("Gaze Calibration")]
        [Tooltip("Optional world-space offset applied to the computed gaze point. "
               + "Used to correct character-specific skeleton alignment issues "
               + "(e.g., neck joint too high/low).")]
        [SerializeField] protected Vector3 m_GazeOffset = Vector3.zero;

        [Header("Vertical Gaze Mapping (for eyelids)")]
        [Tooltip("Optional origin for vertical gaze calculations (e.g., head or eye root). " +
                "If left null, this GameObject's transform is used. " +
                "For the IK-based gaze controller, leave this unset to use the rig's built-in reference.")]
        [SerializeField] protected Transform m_GazeOrigin;

        [Tooltip("Maximum upward eye rotation (degrees) used for normalization (0..1)." +
                "Used with GetVerticalGaze()")]
        [SerializeField] protected float m_MaxUpGazeAngle = 30f;

        [Tooltip("Maximum downward eye rotation (degrees) used for normalization (-1..0)." +
                "Used with GetVerticalGaze()")]
        [SerializeField] protected float m_MaxDownGazeAngle = 40f;

        #endregion

        #region Runtime state

        private GazeState m_GazeState = GazeState.Off;

        /// <summary>
        /// Encapsulates the fade state for a single gaze channel (head, eyes, body, total).
        /// </summary>
        private struct GazeFadeChannel
        {
            public bool IsActive;
            public float Start;
            public float End;
            public float Duration;
            public float Elapsed;

            /// <summary>
            /// Begins a fade from the given start weight to the given end weight
            /// over the specified duration. Resets the fade timer and activates
            /// the channel. If duration is zero or negative, the fade is skipped
            /// and the channel becomes inactive.
            /// </summary>
            public void StartFade(float duration, float start, float end)
            {
                if (duration <= 0f)
                {
                    IsActive = false;
                    Duration = 0f;
                    Elapsed  = 0f;
                    Start    = start;
                    End      = end;
                    return;
                }

                IsActive = true;
                Duration = duration;
                Elapsed  = 0f;
                Start    = start;
                End      = end;
            }

            /// <summary>
            /// Stops any active fade and clears the fade timer. The current
            /// Start/End values are left unchanged. Callers can set new
            /// values before starting another fade.
            /// </summary>
            public void ResetFade()
            {
                IsActive = false;
                Duration = 0f;
                Elapsed  = 0f;
                // Start/End left as-is; callers set them when starting a new fade.
            }

            /// <summary>
            /// Advances the fade timer and computes the current interpolated weight.
            /// </summary>
            /// <param name="dt">Delta time in seconds.</param>
            /// <param name="weight">
            /// The interpolated weight for this frame if the channel is active; 
            /// undefined when the method returns <c>false</c>.
            /// </param>
            /// <returns>
            /// <c>true</c> if the channel was active and produced a new weight for this frame;
            /// <c>false</c> if the channel is inactive or has no valid duration.
            /// </returns>
            public bool UpdateFade(float dt, out float weight)
            {
                weight = 0f;

                if (!IsActive || Duration <= 0f)
                    return false;

                // Advance timer and compute normalized progress (0..1).
                Elapsed += dt;
                float t = Mathf.Clamp01(Elapsed / Duration);

                // SmoothStep gives a softer ease-in/out compared to linear interpolation.
                weight = Mathf.SmoothStep(Start, End, t);

                // When the fade completes, snap to the final value and mark the channel idle.
                if (Elapsed >= Duration)
                {
                    weight   = End;
                    IsActive = false;
                }

                return true;
            }
        }

        // Per-channel fade state.
        private GazeFadeChannel m_HeadFade;
        private GazeFadeChannel m_EyeFade;
        private GazeFadeChannel m_BodyFade;

        // Optional total gaze weight (used by IK implementations).
        private GazeFadeChannel m_TotalFade;

        // Position transition state (for smooth target-to-target movement).
        private bool m_IsTransitioning;
        private Vector3 m_TransitionStartPos;
        private float m_TransitionDuration;
        private float m_TransitionElapsed;

        // When true, we check for fade-completion and call HandleGazeFinished().
        private bool m_PendingGazeFinished;

        #endregion

        #region Properties
        public virtual float HeadGazeWeight { get; set; }
        public virtual float EyeGazeWeight { get; set; }
        public virtual float BodyGazeWeight { get; set; }

        public virtual float CurrentHeadGazeWeight { get; set; }
        public virtual float CurrentEyeGazeWeight { get; set; }
        public virtual float CurrentBodyGazeWeight { get; set; }
        public virtual float CurrentTotalGazeWeight { get; set; }

        protected GazeState CurrentGazeState => m_GazeState;
        #endregion

        private void Update()
        {
            // Skip work when there is nothing to animate. This keeps per-frame
            // overhead minimal for characters whose gaze is currently static.
            if (!m_HeadFade.IsActive && !m_EyeFade.IsActive && !m_BodyFade.IsActive && !m_TotalFade.IsActive && !m_IsTransitioning && !m_PendingGazeFinished)
                return;

            // Central per-frame update:
            // - Advance head/eye/body/total fade timers
            // - Advance positional transition between old/new targets
            // - When all fades complete, finalize the gaze state
            float dt = Time.deltaTime;

            UpdateHeadFade(dt);
            UpdateEyeFade(dt);
            UpdateBodyFade(dt);
            UpdateTotalFade(dt);
            UpdateTransition(dt);

            if (m_PendingGazeFinished && !AnyFadeActive())
            {
                m_PendingGazeFinished = false;
                HandleGazeFinished();
            }
        }

        // IEyeGazeProvider implementation

        /// <summary>
        /// Computes a normalized measure of how far the character is looking up or down
        /// relative to a reference origin.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is primarily used by eyelid and "soft eyes" systems through
        /// <see cref="IEyeGazeProvider"/>.  It converts the current gaze direction
        /// into a value in the range [-1, 1], where:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>-1</c> means looking as far down as configured.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>0</c> means looking roughly straight ahead.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>+1</c> means looking as far up as configured.</description>
        ///   </item>
        /// </list>
        /// <para>
        /// The computation is based on world space geometry rather than the local axes
        /// of the head or eye joints.  In practice, many rigs have non-standard bone
        /// orientations (for example, head.up may point roughly forward instead of
        /// world up), which makes using local forward/up vectors unreliable.  To keep
        /// the result stable across rigs, the method:
        /// </para>
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///     Chooses an origin point: <see cref="m_GazeOrigin"/> if set, otherwise
        ///     this GameObject's transform.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     Computes the vector from that origin to the current gaze position
        ///     (see <see cref="GetGazePosition"/>), and splits it into a vertical
        ///     component <c>dy</c> (world Y) and a horizontal magnitude in the XZ
        ///     plane.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     Uses <c>Mathf.Atan2(dy, horizontal)</c> to get a signed vertical angle
        ///     in degrees.  Positive angles mean the target is above the origin,
        ///     negative angles mean it is below.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     Normalizes that angle by dividing by <see cref="m_MaxUpGazeAngle"/> or
        ///     <see cref="m_MaxDownGazeAngle"/>, clamping to the interval [-1, 1].
        ///     Upward angles map into [0, 1], downward angles into [-1, 0].
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// This world-space approach makes the vertical gaze signal independent of
        /// skeleton specific quirks, while still respecting user configurable limits
        /// for "how far up" and "how far down" the character is allowed to look.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A normalized vertical gaze value in the range [-1, 1], where negative values
        /// indicate downward gaze, zero is approximately straight ahead, and positive
        /// values indicate upward gaze.  If there is no current gaze target, returns 0.
        /// </returns>
        public float GetVerticalGaze()
        {
            if (m_GazeTarget == null)
                return 0f;

            Transform origin = m_GazeOrigin != null ? m_GazeOrigin : transform;

            Vector3 originPos = origin.position;
            Vector3 targetPos = GetGazePosition();

            Vector3 delta = targetPos - originPos;

            // Horizontal distance in the XZ plane (ignore vertical component).
            float horizontal = new Vector3(delta.x, 0f, delta.z).magnitude;

            if (horizontal < 0.0001f)
                return 0f;

            // Vertical difference (world up).
            float dy = delta.y;

            // Angle in degrees: positive = up, negative = down.
            float angleRad = Mathf.Atan2(dy, horizontal);
            float angleDeg = angleRad * Mathf.Rad2Deg;

            float norm;
            if (angleDeg > 0f)
            {
                float maxUp = Mathf.Max(1e-3f, m_MaxUpGazeAngle);
                norm = Mathf.Clamp01(angleDeg / maxUp);
            }
            else
            {
                float maxDown = Mathf.Max(1e-3f, m_MaxDownGazeAngle);
                norm = -Mathf.Clamp01(-angleDeg / maxDown);
            }

            //Debug.Log($"[GetVerticalGaze] origin={origin.name} angleDeg={angleDeg:F1} norm={norm:F2}");

            return norm;
        }

        #region Core API

        // Derived classes (IK / manual) use this to push Current* weights into their rig.
        public abstract void UpdateGaze();

        /// <summary>
        /// Initializes a gaze change request for the specified target object.
        /// 
        /// This sets up the appropriate <see cref="GazeState"/>:
        /// - If transitioning from an existing target, the caller may configure
        ///   a positional transition between old and new targets.
        /// - If no previous gaze target exists, the system will simply fade in.
        /// 
        /// This method does not perform any time-based updates; it only configures
        /// internal state that <see cref="Update"/> will advance each frame.
        /// Subclasses may override to add pre-initialization steps.
        /// </summary>
        /// <param name="gazeTarget">The GameObject to focus on.</param>
        protected virtual void InitGaze(GameObject gazeTarget)
        {
            // Starting a new gaze cancels the idea of "finishing" any previous one.
            m_PendingGazeFinished = false;

            m_GazeTarget = gazeTarget;

            // Cancel any pending total fade; derived classes (IK) will set the total weight explicitly.
            m_TotalFade.ResetFade();

            if (m_GazeTarget != null)
                m_GazeState = GazeState.FadeIn;
            else
                m_GazeState = GazeState.Off;
        }

        /// <summary>
        /// Sets a new gaze target using default speeds for all gaze parts.
        /// Equivalent to calling <see cref="SetGazeTargetWithSpeed(GameObject, GazeParts)"/> with <see cref="GazeParts.All"/>.
        /// </summary>
        /// <param name="gazeTarget">The GameObject to focus the gaze on.</param>
        public void SetGazeTarget(GameObject gazeTarget) => SetGazeTargetWithSpeed(gazeTarget, GazeParts.All);

        /// <summary>
        /// Sets a new gaze target using default speed values for the selected gaze parts.
        /// Any gaze part not included in <paramref name="gazeParts"/> will fade out.
        /// </summary>
        /// <param name="gazeTarget">The GameObject to focus on.</param>
        /// <param name="gazeParts">Which anatomical components should participate in the gaze movement.</param>
        public void SetGazeTargetWithSpeed(GameObject gazeTarget, GazeParts gazeParts) =>
            SetGazeTargetWithSpeed(
                gazeTarget,
                IsSet(gazeParts, GazeParts.Head) ? DefaultHeadGazeSpeed : 0f,
                IsSet(gazeParts, GazeParts.Eyes) ? DefaultEyeGazeSpeed : 0f,
                IsSet(gazeParts, GazeParts.Body) ? DefaultBodyGazeSpeed : 0f);

        /// <summary>
        /// Sets a new gaze target using explicit fade-in speeds for the head, eyes, and body.
        /// A value less than or equal <c>0</c> causes the corresponding part to fade out instead.
        /// </summary>
        /// <param name="gazeTarget">The GameObject to focus on.</param>
        /// <param name="headSpeed">Fade-in speed for the head. Must be &gt; 0 to activate head movement.</param>
        /// <param name="eyeSpeed">Fade-in speed for the eyes. Must be &gt; 0 to activate eye movement.</param>
        /// <param name="bodySpeed">Fade-in speed for the body. Must be &gt; 0 to activate body movement.</param>
        public void SetGazeTargetWithSpeed(GameObject gazeTarget, float headSpeed, float eyeSpeed, float bodySpeed)
        {
            if (gazeTarget == null)
            {
                // Null target means "stop gazing" – delegate to StopGaze() so we
                // respect the configured fade-out behavior
                StopGaze();
                return;
            }

            ComputeGazeTransitionGeometry(gazeTarget, out bool hadTarget, out Vector3 originPos, out Vector3 toTargetPos, out Vector3 fromPos);

            // Compute the angular difference between old and new directions.
            // The head/eye/body durations are derived from this angle and the
            // requested speeds; this keeps behavior stable regardless of distance.
            Vector3 fromDir = fromPos    - originPos;
            Vector3 toDir   = toTargetPos - originPos;

            float angle = 0f;
            if (fromDir.sqrMagnitude > 0.0001f && toDir.sqrMagnitude > 0.0001f)
                angle = Vector3.Angle(fromDir, toDir);

            float headDuration = (headSpeed > 0f && angle > 0.01f) ? angle / Mathf.Max(headSpeed, 0.0001f) : 0f;
            float eyeDuration = (eyeSpeed > 0f && angle > 0.01f) ? angle / Mathf.Max(eyeSpeed, 0.0001f) : 0f;
            float bodyDuration = (bodySpeed > 0f && angle > 0.01f) ? angle / Mathf.Max(bodySpeed, 0.0001f) : 0f;

            // Common target/transition handling.
            ApplyTargetAndTransition(gazeTarget, hadTarget, toTargetPos, fromPos, headDuration, eyeDuration, bodyDuration);

            // Head channel: fade from current head weight to the configured target
            // head weight, if speed and duration are valid. A speed of 0 means this
            // channel should fade out instead.
            if (headSpeed > 0f && HeadGazeWeight > 0f && headDuration > 0f)
                StartChannelFade(headDuration, HeadGazeWeight, CurrentHeadGazeWeight, ref m_HeadFade);
            else if (headSpeed <= 0f)
                // Preserve original semantics: 0 speed = fade this channel out.
                StartChannelFadeOut(DefaultFadeOutTime, CurrentHeadGazeWeight, ref m_HeadFade);

            // Eye channel.
            if (eyeSpeed > 0f && EyeGazeWeight > 0f && eyeDuration > 0f)
                StartChannelFade(eyeDuration, EyeGazeWeight, CurrentEyeGazeWeight, ref m_EyeFade);
            else if (eyeSpeed <= 0f)
                StartChannelFadeOut(DefaultFadeOutTime, CurrentEyeGazeWeight, ref m_EyeFade);

            // Body channel.
            if (bodySpeed > 0f && BodyGazeWeight > 0f && bodyDuration > 0f)
                StartChannelFade(bodyDuration, BodyGazeWeight, CurrentBodyGazeWeight, ref m_BodyFade);
            else if (bodySpeed <= 0f)
                StartChannelFadeOut(DefaultFadeOutTime, CurrentBodyGazeWeight, ref m_BodyFade);
        }

        /// <summary>
        /// Sets a new gaze target using explicit fade-in durations instead of speeds.
        /// A duration must be greater than zero to enable that gaze part.
        /// </summary>
        /// <param name="gazeTarget">The GameObject to focus on.</param>
        /// <param name="headFadeInDuration">Fade-in duration for the head.</param>
        /// <param name="eyeFadeInDuration">Fade-in duration for the eyes.</param>
        /// <param name="bodyFadeInDuration">Fade-in duration for the body.</param>
        public void SetGazeTargetWithDuration(GameObject gazeTarget, float headFadeInDuration, float eyeFadeInDuration, float bodyFadeInDuration)
        {
            if (gazeTarget == null)
            {
                // Null target = stop gazing, with default fade-out behavior.
                StopGaze();
                return;
            }

            ComputeGazeTransitionGeometry(gazeTarget, out bool hadTarget, out Vector3 originPos, out Vector3 toTargetPos, out Vector3 fromPos);

            // Common target/transition handling: here the per-channel values
            // are already durations, so we pass them straight through.
            ApplyTargetAndTransition(gazeTarget, hadTarget, toTargetPos, fromPos, headFadeInDuration, eyeFadeInDuration, bodyFadeInDuration);

            // Head channel: positive duration enables a fade to HeadGazeWeight,
            // non-positive duration means fade this channel out.
            if (headFadeInDuration > 0f && HeadGazeWeight > 0f)
                StartChannelFade(headFadeInDuration, HeadGazeWeight, CurrentHeadGazeWeight, ref m_HeadFade);
             else if (headFadeInDuration <= 0f)
                StartChannelFadeOut(DefaultFadeOutTime, CurrentHeadGazeWeight, ref m_HeadFade);

            // Eyes.
            if (eyeFadeInDuration > 0f && EyeGazeWeight > 0f)
                StartChannelFade(eyeFadeInDuration, EyeGazeWeight, CurrentEyeGazeWeight, ref m_EyeFade);
            else if (eyeFadeInDuration <= 0f)
                StartChannelFadeOut(DefaultFadeOutTime, CurrentEyeGazeWeight, ref m_EyeFade);

            // Body.
            if (bodyFadeInDuration > 0f && BodyGazeWeight > 0f)
                StartChannelFade(bodyFadeInDuration, BodyGazeWeight, CurrentBodyGazeWeight, ref m_BodyFade);
            else if (bodyFadeInDuration <= 0f)
                StartChannelFadeOut(DefaultFadeOutTime, CurrentBodyGazeWeight, ref m_BodyFade);
        }

        /// <summary>
        /// Stops gaze and fades all gaze weights to zero
        /// using <see cref="DefaultFadeOutTime"/>.
        /// </summary>
        public void StopGaze() => StopGaze(DefaultFadeOutTime);

        /// <summary>
        /// Stops gaze and fades all gaze weights to zero over the specified duration.
        /// </summary>
        /// <param name="fadeoutTime">The duration over which gaze influence decays to zero.</param>
        public void StopGaze(float fadeoutTime)
        {
            if (fadeoutTime <= 0f)
            {
                // Hard stop: immediately clear all fade state and zero weights.
                m_HeadFade.ResetFade();
                m_EyeFade.ResetFade();
                m_BodyFade.ResetFade();
                m_TotalFade.ResetFade();

                CurrentHeadGazeWeight  = 0f;
                CurrentEyeGazeWeight   = 0f;
                CurrentBodyGazeWeight  = 0f;
                CurrentTotalGazeWeight = 0f;

                m_GazeTarget           = null;
                m_GazeState            = GazeState.Off;
                m_IsTransitioning      = false;
                m_TransitionDuration   = 0f;
                m_TransitionElapsed    = 0f;
                m_PendingGazeFinished  = false;
                return;
            }

            // Soft stop: mark the controller as fading out, and start a fade
            // on each channel (head, eyes, body, total). When all fades complete,
            // HandleGazeFinished() will transition us to Off.
            m_GazeState = GazeState.FadeOut;

            StartChannelFadeOut(fadeoutTime, CurrentHeadGazeWeight,  ref m_HeadFade);
            StartChannelFadeOut(fadeoutTime, CurrentEyeGazeWeight,   ref m_EyeFade);
            StartChannelFadeOut(fadeoutTime, CurrentBodyGazeWeight,  ref m_BodyFade);

            // Fade total weight as well, so IK look-at intensity ramps down smoothly.
            StartChannelFadeOut(fadeoutTime, CurrentTotalGazeWeight, ref m_TotalFade);

            m_PendingGazeFinished = true;
        }

        /// <summary>
        /// Computes the final world-space position the character should look toward.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///     During a transition between two gaze targets, this method returns a
        ///     linearly interpolated position between the previous and next target
        ///     based on the internal transition timer.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     When not transitioning, this method returns the current gaze target's
        ///     world-space position (or a point in front of the character if no target is set).
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     After the base position is computed, a user-defined calibration offset
        ///     (<see cref="m_GazeOffset"/>) is applied. This allows each character
        ///     prefab to compensate for skeleton differences (e.g., neck joint too
        ///     high/low) while keeping gaze logic shared.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// The final gaze target position in world space, after interpolation and
        /// optional calibration offset.
        /// </returns>
        public Vector3 GetGazePosition()
        {
            if (m_GazeTarget == null)
                return transform.position + transform.forward * 2f;

            Vector3 targetPos = m_GazeTarget.transform.position;

            if (m_IsTransitioning && m_TransitionDuration > 0f)
            {
                float t = Mathf.Clamp01(m_TransitionElapsed / m_TransitionDuration);
                Vector3 basePos = Vector3.Lerp(m_TransitionStartPos, targetPos, t);
                return basePos + m_GazeOffset;
            }

            return targetPos + m_GazeOffset;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Computes common geometric data for a gaze change:
        /// - Whether we already had a gaze target or transition
        /// - The origin position (character)
        /// - The new target position
        /// - The starting position for the positional transition
        /// </summary>
        private void ComputeGazeTransitionGeometry(
            GameObject gazeTarget,
            out bool hadTarget,
            out Vector3 originPos,
            out Vector3 toTargetPos,
            out Vector3 fromPos)
        {
            hadTarget   = m_GazeTarget != null || m_IsTransitioning;
            originPos   = transform.position;
            toTargetPos = gazeTarget.transform.position;

            if (hadTarget)
            {
                // Start from the current gaze position (may already be in-between targets).
                fromPos = GetGazePosition();
            }
            else
            {
                // No existing target: synthesize a point straight ahead at the
                // same depth as the new target.
                float depth = (toTargetPos - originPos).magnitude;
                fromPos = originPos + transform.forward * depth;
            }
        }

        /// <summary>
        /// Applies the common target/transition logic once per gaze request:
        /// - Sets up base gaze state via <see cref="InitGaze"/>
        /// - Computes a transition duration from the provided per-channel durations
        /// - Either starts an in-between transition or snaps directly to the new target
        /// </summary>
        private void ApplyTargetAndTransition(
            GameObject gazeTarget,
            bool hadTarget,
            Vector3 toTargetPos,
            Vector3 fromPos,
            float headDuration,
            float eyeDuration,
            float bodyDuration)
        {
            // Choose a transition duration from the active channels.
            float transitionDuration = headDuration;
            if (transitionDuration <= 0f) transitionDuration = eyeDuration;
            if (transitionDuration <= 0f) transitionDuration = bodyDuration;

            // Configure base state (target, gaze state, total weight fade).
            InitGaze(gazeTarget);

            if (hadTarget && transitionDuration > 0f)
            {
                // Smoothly move from the old gaze point to the new target.
                StartTransition(fromPos, transitionDuration);
            }
            else
            {
                // No previous target or no time to transition: snap to the new target.
                m_IsTransitioning    = false;
                m_TransitionDuration = 0f;
                m_TransitionElapsed  = 0f;
                m_TransitionStartPos = toTargetPos;
            }
        }

        private void StartChannelFade(
            float duration,
            float targetWeight,
            float currentWeight,
            ref GazeFadeChannel channel)
        {
            if (duration <= 0f)
                return;

            // Configure a smooth fade from the current weight to a new target
            // over the given duration. The actual interpolation is advanced
            // in Update*Fade() each frame.
            channel.StartFade(duration, currentWeight, targetWeight);

            m_PendingGazeFinished = true;
        }

        private void StartChannelFadeOut(
            float duration,
            float currentWeight,
            ref GazeFadeChannel channel)
        {
            if (duration <= 0f)
                return;

            // Configure a fade from the current weight down to zero.
            // The channel will be considered "idle" once the duration elapses.
            channel.StartFade(duration, currentWeight, 0f);

            m_PendingGazeFinished = true;
        }

        private void UpdateHeadFade(float dt) { if (m_HeadFade.UpdateFade(dt, out float w)) CurrentHeadGazeWeight = w; }
        private void UpdateEyeFade(float dt) { if (m_EyeFade.UpdateFade(dt, out float w)) CurrentEyeGazeWeight = w; }
        private void UpdateBodyFade(float dt) { if (m_BodyFade.UpdateFade(dt, out float w)) CurrentBodyGazeWeight = w; }
        private void UpdateTotalFade(float dt) { if (m_TotalFade.UpdateFade(dt, out float w)) CurrentTotalGazeWeight = w; }

        private void UpdateTransition(float dt)
        {
            if (!m_IsTransitioning || m_TransitionDuration <= 0f)
                return;

            // Advance the transition timer. The actual interpolation factor
            // is computed on demand in GetGazePosition().
            m_TransitionElapsed += dt;
            if (m_TransitionElapsed >= m_TransitionDuration)
            {
                // Clamp to the end and stop transitioning; from now on we use
                // the live target position directly.
                m_TransitionElapsed = m_TransitionDuration;
                m_IsTransitioning   = false;
            }
        }

        private void StartTransition(Vector3 fromPos, float duration)
        {
            if (duration <= 0f)
            {
                // No time to transition: mark the state as non-transitioning
                // but remember the starting position so GetGazePosition() can
                // still be consistent for this frame if needed.
                m_IsTransitioning    = false;
                m_TransitionDuration = 0f;
                m_TransitionElapsed  = 0f;
                m_TransitionStartPos = fromPos;
                return;
            }

            // Begin a new positional transition from 'fromPos' to the next
            // target position over the specified duration.
            m_IsTransitioning    = true;
            m_TransitionDuration = duration;
            m_TransitionElapsed  = 0f;
            m_TransitionStartPos = fromPos;
        }

        private bool AnyFadeActive() => m_HeadFade.IsActive || m_EyeFade.IsActive || m_BodyFade.IsActive || m_TotalFade.IsActive;

        #endregion

        #region State finalization

        /// <summary>
        /// Finalizes gaze state after a fade-in or fade-out completes.
        /// 
        /// - After <see cref="GazeState.FadeIn"/>, the system becomes fully active (<see cref="GazeState.On"/>).
        /// - After <see cref="GazeState.FadeOut"/>, the target is cleared and the system goes Off.
        /// </summary>
        protected virtual void HandleGazeFinished()
        {
            if (m_GazeState == GazeState.FadeIn)
            {
                m_GazeState = GazeState.On;
            }
            else if (m_GazeState == GazeState.FadeOut)
            {
                m_GazeState        = GazeState.Off;
                m_GazeTarget       = null;
                m_IsTransitioning  = false;
                m_TransitionDuration = 0f;
                m_TransitionElapsed  = 0f;
            }
        }

        private static bool IsSet(GazeParts value, GazeParts flag) => (value & flag) != 0;

        #endregion
    }
}
