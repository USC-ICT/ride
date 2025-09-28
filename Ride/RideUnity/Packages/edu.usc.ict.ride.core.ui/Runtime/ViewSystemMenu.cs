using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    public class ViewSystemMenu : MenuMono, IViewSystemMenu
    {
        public RideTextTMPro m_userInfoText;

        UnityEngine.Coroutine m_displayUserMessageCoroutine = null;

        protected override void Start()
        {
            m_userInfoText.text = "";
            m_userInfoText.color = RideColor.clear;
        }

        /// <summary>
        /// Displays a user message on the screen.
        /// </summary>
        /// <param name="text">The text to display.  All other parameters use defaults.</param>
        public void DisplayUserMessage(string text) { DisplayUserMessage(text, RideColor.white); }

        /// <summary>
        /// Displays a user message on the screen.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="color">The color of the text to display.</param>
        /// <param name="displayTime">How long to display the text in seconds</param>
        /// <param name="fadeInTime">How long to alpha fade in the text in seconds</param>
        /// <param name="fadeOutTime">How long to alpha fade out the text in seconds</param>
        public void DisplayUserMessage(string text, RideColor color, float displayTime = 1, float fadeInTime = 0.1f, float fadeOutTime = 0.2f)
        {
            if (m_displayUserMessageCoroutine != null)
            {
                StopCoroutine(m_displayUserMessageCoroutine);
                m_displayUserMessageCoroutine = null;
            }

            // make sure fade in/out are within displayTime
            fadeOutTime = System.Math.Min(displayTime, fadeOutTime);
            fadeInTime = System.Math.Min(displayTime - fadeOutTime, fadeInTime);

            m_displayUserMessageCoroutine = StartCoroutine(DisplayUserMessageInternal(text, displayTime, color, fadeInTime, fadeOutTime));
        }

        IEnumerator DisplayUserMessageInternal(string text, float displayTime, RideColor color, float fadeInTime, float fadeOutTime)
        {
            m_userInfoText.text = text;
            m_userInfoText.color = new RideColor(color.r, color.g, color.b, m_userInfoText.color.a);

            {
                // fade in
                float fadeInStartTime = UnityEngine.Time.time;
                RideColor fadeInStartColor = m_userInfoText.color;
                RideColor fadeInEndColor = color;
                while ((UnityEngine.Time.time - fadeInStartTime) < fadeInTime)
                {
                    m_userInfoText.color = RideColor.Lerp(fadeInStartColor, fadeInEndColor, (UnityEngine.Time.time - fadeInStartTime) / fadeInTime);
                    yield return new UnityEngine.WaitForEndOfFrame();
                }
                m_userInfoText.color = RideColor.Lerp(fadeInStartColor, fadeInEndColor, 1);
            }

            yield return new UnityEngine.WaitForSeconds(System.Math.Max(displayTime - fadeInTime - fadeOutTime, 0));

            {
                // fade out
                float fadeOutStartTime = UnityEngine.Time.time;
                RideColor fadeOutStartColor = m_userInfoText.color;
                RideColor fadeOutEndColor = new RideColor(m_userInfoText.color.r, m_userInfoText.color.g, m_userInfoText.color.b, 0);
                while ((UnityEngine.Time.time - fadeOutStartTime) < fadeOutTime)
                {
                    m_userInfoText.color = RideColor.Lerp(fadeOutStartColor, fadeOutEndColor, (UnityEngine.Time.time - fadeOutStartTime) / fadeOutTime);
                    yield return new UnityEngine.WaitForEndOfFrame();
                }
                m_userInfoText.color = RideColor.Lerp(fadeOutStartColor, fadeOutEndColor, 1);
            }

            m_userInfoText.text = "";
            m_userInfoText.color = RideColor.clear;
        }
    }
}
