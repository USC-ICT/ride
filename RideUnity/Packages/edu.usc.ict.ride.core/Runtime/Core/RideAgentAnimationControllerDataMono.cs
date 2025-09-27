using UnityEngine;

namespace Ride.Animations
{
    [System.Serializable]
    public struct RideAgentAnimationControllerData
    {
        public RuntimeAnimatorController animatorController;
        public Avatar avatar;
        public bool applyRootMotion;
        public AnimatorUpdateMode updateMode;
        public AnimatorCullingMode cullingMode;

        public RideAgentAnimationControllerData(RuntimeAnimatorController animatorController = null, Avatar avatar = null, bool applyRootMotion = true, AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal, AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate)
        {
            this.animatorController = animatorController;
            this.avatar = avatar;
            this.applyRootMotion = applyRootMotion;
            this.updateMode = updateMode;
            this.cullingMode = cullingMode;
        }
    }

    public class RideAgentAnimationControllerDataMono : RideDataUnityBootstrap
    {
        public RideAgentAnimationControllerData data;

        public override object GetData()
        {
            return data;
        }
    }
}