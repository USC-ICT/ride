using System.Collections;
using UnityEngine;
using VHAssets;

namespace Ride.Samples
{
    /// <summary>
    /// Handles GUI controls and systems for generating and controlling nonverbal behaviors (gaze, head, blink) 
    /// of a virtual human character in the RIDE.
    /// </summary>
    public class SamplesVHHead : RideMonoBehaviour
    {
        private DebugMenu m_debugMenu;
        private MecanimCharacter m_character;

        private bool m_useHead = true;
        private bool m_useEyes = true;
        private bool m_useBody = true;

        private float m_headWeight = 1f;
        private float m_eyeWeight = 1f;
        private float m_bodyWeight = 0.5f;

        private float m_headSpeed = 50f;
        private float m_eyeSpeed = 70f;
        private float m_bodySpeed = 20f;


        /// <summary>
        /// Initializes the debug menu, NVBG system, and character reference.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
            m_character = FindAnyObjectByType<MecanimCharacter>();
        }


        /// <summary>
        /// Draw debug menu for gaze, blink, and head animations.
        /// </summary>
        public void OnGUIHeadControl()
        {
            OnGuiGaze();
            OnGuiBlink();
            OnGuiHead();
        }


        /// <summary>
        /// Draw debug menu for gaze direction and speed.
        /// </summary>
        private void OnGuiGaze()
        {
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Parts:", 80);
                m_useHead = m_debugMenu.Toggle(m_useHead, "Head");
                m_useEyes = m_debugMenu.Toggle(m_useEyes, "Eyes");
                m_useBody = m_debugMenu.Toggle(m_useBody, "Body");
            }

            m_debugMenu.Space();

            m_debugMenu.Label("Gaze Weights (0..1):");
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Head", 60);
                m_headWeight = m_debugMenu.HorizontalSlider(m_headWeight, 0f, 1f);
                m_debugMenu.Label($"{m_headWeight:F2}", 50);
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Eyes", 50);
                m_eyeWeight = m_debugMenu.HorizontalSlider(m_eyeWeight, 0f, 1f);
                m_debugMenu.Label($"{m_eyeWeight:F2}", 50);
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Body", 50);
                m_bodyWeight = m_debugMenu.HorizontalSlider(m_bodyWeight, 0f, 1f);
                m_debugMenu.Label($"{m_bodyWeight:F2}", 50);
            }

            m_debugMenu.Space();

            m_debugMenu.Label("Fade-in Speeds:");
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Head", 60);
                m_headSpeed = m_debugMenu.HorizontalSlider(m_headSpeed, 0f, 100f);
                m_debugMenu.Label($"{m_headSpeed:F1}", 50);
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Eyes", 50);
                m_eyeSpeed = m_debugMenu.HorizontalSlider(m_eyeSpeed, 0f, 100f);
                m_debugMenu.Label($"{m_eyeSpeed:F1}", 50);
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Body", 50);
                m_bodySpeed = m_debugMenu.HorizontalSlider(m_bodySpeed, 0f, 100f);
                m_debugMenu.Label($"{m_bodySpeed:F1}", 50);
            }

            m_debugMenu.Space();

            m_debugMenu.Label("Gaze at (offset from camera):");
            using (m_debugMenu.Horizontal())
            {
                if (m_debugMenu.Button("Center")) { GazeAt("Center"); }
                if (m_debugMenu.Button("Up"))     { GazeAt("Up"); }
                if (m_debugMenu.Button("Down"))   { GazeAt("Down"); }
                if (m_debugMenu.Button("Left"))   { GazeAt("Left"); }
                if (m_debugMenu.Button("Right"))  { GazeAt("Right"); }
            }

            m_debugMenu.Space();

            if (m_debugMenu.Button("Off")) { m_character.StopGaze(); }
        }


        /// <summary>
        /// Draw debug menu for triggering a blink on the character.
        /// </summary>
        private void OnGuiBlink()
        {
            using (m_debugMenu.Horizontal()) //Todo: Investigate soft look
            {
                m_debugMenu.Label("Blink", 150);
                if (m_debugMenu.Button("Blink")) { m_character.GetComponent<BlinkController>().Blink(); }
            }
        }


        /// <summary>
        /// Draw debug menu for nodding and shaking the character's head.
        /// </summary>
        private void OnGuiHead()
        {
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Head Control", 150);
                if (m_debugMenu.Button("Nod"))
                {
                    float amount = 0.5f;
                    float numTimes = 2.0f;
                    float duration = 2.0f;
                    m_character.Nod(amount, numTimes, duration);
                }
                if (m_debugMenu.Button("Shake"))
                {
                    float amount = 0.5f;
                    float numTimes = 2.0f;
                    float duration = 1.0f;
                    m_character.Shake(amount, numTimes, duration);
                }
            }
        }


        /// <summary>
        /// Initiates gaze behavior toward a specified target direction.
        /// </summary>
        /// <param name="gazeTargetString">The name of the gaze target GameObject.</param>
        public void GazeAt(string gazeTargetString)
        {
            StartCoroutine(GazeSequence(m_character, gazeTargetString));
        }


        /// <summary>
        /// Coroutine that performs a timed sequence for gaze with necessary delays.
        /// </summary>
        /// <param name="character">The character performing the gaze.</param>
        /// <param name="gazeTargetString">The name of the gaze target GameObject.</param>
        /// <returns>IEnumerator used by Unity's coroutine system.</returns>
        private IEnumerator GazeSequence(MecanimCharacter character, string gazeTargetString)
        {
            var gazeTarget = GameObject.Find(gazeTargetString);

            // There is a known issue where gaze needs a two-frame delay after activation.
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Set gaze target with specified speed.
            if (gazeTarget == null)
                yield break;

            // First apply weights (participation).
            // If a part is toggled off, force its weight to 0.
            float headWeight = m_useHead ? m_headWeight : 0f;
            float eyeWeight  = m_useEyes ? m_eyeWeight  : 0f;
            float bodyWeight = m_useBody ? m_bodyWeight : 0f;

            character.SetGazeWeights(headWeight, eyeWeight, bodyWeight);

            // Then compute speeds. If a part is toggled off, speed 0 will cause fade-out.
            float headSpeed = m_useHead ? m_headSpeed : 0f;
            float eyeSpeed  = m_useEyes ? m_eyeSpeed  : 0f;
            float bodySpeed = m_useBody ? m_bodySpeed : 0f;

            character.SetGazeTargetWithSpeed(gazeTarget, headSpeed, eyeSpeed, bodySpeed);
        }
    }
}
