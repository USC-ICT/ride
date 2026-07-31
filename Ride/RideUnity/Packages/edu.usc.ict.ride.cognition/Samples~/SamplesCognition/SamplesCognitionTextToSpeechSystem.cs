using System;
using System.Collections.Generic;
using UnityEngine;
using Ride.Audio;
using Ride.TextToSpeech;

namespace Ride.Samples
{
    /// <summary>
    /// Sample script that demonstrates text-to-speech (TTS) functionality using the TTS providers available in the scene.
    /// </summary>
    public class SamplesCognitionTextToSpeechSystem : RideMonoBehaviour
    {
        private sealed class TtsSampleOption
        {
            public string label;
            public ITextToSpeechSystem system;
        }

        public AudioSource m_audioSource;

        private DebugMenu m_debugMenu;
        private readonly List<TtsSampleOption> m_ttsOptions = new();
        private ITextToSpeechSystem m_currentTTS;
        private string[] m_ttsOptionLabels = Array.Empty<string>();

        // Debug menu variables
        private string m_inputText = "Hello world";
        private Vector2 m_scrollPos = Vector2.zero;
        private Vector2 m_lipsyncScrollPos = Vector2.zero;
        private bool m_voiceSelectionToggle = false;
        private int m_tts_mode;
        private int m_tts_voice;
        private string m_lipsyncOutput = string.Empty;

        /// <summary>
        /// Unity Start method override. Initializes TTS systems and audio playback components.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();

            AddTtsOption("Polly", Systems.Get<TextToSpeechSystemAWSPolly>());
            AddTtsOption("Azure", Systems.Get<TextToSpeechSystemAzure>());
            AddTtsOption("11Labs", Systems.Get<TextToSpeechSystemElevenLabs>());
            AddTtsOption("Windows", Systems.Get<TextToSpeechSystemWindows>());

            m_ttsOptionLabels = m_ttsOptions.ConvertAll(option => option.label).ToArray();
            m_currentTTS = m_ttsOptions.Count > 0 ? m_ttsOptions[0].system : null;
        }

        /// <summary>
        /// Renders the main TTS debug menu GUI including system selection, voice selection, and playback.
        /// </summary>
        public void OnGUITextToSpeech()
        {
            m_debugMenu.Label($"<b>TTS</b>");
            OnGUISystemSelection();
            OnGUIVoiceSelection();
            OnGUIStatus();
            OnGUIPlayTTS();
        }

        /// <summary>
        /// Displays a selection grid for choosing the active text-to-speech system.
        /// </summary>
        public void OnGUISystemSelection()
        {
            m_debugMenu.Label($"Current TTS: {m_currentTTS}");
            if (m_ttsOptions.Count == 0)
            {
                m_debugMenu.Label("No TTS systems were found in the current sample setup.");
                return;
            }

            int ttsMode = m_debugMenu.SelectionGrid(
                Mathf.Clamp(m_tts_mode, 0, m_ttsOptions.Count - 1),
                m_ttsOptionLabels,
                Mathf.Min(4, m_ttsOptionLabels.Length));
            if (m_tts_mode != ttsMode)
            {
                m_tts_mode = ttsMode;
                m_currentTTS = m_ttsOptions[m_tts_mode].system;
                m_tts_voice = 0;
            }
        }

        /// <summary>
        /// Displays the voice selection UI within a collapsible toggle section.
        /// </summary>
        public void OnGUIVoiceSelection()
        {
            if (m_currentTTS == null)
                return;

            m_voiceSelectionToggle = m_debugMenu.Toggle(
                m_voiceSelectionToggle,
                m_voiceSelectionToggle ? $"- Select TTS Voice" : $"+ Select TTS Voice"
            );

            if (m_voiceSelectionToggle)
            {
                string[] voices = m_currentTTS.GetAvailableVoices() ?? Array.Empty<string>();

                if (voices.Length == 0)
                {
                    m_debugMenu.Label("No voices are currently available for this provider.");
                    m_debugMenu.Space();
                    return;
                }

                m_tts_voice = Mathf.Clamp(m_tts_voice, 0, voices.Length - 1);

                using (var scrollViewScope = new GUILayout.ScrollViewScope(m_scrollPos, GUILayout.MinHeight(100)))
                {
                    m_scrollPos = scrollViewScope.scrollPosition;

                    m_tts_voice = m_debugMenu.SelectionGrid(
                        m_tts_voice,
                        voices,
                        4
                    );
                }
            }

            m_debugMenu.Space();
        }

