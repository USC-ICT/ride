using UnityEngine;
using Ride;

namespace VHAssets
{
    /// <summary>
    /// Gaze controller implementation that uses Unity's built-in Animator IK
    /// to drive look-at behavior for a character.
    /// 
    /// This component bridges the abstract gaze logic in <see cref="GazeController"/>
    /// with an <see cref="Animator"/> configured for look-at:
    /// - Reads the current gaze target and weights from the base class
    /// - Applies them using <see cref="Animator.SetLookAtPosition(Vector3)"/> and
    ///   <see cref="Animator.SetLookAtWeight(float,float,float,float)"/>
    /// 
    /// This class assumes:
    /// - An <see cref="Animator"/> is present in the hierarchy
    /// - The Animator is configured to use IK for look-at
    /// 
    /// Gaze targeting, blending, and state management are handled by
    /// <see cref="GazeController"/>. This class is focused solely on
    /// integrating that data with the animation system via Unity's IK.
    /// </summary>
    public class GazeController_IK : GazeController
    {
        #region Fields
        [Header("Gaze Weights")]
        [SerializeField][Range(0f, 1f)] private float m_EyeGazeWeight = 1f;
        [SerializeField][Range(0f, 1f)] private float m_HeadGazeWeight = 0.5f;
        [SerializeField][Range(0f, 1f)] private float m_BodyGazeWeight = 0;

        [Header("Runtime (Debug / Visualization Only)")]
        [Tooltip(
            "These values show the *current, runtime-applied* gaze weights after fades and IK blending.\n" +
            "They are for debugging and visualization only.\n" +
            "Changing these in the inspector has no effect; they are overwritten every frame.\n" +
            "They may differ from the configured weights due to fades, angle thresholds, or solver behavior."
        )]
        [SerializeField][Range(0f, 1f)] private float m_CurrentEyeGazeWeight = 0;
        [SerializeField][Range(0f, 1f)] private float m_CurrentHeadGazeWeight = 0;
        [SerializeField][Range(0f, 1f)] private float m_CurrentBodyGazeWeight = 0;
        [SerializeField][Range(0f, 1f)] private float m_CurrentTotalGazeWeight = 0;

        private Animator m_Animator;
        #endregion

        #region Properties
        public override float EyeGazeWeight { get => m_EyeGazeWeight; set => m_EyeGazeWeight = value; }
        public override float HeadGazeWeight { get => m_HeadGazeWeight; set => m_HeadGazeWeight = value; }
        public override float BodyGazeWeight { get => m_BodyGazeWeight; set => m_BodyGazeWeight = value; }
        public override float CurrentEyeGazeWeight { get => m_CurrentEyeGazeWeight; set => m_CurrentEyeGazeWeight = value; }
        public override float CurrentHeadGazeWeight { get => m_CurrentHeadGazeWeight; set => m_CurrentHeadGazeWeight = value; }
        public override float CurrentBodyGazeWeight { get => m_CurrentBodyGazeWeight; set => m_CurrentBodyGazeWeight = value; }
        public override float CurrentTotalGazeWeight { get => m_CurrentTotalGazeWeight; set => m_CurrentTotalGazeWeight = value; }
        #endregion

        #region Functions
        void Start()
        {
            // initialize immediately only when there is NO ILoadableAsset.
            if (!TryGetComponent(out ILoadableAsset _))
                InitializeLoadedAsset();
        }

        void OnAnimatorIK(int layer)
        {
            UpdateGaze();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            DebugDrawRigGizmos();
        }

        /// <summary>
        /// Initializes the gaze system for a loaded asset, ensuring that
        /// an <see cref="Animator"/> is available and that an initial gaze
        /// target is configured if one has been assigned.
        /// 
        /// This method is typically called after an <see cref="ILoadableAsset"/>
        /// has finished loading, but can also be used for immediate initialization
        /// on non-loadable characters.
        /// </summary>
        public override void InitializeLoadedAsset()
        {
            base.InitializeLoadedAsset();

            EnsureAnimator();
            EnsureGazeOrigin();

            if (m_GazeTarget != null)
            {
                InitGaze(m_GazeTarget);
                SetGazeTargetWithSpeed(m_GazeTarget, GazeParts.All);
            }
        }

        public override void ResetLoadedAsset()
        {
            base.ResetLoadedAsset();

            // Clear cached reference; next InitializeLoadedAsset will re-find it.
            m_Animator = null;

            // Reset runtime weights (keeps inspector defaults intact).
            m_CurrentEyeGazeWeight = 0f;
            m_CurrentHeadGazeWeight = 0f;
            m_CurrentBodyGazeWeight = 0f;
            m_CurrentTotalGazeWeight = 0f;
        }

        /// <summary>
        /// Updates the Animator IK look-at position and weights based on the
        /// current gaze state managed by <see cref="GazeController"/>.
        /// 
        /// This method:
        /// - Uses <see cref="GazeController.GetGazePosition"/> as the look-at point
        /// - Maps the per-channel weights (body, head, eyes) and total weight into
        ///   <see cref="Animator.SetLookAtWeight(float,float,float,float)"/>
        /// 
        /// It performs no state changes itself; all fade timing and target
        /// management happens in the base class.
        /// </summary>
        public override void UpdateGaze()
        {
            if (m_Animator == null)
                EnsureAnimator();

            if (m_Animator == null)
                return;

            if (m_GazeTarget == null)
                return;

            // Position: where the character should look.
            m_Animator.SetLookAtPosition(GetGazePosition());

            // Weights: total controls overall IK strength; the remaining parameters
            // control how much of that strength is driven by body, head, and eyes.
            m_Animator.SetLookAtWeight(m_CurrentTotalGazeWeight, m_CurrentBodyGazeWeight, m_CurrentHeadGazeWeight, m_CurrentEyeGazeWeight);
        }

