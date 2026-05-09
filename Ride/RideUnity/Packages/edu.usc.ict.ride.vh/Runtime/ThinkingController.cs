using System;
using UnityEngine;
using VHAssets;

namespace Ride
{
    /// <summary>
    /// Controls character thinking behaviors, primarily gazes
    /// </summary>
    public class ThinkingController : MonoBehaviour
    {
        [Header("Gaze Weights")]
        [Tooltip("Gaze weights for eyes, head, and body while thinking.")]
        [SerializeField][Range(0f, 1f)] private float m_eyeGazeWeight = 0.15f;
        [SerializeField][Range(0f, 1f)] private float m_headGazeWeight = 0.15f;
        [SerializeField][Range(0f, 1f)] private float m_bodyGazeWeight = 0.01f;
        [SerializeField] private float m_enterBlendTime = 1.0f;
        [SerializeField] private float m_exitBlendTime = 1.0f;

        [Header("Gaze Focus Time")]
        [Tooltip("Minimum number of seconds character gazes at a single gaze target.")]
        [SerializeField] private int MinimumGazeFocus = 2;
        [Tooltip("Maximum number of seconds character gazes at a single gaze target.")]
        [SerializeField] private int MaximumGazeFocus = 6;

        [Header("Gaze Speeds")]
        [Tooltip("Minimum speed of head movement (roughly degrees per second of angular gaze movement).")]
        [SerializeField] private int MinimumGazeSpeed = 70;
        [Tooltip("Maximum speed of head movement (roughly degrees per second of angular gaze movement).")]
        [SerializeField] private int MaximumGazeSpeed = 90;

        [Header("Initial Delay Before Thinking")]
        [Tooltip("Initial delay in seconds before thinking behavior starts.")]
        [SerializeField] private float m_initialDelay = 1.2f;

        [Header("Gaze Targets")]
        [Tooltip("Gaze targets / pawns that the character can look at to indicate thinking. " +
            "These are Unity game objects that need to be defined in the scene, typically " +
            "under GazeTargets as part of the Main Camera.")]
        [SerializeField] private string[] m_thinkingGazeTargets = { "GazeTargetUpLeft",
            "GazeTargetUpRight", "GazeTargetDownLeft", "GazeTargetDownRight" };

        [Header("Debug")]
        [Tooltip("Toggle thinking nonverbal behavior on/off.")]
        [SerializeField] private bool m_debugThinkingToggle = false;

        private MecanimCharacter m_character;
        private GazeController m_gazeController;
        private System.Random m_Random = new System.Random();

        private bool m_isThinking = false;
        private string m_previousGazeTarget = "";
        private float m_nextGazeTime = 0f;
        private bool m_lastDebugThinking;

        private float m_origEyeGazeWeight;
        private float m_origHeadGazeWeight;
        private float m_origBodyGazeWeight;

        private float m_currentEyeWeight;
        private float m_currentHeadWeight;
        private float m_currentBodyWeight;

        private float m_blendStartEyeWeight;
        private float m_blendStartHeadWeight;
        private float m_blendStartBodyWeight;

        private bool m_previousThinkingState = false;
        private float m_currentBlendTime;
        private float m_currentBlendElapsed;
        private const float m_weightMargin = 0.0005f;
        private bool m_isBlending = false;

        /// <summary>
        /// Initializes the controller and assigns the MecanimCharacter reference if missing.
        /// </summary>
        protected virtual void Start()
        {
            // Non-loadable characters: initialize immediately.
            // Loadable characters: RideCatalogAsset will SendMessage InitializeLoadedAsset.
            if (!TryGetComponent(out Ride.ILoadableAsset _))
                InitializeLoadedAsset();
        }

        void Update()
        {
            // Debug toggle from Editor logic
            if (m_debugThinkingToggle != m_lastDebugThinking)
            {
                if (m_debugThinkingToggle)
                    StartThinkingBehavior(false);
                else
                    StopThinkingBehavior();

                m_lastDebugThinking = m_debugThinkingToggle;
            }

            // Thinking transition handling, including gaze weight blend in and out setup
            if (m_isThinking != m_previousThinkingState)
            {
                BeginGazeWeightBlend(m_isThinking);
            }

            // Return if no character defined
            if (m_character == null)
                return;

            // Update gaze weights while blending in or out
            if (m_isBlending)
                UpdateGazeWeights();

            // Do nothing else if not thinking
            if (!m_isThinking)
                return;

            // If it's time for a new target, select one and gaze at it
            if (Time.time >= m_nextGazeTime)
            {
                string newGazeTarget = GetNewGazeTarget();

                m_character.Gaze(
                    newGazeTarget,
                    m_Random.Next(MinimumGazeSpeed, MaximumGazeSpeed + 1)
                );

                m_previousGazeTarget = newGazeTarget;
                m_nextGazeTime = Time.time + m_Random.Next(MinimumGazeFocus, MaximumGazeFocus + 1);
            }
        }

