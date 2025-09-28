using UnityEngine;
using VHAssets;
using System.Collections;


namespace Ride.Samples
{
    /// <summary>
    /// Handles GUI controls and systems for generating and controlling nonverbal behaviors (gaze, head, blink) 
    /// of a virtual human character in the RIDE.
    /// </summary>
    public class SamplesVHNonverbalBehaviorGeneratorSystem : RideMonoBehaviour
    {
        private DebugMenu m_debugMenu;
        private NonverbalBehaviorGeneratorSystem m_nvbg;
        private MecanimCharacter m_character;
        private string m_utterance = "Hello world";
        private string m_nvbgResult;
        private float m_gazeHeadSpeed = 50f;


        /// <summary>
        /// Initializes the debug menu, NVBG system, and character reference.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_nvbg = Globals.api.GetSystem<NonverbalBehaviorGeneratorSystem>();
            m_character = FindAnyObjectByType<MecanimCharacter>();
            m_nvbg.StartProcess(m_character.CharacterName);
        }


        /// <summary>
        /// Draw debug for entering an utterance and generating nonverbal behavior.
        /// </summary>
        public void OnGUINonverbalBehaviorGeneration()
        {
            m_debugMenu.Label("<b>NVBG</b>");
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Utterance", 150);
                m_utterance = m_debugMenu.TextField(m_utterance);
            }
            if (m_debugMenu.Button("Generate"))
            {
                m_nvbg.GetNonverbalBehavior(m_character.CharacterName, m_utterance, OnNvbgGenerated);
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Result", 150);
                m_debugMenu.TextArea(m_nvbgResult);
            }
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
                m_debugMenu.Label("Speed", 150);
                m_gazeHeadSpeed = m_debugMenu.HorizontalSlider(m_gazeHeadSpeed, 10, 100);
                m_debugMenu.Label($"{m_gazeHeadSpeed:f1}", 80);
            }

            using (m_debugMenu.Horizontal())
            {
                if (m_debugMenu.Button("Up"))     { GazeAt("Up"); }
                if (m_debugMenu.Button("Down"))   { GazeAt("Down"); }
                if (m_debugMenu.Button("Left"))   { GazeAt("Left"); }
                if (m_debugMenu.Button("Right"))  { GazeAt("Right"); }
                if (m_debugMenu.Button("Center")) { GazeAt("Center"); }
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
        /// Callback function invoked when the nonverbal behavior generation result is received.
        /// </summary>
        /// <param name="result">The resulting nonverbal behavior string.</param>
        private void OnNvbgGenerated(string result)
        {
            m_nvbgResult = result;
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
            character.SetGazeTargetWithSpeed(gazeTarget, m_gazeHeadSpeed, m_gazeHeadSpeed, m_gazeHeadSpeed);
        }
    }
}
