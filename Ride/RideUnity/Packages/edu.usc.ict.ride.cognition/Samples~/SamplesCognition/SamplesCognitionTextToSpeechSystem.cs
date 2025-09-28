using UnityEngine;
using Ride.Audio;
using Ride.TextToSpeech;

namespace Ride.Samples
{
    /// <summary>
    /// Sample script that demonstrates text-to-speech (TTS) functionality using various providers like Polly, Azure, and ElevenLabs.
    /// </summary>
    public class SamplesCognitionTextToSpeechSystem : RideMonoBehaviour
    {
        public AudioSource m_audioSource;

        private DebugMenu m_debugMenu;
        private TextToSpeechSystemAWSPolly m_polly;
        private TextToSpeechSystemAzure m_azure;
        private TextToSpeechSystemElevenLabs m_elevenLabs;
        private ITextToSpeechSystem m_currentTTS;

        // Debug menu variables
        private string m_inputText = "Hello world";
        private Vector2 m_scrollPos = Vector2.zero;
        private bool m_voiceSelectionToggle = false;
        private int m_tts_mode;
        private int m_tts_voice;

        /// <summary>
        /// Unity Start method override. Initializes TTS systems and audio playback components.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu     = Globals.api.GetSystem<DebugMenu>();
            m_polly         = Globals.api.GetSystem<TextToSpeechSystemAWSPolly>();
            m_azure         = Globals.api.GetSystem<TextToSpeechSystemAzure>();
            m_elevenLabs    = Globals.api.GetSystem<TextToSpeechSystemElevenLabs>();
            m_currentTTS    = m_polly;
        }

        /// <summary>
        /// Renders the main TTS debug menu GUI including system selection, voice selection, and playback.
        /// </summary>
        public void OnGUITextToSpeech()
        {
            m_debugMenu.Label($"<b>TTS</b>");
            OnGUISystemSelection();
            OnGUIVoiceSelection();
            OnGUIPlayTTS();
        }

        /// <summary>
        /// Displays a selection grid for choosing the active text-to-speech system.
        /// </summary>
        public void OnGUISystemSelection()
        {
            m_debugMenu.Label($"Current TTS: {m_currentTTS}");
            int ttsMode = m_debugMenu.SelectionGrid(m_tts_mode, new string[] { "Polly", "Azure", "11Labs" }, 3);
            if (m_tts_mode != ttsMode)
            {
                m_tts_mode = ttsMode;
                if (m_tts_mode == 0) { m_currentTTS = m_polly; }
                else if (m_tts_mode == 1) { m_currentTTS = m_azure; }
                else if (m_tts_mode == 2) { m_currentTTS = m_elevenLabs; }
            }
        }

        /// <summary>
        /// Displays the voice selection UI within a collapsible toggle section.
        /// </summary>
        public void OnGUIVoiceSelection()
        {
            m_voiceSelectionToggle = m_debugMenu.Toggle(
                m_voiceSelectionToggle,
                m_voiceSelectionToggle ? $"- Select TTS Voice" : $"+ Select TTS Voice"
            );

            if (m_voiceSelectionToggle)
            {
                using (var scrollViewScope = new GUILayout.ScrollViewScope(m_scrollPos, GUILayout.MinHeight(100)))
                {
                    m_scrollPos = scrollViewScope.scrollPosition;

                    m_tts_voice = m_debugMenu.SelectionGrid(
                        m_tts_voice,
                        m_currentTTS.GetAvailableVoices(),
                        4
                    );
                }
            }

            m_debugMenu.Space();
        }

        /// <summary>
        /// Displays the text input and handles the playback of generated TTS audio.
        /// </summary>
        private void OnGUIPlayTTS()
        {
            m_inputText = m_debugMenu.TextField(m_inputText);

            if (m_debugMenu.Button("Play TTS"))
            {
                if (string.IsNullOrEmpty(m_inputText))
                    return;

                string currentVoice = m_currentTTS.GetAvailableVoices()[m_tts_voice];
                m_currentTTS.CreateTextToSpeech(currentVoice, m_inputText, audioFilePath =>
                {
                    var audioSystem = Systems.Get<AudioSystemUnity>();
                    audioSystem.LoadAudioFile(audioFilePath, clip =>
                    {
                        m_audioSource.clip = clip;
                        m_audioSource.Play();
                    });
                });
            }
        }
    }
}