        /// <summary>
        /// Blends gaze weights into and out of thinking mode. While thinking, gaze weights are
        /// smaller than the typical defaults. They need to be blended into and out of over time so
        /// that thinking behaviors and subsequent other behaviors (e.g., talking to user) are smooth.
        /// </summary>
        private void UpdateGazeWeights()
        {
            float targetEye = m_isThinking ? m_eyeGazeWeight : m_origEyeGazeWeight;
            float targetHead = m_isThinking ? m_headGazeWeight : m_origHeadGazeWeight;
            float targetBody = m_isThinking ? m_bodyGazeWeight : m_origBodyGazeWeight;

            bool eyeDone = IsAtTarget(m_currentEyeWeight, targetEye);
            bool headDone = IsAtTarget(m_currentHeadWeight, targetHead);
            bool bodyDone = IsAtTarget(m_currentBodyWeight, targetBody);

            if (eyeDone && headDone && bodyDone)
            {
                m_currentEyeWeight = targetEye;
                m_currentHeadWeight = targetHead;
                m_currentBodyWeight = targetBody;

                m_character.SetGazeWeights(
                    m_currentHeadWeight,
                    m_currentEyeWeight,
                    m_currentBodyWeight
                );

                m_isBlending = false;
                return;
            }

            if (m_currentBlendTime <= 0f)
            {
                m_currentEyeWeight = targetEye;
                m_currentHeadWeight = targetHead;
                m_currentBodyWeight = targetBody;
            }
            else
            {
                m_currentBlendElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(m_currentBlendElapsed / m_currentBlendTime);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                m_currentEyeWeight = Mathf.Lerp(m_blendStartEyeWeight, targetEye, easedT);
                m_currentHeadWeight = Mathf.Lerp(m_blendStartHeadWeight, targetHead, easedT);
                m_currentBodyWeight = Mathf.Lerp(m_blendStartBodyWeight, targetBody, easedT);
            }

            m_character.SetGazeWeights(
                m_currentHeadWeight,
                m_currentEyeWeight,
                m_currentBodyWeight
            );
        }

        private bool IsAtTarget(float current, float target)
        {
            return Mathf.Abs(current - target) < m_weightMargin;
        }

        private void BeginGazeWeightBlend(bool thinking)
        {
            m_currentBlendTime = thinking ? m_enterBlendTime : m_exitBlendTime;
            m_currentBlendElapsed = 0f;

            m_blendStartEyeWeight = m_currentEyeWeight;
            m_blendStartHeadWeight = m_currentHeadWeight;
            m_blendStartBodyWeight = m_currentBodyWeight;

            m_previousThinkingState = thinking;
            m_isBlending = true;
        }

        /// <summary>
        /// Select next gaze target; ensure it's different from the previous one.   
        /// </summary>
        private string GetNewGazeTarget()
        {
            if (m_thinkingGazeTargets.Length <= 1)
                return m_thinkingGazeTargets[0];

            string randomGazeTarget;
            do
            {
                randomGazeTarget = m_thinkingGazeTargets[m_Random.Next(0, m_thinkingGazeTargets.Length)];
            }
            while (randomGazeTarget.Equals(m_previousGazeTarget));

            return randomGazeTarget;
        }

        /// <summary>
        /// Starts character thinking nonverbal behavior, primarely gazing away from the user.
        /// </summary>
        /// <param name="withDelay">Boolean to indicate wether to delay start of thinking behavior. 
        /// Delay amount can be adjust in Editor.</param>
        public void StartThinkingBehavior(bool withDelay)
        {
            if (m_isThinking)
                return;

            if (m_character != null && m_gazeController != null)
            {
                m_origEyeGazeWeight = m_gazeController.EyeGazeWeight;
                m_origHeadGazeWeight = m_gazeController.HeadGazeWeight;
                m_origBodyGazeWeight = m_gazeController.BodyGazeWeight;

                m_currentEyeWeight = m_origEyeGazeWeight;
                m_currentHeadWeight = m_origHeadGazeWeight;
                m_currentBodyWeight = m_origBodyGazeWeight;
            }

            float delay = withDelay ? m_initialDelay : 0f;
            m_nextGazeTime = Time.time + delay;

            m_isThinking = true;
        }

        /// <summary>
        /// Stops character thinking nonverbal behavior.
        /// </summary>
        public void StopThinkingBehavior()
        {
            if (!m_isThinking)
                return;

            m_isThinking = false;
            if (m_character != null && m_gazeController != null)
                BeginGazeWeightBlend(false);
        }

        /// <summary>
        /// Loads character MecanimCharacter and GazeController.
        /// </summary>
        public virtual void InitializeLoadedAsset()
        {
            m_character = GetComponent<MecanimCharacter>();
            if (m_character != null)
            {
                m_gazeController = m_character.GetComponent<GazeController>();
                if (m_gazeController == null)
                    Debug.Log("Error: thinking controller: can't find GazeController");
            }
            else
                Debug.LogError("Error: thinking controller: can't find MecanimCharacter");
        }

        /// <summary>
        /// Resets loaded character assets.
        /// </summary>
        public virtual void ResetLoadedAsset()
        {
            StopThinkingBehavior();

            m_isThinking = false;
            m_previousThinkingState = false;
            m_isBlending = false;

            m_gazeController = null;
            m_character = null;
        }
    }
}