        /// <summary>
        /// Displays current synthesis/lipsync processing state for the selected provider.
        /// </summary>
        private void OnGUIStatus()
        {
            if (m_currentTTS == null)
                return;

            string status = m_currentTTS.textToSpeechProcessing ? "Generating audio..." : "Idle";
            if (m_currentTTS is ILipsyncedTextToSpeechSystem lipsyncedTts && lipsyncedTts.lipsyncProcessing)
                status = "Generating audio + lipsync...";

            m_debugMenu.Label($"Status: {status}");
            m_debugMenu.Space();
        }

        /// <summary>
        /// Displays the text input and handles the playback of generated TTS audio.
        /// </summary>
        private void OnGUIPlayTTS()
        {
            m_inputText = m_debugMenu.TextField(m_inputText);

            if (m_currentTTS == null)
            {
                m_debugMenu.Label("Add a TTS system to the sample scene to enable synthesis.");
                return;
            }

            if (m_debugMenu.Button("Play TTS"))
            {
                if (string.IsNullOrEmpty(m_inputText))
                    return;

                if (!TryGetSelectedVoice(out string currentVoice))
                    return;

                m_currentTTS.CreateTextToSpeech(currentVoice, m_inputText, PlayGeneratedAudio);
            }

            if (m_currentTTS is ILipsyncedTextToSpeechSystem selectedLipsyncedTts)
            {
                if (m_debugMenu.Button("Play TTS + Show Lipsync XML"))
                {
                    if (string.IsNullOrEmpty(m_inputText))
                        return;

                    if (!TryGetSelectedVoice(out string currentVoice))
                        return;

                    selectedLipsyncedTts.CreateTextToSpeech(currentVoice, m_inputText, (lipsyncXml, audioFilePath) =>
                    {
                        m_lipsyncOutput = lipsyncXml ?? string.Empty;
                        PlayGeneratedAudio(audioFilePath);
                    });
                }

                m_debugMenu.Label("<b>Lipsync XML</b>");
                if (string.IsNullOrWhiteSpace(m_lipsyncOutput))
                {
                    m_debugMenu.Label("No lipsync XML has been generated yet.");
                }
                else
                {
                    using (var lipsyncScrollView = new GUILayout.ScrollViewScope(m_lipsyncScrollPos, GUILayout.MinHeight(300)))
                    {
                        m_lipsyncScrollPos = lipsyncScrollView.scrollPosition;
                        m_debugMenu.TextArea(m_lipsyncOutput);
                    }
                }
            }
            else
            {
                m_debugMenu.Label("Selected provider does not expose lipsync output.");
            }
        }

        private void AddTtsOption(string label, ITextToSpeechSystem system)
        {
            if (system == null)
                return;

            m_ttsOptions.Add(new() { label = label, system = system });
        }

        private bool TryGetSelectedVoice(out string voice)
        {
            voice = null;

            string[] voices = m_currentTTS?.GetAvailableVoices() ?? Array.Empty<string>();
            if (voices.Length == 0)
                return false;

            m_tts_voice = Mathf.Clamp(m_tts_voice, 0, voices.Length - 1);
            voice = voices[m_tts_voice];
            return !string.IsNullOrWhiteSpace(voice);
        }

        private void PlayGeneratedAudio(string audioFilePath)
        {
            if (string.IsNullOrWhiteSpace(audioFilePath))
            {
                Debug.LogWarning("[SamplesCognitionTextToSpeechSystem] TTS generation did not return an audio file path.");
                return;
            }

            var audioSystem = Systems.Get<AudioSystemUnity>();
            if (audioSystem == null)
            {
                Debug.LogWarning("[SamplesCognitionTextToSpeechSystem] AudioSystemUnity was not found.");
                return;
            }

            audioSystem.LoadAudioFile(audioFilePath, clip =>
            {
                if (clip == null)
                {
                    Debug.LogWarning($"[SamplesCognitionTextToSpeechSystem] Failed to load generated audio file: {audioFilePath}");
                    return;
                }

                m_audioSource.clip = clip;
                m_audioSource.Play();
            });
        }
    }
}
