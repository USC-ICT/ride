using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Unity component that continuously rotates a GameObject to face a target position each frame.
    /// Implements <see cref="IFaceTarget"/>.
    /// </summary>
    /// <remarks>
    /// If <c>m_facer</c> is not assigned, it defaults to this GameObject.
    /// If <c>m_target</c> is not assigned, it defaults to the main camera.
    /// </remarks>
    public class FaceTargetUnity : RideMonoBehaviour, IFaceTarget
    {
        [Tooltip("Transform that rotates to face the target. Defaults to this GameObject if not assigned.")]
        public GameObject m_facer;

        [Tooltip("Transform to face each frame. Defaults to Camera.main if not assigned.")]
        public GameObject m_target;

        [Tooltip("If enabled, reverses the forward direction after facing (180 rotation).")]
        public bool m_reverseForward = false;


        protected override void Start()
        {
            if (m_facer == null)
                m_facer = gameObject;

            if (m_target == null)
            {
                //Debug.LogWarningFormat("{0} has no facing target so it is being defaulted to the camera", name);
                m_target = Camera.main != null ? Camera.main.gameObject : null;
            }
        }

        protected override void Update()
        {
            if (m_target != null)
                FaceTarget(m_target.transform.position);
        }

        /// <summary>
        /// Rotates the assigned <c>m_facer</c> transform to face the given world-space position.
        /// If <c>m_reverseForward</c> is true, the forward direction is flipped after rotation.
        /// </summary>
        /// <param name="worldPosition">The world-space position to face.</param>
        public void FaceTarget(RideVector3 worldPosition)
        {
            if (m_facer != null && m_facer.transform != null)
                m_facer.transform.LookAt(worldPosition);

            if (m_reverseForward && m_facer != null)
            {
                Vector3 fwd = m_facer.transform.forward;
                m_facer.transform.forward = -fwd;
            }
        }
    }
}
