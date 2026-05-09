using UnityEngine;

namespace VHAssets
{
    public abstract class EyelidController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip(
            "Optional. If set, this component will be used as the Blink controller. " +
            "If empty, EyelidController will try GetComponent on this GameObject; " +
            "if still missing, it will search children recursively. Logs an error if not found.")]
        [SerializeField]
        private BlinkController m_blinkController;

        [Tooltip(
            "Optional. If set, this component will be used as the Gaze provider. " +
            "If empty, EyelidController will try GetComponent on this GameObject; " +
            "if still missing, it will search children recursively. Logs an error if not found.")]
        [SerializeField]
        private GazeController m_gazeProvider;

        [Header("Soft Eyes (Gaze-Eyelid Coupling)")]
        [SerializeField]
        private bool m_enableSoftEyes = true;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("lid closure at gaze = -1")]
        private float m_downwardLidAmount = 0.35f;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("lid closure at gaze = 0")]
        private float m_straightLidAmount = 0.05f;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("lid closure at gaze = +1")]
        private float m_upwardLidAmount = 0.00f;

        [Header("Blink Influence")]
        [SerializeField]
        private bool m_enableBlink = true;

        [Tooltip("Scales how strongly the blink closes the eyelids. 1 = full close, 0.5 = half as strong.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float m_blinkStrength = 1f;

        [Tooltip("Optional smoothing time (seconds) for the final lid value. 0 = no smoothing.")]
        [SerializeField]
        private float m_smoothingTime = 0.02f;

        private bool m_isLoadedAssetInitialized = true;
        private float m_currentLidValue = 0f;
        private float m_lidVelocity = 0f; // used by SmoothDamp


        protected virtual void Start()
        {
            // Blink controller resolution (inspector -> self -> children -> error)
            if (m_blinkController == null)
                m_blinkController = GetComponent<BlinkController>();

            if (m_blinkController == null)
                m_blinkController = GetComponentInChildren<BlinkController>(true);

            if (m_blinkController == null)
                Debug.LogError($"{nameof(EyeController)}: No BlinkController controller found. Assign one, or add a BlinkController to this GameObject or its children.", this);

            // Gaze provider resolution (inspector -> self -> children -> error)
            if (m_gazeProvider == null)
                m_gazeProvider = GetComponent<GazeController>();

            if (m_gazeProvider == null)
                m_gazeProvider = GetComponentInChildren<GazeController>(true);

            if (m_gazeProvider == null)
                Debug.LogError($"{nameof(EyeController)}: No GazeController found. Assign one, or add a GazeController to this GameObject or its children.", this);
        }

        protected virtual void Update()
        {
            if (!m_isLoadedAssetInitialized)
                return;

            float baseLid = ComputeBaseLidFromGaze();
            float blinkLid = ComputeBlinkContribution();

            // Combine base (soft eyes) and blink:
            // blinkLid is interpreted as "how far toward fully closed we move from base".
            float targetLid = Mathf.Lerp(baseLid, 1f, blinkLid);

            // Optional smoothing so lids do not snap.
            if (m_smoothingTime > 0f)
                m_currentLidValue = Mathf.SmoothDamp(m_currentLidValue, targetLid, ref m_lidVelocity, m_smoothingTime);
            else
                m_currentLidValue = targetLid;

            m_currentLidValue = Mathf.Clamp01(m_currentLidValue);

            //Debug.Log($"Lid: {m_currentLidValue}");

            // Let the concrete implementation push this to Animator / blendshapes / bones.
            ApplyLid(m_currentLidValue);
        }

        public virtual void InitializeLoadedAsset()
        {
            m_isLoadedAssetInitialized = true;
        }

        public virtual void ResetLoadedAsset()
        {
            // Stop driving until the loaded art is ready again.
            m_isLoadedAssetInitialized = false;

            // Reset smoothing state so reload starts clean.
            m_currentLidValue = 0f;
            m_lidVelocity = 0f;

            ApplyLid(0f);
        }

        /// <summary>
        /// Called every frame with the final eyelid closure value in [0, 1].
        /// 0 = fully open, 1 = fully closed.
        /// Concrete subclasses should use this to drive Animator parameters,
        /// blendshapes, or bones.
        /// </summary>
        /// <param name="lidValue">Final eyelid closure value.</param>
        protected abstract void ApplyLid(float lidValue);

        /// <summary>
        /// Computes the base lid closure from vertical gaze ("soft eyes").
        /// </summary>
        private float ComputeBaseLidFromGaze()
        {
            if (!m_enableSoftEyes || m_gazeProvider == null)
                return 0f;

            float gazeNorm = Mathf.Clamp(m_gazeProvider.GetVerticalGaze(), -1f, 1f);

            float lid;
            if (gazeNorm <= 0f)
            {
                // Blend from "down" (-1) to "straight" (0).
                // Map [-1, 0] -> [0, 1]
                float t = gazeNorm + 1f; // -1 -> 0, 0 -> 1
                lid = Mathf.Lerp(m_downwardLidAmount, m_straightLidAmount, t);
            }
            else
            {
                // Blend from "straight" (0) to "up" (1).
                // Map [0, 1] -> [0, 1]
                float t = gazeNorm; // 0 -> 0, 1 -> 1
                lid = Mathf.Lerp(m_straightLidAmount, m_upwardLidAmount, t);
            }

            //Debug.Log($"Gaze: {m_gazeProvider.GetVerticalGaze()}, Norm: {gazeNorm}, lid: {lid}");

            return Mathf.Clamp01(lid);
        }

        /// <summary>
        /// Computes the blink contribution as a value in [0, 1].
        /// 0 = no blink, 1 = fully closed by blink alone.
        /// </summary>
        private float ComputeBlinkContribution()
        {
            if (!m_enableBlink || m_blinkController == null)
                return 0f;

            // Assumes BlinkController exposes a normalized blink value.
            // 0 = fully open, 1 = fully closed.
            float blinkValue = Mathf.Clamp01(m_blinkController.BlinkValue);

            blinkValue *= m_blinkStrength;

            return Mathf.Clamp01(blinkValue);
        }
    }
}
