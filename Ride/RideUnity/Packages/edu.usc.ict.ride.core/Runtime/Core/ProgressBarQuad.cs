using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Uses the Quad mesh renderer to represent a health bar.
    /// </summary>
    public class ProgressBarQuad : MonoBehaviour, IProgressDisplay
    {
        /// <summary>
        /// The background of the health bar. Its size remains constant
        /// </summary>
        [Tooltip("The background of the health bar. Its size remains constant")]
        public GameObject m_bg;

        /// <summary>
        /// The foreground of the health bar. Its size changes based on health
        /// </summary>
        [Tooltip("The foreground of the health bar. Its size changes based on health")]
        public GameObject m_fg;

        public void SetHealth(float curr, float max)
        {
            if (max == 0) return;
            Vector3 scale = m_fg.transform.localScale;
            scale.x = Mathf.Lerp(0, m_bg.transform.localScale.x, Mathf.Clamp01(curr / max));
            m_fg.transform.localScale = scale;
        }

        public void SetVisible(bool visible)
        {
            if (enabled)
                gameObject.SetActive(visible);
        }

        public void SetEnable(bool enable)
        {
            enabled = enable;
        }
    }
}
