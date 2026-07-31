using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Dynamically adjusts the movement speeds of a <see cref="FreeFlyController"/> based on the current height above terrain.
    /// This allows free-fly navigation to feel responsive at low altitudes and faster at high altitudes,
    /// making large simulation environments easier to traverse.
    /// </summary>
    /// <remarks>
    /// Speed is interpolated between <c>minSpeed</c> and <c>maxSpeed</c> over the <c>minHeight</c> to <c>maxHeight</c> range.
    /// The adjustment is applied to both primary and secondary movement speeds. If terrain height is unavailable,
    /// default speed values are used instead.
    /// 
    /// This component assumes that a <see cref="FreeFlyController"/> is attached to the same GameObject.
    /// </remarks>
    [RequireComponent(typeof(FreeFlyController))]
    class FreeFlyTerrainSpeedScaler : RideMonoBehaviour
    {
        [Header("Default Speeds")]
        public float m_defaultSpeed1 = 100;
        public float m_defaultSpeed2 = 20;

        [Header("Minimum Speeds")]
        public float m_minSpeed1 = 100;
        public float m_minSpeed2 = 20;

        [Header("Maximum Speeds")]
        public float m_maxSpeed1 = 1000;
        public float m_maxSpeed2 = 200;

        [Header("Height Range")]
        public float m_minHeight = 0;
        public float m_maxHeight = 1000;

        FreeFlyController m_freeFlyController;

        /// <summary>
        /// Initializes the speed scaler by retrieving the attached <see cref="FreeFlyController"/> component.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_freeFlyController = GetComponent<FreeFlyController>();
        }

        /// <summary>
        /// Polls terrain height every frame and updates the <see cref="FreeFlyController"/>'s movement speeds
        /// based on the current height above terrain. If no terrain is detected (i.e., height equals <c>float.MaxValue</c>),
        /// the controller falls back to the configured default speeds.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            float height = Systems.Terrain.GetHeightAboveTerrain(transform.position);

            float speed1 = m_defaultSpeed1;
            float speed2 = m_defaultSpeed2;
            if (height != float.MaxValue)
            {
                speed1 = ComputeSpeed(height, m_minSpeed1, m_maxSpeed1, m_minHeight, m_maxHeight);
                speed2 = ComputeSpeed(height, m_minSpeed2, m_maxSpeed2, m_minHeight, m_maxHeight);
            }

            m_freeFlyController.movementSpeed = speed1;
            m_freeFlyController.secondaryMovementSpeed = speed2;
        }

        /// <summary>
        /// Computes a speed value between <paramref name="minSpeed"/> and <paramref name="maxSpeed"/>
        /// based on a given height and the configured interpolation range.
        /// </summary>
        /// <param name="height">The current height above terrain.</param>
        /// <param name="minSpeed">The minimum possible speed when at or below <paramref name="minHeight"/>.</param>
        /// <param name="maxSpeed">The maximum possible speed when at or above <paramref name="maxHeight"/>.</param>
        /// <param name="minHeight">The lower bound of the interpolation range.</param>
        /// <param name="maxHeight">The upper bound of the interpolation range.</param>
        /// <returns>The calculated speed at the given height using a cubic easing function.</returns>
        static float ComputeSpeed(float height, float minSpeed, float maxSpeed, float minHeight, float maxHeight)
        {
            if (height < minHeight)
                return minSpeed;
            if (height > maxHeight)
                return maxSpeed;

            float t = Mathf.InverseLerp(minHeight, maxHeight, height);
            return CubicEase(minSpeed, maxSpeed, t);
        }

        /// <summary>
        /// Applies a cubic ease-in interpolation between two values. This produces smooth acceleration between speeds,
        /// useful for natural-feeling motion transitions.
        /// </summary>
        /// <param name="from">The starting value.</param>
        /// <param name="to">The ending value.</param>
        /// <param name="t">The normalized interpolation value (0 to 1).</param>
        /// <returns>The eased value between <paramref name="from"/> and <paramref name="to"/>.</returns>
        /// <remarks>
        /// Formula: <c>result = (to - from) * t^3 + from</c>
        /// </remarks>
        static float CubicEase(float from, float to, float t)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.animation.cubicease?redirectedfrom=MSDN&view=netframework-4.8
            // https://github.com/lordofduct/spacepuppy-unity-framework/blob/master/SpacepuppyBase/Tween/Easing.cs
            float b = from;
            float c = to - from;
            return c * t * t * t + b;
        }
    }
}
