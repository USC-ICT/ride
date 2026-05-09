using UnityEngine;
using Ride;

namespace VHAssets
{
    public class EyelidController_Animator : EyelidController
    {
        [Header("Animator Settings")]
        [SerializeField]
        private Animator m_animator;

        [Tooltip("Animator float parameters used to control eyelid openness (0=open, 1=closed). " +
                 "Typically these match the AU 45 blink parameters, e.g. 045_blink_lf, 045_blink_rt.")]
        [SerializeField]
        private string[] m_eyelidParams = new string[] { "045_blink_lf", "045_blink_rt" };

        [Tooltip("Maximum eyelid value written to the Animator parameters. " +
                 "Final value = lidValue * this scale.")]
        [SerializeField]
        private float m_lidBlendMax = 1.0f;

        private int[] m_eyelidHashes;


        protected override void Start()
        {
            base.Start();

            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        protected override void ApplyLid(float lidValue)
        {
            if (m_animator == null || m_eyelidHashes == null)
                return;

            float v = Mathf.Clamp01(lidValue) * m_lidBlendMax;

            for (int i = 0; i < m_eyelidHashes.Length; i++)
                m_animator.SetFloat(m_eyelidHashes[i], v);
        }

        public override void InitializeLoadedAsset()
        {
            base.InitializeLoadedAsset();

            if (m_animator == null)
                m_animator = GetComponentInChildren<Animator>();

            if (m_eyelidParams == null)
                m_eyelidParams = new string[0];

            m_eyelidHashes = new int[m_eyelidParams.Length];
            for (int i = 0; i < m_eyelidParams.Length; i++)
                m_eyelidHashes[i] = Animator.StringToHash(m_eyelidParams[i]);
        }

        public override void ResetLoadedAsset()
        {
            base.ResetLoadedAsset();

            // Drop references into unloaded hierarchy.
            m_animator = null;
            m_eyelidHashes = null;
        }
    }
}
