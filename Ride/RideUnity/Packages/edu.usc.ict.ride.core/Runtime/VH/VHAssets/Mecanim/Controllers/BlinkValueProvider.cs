using System;
using UnityEngine;

namespace VHAssets
{
    /// <summary>
    /// Variant of BlinkController that updates blink timing and BlinkValue,
    /// but does not drive any Animator parameters or blend shapes.
    /// Intended for use with an external eyelid controller (e.g. EyelidController).
    /// </summary>
    public class BlinkValueProvider : BlinkController
    {
        protected void Awake()
        {
            // For this provider, we always want the internal state machine.
            // Forcing BlendTree mode disables the Animation-only behavior.
            if (m_BlinkMode == BlinkMode.Animation)
            {
                Debug.LogWarning("[BlinkValueProvider] BlinkMode was Animation, forcing BlendTree so BlinkValue can be computed.");
                m_BlinkMode = BlinkMode.BlendTree;
            }

            m_BlinkAnimName = "unused";
            m_EyeLidControllerParams = Array.Empty<string>();
            m_EyeLidBlendShapes = Array.Empty<string>();
            m_BlendShapeSkinnedMeshName = "unused";
        }

        /// <summary>
        /// Override to suppress any direct eyelid driving. The base class still
        /// updates blink state (m_blinkProgress, BlinkValue, scheduling, etc.).
        /// </summary>
        /// <param name="t">Blink factor in [0, 1].</param>
        protected override void ApplyBlinkWeights(float t)
        {
            // Intentionally empty: eyelids are driven elsewhere.
        }
    }
}
