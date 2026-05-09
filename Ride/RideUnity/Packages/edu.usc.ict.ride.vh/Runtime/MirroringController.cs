using System.Collections.Generic;
using UnityEngine;
using VHAssets;

namespace Ride
{
    /// <summary>
    /// Controls facial emotion mirroring for a character by driving FacialAnimationPlayer face shapes.
    ///
    /// This component maps high-level emotion names (e.g. "Happy", "Sadness", "Anger")
    /// to specific <see cref="FacialAnimationPlayer"/> face-shape keys and blends between
    /// them over time using the configured facial animation backend.
    ///
    /// The controller is designed to be safe for dynamic asset load/unload scenarios:
    /// - It does not cache references to loaded art beyond the FacialAnimationPlayer.
    /// - Runtime behavior is inert until <see cref="InitializeLoadedAsset"/> is called.
    /// - All active mirroring is stopped deterministically via <see cref="ResetLoadedAsset"/>.
    ///
    /// Initialization follows the standard RIDE loadable-asset pattern:
    /// - For non-loadable characters, initialization occurs automatically in <c>Start()</c>.
    /// - For loadable characters, initialization is deferred until
    ///   <c>InitializeLoadedAsset()</c> is invoked (typically via <c>RideCatalogAsset</c>).
    ///
    /// Emotion changes are blended smoothly using configurable blend-in and blend-out
    /// durations, and the controller always returns the character to a neutral expression
    /// when mirroring is stopped.
    /// </summary>
    public class MirroringController : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip(
            "Optional FacialAnimationPlayer used to drive facial emotion parameters. " +
            "If left unassigned, the component will try GetComponent on this GameObject; " +
            "if still missing, it will search children recursively. Logs an error if not found."
        )]
        [SerializeField] FacialAnimationPlayer m_facialAnimationPlayer;

        [Tooltip("Duration to blend into a new emotion.")]
        [SerializeField] float blendInTime = 0.5f;

        [Tooltip("Duration to blend out of the current emotion.")]
        [SerializeField] float blendOutTime = 0.5f;

        /// <summary>
        /// Name of the previously active emotion.
        /// </summary>
        [SerializeField] protected string m_prevEmotion = string.Empty;

        /// <summary>
        /// Name of the currently active emotion.
        /// </summary>
        [SerializeField] protected string m_currentEmotion = "Neutral";

        #endregion

        /// <summary>
        /// Maps emotion names to corresponding FacialAnimationPlayer face-shape names.
        /// </summary>
        readonly Dictionary<string, string> m_emotionParamMap = new Dictionary<string, string>
        {
            { "Happy", "_112_happy" },
            { "Anger", "_129_angry" },
            { "Surprise", "_127_surprise" },
            { "Contempt", "_131_contempt" },
            { "Disgust", "_124_disgust" },
            { "Fear", "_126_fear" },
            { "Sadness", "_130_sad" },
            { "Neutral", "face_neutral" }
        };

        readonly Dictionary<string, float> m_faceShapeWeights = new Dictionary<string, float>();

        string m_blendOutFaceShape = string.Empty;
        string m_blendInFaceShape = string.Empty;
        float m_blendOutStartValue;
        float m_blendInStartValue;
        float m_blendOutTargetValue;
        float m_blendInTargetValue;
        float m_blendElapsedTime;
        float m_blendDuration;
        bool m_isBlending;

        /// <summary>
        /// Initializes the controller and assigns the FacialAnimationPlayer reference if missing.
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
            if (!m_isBlending || m_facialAnimationPlayer == null)
                return;

            m_blendElapsedTime += Time.deltaTime;
            float t = m_blendDuration <= 0f ? 1f : Mathf.Clamp01(m_blendElapsedTime / m_blendDuration);

            if (!string.IsNullOrEmpty(m_blendOutFaceShape))
            {
                float blendOutValue = Mathf.Lerp(m_blendOutStartValue, m_blendOutTargetValue, t);
                SetFaceShape(m_blendOutFaceShape, blendOutValue);
            }

            if (!string.IsNullOrEmpty(m_blendInFaceShape))
            {
                float blendInValue = Mathf.Lerp(m_blendInStartValue, m_blendInTargetValue, t);
                SetFaceShape(m_blendInFaceShape, blendInValue);
            }

            if (t >= 1f)
                StopBlendState();
        }

        public virtual void InitializeLoadedAsset()
        {
            // Ensure references are valid after reload.
            if (m_facialAnimationPlayer == null)
                m_facialAnimationPlayer = GetComponent<FacialAnimationPlayer>();

            if (m_facialAnimationPlayer == null)
                m_facialAnimationPlayer = GetComponentInChildren<FacialAnimationPlayer>(true);

            if (m_facialAnimationPlayer == null)
                Debug.LogError($"{nameof(MirroringController)}: No FacialAnimationPlayer found. Assign one, or add a FacialAnimationPlayer component to this GameObject or its children.", this);

            StopBlendState();
            m_faceShapeWeights.Clear();
            m_facialAnimationPlayer.ResetExpressions();
            SetEmotionWeightImmediate("Neutral", 1f);

            // Force a known state on load.
            m_prevEmotion = string.Empty;
            m_currentEmotion = "Neutral";
        }

        public virtual void ResetLoadedAsset()
        {
            // Best-effort: stop mirroring and return to neutral before the art goes away.
            StopBlendState();

            if (m_facialAnimationPlayer != null)
            {
                m_facialAnimationPlayer.ResetExpressions();
                SetEmotionWeightImmediate("Neutral", 1f);
            }

            m_faceShapeWeights.Clear();
            m_prevEmotion = string.Empty;
            m_currentEmotion = "Neutral";

            // Drop references into any unloaded hierarchy (cheap to reacquire later).
            m_facialAnimationPlayer = null;
        }

        /// <summary>
        /// Changes the character's facial expression to mirror the given emotion.
        /// Blends out the previous emotion and blends into the new one.
        /// </summary>
        /// <param name="emotion">The name of the emotion to mirror.</param>
        public void MirrorEmotion(string emotion)
        {
            if (m_facialAnimationPlayer == null)
                return;

            if (emotion == m_currentEmotion)
                return;

            m_prevEmotion = m_currentEmotion;
            m_currentEmotion = emotion;

            string blendOutFaceShape = string.Empty;
            if (!string.IsNullOrEmpty(m_prevEmotion) && m_emotionParamMap.TryGetValue(m_prevEmotion, out string prevFaceShape))
                blendOutFaceShape = prevFaceShape;

            string blendInFaceShape = string.Empty;
            if (m_emotionParamMap.TryGetValue(emotion, out string nextFaceShape))
                blendInFaceShape = nextFaceShape;

            if (string.IsNullOrEmpty(blendOutFaceShape) && string.IsNullOrEmpty(blendInFaceShape))
                return;

            StartBlend(blendInFaceShape, string.IsNullOrEmpty(blendInFaceShape) ? 0f : 1f, blendOutFaceShape, string.IsNullOrEmpty(blendOutFaceShape) ? 0f : 0f, Mathf.Max(blendInTime, blendOutTime));
        }

        /// <summary>
        /// Stops any currently mirrored emotion and returns the character to a neutral expression.
        /// </summary>
        public void StopMirroring()
        {
            if (m_facialAnimationPlayer == null)
            {
                m_prevEmotion = string.Empty;
                m_currentEmotion = "Neutral";
                return;
            }

            if (m_currentEmotion != string.Empty && m_currentEmotion != "Neutral")
            {
                StartBlend(m_emotionParamMap["Neutral"], 1f, m_emotionParamMap[m_currentEmotion], 0f, Mathf.Max(blendOutTime, blendInTime));
            }
            else
            {
                StopBlendState();
                SetEmotionWeightImmediate("Neutral", 1f);
            }

            m_prevEmotion = string.Empty;
            m_currentEmotion = "Neutral";
        }

        void StartBlend(string blendInFaceShape, float blendInTargetValue, string blendOutFaceShape, float blendOutTargetValue, float blendTime)
        {
            if (blendTime <= 0f)
            {
                StopBlendState();

                if (!string.IsNullOrEmpty(blendOutFaceShape))
                    SetFaceShape(blendOutFaceShape, blendOutTargetValue);

                if (!string.IsNullOrEmpty(blendInFaceShape))
                    SetFaceShape(blendInFaceShape, blendInTargetValue);

                return;
            }

            m_blendInFaceShape = blendInFaceShape;
            m_blendInStartValue = string.IsNullOrEmpty(blendInFaceShape) ? 0f : GetCurrentFaceShapeValue(blendInFaceShape);
            m_blendInTargetValue = blendInTargetValue;

            m_blendOutFaceShape = blendOutFaceShape;
            m_blendOutStartValue = string.IsNullOrEmpty(blendOutFaceShape) ? 0f : GetCurrentFaceShapeValue(blendOutFaceShape);
            m_blendOutTargetValue = blendOutTargetValue;

            m_blendElapsedTime = 0f;
            m_blendDuration = blendTime;
            m_isBlending = true;
        }

        float GetCurrentFaceShapeValue(string emotion)
        {
            string faceShape = emotion;
            if (m_emotionParamMap.TryGetValue(emotion, out string mappedFaceShape))
                faceShape = mappedFaceShape;

            return m_faceShapeWeights.TryGetValue(faceShape, out float currentValue) ? currentValue : 0f;
        }

        void SetEmotionWeightImmediate(string emotion, float value)
        {
            if (!m_emotionParamMap.TryGetValue(emotion, out string faceShape))
                return;

            SetFaceShape(faceShape, value);
        }

        void SetFaceShape(string faceShape, float value)
        {
            if (m_facialAnimationPlayer == null)
                return;

            float clampedValue = Mathf.Clamp01(value);
            m_faceShapeWeights[faceShape] = clampedValue;
            ApplyFaceShape(faceShape, clampedValue);
        }

        protected virtual void ApplyFaceShape(string faceShape, float value)
        {
            m_facialAnimationPlayer.RampViseme(faceShape, value, 0f, 0f, 0f);
        }

        void StopBlendState()
        {
            m_blendOutFaceShape = string.Empty;
            m_blendInFaceShape = string.Empty;
            m_blendOutStartValue = 0f;
            m_blendInStartValue = 0f;
            m_blendOutTargetValue = 0f;
            m_blendInTargetValue = 0f;
            m_blendElapsedTime = 0f;
            m_blendDuration = 0f;
            m_isBlending = false;
        }
    }
}
