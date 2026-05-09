using UnityEngine;
using Ride.NLP;

namespace Ride.Samples
{
    /// <summary>
    /// Demonstrates integration and GUI interaction with various NLP systems like ChatGPT, AWS Lex, etc.
    /// </summary>
    public class SamplesCognitionNlpSystem : RideMonoBehaviour
    {
        private DebugMenu m_debugMenu;
        private NlpSystemChatGPT m_chatGpt;
        private NlpSystemAnthropic m_anthropic;
        private NlpSystemAWSLex m_awsLex;
        private NlpSystemUnity m_currentLlm;

        private float m_LLMTemperature = 0.3f;
        private int m_LLMMaxToken = 200;
        private int m_llmMode;
        private string m_inputText;
        private string m_outputText;
        private string m_initialPromptText = string.Empty;

        /// <summary>
        /// Initializes NLP systems and sets the default system at the start of the scene.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu     = Systems.Get<DebugMenu>();
            m_chatGpt       = Systems.Get<NlpSystemChatGPT>();
            m_anthropic     = Systems.Get<NlpSystemAnthropic>();
            m_awsLex        = Systems.Get<NlpSystemAWSLex>();

            m_currentLlm = m_chatGpt;
        }

        /// <summary>
        /// Draws the main GUI section for interacting with the selected NLP system.
        /// </summary>
        public void OnGUINlp()
        {
            OnGUISystemSelection();
        }

        /// <summary>
        /// Draws the GUI elements for selecting the NLP system, setting parameters (e.g., LLM temperature, token limit),
        /// sending input, and displaying the NLP response.
        /// </summary>
        public void OnGUISystemSelection()
        {
            int nlpMode = m_debugMenu.SelectionGrid(m_llmMode, new string[] { "ChatGPT", "Anthropic", "Lex" }, 3);

            if (m_llmMode != nlpMode)
            {
                m_llmMode = nlpMode;
                if (m_llmMode == 0) m_currentLlm = m_chatGpt;
                else if (m_llmMode == 1) m_currentLlm = m_anthropic;
                else if (m_llmMode == 2) m_currentLlm = m_awsLex;
            }

            GUI.enabled = false;

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"Temperature: {$"{m_LLMTemperature:f1}"}", 200f);
                m_LLMTemperature = m_debugMenu.HorizontalSlider(m_LLMTemperature, 0f, 1f);
            }

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"Max Token: {m_LLMMaxToken}", 200f);
                m_LLMMaxToken = (int)m_debugMenu.HorizontalSlider(m_LLMMaxToken, 0, 200);
            }

            GUI.enabled = true;

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label("Initial Prompt: ");
                m_initialPromptText = m_debugMenu.TextField(m_initialPromptText);
                if (m_debugMenu.Button("Set")) { m_currentLlm.SetSystemPrompt(m_initialPromptText); }
            }

            m_inputText = m_debugMenu.TextField(m_inputText);
            if (m_debugMenu.Button("Send"))
            {
                if (m_inputText.Length <= 0) { return; }

                m_currentLlm.Request(new NlpRequest(m_inputText), QuestionResponse);
                m_outputText = "Processing...";
            }

            m_debugMenu.TextField(m_outputText);
        }

        /// <summary>
        /// Handles the response returned from the NLP system and updates the output text.
        /// </summary>
        /// <param name="response">The response object containing the NLP-generated content.</param>
        private void QuestionResponse(NlpResponse response)
        {
            m_outputText = response.content[0];
        }
    }
}
