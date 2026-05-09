using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Provides lightweight on-screen messaging for the Ride view system by displaying
    /// temporary text with configurable fade-in and fade-out behavior.
    /// </summary>
    public class ViewSystemMenu : MenuUnity, IViewSystemMenu
    {
        [Tooltip("Text element used by the view system to show temporary on-screen user messages.")]
        public RideTextTMPro m_userInfoText;

        Coroutine m_displayUserMessageCoroutine = null;


        /// <summary>Initializes the user-message text element to an empty, fully transparent state.</summary>
        protected override void Start()
        {
            base.Start();

            m_userInfoText.text = "";
            m_userInfoText.color = RideColor.clear;
        }

        /// <summary>
        /// Displays a user message on the screen using the default message color and timing values.
        /// </summary>
        /// <param name="text">The text to display. All other parameters use defaults.</param>
        public void DisplayUserMessage(string text) => DisplayUserMessage(text, RideColor.white);

        /// <summary>
        /// Displays a user message on the screen using the supplied color and timing settings.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="color">The color of the text to display.</param>
        /// <param name="displayTime">How long to display the text in seconds.</param>
        /// <param name="fadeInTime">How long to alpha fade in the text in seconds.</param>
        /// <param name="fadeOutTime">How long to alpha fade out the text in seconds.</param>
        public void DisplayUserMessage(string text, RideColor color, float displayTime = 1, float fadeInTime = 0.1f, float fadeOutTime = 0.2f)
        {
            if (m_displayUserMessageCoroutine != null)
            {
                StopCoroutine(m_displayUserMessageCoroutine);
                m_displayUserMessageCoroutine = null;
            }

            // make sure fade in/out are within displayTime
            fadeOutTime = Math.Min(displayTime, fadeOutTime);
            fadeInTime = Math.Min(displayTime - fadeOutTime, fadeInTime);

            m_displayUserMessageCoroutine = StartCoroutine(DisplayUserMessageInternal(text, displayTime, color, fadeInTime, fadeOutTime));
        }

        /// <summary>
        /// Displays a message, fades it in, waits for the requested duration, then fades it back out.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="displayTime">The total time the message should remain scheduled on screen.</param>
        /// <param name="color">The target text color while fully visible.</param>
        /// <param name="fadeInTime">The duration of the fade-in portion.</param>
        /// <param name="fadeOutTime">The duration of the fade-out portion.</param>
        /// <returns>An enumerator for the message display coroutine.</returns>
        IEnumerator DisplayUserMessageInternal(string text, float displayTime, RideColor color, float fadeInTime, float fadeOutTime)
        {
            m_userInfoText.text = text;
            m_userInfoText.color = new RideColor(color.r, color.g, color.b, m_userInfoText.color.a);

            {
                // fade in
                float fadeInStartTime = Time.time;
                RideColor fadeInStartColor = m_userInfoText.color;
                RideColor fadeInEndColor = color;
                while ((Time.time - fadeInStartTime) < fadeInTime)
                {
                    m_userInfoText.color = RideColor.Lerp(fadeInStartColor, fadeInEndColor, (Time.time - fadeInStartTime) / fadeInTime);
                    yield return new WaitForEndOfFrame();
                }
                m_userInfoText.color = RideColor.Lerp(fadeInStartColor, fadeInEndColor, 1);
            }

            yield return new WaitForSeconds(Math.Max(displayTime - fadeInTime - fadeOutTime, 0));

            {
                // fade out
                float fadeOutStartTime = Time.time;
                RideColor fadeOutStartColor = m_userInfoText.color;
                RideColor fadeOutEndColor = new RideColor(m_userInfoText.color.r, m_userInfoText.color.g, m_userInfoText.color.b, 0);
                while ((Time.time - fadeOutStartTime) < fadeOutTime)
                {
                    m_userInfoText.color = RideColor.Lerp(fadeOutStartColor, fadeOutEndColor, (Time.time - fadeOutStartTime) / fadeOutTime);
                    yield return new WaitForEndOfFrame();
                }
                m_userInfoText.color = RideColor.Lerp(fadeOutStartColor, fadeOutEndColor, 1);
            }

            m_userInfoText.text = "";
            m_userInfoText.color = RideColor.clear;
        }
    }
}
