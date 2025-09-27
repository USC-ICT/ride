using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;


namespace Ride.Samples
{

    /// <summary>
    /// Handles GUI controls and systems for generating and controlling nonverbal behaviors (gaze, head, blink) 
    /// of a virtual human character in the RIDE.
    /// </summary>
    public class SamplesVHSIMA : RideMonoBehaviour
    {
        private DebugMenu m_debugMenu;
        //private NonverbalBehaviorGeneratorSystem m_nvbg;
        public SIMA m_sima;
        private MecanimCharacter m_character;
        private string m_utterance;
        private string m_simaResult;
        private float m_gazeHeadSpeed = 50f;


        /// <summary>
        /// Initializes the debug menu, NVBG system, and character reference.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            //m_nvbg = Globals.api.GetSystem<NonverbalBehaviorGeneratorSystem>();
            m_character = FindAnyObjectByType<MecanimCharacter>();
            //m_nvbg.StartProcess(m_character.CharacterName);
        }


        /// <summary>
        /// Draw debug for entering an utterance and generating nonverbal behavior.
        /// </summary>
        public void OnGUINonverbalBehaviorGeneration()
        {
            m_debugMenu.Label("<b>Sima</b>");
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Utterance", 150);
                m_utterance = m_debugMenu.TextField(m_utterance);
            }

            if (m_debugMenu.Button("Hi my name"))
            {
                m_sima.GetBehavior("Hi my name is Kevin", OnSimaGenerated);
            }

            if (m_debugMenu.Button("great idea"))
            {
                m_sima.GetBehavior("this is a great idea", OnSimaGenerated);
            }

            if (m_debugMenu.Button("Generate"))
            {
                //m_nvbg.GetNonverbalBehavior(m_character.CharacterName, m_utterance, OnNvbgGenerated);
                m_sima.GetBehavior(m_utterance, OnSimaGenerated);
            }

            if (m_debugMenu.Button("Play"))
            {
                string xmlPlayTest = "<act><participant id = \"ChrKevin\" role = \"actor\" /><bml><speech id = \"sp1\" ref= \"unused\" type = \"application/ssml+xml\" >< mark name = \"T0\" /> this < mark name = \"T1\" />< mark name = \"T2\" />is < mark name = \"T3\" />< mark name = \"T4\" /> a < mark name = \"T5\" />< mark name = \"T6\" /> great < mark name = \"T7\" />< mark name = \"T8\" /> idea < mark name = \"T9\" /></ speech ><event message=\"vrAgentSpeech partial 1488584035542-92-1 T1 this\" stroke=\"sp1:T1\" /><event message=\"vrAgentSpeech partial 1488584035542-92-1 T3 this is\" stroke=\"sp1:T3\" /><event message=\"vrAgentSpeech partial 1488584035542-92-1 T5 this is a\" stroke=\"sp1:T5\" /><event message=\"vrAgentSpeech partial 1488584035542-92-1 T7 this is a great\" stroke=\"sp1:T7\" /><event message=\"vrAgentSpeech partial 1488584035542-92-1 T9 this is a great idea\" stroke=\"sp1:T9\" /><gaze participant=\"ChrKevin\" target=\"all\" direction=\"POLAR 0\" angle=\"0\" start=\"sp1:T0\" joint-range=\"HEAD EYES\" xmlns:sbm=\"http://ict.usc.edu\" /><event message=\"vrSpoke ChrKevin all 1488584035542-92-1 this is a great idea\" stroke=\"sp1:relax\" xmlns:sbm=\"http://ict.usc.edu\" /><animation name=\"IdleStandingUpright01_ChopLf01\" stroke=\"sp1:T6\" /><head type=\"NOD\" amount=\"0.1\" repeats=\"0.5\" relax=\"sp1:T8\" /><face type=\"facs\" au=\"6\" side=\"BOTH\" relax=\"sp1:T8\" /></bml></act>";
                string xmlPlayTest2 = xmlPlayTest.Replace("< ", "<");
                string xmlPlayTest3 = xmlPlayTest2.Replace(" />", "/>");
                m_character.PlayXml(m_simaResult);
                //m_character.PlayXml(xmlPlayTest3);
            }

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Result", 150);
                m_debugMenu.TextArea(m_simaResult);
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
                if (m_debugMenu.Button("Up")) { GazeAt("Up"); }
                if (m_debugMenu.Button("Down")) { GazeAt("Down"); }
                if (m_debugMenu.Button("Left")) { GazeAt("Left"); }
                if (m_debugMenu.Button("Right")) { GazeAt("Right"); }
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
        //private void OnNvbgGenerated(string result)
        //{
            //m_nvbgResult = result;
        //}

        private void OnSimaGenerated(string result)
        {
            Debug.Log($"OnSimaGenerated() - {result}");
            m_simaResult = result;
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
