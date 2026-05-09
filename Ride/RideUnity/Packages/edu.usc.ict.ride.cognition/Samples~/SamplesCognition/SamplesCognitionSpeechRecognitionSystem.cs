using System;
using UnityEngine;
using Ride.SpeechRecognition;

namespace Ride.Samples
{
    /// <summary>
    /// Sample class to demonstrate and manage different automated speech recognition (ASR) systems using RIDE's APIs.
    /// </summary>
    public class SamplesCognitionSpeechRecognitionSystem : RideMonoBehaviour
    {
        private DebugMenu m_debugMenu;
        private SpeechRecognitionSystemAzure m_asr_azure;
        private SpeechRecognitionSystemWindows m_asr_windows;
        private SpeechRecognitionSystemOpenAI m_asr_openai;
        private SpeechRecognitionSystemAzureWebGL m_asr_azure_webgl;
        private SpeechRecognitionSystemUnity m_current_asr;
        private int m_asr_mode;
        private string m_filePath = "<Enter path to .wav audio file>";
        private string m_timeout_autoSilence;
        private string m_timeout_initialSilence;

        /// <summary>
        /// Initializes the speech recognition systems and debug menu at the start of the scene.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
            m_asr_azure = Systems.Get<SpeechRecognitionSystemAzure>();
            m_asr_windows = Systems.Get<SpeechRecognitionSystemWindows>();
            m_asr_openai = Systems.Get<SpeechRecognitionSystemOpenAI>();
            m_asr_azure_webgl = Systems.Get<SpeechRecognitionSystemAzureWebGL>();
            m_current_asr = m_asr_azure;
            m_timeout_autoSilence = m_current_asr.AutoSilenceTimeoutSeconds.ToString();
            m_timeout_initialSilence = m_current_asr.InitialSilenceTimeoutSeconds.ToString();
        }

        /// <summary>
        /// Draws and manages the GUI for controlling and interacting with different speech recognition systems.
        /// Allows switching between different ASR solutions, setting API keys, configuring timeouts,
        /// selecting microphones, and adjusting input sources (e.g., live or file input).
        /// </summary>
        public void OnGUISpeechRecognition()
        {
            m_debugMenu.Label($"<b>ASR</b>");

            int asrMode = m_debugMenu.SelectionGrid(m_asr_mode, new string[] { "Azure", "Windows", "OpenAI", "AzureWebGL" }, 2);

            if (m_asr_mode != asrMode)
            {
                m_asr_mode = asrMode;

                if (m_asr_mode == 0) m_current_asr = m_asr_azure;
                else if (m_asr_mode == 1) m_current_asr = m_asr_windows;
                else if (m_asr_mode == 2) m_current_asr = m_asr_openai;
                else if (m_asr_mode == 3) m_current_asr = m_asr_azure_webgl;
            }

            m_debugMenu.Space();
            m_debugMenu.Label($"Current ASR: {m_current_asr.GetType()}");

            if (RideUtils.IsWebGL())
            {
                if (m_current_asr == m_asr_windows ||
                    m_current_asr == m_asr_azure   ||
                    m_current_asr == m_asr_openai)
                {
                    m_debugMenu.Space();
                    m_debugMenu.Space();
                    m_debugMenu.Label($"Current ASR System not supported on this platform");
                    return;
                }
            }

            m_debugMenu.Label($"IsSupported: {m_current_asr.IsSupported}");
            m_debugMenu.Label($"Confidence: {m_current_asr.Confidence}");
            m_debugMenu.Label($"IsContinious: {m_current_asr.SupportsContinuousRecognition}");

            m_debugMenu.Space();
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"Auto Silence Timeout: ", 300);
                m_timeout_autoSilence = m_debugMenu.TextField(m_timeout_autoSilence, 300);
                if (m_debugMenu.Button("Set", 100)) { m_current_asr.AutoSilenceTimeoutSeconds = float.Parse(m_timeout_autoSilence); }
            }
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"Initial Silence Timeout: ", 300);
                m_timeout_initialSilence = m_debugMenu.TextField(m_timeout_initialSilence, 300);
                if (m_debugMenu.Button("Set", 100)) { m_current_asr.InitialSilenceTimeoutSeconds = float.Parse(m_timeout_initialSilence); }
            }
           
            m_debugMenu.Space();
            m_debugMenu.Label($"Selected Microphone: {m_current_asr.SelectedMicrophone}");

#if UNITY_WEBGL
            string[] microphones = new string [] { };
#else
            string[] microphones = Microphone.devices;
#endif
            int selectedIndex = Array.IndexOf(microphones, m_current_asr.SelectedMicrophone);
            if (selectedIndex < 0)
                selectedIndex = 0;

            if (microphones.Length > 0)
            {
                int newIndex = m_debugMenu.SelectionGrid(selectedIndex, microphones, Mathf.Min(microphones.Length, 3));
                if (newIndex != selectedIndex)
                    m_current_asr.SetMicrophone(microphones[newIndex]);
            }
            else
            {
                m_debugMenu.Label("<i>No microphones found</i>");
            }

            m_debugMenu.Label($"Currently Recognizing: {m_current_asr.IsRecognizing}");
            if (m_current_asr.IsRecognizing)
            {
                if (m_debugMenu.Button("Stop Recognizing"))
                {
                    m_current_asr.StopRecognizing();
                }
            }
            else
            {
                if (m_debugMenu.Button("Start Recognizing"))
                {
                    m_current_asr.StartRecognizing();
                }
            }

            m_debugMenu.TextArea(m_current_asr.RecognizedSpeech);

            m_debugMenu.Space();

            // Advanced input source types
            if (m_current_asr is IInputSourceConfigurableSpeechRecognition inputSourceCapability)
            {
                m_debugMenu.Label("Advanced Input");

                // Allow user to pick input source
                var options = Enum.GetNames(typeof(SpeechRecognitionSystemAzure.SpeechRecognitionType));
                int currentSelection = (int)inputSourceCapability.CurrentInputSource;

                int newSelection = m_debugMenu.SelectionGrid(currentSelection, options, options.Length);
                var selectedType = (SpeechRecognitionSystemAzure.SpeechRecognitionType)newSelection;
                if (selectedType == SpeechRecognitionSystemAzure.SpeechRecognitionType.FILE)
                {
                    m_debugMenu.Label("Enter path to .wav file");
                    m_filePath = m_debugMenu.TextField(m_filePath);
                    inputSourceCapability.SetInputSource(selectedType, m_filePath);
                }
                else
                {
                    inputSourceCapability.SetInputSource(selectedType);
                }

                m_debugMenu.Space();
                m_debugMenu.Label($"Current Input Source: {inputSourceCapability.CurrentInputSource}");
            }
            else
            {
                m_debugMenu.Label("<i>IInputSourceConfigurableSpeechRecognition not available</i>");
            }
        }
    }
}
