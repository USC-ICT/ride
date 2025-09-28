using UnityEngine;
using Ride.IO;

namespace Ride.Samples
{
    /// <summary>
    /// Sample class for demonstrating RIDE cognition systems such as speech recognition, NLP, TTS, and sensing.
    /// </summary>
    public class SamplesCognition : RideMonoBehaviour
    {
        [SerializeField] SamplesCognitionSpeechRecognitionSystem m_speechRecognition;
        [SerializeField] SamplesCognitionNlpSystem m_nlp;
        [SerializeField] SamplesCognitionTextToSpeechSystem m_tts;
        [SerializeField] SamplesCognitionSensingSystem m_sensing;

        private DebugMenu m_debugMenu;

        /// <summary>
        /// Initializes and configures the cognition debug menu and its associated tabs.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();

            m_debugMenu.InsertMenu(0, "SamplesCognition", OnGUISamplesCognition);
            m_debugMenu.InsertMenu(1, "Speech Recognition", m_speechRecognition.OnGUISpeechRecognition);
            m_debugMenu.InsertMenu(2, "Natural Language", m_nlp.OnGUINlp);
            m_debugMenu.InsertMenu(3, "Text To Speech", m_tts.OnGUITextToSpeech);
            m_debugMenu.InsertMenu(4, "Sensing", m_sensing.OnGUISensing);

            m_debugMenu.SetMenu(0);
            m_debugMenu.ShowMenu(true);
            m_debugMenu.SetMenuSize(0, 0, 0.3f, 1f);
            m_debugMenu.SetWideMenuSize(0, 0, 0.4f, 1f);
        }

        /// <summary>
        /// Listens for user input to quit the application or toggle the debug menu.
        /// </summary>
        protected override void Update()
        {
            if (Systems.Input.GetKeyDown(RideKeyCode.Escape))
            {
                RideUtils.QuitApplication();
            }

            if (Systems.Input.GetKeyDown(RideKeyCode.F11))
            {
                m_debugMenu.ToggleMenu();
            }
        }

        /// <summary>
        /// Draw SamplesCognitiondebug menu tab.
        /// </summary>
        void OnGUISamplesCognition()
        {
            m_debugMenu.Label($"RIDE Cognition Sample");
            m_debugMenu.Space();
            m_debugMenu.Label($"<b>Use the arrows above to scroll through the different tabs</b>");
            m_debugMenu.Label($"<b>Use the '<>' button to change the menu width</b>");
            m_debugMenu.Label($"<b>Use F11 key to toggle this menu</b>");
            m_debugMenu.Space();
            m_debugMenu.Space();

            if (m_debugMenu.Button("Hide Window"))
                m_debugMenu.ToggleMenu();
        }
    }
}