        /// <summary>
        /// Ensures that an <see cref="Animator"/> reference is available.
        /// 
        /// If no Animator has been cached yet, this method searches the
        /// current GameObject hierarchy using <see cref="Component.GetComponentInChildren{T}()"/>.
        /// If no Animator is found, an error is logged and gaze will not function.
        /// </summary>
        private void EnsureAnimator()
        {
            if (m_Animator != null)
                return;

            m_Animator = GetComponentInChildren<Animator>();
            if (m_Animator == null)
                Debug.LogError($"No animator found in hierarchy of {name}. Gaze won't work");
        }

        /// <summary>
        /// Ensures that a transform is assigned as the origin used for vertical
        /// gaze calculations (see <c>GetVerticalGaze()</c>).
        ///
        /// If the user has not explicitly assigned <c>m_GazeOrigin</c>, this method
        /// attempts to choose a sensible default from the humanoid rig:
        /// <list type="bullet">
        ///   <item><description>Head bone (preferred)</description></item>
        ///   <item><description>Left or right eye bone (if available)</description></item>
        ///   <item><description>The Animator's transform as a fallback</description></item>
        /// </list>
        ///
        /// This origin is only used to measure the up/down angle of the gaze
        /// (for eyelids and other "soft eye" effects). The IK look-at itself is
        /// handled entirely by the Animator, so assigning this field is optional.
        /// 
        /// For <see cref="GazeController_IK"/>, it is recommended to leave
        /// <c>m_GazeOrigin</c> unset-this method will automatically choose an
        /// appropriate bone from the Animator.
        /// </summary>
        private void EnsureGazeOrigin()
        {
            if (m_GazeOrigin != null)
                return;

            EnsureAnimator();
            if (m_Animator == null)
                return;

            var avatar = m_Animator.avatar;
            if (avatar != null && avatar.isValid && avatar.isHuman)
            {
                Transform head = m_Animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    m_GazeOrigin = head;
                    return;
                }

                Transform leftEye = m_Animator.GetBoneTransform(HumanBodyBones.LeftEye);
                if (leftEye != null)
                {
                    m_GazeOrigin = leftEye;
                    return;
                }

                Transform rightEye = m_Animator.GetBoneTransform(HumanBodyBones.RightEye);
                if (rightEye != null)
                {
                    m_GazeOrigin = rightEye;
                    return;
                }
            }

            // final fallback
            m_GazeOrigin = m_Animator.transform;
        }

        /// <summary>
        /// Initializes a new gaze request for the specified target using
        /// IK-specific behavior.
        /// 
        /// This override:
        /// - Ensures that an <see cref="Animator"/> is available
        /// - Delegates to the base implementation to configure gaze state,
        ///   fades, and positional transitions
        /// </summary>
        protected override void InitGaze(GameObject gazeTarget)
        {
            EnsureAnimator();

            base.InitGaze(gazeTarget);
        }

        private static readonly Color DebugColorEyes = new(0.2f, 1f, 0.2f); // green
        private static readonly Color DebugColorHead = new(1f, 0.8f, 0.1f); // yellow
        private static readonly Color DebugColorBody = new(0.2f, 0.6f, 1f); // blue

        private void DebugDrawRigGizmos()
        {
            if (!m_DebugDrawGaze)
                return;

            if (m_GazeTarget == null)
                return;

            if (m_Animator == null)
                return;

            Avatar avatar = m_Animator.avatar;
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                return;

            Vector3 origin = (m_GazeOrigin != null) ? m_GazeOrigin.position : transform.position;
            Vector3 rawTargetPos = m_GazeTarget.transform.position;
            Vector3 finalGazePos = GetGazePosition();

            Vector3 debugTargetPos = finalGazePos;

            Transform head = m_Animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftEye = m_Animator.GetBoneTransform(HumanBodyBones.LeftEye);
            Transform rightEye = m_Animator.GetBoneTransform(HumanBodyBones.RightEye);

            Transform chest = m_Animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform upperChest = m_Animator.GetBoneTransform(HumanBodyBones.UpperChest);
            Transform spine = m_Animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform body = upperChest != null ? upperChest : (chest != null ? chest : spine);

            DebugDrawBoneGizmos(leftEye, debugTargetPos, DebugColorEyes);
            DebugDrawBoneGizmos(rightEye, debugTargetPos, DebugColorEyes);
            DebugDrawBoneGizmos(head, debugTargetPos, DebugColorHead);
            DebugDrawBoneGizmos(body, debugTargetPos, DebugColorBody);
        }

        private static readonly Color DebugColorFwd  = new(1f, 0.3f, 0.3f); // red

        private void DebugDrawBoneGizmos(Transform bone, Vector3 targetPos, Color boneColor)
        {
            const float DebugRigRayLength = 0.25f;

            if (bone == null)
                return;

            Vector3 bonePos = bone.position;
            Vector3 toTarget = targetPos - bonePos;
            if (toTarget.sqrMagnitude < 0.000001f)
                return;

            Vector3 toTargetDir = toTarget.normalized;
            Vector3 boneForward = bone.forward;

            // Draw bone forward ray and bone->target ray
            var origColor = Gizmos.color;
            Gizmos.color = DebugColorFwd;
            Gizmos.DrawLine(bonePos, bonePos + boneForward * DebugRigRayLength);
            Gizmos.color = boneColor;
            Gizmos.DrawLine(bonePos, bonePos + toTargetDir * DebugRigRayLength);
            Gizmos.DrawWireSphere(bonePos, 0.01f);
            Gizmos.color = origColor;
        }
        #endregion
    }
}
