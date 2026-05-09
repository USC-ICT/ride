using UnityEngine;
using VHAssets;

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


        /// <summary>
        /// Initializes the debug menu, NVBG system, and character reference.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
            m_nvbg = Systems.Get<NonverbalBehaviorGeneratorSystem>();
            m_character = FindAnyObjectByType<MecanimCharacter>();
            m_nvbg.StartProcess(m_character.CharacterName);
        }


        /// <summary>
        /// Draw debug for entering an utterance and generating nonverbal behavior.
        /// </summary>
        public void OnGUINonverbalBehaviorGeneration()
        {
            m_debugMenu.Label("<b>NVBG</b>");
            m_debugMenu.Label("<b>Text Generation Only</b>");

            m_debugMenu.Space();
            m_debugMenu.Label("<i>Generates NVBG text only - does not drive the current character.</i>");
            m_debugMenu.Space();

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Utterance", 150);
                m_utterance = m_debugMenu.TextField(m_utterance);
            }

            if (m_debugMenu.Button("Generate"))
                m_nvbg.GetNonverbalBehavior(m_character.CharacterName, m_utterance, OnNvbgGenerated);

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Result", 150);
                m_debugMenu.TextArea(m_nvbgResult);
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
    }
}
