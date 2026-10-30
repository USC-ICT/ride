using UnityEngine;

namespace Ride.Animations
{
    /// <summary>Defines the Unity Animator configuration applied when a RIDE agent animation controller is created.</summary>
    /// <remarks><c>AnimationSystemUnity</c> reads this value from converted entity data.</remarks>
    [System.Serializable]
    public struct RideAgentAnimationControllerData
    {
        /// <summary>The runtime Animator controller assigned to the agent.</summary>
        public RuntimeAnimatorController animatorController;

        /// <summary>The Avatar used to map the controller onto the agent hierarchy.</summary>
        public Avatar avatar;

        /// <summary>Whether movement and rotation are controlled by animation root motion.</summary>
        public bool applyRootMotion;

        /// <summary>The clock used to update the Animator.</summary>
        public AnimatorUpdateMode updateMode;

        /// <summary>The policy controlling animation evaluation when renderers are culled.</summary>
        public AnimatorCullingMode cullingMode;

        /// <summary>Initializes an Animator configuration for an agent.</summary>
        /// <param name="animatorController">The runtime Animator controller.</param>
        /// <param name="avatar">The Avatar mapped to the agent.</param>
        /// <param name="applyRootMotion">Whether to apply animation root motion.</param>
        /// <param name="updateMode">The Animator update clock.</param>
        /// <param name="cullingMode">The Animator culling policy.</param>
        public RideAgentAnimationControllerData(RuntimeAnimatorController animatorController = null, Avatar avatar = null, bool applyRootMotion = true, AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal, AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate)
        {
            this.animatorController = animatorController;
            this.avatar = avatar;
            this.applyRootMotion = applyRootMotion;
            this.updateMode = updateMode;
            this.cullingMode = cullingMode;
        }
    }

    /// <summary>Exposes serialized <see cref="RideAgentAnimationControllerData"/> to RIDE's Unity conversion pipeline.</summary>
    public class RideAgentAnimationControllerDataMono : RideDataUnityBootstrap
    {
        /// <summary>The Animator configuration returned during entity conversion.</summary>
        public RideAgentAnimationControllerData data;

        /// <inheritdoc/>
        public override object GetData()
        {
            return data;
        }
    }
}
